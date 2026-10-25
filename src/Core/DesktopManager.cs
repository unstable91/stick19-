using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using DesktopStickyNote.Interop;

namespace DesktopStickyNote.Core
{
    public class DesktopManager
    {
        private readonly Window _window;
        private IntPtr _hwnd;
        private HwndSource _hwndSource;
        private AttachmentMode _attachmentMode;
        private uint _taskbarCreatedMsg;
        private IntPtr _currentProgman = IntPtr.Zero;
        private IntPtr _currentWorkerW = IntPtr.Zero;

        public event Action ExplorerRestarted;
        public event Action DisplaySettingsChanged;

        public DesktopManager(Window window, AttachmentMode mode)
        {
            _window = window;
            _attachmentMode = mode;
        }

        public void Initialize()
        {
            WindowInteropHelper helper = new WindowInteropHelper(_window);
            _hwnd = helper.Handle;

            _taskbarCreatedMsg = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }

            // Remove from taskbar and Alt+Tab
            SetToolWindowStyle();

            // Attach to desktop
            AttachToDesktop(_attachmentMode);
        }

        public void SetAttachmentMode(AttachmentMode mode)
        {
            _attachmentMode = mode;
            AttachToDesktop(mode);
        }

        private void SetToolWindowStyle()
        {
            if (_hwnd == IntPtr.Zero) return;

            int exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt32();
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));
        }

        public void AttachToDesktop(AttachmentMode mode)
        {
            if (_hwnd == IntPtr.Zero) return;

            IntPtr progman = FindProgman();
            _currentProgman = progman;

            if (mode == AttachmentMode.WorkerWChild)
            {
                IntPtr workerW = FindWorkerW(progman);
                _currentWorkerW = workerW;

                if (workerW != IntPtr.Zero)
                {
                    // Attach as child of WorkerW
                    NativeMethods.SetParent(_hwnd, workerW);

                    // Update styles for child window
                    int style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt32();
                    style |= NativeMethods.WS_CHILD;
                    style &= ~NativeMethods.WS_POPUP;
                    NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE, new IntPtr(style));

                    NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOP, 0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);

                    Debug.WriteLine("DesktopManager: Attached to WorkerW 0x" + workerW.ToString("X"));
                    return;
                }
                else
                {
                    Debug.WriteLine("DesktopManager: WorkerW not found, falling back to DesktopBottomLayer");
                    _attachmentMode = AttachmentMode.DesktopBottomLayer;
                }
            }

            // DesktopBottomLayer mode (Recommended & Default for Windows 11)
            // If previously reparented, restore parent to desktop root
            NativeMethods.SetParent(_hwnd, IntPtr.Zero);

            int currentStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt32();
            currentStyle |= NativeMethods.WS_POPUP;
            currentStyle &= ~NativeMethods.WS_CHILD;
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE, new IntPtr(currentStyle));

            // Set Progman as owner window so Desktop Window Manager associates it with the desktop layer
            if (progman != IntPtr.Zero)
            {
                NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_HWNDPARENT, progman);
            }

            SetToolWindowStyle();

            // Send to bottom of Z-order
            SendToBottom();
            Debug.WriteLine("DesktopManager: Attached to DesktopBottomLayer (Progman: 0x" + progman.ToString("X") + ")");
        }

        public void SendToBottom()
        {
            if (_hwnd == IntPtr.Zero) return;

            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_BOTTOM,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOSENDCHANGING);
        }

        public void RecheckDesktopAttachment()
        {
            AttachToDesktop(_attachmentMode);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_taskbarCreatedMsg != 0 && (uint)msg == _taskbarCreatedMsg)
            {
                // Explorer restarted
                Debug.WriteLine("DesktopManager: TaskbarCreated received. Re-attaching to desktop.");
                AttachToDesktop(_attachmentMode);
                if (ExplorerRestarted != null)
                {
                    ExplorerRestarted.Invoke();
                }
                handled = true;
                return IntPtr.Zero;
            }

            switch (msg)
            {
                case NativeMethods.WM_ACTIVATE:
                    int wa = wParam.ToInt32() & 0xFFFF;
                    if (wa == NativeMethods.WA_INACTIVE)
                    {
                        // Window lost focus: immediately send to bottom
                        if (_attachmentMode == AttachmentMode.DesktopBottomLayer)
                        {
                            SendToBottom();
                        }

                        // Minimize memory footprint when deactivated
                        TrimMemory();
                    }
                    break;

                case NativeMethods.WM_WINDOWPOSCHANGING:
                    if (_attachmentMode == AttachmentMode.DesktopBottomLayer)
                    {
                        // If this window is not currently active, ensure it stays at bottom
                        IntPtr foreground = NativeMethods.GetForegroundWindow();
                        if (foreground != _hwnd)
                        {
                            NativeMethods.WINDOWPOS pos = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(NativeMethods.WINDOWPOS));
                            // Force hwndInsertAfter to HWND_BOTTOM unless SWP_NOZORDER is set
                            if ((pos.flags & NativeMethods.SWP_NOZORDER) == 0)
                            {
                                pos.hwndInsertAfter = NativeMethods.HWND_BOTTOM;
                                Marshal.StructureToPtr(pos, lParam, false);
                            }
                        }
                    }
                    break;

                case NativeMethods.WM_MOUSEACTIVATE:
                    // Enable immediate activation on click
                    handled = true;
                    return new IntPtr(NativeMethods.MA_ACTIVATE);

                case NativeMethods.WM_DISPLAYCHANGE:
                    if (DisplaySettingsChanged != null)
                    {
                        DisplaySettingsChanged.Invoke();
                    }
                    EnsureOnScreen();
                    break;
            }

            return IntPtr.Zero;
        }

        public void EnsureOnScreen()
        {
            try
            {
                double virtLeft = SystemParameters.VirtualScreenLeft;
                double virtTop = SystemParameters.VirtualScreenTop;
                double virtWidth = SystemParameters.VirtualScreenWidth;
                double virtHeight = SystemParameters.VirtualScreenHeight;

                double noteLeft = _window.Left;
                double noteTop = _window.Top;
                double noteWidth = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
                double noteHeight = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;

                // Check if the note is completely outside the virtual desktop bounds
                bool isOutside = (noteLeft + noteWidth < virtLeft + 50) ||
                                 (noteLeft > virtLeft + virtWidth - 50) ||
                                 (noteTop + noteHeight < virtTop + 50) ||
                                 (noteTop > virtTop + virtHeight - 50);

                if (isOutside)
                {
                    // Center on primary screen
                    _window.Left = (SystemParameters.PrimaryScreenWidth - noteWidth) / 2;
                    _window.Top = (SystemParameters.PrimaryScreenHeight - noteHeight) / 2;
                    Debug.WriteLine("DesktopManager: Note was off-screen, repositioned to center.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EnsureOnScreen exception: " + ex.Message);
            }
        }

        public void CenterOnPrimaryScreen()
        {
            double w = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
            double h = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;
            _window.Left = Math.Max(50, (SystemParameters.PrimaryScreenWidth - w) / 2);
            _window.Top = Math.Max(50, (SystemParameters.PrimaryScreenHeight - h) / 2);
        }

        private static IntPtr FindProgman()
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                progman = NativeMethods.GetShellWindow();
            }
            return progman;
        }

        private static IntPtr FindWorkerW(IntPtr progman)
        {
            if (progman == IntPtr.Zero)
            {
                progman = FindProgman();
            }

            if (progman == IntPtr.Zero) return IntPtr.Zero;

            // Send message 0x052C to Progman to spawn the WorkerW behind desktop icons
            IntPtr result;
            NativeMethods.SendMessageTimeout(
                progman,
                0x052C,
                new IntPtr(0xD),
                new IntPtr(0x1),
                NativeMethods.SMTO_NORMAL,
                1000,
                out result);

            IntPtr targetWorkerW = IntPtr.Zero;

            // Enumerate windows to find the WorkerW that sits behind icons
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                NativeMethods.GetClassName(hWnd, sb, 256);
                if (sb.ToString() == "WorkerW")
                {
                    IntPtr defView = NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        // In some Windows versions, the wallpaper WorkerW is the next sibling
                        targetWorkerW = NativeMethods.FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (targetWorkerW == IntPtr.Zero)
            {
                // Fallback: check if DefView is inside Progman itself (common in Windows 11)
                IntPtr defViewInProgman = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defViewInProgman != IntPtr.Zero)
                {
                    // Find WorkerW child or sibling
                    NativeMethods.EnumChildWindows(progman, (hWnd, lParam) =>
                    {
                        StringBuilder sb = new StringBuilder(256);
                        NativeMethods.GetClassName(hWnd, sb, 256);
                        if (sb.ToString() == "WorkerW")
                        {
                            targetWorkerW = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                }
            }

            return targetWorkerW != IntPtr.Zero ? targetWorkerW : progman;
        }

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(0, GCCollectionMode.Optimized);
                NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch { }
        }
    }
}
