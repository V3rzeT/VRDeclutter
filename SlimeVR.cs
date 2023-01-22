using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using VRDeclutter.Interop;

namespace VRDeclutter
{
    public class SlimeVR
    {
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;
        public List<IntPtr> WindowHandles = new();

        public SlimeVR(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToSlimeVR(); // Wait for SlimeVR to be open
            }).Start();
        }

        public void AttachToSlimeVR()
        {
            // Wait for at least one process to appear
            while (!GetWindowHandles(out WindowHandles)) Thread.Sleep(1000); // CPU usage "optimization"

            // Get Java Process from Window Handle
            foreach (var windowHandle in WindowHandles)
            {
                NativeMethods.GetWindowThreadProcessId(windowHandle, out uint windowProcessId);
                Process process = Process.GetProcessById((int)windowProcessId);

                if (process.MainModule != null && process.MainModule.ModuleName == "java.exe")
                {
                    Process = process;
                    break;
                }
            }

            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;

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

            new Thread(delegate(object? o)
            {
                AttachToSlimeVR(); // Wait for SlimeVR to be open
            }).Start();
        }

        public void ShowWindows()
        {
            if (IsHidden)
            {
                foreach (var windowHandle in WindowHandles)
                {
                    if (NativeMethods.IsWindowMinimized(windowHandle))
                        // Hide SlimeVR Window
                        NativeMethods.ShowNormalWindow(windowHandle);

                    Thread.Sleep(100); // So Windows UI doesn't glitch
                }

                IsHidden = false;
            }
        }

        public void HideWindows()
        {
            if (!IsHidden)
            {
                foreach (var windowHandle in WindowHandles)
                    // Hide SlimeVR Window
                    NativeMethods.HideNormalWindow(windowHandle);
                IsHidden = true;
            }
        }

        public bool GetWindowHandles(out List<IntPtr> intPtr)
        {
            intPtr = new List<IntPtr>();
            List<string> windowTitles = new List<string>();

            foreach (IntPtr window in NativeMethods.GetWindows()) // Get all windows
            {
                int titleLength = NativeMethods.GetWindowTextLength(window); // Get window title length
                StringBuilder buffer = new StringBuilder(titleLength);
                NativeMethods.GetWindowText(window, buffer, titleLength + 1); // Get full window title
                string className = NativeMethods.GetClassName(window); // Get class name

                string windowTitle = buffer.ToString();
                if (!string.IsNullOrWhiteSpace(windowTitle))
                    windowTitles.Add(windowTitle);

                if (buffer.ToString().StartsWith("SlimeVR Server") &&
                    (className == "SunAwtFrame") | (className == "ConsoleWindowClass")) intPtr.Add(window);
            }

            if (intPtr.Count > 1)
                return true;

            return false;
        }

        private void WatcherWork()
        {
            while (true)
            {
                if (IsWatching)
                {
                    // If hide on minimize is enabled
                    if (_mainWindow.Settings.SlimeVrActionSetting == Settings.ActionSetting.MinimizeHide)
                        // Check for window state
                        foreach (var windowHandle in WindowHandles)
                            if (NativeMethods.IsWindowMinimized(windowHandle))
                            {
                                // Hide SlimeVR Window
                                NativeMethods.HideNormalWindow(windowHandle);
                                IsHidden = true;
                            }

                    // If autohide is enabled
                    if (_mainWindow.Settings.SlimeVrActionSetting == Settings.ActionSetting.AutoHide && HidingEnabled)
                        foreach (var windowHandle in WindowHandles)
                        {
                            // Hide SlimeVR Window
                            NativeMethods.HideNormalWindow(windowHandle);
                            IsHidden = true;
                        }
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}