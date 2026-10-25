using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using DesktopStickyNote.Interop;

namespace DesktopStickyNote
{
    public partial class App : Application
    {
        private const string AppMutexName = "stick19_SingleInstance_Mutex_98F8A210";
        private Mutex _singleInstanceMutex;
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Enforce single instance
            bool isNewInstance;
            _singleInstanceMutex = new Mutex(true, AppMutexName, out isNewInstance);

            if (!isNewInstance)
            {
                // Another instance is already running; signal it or exit
                MessageBox.Show(
                    "stick19 is already running in your system tray.\nLook for the sticky note icon near your clock.",
                    "stick19",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Shutdown();
                return;
            }

            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Debug.WriteLine("Unhandled exception: " + args.ExceptionObject);
            };

            // Instantiate and show main window
            _mainWindow = new MainWindow();
            _mainWindow.Show();
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);
            // Synchronously save before Windows reboot or shutdown
            if (_mainWindow != null)
            {
                _mainWindow.ExitApplication();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch { }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }

            base.OnExit(e);
        }
    }
}
