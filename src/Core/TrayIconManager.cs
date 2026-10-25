using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using DesktopStickyNote.Controls;

namespace DesktopStickyNote.Core
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private readonly MainWindow _mainWindow;
        private readonly NoteSettings _settings;
        private readonly StorageManager _storage;
        private readonly DesktopManager _desktopManager;

        private ToolStripMenuItem _toggleVisibleItem;
        private ToolStripMenuItem _lockItem;
        private ToolStripMenuItem _autoSaveItem;
        private ToolStripMenuItem _startupItem;
        private ToolStripMenuItem _modeBottomItem;
        private ToolStripMenuItem _modeWorkerWItem;

        public TrayIconManager(MainWindow mainWindow, NoteSettings settings, StorageManager storage, DesktopManager desktopManager)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            _storage = storage;
            _desktopManager = desktopManager;

            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "stick19";
            _notifyIcon.Icon = LoadAppIcon();
            _notifyIcon.Visible = true;

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleNoteVisibility();
                }
            };

            _contextMenu = BuildContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        public void RecreateIcon()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                InitializeTrayIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RecreateIcon error: " + ex.Message);
            }
        }

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI", 9f);

            // 1. Show/Hide
            _toggleVisibleItem = new ToolStripMenuItem(_mainWindow.IsVisible ? "Hide Note" : "Show Note", null, (s, e) => ToggleNoteVisibility());
            _toggleVisibleItem.Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(_toggleVisibleItem);

            // 2. Center Note
            menu.Items.Add(new ToolStripMenuItem("Center on Screen", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _desktopManager.CenterOnPrimaryScreen();
                    _mainWindow.Show();
                    _desktopManager.SendToBottom();
                    _settings.Left = _mainWindow.Left;
                    _settings.Top = _mainWindow.Top;
                    _storage.SaveImmediate(_settings);
                });
            }));

            menu.Items.Add(new ToolStripSeparator());

            // 3. Color Theme Submenu
            ToolStripMenuItem themeMenu = new ToolStripMenuItem("Color Theme");
            foreach (var t in ColorThemes.AllThemes)
            {
                var captured = t;
                ToolStripMenuItem item = new ToolStripMenuItem(captured.DisplayName, null, (s, e) =>
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.ApplyTheme(captured.Name);
                        UpdateThemeChecks(themeMenu);
                    });
                });
                item.Checked = string.Equals(_settings.Theme, captured.Name, StringComparison.OrdinalIgnoreCase);
                item.Tag = captured.Name;
                themeMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(themeMenu);

            // 4. Font Size Submenu
            ToolStripMenuItem fontMenu = new ToolStripMenuItem("Font Size");
            double[] sizes = new double[] { 11.0, 13.0, 15.0, 18.0, 22.0 };
            string[] sizeLabels = new string[] { "Small (11 pt)", "Normal (13 pt)", "Medium (15 pt)", "Large (18 pt)", "Extra Large (22 pt)" };
            for (int i = 0; i < sizes.Length; i++)
            {
                double sz = sizes[i];
                ToolStripMenuItem fItem = new ToolStripMenuItem(sizeLabels[i], null, (s, e) =>
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.ApplyFontSize(sz);
                        UpdateFontSizeChecks(fontMenu, sz);
                    });
                });
                fItem.Checked = Math.Abs(_settings.FontSize - sz) < 0.5;
                fItem.Tag = sz;
                fontMenu.DropDownItems.Add(fItem);
            }
            menu.Items.Add(fontMenu);

            // 5. Opacity / Transparency Submenu
            ToolStripMenuItem opacityMenu = new ToolStripMenuItem("Transparency / Opacity");
            double[] opacities = new double[] { 1.0, 0.90, 0.80, 0.70, 0.60, 0.50, 0.40 };
            string[] opacityLabels = new string[] { "100% (Solid)", "90% (Subtle)", "80% (Glossy)", "70%", "60%", "50%", "40% (Ultra Glass)" };
            for (int i = 0; i < opacities.Length; i++)
            {
                double op = opacities[i];
                ToolStripMenuItem opItem = new ToolStripMenuItem(opacityLabels[i], null, (s, e) =>
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.ApplyOpacity(op);
                        UpdateOpacityChecks(opacityMenu, op);
                    });
                });
                opItem.Checked = Math.Abs(_settings.Opacity - op) < 0.05;
                opItem.Tag = op;
                opacityMenu.DropDownItems.Add(opItem);
            }
            menu.Items.Add(opacityMenu);

            // 6. Glossy Effect Toggle
            ToolStripMenuItem glossyItem = new ToolStripMenuItem("Glossy Glass Sheen", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.ApplyGlossy(!_settings.IsGlossy);
                    ((ToolStripMenuItem)s).Checked = _settings.IsGlossy;
                });
            });
            glossyItem.Checked = _settings.IsGlossy;
            menu.Items.Add(glossyItem);

            // 7. Lock Position
            _lockItem = new ToolStripMenuItem("Lock Note (Prevent Move/Resize)", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.ToggleLock();
                    _lockItem.Checked = _settings.IsLocked;
                });
            });
            _lockItem.Checked = _settings.IsLocked;
            menu.Items.Add(_lockItem);

            // 8. Auto-Save Toggle
            _autoSaveItem = new ToolStripMenuItem("Auto-Save", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.ToggleAutoSave();
                    _autoSaveItem.Checked = _settings.AutoSave;
                });
            });
            _autoSaveItem.Checked = _settings.AutoSave;
            menu.Items.Add(_autoSaveItem);

            menu.Items.Add(new ToolStripSeparator());

            // 9. Desktop Integration Mode Submenu
            ToolStripMenuItem modeMenu = new ToolStripMenuItem("Desktop Layer Mode");
            _modeBottomItem = new ToolStripMenuItem("Desktop Bottom Layer (Recommended for Win 11)", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _desktopManager.SetAttachmentMode(AttachmentMode.DesktopBottomLayer);
                    _settings.AttachmentMode = AttachmentMode.DesktopBottomLayer;
                    _storage.SaveImmediate(_settings);
                    _modeBottomItem.Checked = true;
                    _modeWorkerWItem.Checked = false;
                });
            });
            _modeWorkerWItem = new ToolStripMenuItem("WorkerW Shell Child (Direct reparenting)", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _desktopManager.SetAttachmentMode(AttachmentMode.WorkerWChild);
                    _settings.AttachmentMode = AttachmentMode.WorkerWChild;
                    _storage.SaveImmediate(_settings);
                    _modeBottomItem.Checked = false;
                    _modeWorkerWItem.Checked = true;
                });
            });
            _modeBottomItem.Checked = (_settings.AttachmentMode == AttachmentMode.DesktopBottomLayer);
            _modeWorkerWItem.Checked = (_settings.AttachmentMode == AttachmentMode.WorkerWChild);
            modeMenu.DropDownItems.Add(_modeBottomItem);
            modeMenu.DropDownItems.Add(_modeWorkerWItem);
            menu.Items.Add(modeMenu);

            // 8. Startup toggle
            _startupItem = new ToolStripMenuItem("Launch at Windows Startup", null, (s, e) =>
            {
                bool newState = !_settings.LaunchAtStartup;
                StartupManager.SetStartup(newState);
                _settings.LaunchAtStartup = newState;
                _storage.SaveImmediate(_settings);
                _startupItem.Checked = newState;
            });
            _startupItem.Checked = StartupManager.IsStartupEnabled();
            menu.Items.Add(_startupItem);

            menu.Items.Add(new ToolStripSeparator());

            // 9. About
            menu.Items.Add(new ToolStripMenuItem("About stick19", null, (s, e) =>
            {
                MessageBox.Show(
                    "stick19 for Windows 11\n" +
                    "Developed by unstable19\n\n" +
                    "Version 1.2.0\n\n" +
                    "Features:\n" +
                    "• Attaches directly to Windows Desktop shell layer\n" +
                    "• Always stays behind regular applications\n" +
                    "• Survives Win+D and Explorer restarts\n" +
                    "• Multi-monitor and high-DPI aware\n" +
                    "• Configurable Auto-Save option\n" +
                    "• Sleek minimalist thin scrollbar\n" +
                    "• Continuous local persistence\n\n" +
                    "Keyboard Shortcuts:\n" +
                    "• Ctrl+S: Save immediately\n" +
                    "• Ctrl++ / Ctrl+-: Adjust font size\n" +
                    "• Escape: Unfocus and drop to desktop",
                    "About stick19",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }));

            // 10. Exit
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.ExitApplication();
                });
            }));

            return menu;
        }

        public void UpdateVisibilityMenu(bool isVisible)
        {
            if (_toggleVisibleItem != null)
            {
                _toggleVisibleItem.Text = isVisible ? "Hide Note" : "Show Note";
            }
        }

        public void UpdateLockMenu(bool isLocked)
        {
            if (_lockItem != null)
            {
                _lockItem.Checked = isLocked;
            }
        }

        private void UpdateThemeChecks(ToolStripMenuItem themeMenu)
        {
            foreach (ToolStripItem item in themeMenu.DropDownItems)
            {
                ToolStripMenuItem tItem = item as ToolStripMenuItem;
                if (tItem != null && tItem.Tag != null)
                {
                    tItem.Checked = string.Equals(_settings.Theme, tItem.Tag.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private void UpdateFontSizeChecks(ToolStripMenuItem fontMenu, double activeSize)
        {
            foreach (ToolStripItem item in fontMenu.DropDownItems)
            {
                ToolStripMenuItem fItem = item as ToolStripMenuItem;
                if (fItem != null && fItem.Tag != null)
                {
                    double sz = (double)fItem.Tag;
                    fItem.Checked = Math.Abs(sz - activeSize) < 0.5;
                }
            }
        }

        private void UpdateOpacityChecks(ToolStripMenuItem opacityMenu, double activeOpacity)
        {
            foreach (ToolStripItem item in opacityMenu.DropDownItems)
            {
                ToolStripMenuItem opItem = item as ToolStripMenuItem;
                if (opItem != null && opItem.Tag != null)
                {
                    double op = (double)opItem.Tag;
                    opItem.Checked = Math.Abs(op - activeOpacity) < 0.05;
                }
            }
        }

        public void ToggleNoteVisibility()
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                    _settings.IsVisible = false;
                    _storage.SaveImmediate(_settings);
                    UpdateVisibilityMenu(false);
                }
                else
                {
                    _mainWindow.Show();
                    _desktopManager.SendToBottom();
                    _settings.IsVisible = true;
                    _storage.SaveImmediate(_settings);
                    UpdateVisibilityMenu(true);
                }
            });
        }

        public void ShowFirstLaunchBalloon()
        {
            try
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "stick19",
                    "Note is attached to your desktop. Right-click this tray icon anytime for settings.",
                    ToolTipIcon.Info);
            }
            catch { }
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // Check directory above (in case running from bin\Release)
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "app.ico");
                if (File.Exists(fallbackPath))
                {
                    return new Icon(fallbackPath);
                }
            }
            catch { }

            // Dynamic programmatic fallback icon
            return SystemIcons.Application;
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            if (_contextMenu != null)
            {
                _contextMenu.Dispose();
                _contextMenu = null;
            }
        }
    }
}
