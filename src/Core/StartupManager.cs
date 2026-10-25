using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace DesktopStickyNote.Core
{
    public static class StartupManager
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "stick19";
        private const string LegacyAppName = "DesktopStickyNote";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(AppName) ?? key.GetValue(LegacyAppName);
                        return val != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("IsStartupEnabled error: " + ex.Message);
            }
            return false;
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key != null)
                    {
                        // Clean up legacy registry key if present
                        try { key.DeleteValue(LegacyAppName, false); } catch { }

                        if (enable)
                        {
                            string exePath = GetExecutablePath();
                            if (!string.IsNullOrEmpty(exePath))
                            {
                                key.SetValue(AppName, "\"" + exePath + "\"");
                            }
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                            {
                                key.DeleteValue(AppName, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetStartup error: " + ex.Message);
            }
        }

        private static string GetExecutablePath()
        {
            try
            {
                Process proc = Process.GetCurrentProcess();
                if (proc.MainModule != null)
                {
                    return proc.MainModule.FileName;
                }
            }
            catch { }

            return Assembly.GetExecutingAssembly().Location;
        }
    }
}
