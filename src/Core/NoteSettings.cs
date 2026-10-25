using System;

namespace DesktopStickyNote.Core
{
    public enum AttachmentMode
    {
        /// <summary>
        /// Pinned to desktop layer by setting Progman as owner window and enforcing HWND_BOTTOM Z-order.
        /// Fully multi-monitor aware, never clipped, perfectly handles keyboard focus and Win+D.
        /// (Recommended default for Windows 10 and Windows 11).
        /// </summary>
        DesktopBottomLayer = 0,

        /// <summary>
        /// Reparented as a direct child of the desktop WorkerW or Progman window using SetParent.
        /// Attached directly inside the Windows shell hierarchy.
        /// </summary>
        WorkerWChild = 1
    }

    public class NoteSettings
    {
        public string Text { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Theme { get; set; }
        public double FontSize { get; set; }
        public bool IsLocked { get; set; }
        public bool IsVisible { get; set; }
        public bool LaunchAtStartup { get; set; }
        public AttachmentMode AttachmentMode { get; set; }
        public bool AutoSave { get; set; }
        public bool IsGlossy { get; set; }
        public double Opacity { get; set; }

        public NoteSettings()
        {
            // Default settings
            Text = "Welcome to stick19!\n" +
                   "Developed by unstable19\n\n" +
                   "• Pinned directly to your desktop layer\n" +
                   "• Stays behind all applications (Chrome, VS Code, Explorer)\n" +
                   "• Drag header to move anywhere (even across multiple monitors)\n" +
                   "• Resize using any edge or corner\n" +
                   "• Formatting: Bold (Ctrl+B), Italic (Ctrl+I), Underline (Ctrl+U)\n" +
                   "• Adjustable transparency & glossy glass effect\n" +
                   "• Right-click the system tray icon for settings & themes\n" +
                   "• Position, size, and content auto-save continuously";
            Left = 120.0;
            Top = 120.0;
            Width = 340.0;
            Height = 320.0;
            Theme = "Yellow";
            FontSize = 14.0;
            IsLocked = false;
            IsVisible = true;
            LaunchAtStartup = false;
            AttachmentMode = AttachmentMode.DesktopBottomLayer;
            AutoSave = true;
            IsGlossy = true;
            Opacity = 0.90;
        }
    }
}
