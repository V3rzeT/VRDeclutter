using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRDeclutter.Interop;
using WindowsDesktop;

namespace VRDeclutter
{
    public class WMRPortal
    {
        private readonly VirtualDesktop _mainDesktop;
        private readonly MainWindow _mainWindow;
        private readonly Thread _watcherThread;
        public VirtualDesktop? HiddenDesktop;
        public bool HidingEnabled = true;
        public bool IsHidden;
        public bool IsWatching;
        public Process Process;
        public IntPtr WindowHandle;

        public WMRPortal(VirtualDesktop mainDesktop, MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _mainDesktop = mainDesktop;
            _watcherThread = new Thread(WatcherWork) { IsBackground = true };
            _watcherThread.SetApartmentState(ApartmentState.STA);

            new Thread(delegate(object? o)
            {
                AttachToPortal(); // Wait for portal to be open
            }).Start();
        }

        // Might remove later, opens WMR
        public bool StartPortal()
        {
            try
            {
                // Can also use "shell:appsfolder\\Microsoft.MixedReality.Portal_8wekyb3d8bbwe!App" instead
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                        { UseShellExecute = true, FileName = "ms-holographicfirstrun://", CreateNoWindow = true }
                };
                process.Start(); // Execute launch portal command

                return true;
            }
            catch
            {
            }

            return false;
        }

        public void AttachToPortal()
        {
            // Wait for at least one process to appear
            while (Process.GetProcessesByName("MixedRealityPortal").Length == 0)
                Thread.Sleep(1000); // CPU usage "optimization"

            // Wait for Proper Window Handle
            while (!GetPortalWindowHandle(out IntPtr portalHandle) | (portalHandle == IntPtr.Zero))
                Thread.Sleep(1000); // CPU usage "optimization"

            GetPortalWindowHandle(out IntPtr portalHandle1);
            WindowHandle = portalHandle1;

            // Get process
            Process = Process.GetProcessesByName("MixedRealityPortal")[0];
            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;

            // Add event handlers
            VirtualDesktop.Destroyed += OnDesktopDestroyed;

            // Start watcher
            IsWatching = true;
            _watcherThread.Start();
        }

        private void OnDesktopDestroyed(object? sender, VirtualDesktopDestroyEventArgs e)
        {
            // Rehide desktop
            var hiddenDesktop = HidePortal();
            if (hiddenDesktop != null)
                HiddenDesktop = hiddenDesktop;
        }

        // Reset window attachment
        private void OnProcessExited(object? sender, EventArgs e)
        {
            IsWatching = false;
            HidingEnabled = true;

            new Thread(delegate(object? o)
            {
                AttachToPortal(); // Wait for portal to be open
            }).Start();
        }

        public static bool GetPortalWindowHandle(out IntPtr intPtr)
        {
            string resourcePath =
                "@{Microsoft.MixedReality.Portal_2000.21051.1282.0_x64__8wekyb3d8bbwe?ms-resource://Microsoft.MixedReality.Portal/Resources/PkgDisplayName}";
            StringBuilder outBuf = new StringBuilder();
            int result =
                NativeMethods.SHLoadIndirectString(resourcePath, outBuf, -1,
                    IntPtr.Zero); // Get localized window name (app name)

            intPtr = IntPtr.Zero;

            if (result == 0)
            {
                intPtr = NativeMethods.FindWindow(null, outBuf.ToString()); // Find window by using it's title
                return true;
            }

            return false;
        }

        public void ShowPortal()
        {
            // Move back to main desktop
            VirtualDesktop.MoveToDesktop(WindowHandle, _mainDesktop);

            // Restore
            NativeMethods.ShowWindow(WindowHandle, (int)NativeMethods.ShowWindowCommands.SW_RESTORE);

            // Set focus
            NativeMethods.SetForegroundWindow(WindowHandle);

            IsHidden = false;
        }

        public VirtualDesktop? HidePortal()
        {
            // Move to other desktop to get rid of taskbar icon
            try
            {
                List<VirtualDesktop> desktops = VirtualDesktop.GetDesktops().ToList();
                desktops.Remove(_mainDesktop); // Remove main desktop from desktops list

                if (desktops.Count < 2)
                    while (desktops.Count < 2)
                        desktops.Add(VirtualDesktop
                            .Create()); // Create a desktop until there are at least 2 non main ones

                // Move to random desktop and confirm move
                var randomDesktop = desktops[new Random().Next(0, desktops.Count)];
                while (VirtualDesktop.FromHwnd(WindowHandle) != randomDesktop)
                    VirtualDesktop.MoveToDesktop(WindowHandle, randomDesktop);

                IsHidden = true;
                return randomDesktop;
            }
            catch
            {
            }

            return null;
        }

        private void WatcherWork()
        {
            while (true)
            {
                if (IsWatching)
                {
                    // If hide on minimize is enabled
                    if (_mainWindow.Settings.WmrPortalActionSetting == Settings.ActionSetting.MinimizeHide && !IsHidden)
                        // Check for window state
                        if (NativeMethods.IsWindowMinimized(WindowHandle))
                        {
                            // Hide portal
                            var hiddenDesktop = HidePortal();
                            if (hiddenDesktop != null)
                                HiddenDesktop = hiddenDesktop;

                            IsHidden = true;
                        }

                    // If autohide is enabled
                    if (_mainWindow.Settings.WmrPortalActionSetting == Settings.ActionSetting.AutoHide &&
                        HidingEnabled && !IsHidden)
                    {
                        // Hide portal
                        var hiddenDesktop = HidePortal();
                        if (hiddenDesktop != null)
                            HiddenDesktop = hiddenDesktop;

                        IsHidden = true;
                    }

                    // Check for desktop switch to hidden one
                    // VirtualDesktop.CurrentChanged event might be able to replace this, but I cba to test it, this probably works faster anyway :v
                    if (IsHidden && HidingEnabled && VirtualDesktop.Current == HiddenDesktop)
                    {
                        var hiddenDesktop = HidePortal();
                        if (hiddenDesktop != null)
                            HiddenDesktop = hiddenDesktop;
                    }
                }

                // CPU Usage "optimization"
                Thread.Sleep(1);
            }
        }
    }
}