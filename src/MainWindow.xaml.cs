using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using DesktopStickyNote.Controls;
using DesktopStickyNote.Core;

namespace DesktopStickyNote
{
    public partial class MainWindow : Window
    {
        private readonly StorageManager _storage;
        private readonly NoteSettings _settings;
        private DesktopManager _desktopManager;
        private TrayIconManager _trayManager;
        private bool _isInitializing = true;
        private bool _hasUnsavedChanges = false;
        private bool _isExiting = false;

        public MainWindow()
        {
            InitializeComponent();

            _storage = new StorageManager();
            _settings = _storage.Load();

            // Apply saved window geometry
            Left = _settings.Left;
            Top = _settings.Top;
            Width = Math.Max(MinWidth, _settings.Width);
            Height = Math.Max(MinHeight, _settings.Height);

            // Populate document content
            LoadDocumentContent(_settings.Text);
            UpdatePlaceholder();
            UpdateSaveStatusUI();

            // Apply theme & font size
            ApplyTheme(_settings.Theme);
            ApplyFontSize(_settings.FontSize);
            ApplyLockState();

            // Apply opacity & glossy
            ApplyOpacity(_settings.Opacity);
            ApplyGlossy(_settings.IsGlossy);

            SourceInitialized += MainWindow_SourceInitialized;
            LocationChanged += MainWindow_LocationChanged;
            SizeChanged += MainWindow_SizeChanged;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Initialize Desktop Shell Manager
            _desktopManager = new DesktopManager(this, _settings.AttachmentMode);
            _desktopManager.Initialize();
            _desktopManager.EnsureOnScreen();

            // Initialize System Tray Icon
            _trayManager = new TrayIconManager(this, _settings, _storage, _desktopManager);

            _desktopManager.ExplorerRestarted += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _trayManager.RecreateIcon();
                });
            };

            // If visibility was previously false, hide
            if (!_settings.IsVisible)
            {
                Hide();
                _trayManager.UpdateVisibilityMenu(false);
            }
            else
            {
                _desktopManager.SendToBottom();
            }

            UpdateSettingsMenuState();
            _isInitializing = false;
        }

        #region Theme & Font Formatting
        public void ApplyTheme(string themeName)
        {
            NoteTheme theme = ColorThemes.GetTheme(themeName);
            _settings.Theme = theme.Name;

            // Window root & header
            RootBorder.Background = theme.BodyBrush;
            RootBorder.BorderBrush = theme.BorderBrush;
            HeaderBorder.Background = theme.HeaderBrush;

            // Rich text editor
            NoteRichTextBox.Foreground = theme.TextBrush;
            PlaceholderText.Foreground = theme.IsDark
                ? new SolidColorBrush(Color.FromArgb(120, 200, 200, 200))
                : new SolidColorBrush(Color.FromArgb(140, 60, 60, 60));

            // Header icons
            PinPath.Fill = theme.HeaderButtonForeground;
            SettingsIconPath.Fill = theme.HeaderButtonForeground;
            LockIconPath.Fill = theme.HeaderButtonForeground;
            CloseIconPath.Fill = theme.HeaderButtonForeground;

            // Header buttons
            BtnSettings.Foreground = theme.HeaderButtonForeground;
            BtnLock.Foreground = theme.HeaderButtonForeground;
            BtnClose.Foreground = theme.HeaderButtonForeground;

            // Dynamic ContextMenu theme brushes matching active theme
            Resources["MenuBgBrush"] = theme.HeaderBrush;
            Resources["MenuBorderBrush"] = theme.BorderBrush;
            Resources["MenuFgBrush"] = theme.TextBrush;
            Resources["MenuHoverBrush"] = theme.ButtonHoverBrush;

            // Footer save status & manual save button
            TxtSaveStatus.Foreground = theme.IsDark
                ? new SolidColorBrush(Color.FromArgb(140, 180, 180, 180))
                : new SolidColorBrush(Color.FromArgb(140, 100, 100, 100));
            BtnSaveManual.Foreground = theme.HeaderButtonForeground;

            // Dynamic scrollbar thumb color
            if (theme.ScrollThumbBrush != null)
            {
                Resources["ScrollThumbBrush"] = theme.ScrollThumbBrush;
            }

            if (!_isInitializing)
            {
                if (_settings.AutoSave)
                {
                    _storage.RequestSave(_settings);
                }
            }
        }

        public void ApplyFontSize(double size)
        {
            if (size < 9) size = 9;
            if (size > 36) size = 36;
            _settings.FontSize = size;
            NoteRichTextBox.FontSize = size;
            PlaceholderText.FontSize = size;

            if (!_isInitializing)
            {
                if (_settings.AutoSave)
                {
                    _storage.RequestSave(_settings);
                }
            }
        }

        public void ToggleLock()
        {
            _settings.IsLocked = !_settings.IsLocked;
            ApplyLockState();
            _storage.SaveImmediate(_settings);
            if (_trayManager != null)
            {
                _trayManager.UpdateLockMenu(_settings.IsLocked);
            }
            UpdateSettingsMenuState();
        }

        private void ApplyLockState()
        {
            LockedIndicator.Visibility = _settings.IsLocked ? Visibility.Visible : Visibility.Collapsed;

            // Update lock button visual (No keyboard shortcut)
            if (_settings.IsLocked)
            {
                BtnLock.ToolTip = "Unlock note position and size";
                LockIconPath.Data = Geometry.Parse("M 12,2 C 9.79,2 8,3.79 8,6 L 8,8 L 6,8 C 4.9,8 4,8.9 4,10 L 4,20 C 4,21.1 4.9,22 6,22 L 18,22 C 19.1,22 20,21.1 20,20 L 20,10 C 20,8.9 19.1,8 18,8 L 16,8 L 16,6 C 16,3.79 14.21,2 12,2 Z M 10,6 C 10,4.9 10.9,4 12,4 C 13.1,4 14,4.9 14,6 L 14,8 L 10,8 L 10,6 Z");
            }
            else
            {
                BtnLock.ToolTip = "Lock note position and size";
                LockIconPath.Data = Geometry.Parse("M 12,2 C 9.79,2 8,3.79 8,6 L 8,8 L 6,8 C 4.9,8 4,8.9 4,10 L 4,20 C 4,21.1 4.9,22 6,22 L 18,22 C 19.1,22 20,21.1 20,20 L 20,10 C 20,8.9 19.1,8 18,8 L 16,8 L 16,6 C 16,3.79 14.21,2 12,2 Z M 10,6 C 10,4.9 10.9,4 12,4 C 13.1,4 14,4.9 14,6 L 14,8 L 10,8 L 10,6 Z");
            }
        }
        #endregion

        #region Glossy & Transparency Control
        public void ApplyOpacity(double val)
        {
            if (val < 0.20) val = 0.20;
            if (val > 1.0) val = 1.0;
            _settings.Opacity = val;
            RootBorder.Opacity = val;

            if (!_isInitializing)
            {
                _storage.RequestSave(_settings);
            }
            UpdateSettingsMenuState();
        }

        public void ApplyGlossy(bool isGlossy)
        {
            _settings.IsGlossy = isGlossy;
            GlossyHighlight.Visibility = isGlossy ? Visibility.Visible : Visibility.Collapsed;

            if (!_isInitializing)
            {
                _storage.SaveImmediate(_settings);
            }
            UpdateSettingsMenuState();
        }
        #endregion

        #region Header Drag & Controls
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !_settings.IsLocked)
            {
                DragMove();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            UpdateSettingsMenuState();
            SettingsMenu.PlacementTarget = BtnSettings;
            SettingsMenu.Placement = PlacementMode.Bottom;
            SettingsMenu.IsOpen = true;
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            ToggleLock();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Close button hides to system tray
            Hide();
            _settings.IsVisible = false;
            _storage.SaveImmediate(_settings);
            if (_trayManager != null)
            {
                _trayManager.UpdateVisibilityMenu(false);
            }
        }
        #endregion

        #region Settings Menu Handlers & State Sync
        private void UpdateSettingsMenuState()
        {
            if (SettingsMenu == null) return;

            // 1. Color Themes
            if (MenuThemeParent != null)
            {
                foreach (var item in MenuThemeParent.Items)
                {
                    MenuItem mi = item as MenuItem;
                    if (mi != null && mi.Tag != null)
                    {
                        mi.IsChecked = string.Equals(_settings.Theme, mi.Tag.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            // 2. Font Size
            if (MenuFontParent != null)
            {
                foreach (var item in MenuFontParent.Items)
                {
                    MenuItem mi = item as MenuItem;
                    if (mi != null && mi.Tag != null)
                    {
                        double sz;
                        if (double.TryParse(mi.Tag.ToString(), out sz))
                        {
                            mi.IsChecked = Math.Abs(_settings.FontSize - sz) < 0.5;
                        }
                    }
                }
            }

            // 3. Opacity
            if (MenuOpacityParent != null)
            {
                foreach (var item in MenuOpacityParent.Items)
                {
                    MenuItem mi = item as MenuItem;
                    if (mi != null && mi.Tag != null)
                    {
                        double op;
                        if (double.TryParse(mi.Tag.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out op))
                        {
                            mi.IsChecked = Math.Abs(_settings.Opacity - op) < 0.05;
                        }
                    }
                }
            }

            // 4. Glossy
            if (MenuOptGlossy != null)
            {
                MenuOptGlossy.IsChecked = _settings.IsGlossy;
            }

            // 5. Auto-Save
            if (MenuOptAutoSave != null)
            {
                MenuOptAutoSave.IsChecked = _settings.AutoSave;
            }

            // 6. Lock
            if (MenuOptLock != null)
            {
                MenuOptLock.IsChecked = _settings.IsLocked;
            }

            // 7. Startup
            if (MenuOptStartup != null)
            {
                MenuOptStartup.IsChecked = StartupManager.IsStartupEnabled();
            }

            // 8. Desktop Layer Mode
            if (MenuOptModeBottom != null && MenuOptModeWorkerW != null)
            {
                MenuOptModeBottom.IsChecked = (_settings.AttachmentMode == AttachmentMode.DesktopBottomLayer);
                MenuOptModeWorkerW.IsChecked = (_settings.AttachmentMode == AttachmentMode.WorkerWChild);
            }
        }

        private void MenuThemeItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null && mi.Tag != null)
            {
                ApplyTheme(mi.Tag.ToString());
                UpdateSettingsMenuState();
            }
        }

        private void MenuFontSizeItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null && mi.Tag != null)
            {
                double sz;
                if (double.TryParse(mi.Tag.ToString(), out sz))
                {
                    ApplyFontSize(sz);
                    UpdateSettingsMenuState();
                }
            }
        }

        private void MenuOpacityItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null && mi.Tag != null)
            {
                double op;
                if (double.TryParse(mi.Tag.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out op))
                {
                    ApplyOpacity(op);
                    UpdateSettingsMenuState();
                }
            }
        }

        private void MenuOptGlossy_Click(object sender, RoutedEventArgs e)
        {
            ApplyGlossy(!_settings.IsGlossy);
        }

        private void MenuOptAutoSave_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoSave();
        }

        private void MenuOptLock_Click(object sender, RoutedEventArgs e)
        {
            ToggleLock();
        }

        private void MenuOptStartup_Click(object sender, RoutedEventArgs e)
        {
            bool newState = !_settings.LaunchAtStartup;
            StartupManager.SetStartup(newState);
            _settings.LaunchAtStartup = newState;
            _storage.SaveImmediate(_settings);
            UpdateSettingsMenuState();
        }

        private void MenuOptModeBottom_Click(object sender, RoutedEventArgs e)
        {
            if (_desktopManager != null)
            {
                _desktopManager.SetAttachmentMode(AttachmentMode.DesktopBottomLayer);
            }
            _settings.AttachmentMode = AttachmentMode.DesktopBottomLayer;
            _storage.SaveImmediate(_settings);
            UpdateSettingsMenuState();
        }

        private void MenuOptModeWorkerW_Click(object sender, RoutedEventArgs e)
        {
            if (_desktopManager != null)
            {
                _desktopManager.SetAttachmentMode(AttachmentMode.WorkerWChild);
            }
            _settings.AttachmentMode = AttachmentMode.WorkerWChild;
            _storage.SaveImmediate(_settings);
            UpdateSettingsMenuState();
        }

        private void MenuCenter_Click(object sender, RoutedEventArgs e)
        {
            if (_desktopManager != null)
            {
                _desktopManager.CenterOnPrimaryScreen();
                Show();
                _desktopManager.SendToBottom();
                _settings.Left = Left;
                _settings.Top = Top;
                _storage.SaveImmediate(_settings);
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "stick19 for Windows 11\n" +
                "Developed by unstable19\n\n" +
                "Features:\n" +
                "• Attaches directly to Windows Desktop shell layer\n" +
                "• Always stays behind regular applications\n" +
                "• Survives Win+D and Explorer restarts\n" +
                "• Multi-monitor and high-DPI aware\n" +
                "• Themed settings menu & tray options\n" +
                "• Rich formatting: Bold (Ctrl+B), Italic (Ctrl+I), Underline (Ctrl+U)\n" +
                "• Sleek minimalist thin scrollbar & glossy glass sheen\n\n" +
                "Shortcuts:\n" +
                "• Ctrl+S: Save note\n" +
                "• Ctrl++ / Ctrl+-: Change font size\n" +
                "• Escape: Unfocus and drop to desktop",
                "About stick19",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
        #endregion

        #region RichText Content & Formatting
        private void LoadDocumentContent(string savedContent)
        {
            NoteRichTextBox.Document.PagePadding = new Thickness(0);

            if (string.IsNullOrEmpty(savedContent))
            {
                NoteRichTextBox.Document.Blocks.Clear();
                NoteRichTextBox.Document.Blocks.Add(new Paragraph());
                return;
            }

            Style pStyle = new Style(typeof(Paragraph));
            pStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));

            string trimmed = savedContent.TrimStart();
            if (trimmed.StartsWith("<FlowDocument", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    FlowDocument doc = (FlowDocument)XamlReader.Parse(savedContent);
                    doc.PagePadding = new Thickness(0);
                    // Use indexer assignment instead of .Add() to prevent System.ArgumentException
                    // when the serialized FlowDocument already contains the Paragraph style key
                    doc.Resources[typeof(Paragraph)] = pStyle;

                    // Automatically unwrap if raw FlowDocument XAML was ever saved as plain text inside a paragraph
                    bool unnested = false;
                    while (true)
                    {
                        TextRange tr = new TextRange(doc.ContentStart, doc.ContentEnd);
                        string innerText = tr.Text.Trim();
                        if (innerText.StartsWith("<FlowDocument", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                FlowDocument innerDoc = (FlowDocument)XamlReader.Parse(innerText);
                                innerDoc.PagePadding = new Thickness(0);
                                innerDoc.Resources[typeof(Paragraph)] = pStyle;
                                doc = innerDoc;
                                unnested = true;
                                continue;
                            }
                            catch
                            {
                                break;
                            }
                        }
                        break;
                    }

                    NoteRichTextBox.Document = doc;
                    if (unnested)
                    {
                        _settings.Text = GetDocumentXaml();
                        _storage.SaveImmediate(_settings);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error parsing FlowDocument XAML: " + ex.Message);
                }
            }

            // Fallback: Populate plain text
            SetPlainTextContent(savedContent);
        }

        private void SetPlainTextContent(string text)
        {
            NoteRichTextBox.Document.Blocks.Clear();
            string[] lines = (text ?? string.Empty).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                Paragraph p = new Paragraph(new Run(line));
                p.Margin = new Thickness(0);
                NoteRichTextBox.Document.Blocks.Add(p);
            }
        }

        private string GetPlainText()
        {
            TextRange tr = new TextRange(NoteRichTextBox.Document.ContentStart, NoteRichTextBox.Document.ContentEnd);
            return tr.Text.TrimEnd(new char[] { '\r', '\n' });
        }

        private string GetDocumentXaml()
        {
            return XamlWriter.Save(NoteRichTextBox.Document);
        }

        private void NoteRichTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Suppress default Cut, Copy, Paste context menu completely
            e.Handled = true;
        }

        private void NoteRichTextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Suppress right-click actions on text box
            e.Handled = true;
        }

        private void NoteRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholder();

            if (!_isInitializing)
            {
                _settings.Text = GetDocumentXaml();
                if (_settings.AutoSave)
                {
                    _storage.RequestSave(_settings);
                    _hasUnsavedChanges = false;
                    TxtSaveStatus.Text = "Saved";
                    BtnSaveManual.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _hasUnsavedChanges = true;
                    TxtSaveStatus.Text = "Unsaved";
                    BtnSaveManual.Visibility = Visibility.Visible;
                }
            }
        }

        private void NoteRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateFormattingButtonsState();
        }

        private void UpdateFormattingButtonsState()
        {
            // Update Bold
            object weight = NoteRichTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            BtnBold.IsChecked = (weight != DependencyProperty.UnsetValue && (FontWeight)weight == FontWeights.Bold);

            // Update Italic
            object style = NoteRichTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            BtnItalic.IsChecked = (style != DependencyProperty.UnsetValue && (FontStyle)style == FontStyles.Italic);

            // Update Underline
            object dec = NoteRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            BtnUnderline.IsChecked = (dec != DependencyProperty.UnsetValue && dec != null && ((TextDecorationCollection)dec).Count > 0);
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleBold.Execute(null, NoteRichTextBox);
            UpdateFormattingButtonsState();
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleItalic.Execute(null, NoteRichTextBox);
            UpdateFormattingButtonsState();
        }

        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleUnderline.Execute(null, NoteRichTextBox);
            UpdateFormattingButtonsState();
        }

        private void UpdatePlaceholder()
        {
            string txt = GetPlainText();
            PlaceholderText.Visibility = string.IsNullOrEmpty(txt) ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ToggleAutoSave()
        {
            _settings.AutoSave = !_settings.AutoSave;
            if (_settings.AutoSave && _hasUnsavedChanges)
            {
                SaveCurrentContent();
            }
            _storage.SaveImmediate(_settings);
            UpdateSaveStatusUI();
            UpdateSettingsMenuState();
        }

        private void UpdateSaveStatusUI()
        {
            if (_hasUnsavedChanges)
            {
                TxtSaveStatus.Text = "Unsaved";
                BtnSaveManual.Visibility = Visibility.Visible;
            }
            else
            {
                TxtSaveStatus.Text = "Saved";
                BtnSaveManual.Visibility = Visibility.Collapsed;
            }
        }

        public void SaveCurrentContent()
        {
            _settings.Text = GetDocumentXaml();
            _storage.SaveImmediate(_settings);
            _hasUnsavedChanges = false;
            TxtSaveStatus.Text = "Saved";
            BtnSaveManual.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveManual_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentContent();
        }

        private void NoteRichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Keyboard shortcuts (Ctrl+L for lock has been completely removed)
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.S)
                {
                    SaveCurrentContent();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    ApplyFontSize(_settings.FontSize + 2);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    ApplyFontSize(_settings.FontSize - 2);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                // Unfocus and drop note to desktop
                Keyboard.ClearFocus();
                if (_desktopManager != null)
                {
                    _desktopManager.SendToBottom();
                }
                e.Handled = true;
            }
        }
        #endregion

        #region Resizing Handles (Thumbs)
        private void ThumbTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            double newHeight = Height - e.VerticalChange;
            if (newHeight >= MinHeight)
            {
                Top += e.VerticalChange;
                Height = newHeight;
            }
        }

        private void ThumbBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            double newHeight = Height + e.VerticalChange;
            if (newHeight >= MinHeight)
            {
                Height = newHeight;
            }
        }

        private void ThumbLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            double newWidth = Width - e.HorizontalChange;
            if (newWidth >= MinWidth)
            {
                Left += e.HorizontalChange;
                Width = newWidth;
            }
        }

        private void ThumbRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            double newWidth = Width + e.HorizontalChange;
            if (newWidth >= MinWidth)
            {
                Width = newWidth;
            }
        }

        private void ThumbTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            ThumbTop_DragDelta(sender, e);
            ThumbLeft_DragDelta(sender, e);
        }

        private void ThumbTopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            ThumbTop_DragDelta(sender, e);
            ThumbRight_DragDelta(sender, e);
        }

        private void ThumbBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            ThumbBottom_DragDelta(sender, e);
            ThumbLeft_DragDelta(sender, e);
        }

        private void ThumbBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_settings.IsLocked) return;
            ThumbBottom_DragDelta(sender, e);
            ThumbRight_DragDelta(sender, e);
        }
        #endregion

        #region Location & Size Changed
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!_isInitializing)
            {
                _settings.Left = Left;
                _settings.Top = Top;
                _storage.RequestSave(_settings);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                _settings.Width = Width;
                _settings.Height = Height;
                _storage.RequestSave(_settings);
            }
        }
        #endregion

        #region Application Lifecycle
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Intercept close button if triggered via Alt+F4 to hide to tray rather than exiting
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
                _settings.IsVisible = false;
                _storage.SaveImmediate(_settings);
                if (_trayManager != null)
                {
                    _trayManager.UpdateVisibilityMenu(false);
                }
            }
        }

        public void ExitApplication()
        {
            _isExiting = true;
            _settings.Text = GetDocumentXaml();
            _settings.Left = Left;
            _settings.Top = Top;
            _settings.Width = Width;
            _settings.Height = Height;
            _storage.SaveImmediate(_settings);

            if (_trayManager != null)
            {
                _trayManager.Dispose();
                _trayManager = null;
            }

            Application.Current.Shutdown();
        }
        #endregion
    }
}
