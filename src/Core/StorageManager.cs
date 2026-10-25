using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace DesktopStickyNote.Core
{
    public class StorageManager
    {
        private static readonly object _fileLock = new object();
        private readonly string _storageDir;
        private readonly string _filePath;
        private readonly string _tempFilePath;
        private Timer _debounceTimer;
        private NoteSettings _pendingSettings;
        private const int DebounceMs = 400;

        public StorageManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storageDir = Path.Combine(appData, "stick19");
            _filePath = Path.Combine(_storageDir, "note_data.json");
            _tempFilePath = Path.Combine(_storageDir, "note_data.tmp");

            try
            {
                if (!Directory.Exists(_storageDir))
                {
                    Directory.CreateDirectory(_storageDir);
                }

                // Migrate legacy storage data if available
                string legacyDir = Path.Combine(appData, "DesktopStickyNote");
                string legacyFilePath = Path.Combine(legacyDir, "note_data.json");
                if (!File.Exists(_filePath) && File.Exists(legacyFilePath))
                {
                    File.Copy(legacyFilePath, _filePath, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StorageManager init error: " + ex.Message);
            }
        }

        public NoteSettings Load()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_filePath))
                    {
                        string json = File.ReadAllText(_filePath, Encoding.UTF8);
                        return ParseJson(json);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading note data: " + ex.Message);
                }

                // If file doesn't exist or failed to parse, return defaults
                return new NoteSettings();
            }
        }

        public void RequestSave(NoteSettings settings)
        {
            _pendingSettings = settings;
            if (_debounceTimer == null)
            {
                _debounceTimer = new Timer(OnDebounceTimerElapsed, null, DebounceMs, Timeout.Infinite);
            }
            else
            {
                _debounceTimer.Change(DebounceMs, Timeout.Infinite);
            }
        }

        private void OnDebounceTimerElapsed(object state)
        {
            if (_pendingSettings != null)
            {
                SaveImmediate(_pendingSettings);
            }
        }

        public void SaveImmediate(NoteSettings settings)
        {
            if (settings == null) return;

            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(_storageDir))
                    {
                        Directory.CreateDirectory(_storageDir);
                    }

                    string json = SerializeJson(settings);

                    // Write to temp file first for atomic safety
                    File.WriteAllText(_tempFilePath, json, Encoding.UTF8);

                    if (File.Exists(_filePath))
                    {
                        File.Delete(_filePath);
                    }
                    File.Move(_tempFilePath, _filePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error saving note data: " + ex.Message);
                }
            }
        }

        #region Lightweight Native JSON Serialization
        private string SerializeJson(NoteSettings s)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendFormat("  \"text\": \"{0}\",\n", EscapeString(s.Text ?? string.Empty));
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"left\": {0:F2},\n", s.Left);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"top\": {0:F2},\n", s.Top);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"width\": {0:F2},\n", s.Width);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"height\": {0:F2},\n", s.Height);
            sb.AppendFormat("  \"theme\": \"{0}\",\n", EscapeString(s.Theme ?? "Yellow"));
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"fontSize\": {0:F2},\n", s.FontSize);
            sb.AppendFormat("  \"isLocked\": {0},\n", s.IsLocked ? "true" : "false");
            sb.AppendFormat("  \"isVisible\": {0},\n", s.IsVisible ? "true" : "false");
            sb.AppendFormat("  \"launchAtStartup\": {0},\n", s.LaunchAtStartup ? "true" : "false");
            sb.AppendFormat("  \"attachmentMode\": {0},\n", (int)s.AttachmentMode);
            sb.AppendFormat("  \"autoSave\": {0},\n", s.AutoSave ? "true" : "false");
            sb.AppendFormat("  \"isGlossy\": {0},\n", s.IsGlossy ? "true" : "false");
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"opacity\": {0:F2}\n", s.Opacity);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private NoteSettings ParseJson(string json)
        {
            NoteSettings s = new NoteSettings();
            if (string.IsNullOrEmpty(json)) return s;

            try
            {
                s.Text = ExtractString(json, "text", s.Text);
                s.Left = ExtractDouble(json, "left", s.Left);
                s.Top = ExtractDouble(json, "top", s.Top);
                s.Width = ExtractDouble(json, "width", s.Width);
                s.Height = ExtractDouble(json, "height", s.Height);
                s.Theme = ExtractString(json, "theme", s.Theme);
                s.FontSize = ExtractDouble(json, "fontSize", s.FontSize);
                s.IsLocked = ExtractBool(json, "isLocked", s.IsLocked);
                s.IsVisible = ExtractBool(json, "isVisible", s.IsVisible);
                s.LaunchAtStartup = ExtractBool(json, "launchAtStartup", s.LaunchAtStartup);
                s.AttachmentMode = (AttachmentMode)ExtractInt(json, "attachmentMode", (int)s.AttachmentMode);
                s.AutoSave = ExtractBool(json, "autoSave", s.AutoSave);
                s.IsGlossy = ExtractBool(json, "isGlossy", s.IsGlossy);
                s.Opacity = ExtractDouble(json, "opacity", s.Opacity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("JSON Parse exception: " + ex.Message);
            }

            return s;
        }

        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            StringBuilder sb = new StringBuilder(str.Length + 16);
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append(@"\"""); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\t': sb.Append(@"\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.AppendFormat(@"\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private static string ExtractString(string json, string key, string defaultValue)
        {
            string pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return defaultValue;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx == -1) return defaultValue;

            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart == -1) return defaultValue;

            StringBuilder sb = new StringBuilder();
            bool inEscape = false;
            for (int i = quoteStart + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (inEscape)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else if (c == '"') sb.Append('"');
                    else if (c == '\\') sb.Append('\\');
                    else if (c == 'u' && i + 4 < json.Length)
                    {
                        string hex = json.Substring(i + 1, 4);
                        int val;
                        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                        {
                            sb.Append((char)val);
                            i += 4;
                        }
                        else
                        {
                            sb.Append('u');
                        }
                    }
                    else sb.Append(c);
                    inEscape = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        inEscape = true;
                    }
                    else if (c == '"')
                    {
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }

        private static double ExtractDouble(string json, string key, double defaultValue)
        {
            string raw = ExtractRawValue(json, key);
            double val;
            if (!string.IsNullOrEmpty(raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                return val;
            }
            return defaultValue;
        }

        private static int ExtractInt(string json, string key, int defaultValue)
        {
            string raw = ExtractRawValue(json, key);
            int val;
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                return val;
            }
            return defaultValue;
        }

        private static bool ExtractBool(string json, string key, bool defaultValue)
        {
            string raw = ExtractRawValue(json, key);
            if (string.IsNullOrEmpty(raw)) return defaultValue;
            raw = raw.Trim().ToLowerInvariant();
            if (raw == "true") return true;
            if (raw == "false") return false;
            return defaultValue;
        }

        private static string ExtractRawValue(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return null;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx == -1) return null;

            int start = colonIdx + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;

            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '\r' && json[end] != '\n' && json[end] != '}')
            {
                end++;
            }

            return json.Substring(start, end - start).Trim();
        }
        #endregion
    }
}
