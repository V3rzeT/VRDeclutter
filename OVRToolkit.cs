using System;
using System.Diagnostics;
using System.Threading;
using VRDeclutter.Interop;

namespace VRDeclutter
{
    public class OVRToolkit
    {
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;

        public OVRToolkit(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToOVRToolkit(); // Wait for OVRToolkit to be open
            }).Start();
        }

        public void AttachToOVRToolkit()
        {
            // Wait for at least one process to appear
            while (Process.GetProcessesByName("OVR Toolkit Settings").Length == 0)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Wait for Proper Window Handle
            while (Process.GetProcessesByName("OVR Toolkit Settings")[0].MainWindowHandle == IntPtr.Zero)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Get Process
            Process = Process.GetProcessesByName("OVR Toolkit Settings")[0];
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
                AttachToOVRToolkit(); // Wait for OVRToolkit to be open
            }).Start();
        }

        public void ShowOVR()
        {
            if (IsHidden)
            {
                // Show OVRToolkit Window
                NativeMethods.ShowNormalWindow(Process.MainWindowHandle);
                IsHidden = false;
            }
        }

        public void HideOVR()
        {
            if (!IsHidden)
            {
                // Hide OVRToolkit Window
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
                    if (_mainWindow.Settings.OvrToolkitActionSetting == Settings.ActionSetting.MinimizeHide)
                        // Check for window state
                        if (NativeMethods.IsWindowMinimized(Process.MainWindowHandle))
                            // Hide OVRToolkit Window
                            HideOVR();

                    // If autohide is enabled
                    if (_mainWindow.Settings.OvrToolkitActionSetting == Settings.ActionSetting.AutoHide &&
                        HidingEnabled)
                        // Hide OVRToolkit Window
                        HideOVR();
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}