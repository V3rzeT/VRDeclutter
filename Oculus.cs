using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VRDeclutter.Interop;

namespace VRDeclutter
{
    public class Oculus
    {
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;

        public Oculus(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToOculus(); // Wait for Oculus to be open
            }).Start();
        }

        public void AttachToOculus()
        {
            // Wait for at least one process to appear
            while (Process.GetProcessesByName("OculusClient").Length == 0)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Wait for Proper Window Handle
            while (!GetWindowHandle(out Process windowHandleProc)) Thread.Sleep(1000); // CPU usage "optimization"

            // Get Process
            GetWindowHandle(out Process windowHandleProc1);
            Process = windowHandleProc1;
            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;

            // Start Watcher
            IsWatching = true;
            _watcherThread.Start();
        }

        // Reset window attachment
        private void OnProcessExited(object? sender, EventArgs e)
        {
            IsWatching = false;
            HidingEnabled = true;

            new Thread(delegate(object? o)
            {
                AttachToOculus(); // Wait for Oculus to be open
            }).Start();
        }

        public void ShowOculus()
        {
            if (IsHidden)
            {
                // Show Oculus Window
                NativeMethods.ShowNormalWindow(Process.MainWindowHandle);
                IsHidden = false;
            }
        }

        public void HideOculus()
        {
            if (!IsHidden)
            {
                // Hide Oculus Window
                NativeMethods.HideNormalWindow(Process.MainWindowHandle);
                IsHidden = true;
            }
        }

        private bool GetWindowHandle(out Process windowHandleProc)
        {
            windowHandleProc = new Process();
            Process[] processes = Process.GetProcessesByName("OculusClient");
            Process? mainWindowHandleProc = processes.FirstOrDefault(x => x.MainWindowHandle != IntPtr.Zero);

            if (mainWindowHandleProc != null && mainWindowHandleProc.MainWindowHandle != IntPtr.Zero)
            {
                windowHandleProc = mainWindowHandleProc;
                return true;
            }

            return false;
        }

        private void WatcherWork()
        {
            while (true)
            {
                if (IsWatching)
                {
                    // If hide on minimize is enabled
                    if (_mainWindow.Settings.OculusActionSetting == Settings.ActionSetting.MinimizeHide)
                        // Check for window state
                        if (NativeMethods.IsWindowMinimized(Process.MainWindowHandle))
                            // Hide Oculus Window
                            HideOculus();

                    // If autohide is enabled
                    if (_mainWindow.Settings.OculusActionSetting == Settings.ActionSetting.AutoHide && HidingEnabled)
                        // Hide Oculus Window
                        HideOculus();
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}