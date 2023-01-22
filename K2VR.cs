using System;
using System.Diagnostics;
using System.Threading;
using VRDeclutter.Interop;

namespace VRDeclutter
{
    public class K2VR
    {
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;

        public K2VR(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToK2VR(); // Wait for K2VR to be open
            }).Start();
        }

        public void AttachToK2VR()
        {
            // Wait for at least one process to appear
            while (GetK2VRProcess() == null)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Wait for Proper Window Handle
            while (GetK2VRProcess().MainWindowHandle == IntPtr.Zero)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Get Process
            Process = GetK2VRProcess();

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
                AttachToK2VR(); // Wait for K2VR to be open
            }).Start();
        }

        private Process? GetK2VRProcess()
        {
            Process[] oldProcess = Process.GetProcessesByName("KinectV1Process");
            Process[] newProcess = Process.GetProcessesByName("KinectToVR");

            foreach (Process process in oldProcess)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                    return process;
            }

            foreach (Process process in newProcess)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                    return process;
            }

            return null;
        }

        public void ShowK2VR()
        {
            if (IsHidden)
            {
                // Show K2VR Window
                NativeMethods.ShowNormalWindow(Process.MainWindowHandle);
                IsHidden = false;
            }
        }

        public void HideK2VR()
        {
            if (!IsHidden || NativeMethods.IsWindowVisible(Process.MainWindowHandle))
            {
                // Hide K2VR Window
                NativeMethods.HideNormalWindow(Process.MainWindowHandle);
                IsHidden = true;
            }
        }

        private void WatcherWork()
        {
            while (true)
            {
                if (IsWatching)
                {
                    // If hide on minimize is enabled
                    if (_mainWindow.Settings.K2VrActionSetting == Settings.ActionSetting.MinimizeHide)
                        // Check for window state
                        if (NativeMethods.IsWindowMinimized(Process.MainWindowHandle))
                            // Hide K2VR Window
                            HideK2VR();

                    // If autohide is enabled
                    if (_mainWindow.Settings.K2VrActionSetting == Settings.ActionSetting.AutoHide && HidingEnabled)
                        // Hide K2VR Window
                        HideK2VR();
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}