using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRDeclutter.Interop;

namespace VRDeclutter
{
    public class SteamVR
    {
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;

        public SteamVR(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToSteamVR(); // Wait for SteamVR to be open
            }).Start();
        }

        public void AttachToSteamVR()
        {
            // Wait for at least one process to appear
            while (Process.GetProcessesByName("vrmonitor").Length == 0)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Get process
            Process = Process.GetProcessesByName("vrmonitor")[0];
            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;

            // Wait for at least one visible window to appear
            while (!GetAllVisibleWindows(out List<IntPtr> visibleWindows) | (visibleWindows.Count == 0))
                Thread.Sleep(1000); // CPU usage "optimization"

            // Start Watcher
            IsWatching = true;
            if (_watcherThread.IsAlive == false)
                _watcherThread.Start();
        }

        // Reset window attachment
        private void OnProcessExited(object? sender, EventArgs e)
        {
            IsWatching = false;
            HidingEnabled = true;

            // Close all other apps on SteamVR exit
            if (_mainWindow.Settings.CloseAllWithSteamVR)
                _mainWindow.TrayMenu.Dispatcher.Invoke(() => _mainWindow.TrayMenu.BtnCloseAll_OnClick(null, null));

            new Thread(delegate(object? o)
            {
                AttachToSteamVR(); // Wait for SteamVR to be open
            }).Start();
        }

        public void ShowSteamVR()
        {
            if (IsHidden)
            {
                NativeMethods.ShowNormalWindow(Process.MainWindowHandle); // Show main window

                IsHidden = false;
            }
        }

        public void HideSteamVR()
        {
            if (GetAllVisibleWindows(out List<IntPtr> visibleWindows) && visibleWindows.Count > 0)
            {
                foreach (var visibleWindow in visibleWindows)
                    NativeMethods.HideNormalWindow(visibleWindow); // Hide window

                IsHidden = true;
            }
        }

        public bool GetAllVisibleWindows(out List<IntPtr> visibleWindows)
        {
            visibleWindows = new List<IntPtr>();

            try
            {
                // Get all windows under UI Thread
                var windowList = NativeMethods.GetWindowsUnderSameUIThread(Process.MainWindowHandle);

                // Filter non visible windows
                foreach (var windowHandle in windowList)
                    // Check if the window is visible or not
                    if (NativeMethods.IsWindowVisible(windowHandle))
                        visibleWindows.Add(windowHandle);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void WatcherWork()
        {
            while (true)
            {
                if (IsWatching)
                {
                    // If hide on minimize is enabled
                    if (_mainWindow.Settings.SteamVrActionSetting == Settings.ActionSetting.MinimizeHide)
                    {
                        // Check for window states
                        GetAllVisibleWindows(out List<IntPtr> visibleWindows);
                        foreach (var window in visibleWindows)
                            if (NativeMethods.IsWindowMinimized(window))
                            {
                                // Hide SteamVR Windows
                                HideSteamVR();
                                break;
                            }
                    }

                    // If autohide is enabled
                    if (_mainWindow.Settings.SteamVrActionSetting == Settings.ActionSetting.AutoHide && HidingEnabled)
                        HideSteamVR();
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}