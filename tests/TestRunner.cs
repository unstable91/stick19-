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
            });

            Test("StartupManager: Registry query succeeds without exception", ref passed, ref failed, () =>
            {
                bool enabled = StartupManager.IsStartupEnabled();
                Console.WriteLine("    [Info] Current Run at startup state: " + enabled);
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
