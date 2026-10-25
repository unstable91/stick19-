using System;
using System.IO;
using DesktopStickyNote.Controls;
using DesktopStickyNote.Core;

namespace DesktopStickyNote.Tests
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("RUNNING STICK19 TEST SUITE");
            Console.WriteLine("Developed by unstable19");
            Console.WriteLine("========================================");

            int passed = 0;
            int failed = 0;

            Test("ColorThemes: All predefined themes exist", ref passed, ref failed, () =>
            {
                string[] expected = { "Yellow", "Mint", "Blue", "Lavender", "Peach", "Dark" };
                foreach (string name in expected)
                {
                    NoteTheme theme = ColorThemes.GetTheme(name);
                    Assert(theme != null, "Theme " + name + " is null");
                    Assert(theme.HeaderBrush != null, "Theme " + name + " HeaderBrush is null");
                    Assert(theme.BodyBrush != null, "Theme " + name + " BodyBrush is null");
                    Assert(theme.BorderBrush != null, "Theme " + name + " BorderBrush is null");
                    Assert(theme.TextBrush != null, "Theme " + name + " TextBrush is null");
                }
            });

            Test("ColorThemes: Fallback for unknown theme returns Yellow", ref passed, ref failed, () =>
            {
                NoteTheme fallback = ColorThemes.GetTheme("NonExistentThemeXYZ");
                Assert(fallback != null, "Fallback theme is null");
                Assert(fallback.Name == "Yellow", "Fallback did not return Yellow theme");
            });

            Test("StorageManager: Roundtrip JSON serialization & parsing with complex text", ref passed, ref failed, () =>
            {
                StorageManager storage = new StorageManager();
                NoteSettings backup = null;
                try
                {
                    backup = storage.Load();
                }
                catch { }

                try
                {
                    NoteSettings original = new NoteSettings
                    {
                        Text = "Line 1: Special \"quotes\" and \\backslashes\\\nLine 2: Tabs\there and newlines\r\nLine 3: Symbols & Unicode: ⚡ ✓ 🚀 \u00A9 2026",
                        Left = 245.5,
                        Top = 180.25,
                        Width = 450.0,
                        Height = 380.0,
                        Theme = "Mint",
                        FontSize = 16.5,
                        IsLocked = true,
                        IsVisible = false,
                        LaunchAtStartup = true,
                        AttachmentMode = AttachmentMode.WorkerWChild,
                        AutoSave = false,
                        IsGlossy = true,
                        Opacity = 0.95
                    };

                    storage.SaveImmediate(original);
                    NoteSettings loaded = storage.Load();

                    Assert(loaded.Text == original.Text, "Text mismatch: expected '" + original.Text + "' got '" + loaded.Text + "'");
                    Assert(Math.Abs(loaded.Left - original.Left) < 0.01, "Left mismatch");
                    Assert(Math.Abs(loaded.Top - original.Top) < 0.01, "Top mismatch");
                    Assert(Math.Abs(loaded.Width - original.Width) < 0.01, "Width mismatch");
                    Assert(Math.Abs(loaded.Height - original.Height) < 0.01, "Height mismatch");
                    Assert(loaded.Theme == original.Theme, "Theme mismatch");
                    Assert(Math.Abs(loaded.FontSize - original.FontSize) < 0.01, "FontSize mismatch");
                    Assert(loaded.IsLocked == original.IsLocked, "IsLocked mismatch");
                    Assert(loaded.IsVisible == original.IsVisible, "IsVisible mismatch");
                    Assert(loaded.LaunchAtStartup == original.LaunchAtStartup, "LaunchAtStartup mismatch");
                    Assert(loaded.AttachmentMode == original.AttachmentMode, "AttachmentMode mismatch");
                    Assert(loaded.AutoSave == original.AutoSave, "AutoSave mismatch");
                    Assert(loaded.IsGlossy == original.IsGlossy, "IsGlossy mismatch");
                    Assert(Math.Abs(loaded.Opacity - original.Opacity) < 0.01, "Opacity mismatch");
                }
                finally
                {
                    if (backup != null)
                    {
                        storage.SaveImmediate(backup);
                    }
                }
            });

            Test("StartupManager: Registry query succeeds without exception", ref passed, ref failed, () =>
            {
                bool enabled = StartupManager.IsStartupEnabled();
                Console.WriteLine("    [Info] Current Run at startup state: " + enabled);
            });

            Test("FlowDocument: Parse with pre-existing resources without collision", ref passed, ref failed, () =>
            {
                string rawXaml = "<FlowDocument PagePadding=\"5,0,5,0\" AllowDrop=\"True\" NumberSubstitution.CultureSource=\"User\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><FlowDocument.Resources><Style TargetType=\"Paragraph\" x:Key=\"{x:Type Paragraph}\"><Style.Resources><ResourceDictionary /></Style.Resources><Setter Property=\"Block.Margin\"><Setter.Value><Thickness>0,0,0,0</Thickness></Setter.Value></Setter></Style></FlowDocument.Resources><Paragraph>aws</Paragraph><Paragraph>frontend react</Paragraph><Paragraph>ca2 se certificate</Paragraph><Paragraph>27 java viva</Paragraph><Paragraph xml:space=\"preserve\">15 15 bet </Paragraph><Paragraph xml:space=\"preserve\">skill builder 4 </Paragraph><Paragraph xml:space=\"preserve\" /></FlowDocument>";

                System.Windows.Documents.FlowDocument doc = (System.Windows.Documents.FlowDocument)System.Windows.Markup.XamlReader.Parse(rawXaml);
                doc.PagePadding = new System.Windows.Thickness(0);
                System.Windows.Style pStyle = new System.Windows.Style(typeof(System.Windows.Documents.Paragraph));
                pStyle.Setters.Add(new System.Windows.Setter(System.Windows.Documents.Paragraph.MarginProperty, new System.Windows.Thickness(0)));
                doc.Resources[typeof(System.Windows.Documents.Paragraph)] = pStyle;

                System.Windows.Documents.TextRange tr = new System.Windows.Documents.TextRange(doc.ContentStart, doc.ContentEnd);
                string text = tr.Text;
                Assert(text.Contains("aws"), "Missing 'aws' text in parsed document");
                Assert(text.Contains("frontend react"), "Missing 'frontend react' in parsed document");
                Assert(text.Contains("skill builder 4"), "Missing 'skill builder 4' in parsed document");
                Assert(!text.Contains("<FlowDocument"), "Document text should not contain raw FlowDocument XML tags");
            });

            Console.WriteLine("========================================");
            Console.WriteLine("TEST RESULTS: " + passed + " Passed, " + failed + " Failed");
            Console.WriteLine("========================================");

            return failed == 0 ? 0 : 1;
        }

        static void Test(string name, ref int passed, ref int failed, Action action)
        {
            try
            {
                action();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(name);
                passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine(name + ": " + ex.Message);
                failed++;
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }
    }
}
