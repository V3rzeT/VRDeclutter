using System;
using System.Threading;
using System.Windows;
using Point = System.Drawing.Point;

namespace VRDeclutter.Views
{
    /// <summary>
    ///     Interaction logic for TrayMenu.xaml
    /// </summary>
    public partial class TrayMenu : Window
    {
        private readonly MainWindow _mainWindow;

        public TrayMenu(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        // Force loads UI to get correct Height
        public void ForceLoad()
        {
            Opacity = 0;
            Show();
            Hide();
            Opacity = 1;
        }

        // Sets position so Window can appear at cursor
        public void SetCenter()
        {
            Point mousePoint = System.Windows.Forms.Cursor.Position;
            Top = mousePoint.Y - Height;
            Left = mousePoint.X - Width;
        }

        private void BtnShowAll_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();

            Thread.Sleep(200);

            // SlimeVR
            SlimeVR slimeVr = _mainWindow.SlimeVr;
            if (slimeVr.IsWatching && _mainWindow.Settings.SlimeVrActionSetting == Settings.ActionSetting.AutoHide)
            {
                slimeVr.HidingEnabled = false;
                slimeVr.ShowWindows();
                Thread.Sleep(200);
            }

            // WMR
            WMRPortal wmrPortal = _mainWindow.WmrPortal;
            if (wmrPortal.IsWatching && _mainWindow.Settings.WmrPortalActionSetting == Settings.ActionSetting.AutoHide)
            {
                wmrPortal.HidingEnabled = false;
                wmrPortal.ShowPortal();
                Thread.Sleep(200);
            }

            // SteamVR
            SteamVR steamVr = _mainWindow.SteamVr;
            if (steamVr.IsWatching && _mainWindow.Settings.SteamVrActionSetting == Settings.ActionSetting.AutoHide)
            {
                steamVr.HidingEnabled = false;
                steamVr.ShowSteamVR();
                Thread.Sleep(200);
            }

            // Oculus
            Oculus oculus = _mainWindow.Oculus;
            if (oculus.IsWatching && _mainWindow.Settings.OculusActionSetting == Settings.ActionSetting.AutoHide)
            {
                oculus.HidingEnabled = false;
                oculus.ShowOculus();
                Thread.Sleep(200);
            }

            // OVRToolkit
            OVRToolkit ovrToolkit = _mainWindow.OvrToolkit;
            if (ovrToolkit.IsWatching && _mainWindow.Settings.OvrToolkitActionSetting == Settings.ActionSetting.AutoHide)
            {
                ovrToolkit.HidingEnabled = false;
                ovrToolkit.ShowOVR();
                Thread.Sleep(200);
            }

            // K2VR
            K2VR k2Vr = _mainWindow.K2VR;
            if (k2Vr.IsWatching && _mainWindow.Settings.K2VrActionSetting == Settings.ActionSetting.AutoHide)
            {
                k2Vr.HidingEnabled = false;
                k2Vr.ShowK2VR();
                Thread.Sleep(200);
            }
        }

        private void BtnHideAll_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();

            Thread.Sleep(200);

            // SlimeVR
            SlimeVR slimeVr = _mainWindow.SlimeVr;
            if (slimeVr.IsWatching && _mainWindow.Settings.SlimeVrActionSetting == Settings.ActionSetting.AutoHide)
            {
                slimeVr.HideWindows();
                slimeVr.HidingEnabled = true;
                Thread.Sleep(200);
            }

            // WMR
            WMRPortal wmrPortal = _mainWindow.WmrPortal;
            if (wmrPortal.IsWatching && _mainWindow.Settings.WmrPortalActionSetting == Settings.ActionSetting.AutoHide)
            {
                var hiddenDesktop = wmrPortal.HidePortal();
                if (hiddenDesktop != null)
                    wmrPortal.HiddenDesktop = hiddenDesktop;
                else
                    wmrPortal.IsHidden = false;
                wmrPortal.IsHidden = true;
                wmrPortal.HidingEnabled = true;
                Thread.Sleep(200);
            }

            // SteamVR
            SteamVR steamVr = _mainWindow.SteamVr;
            if (steamVr.IsWatching && _mainWindow.Settings.SteamVrActionSetting == Settings.ActionSetting.AutoHide)
            {
                steamVr.HideSteamVR();
                steamVr.HidingEnabled = true;
                Thread.Sleep(200);
            }

            // Oculus
            Oculus oculus = _mainWindow.Oculus;
            if (oculus.IsWatching && _mainWindow.Settings.OculusActionSetting == Settings.ActionSetting.AutoHide)
            {
                oculus.HideOculus();
                oculus.HidingEnabled = true;
                Thread.Sleep(200);
            }

            // OVRToolkit
            OVRToolkit ovrToolkit = _mainWindow.OvrToolkit;
            if (ovrToolkit.IsWatching && _mainWindow.Settings.OvrToolkitActionSetting == Settings.ActionSetting.AutoHide)
            {
                ovrToolkit.HideOVR();
                ovrToolkit.HidingEnabled = true;
                Thread.Sleep(200);
            }

            // K2VR
            K2VR k2Vr = _mainWindow.K2VR;
            if (k2Vr.IsWatching && _mainWindow.Settings.K2VrActionSetting == Settings.ActionSetting.AutoHide)
            {
                k2Vr.HideK2VR();
                k2Vr.HidingEnabled = true;
                Thread.Sleep(200);
            }
        }

        private void BtnSettings_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();
            _mainWindow.Show();
            _mainWindow.ShowInTaskbar = true;
        }

        private void BtnExit_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();
            Application.Current.Shutdown();
        }

        public void BtnCloseAll_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();

            // Show all windows first to avoid weird bugs
            BtnShowAll_OnClick(null, null);

            // SlimeVR
            SlimeVR slimeVr = _mainWindow.SlimeVr;
            if (slimeVr.Process != null)
                slimeVr.Process.Kill();

            // WMR
            WMRPortal wmrPortal = _mainWindow.WmrPortal;
            if (wmrPortal.Process != null)
                wmrPortal.Process.Kill();

            // SteamVR
            SteamVR steamVr = _mainWindow.SteamVr;
            if (steamVr.Process != null)
                steamVr.Process.Kill();

            // Oculus
            Oculus oculus = _mainWindow.Oculus;
            if (oculus.Process != null)
                oculus.Process.Kill();

            // OVRToolkit
            OVRToolkit ovrToolkit = _mainWindow.OvrToolkit;
            if (ovrToolkit.Process != null)
                ovrToolkit.Process.Kill();

            // K2VR
            K2VR k2Vr = _mainWindow.K2VR;
            if (k2Vr.Process != null)
                k2Vr.Process.Kill();
        }

        private void TrayMenu_OnDeactivated(object? sender, EventArgs e)
        {
            Hide();
        }
    }
}