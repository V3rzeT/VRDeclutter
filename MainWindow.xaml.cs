using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Hardcodet.Wpf.TaskbarNotification;
using VRDeclutter.Views;
using WindowsDesktop;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace VRDeclutter
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool _createdNew;

        // Configurable Settings FilePath
        private readonly string _settingsFilePath = "settings.ini";
        private Mutex _mutex = new(true, "VROrganizer", out _createdNew);

        private TaskbarIcon _tbIcon;

        private bool AllowDragging = true;

        // Supported Apps
        public K2VR K2VR;
        public Oculus Oculus;
        public OVRToolkit OvrToolkit;
        public Settings Settings;
        public SlimeVR SlimeVr;
        public SteamVR SteamVr;

        public TrayMenu TrayMenu;
        public WMRPortal WmrPortal;

        public MainWindow()
        {
            // Mutex check
            if (!_createdNew)
            {
                MessageBox.Show("VROrganizer is already running", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Window_Closing(null, null);
            }
            else
            {
                InitializeComponent();
            }
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && AllowDragging)
                DragMove();
        }

        public void DockToTray()
        {
            Top = Screen.PrimaryScreen.WorkingArea.Height - Height;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width;
            AllowDragging = false;
        }

        public void UndockFromTray()
        {
            Top = Screen.PrimaryScreen.Bounds.Height / 2 - Height / 2;
            Left = Screen.PrimaryScreen.Bounds.Width / 2 - Width / 2;
            AllowDragging = true;
        }

        private void MainWindow_OnActivated(object? sender, EventArgs e)
        {
            DropShadowEffect shadowEffect = (DropShadowEffect)WindowBorder.Effect;

            WindowBorder.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                {
                    new((Color)ColorConverter.ConvertFromString("#212935"), 0),
                    new((Color)ColorConverter.ConvertFromString("#2A2E33"), 0.1)
                }
            };
            WindowTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D5D9EA"));
            shadowEffect.Opacity = 0.4;
        }

        private void MainWindow_OnDeactivated(object? sender, EventArgs e)
        {
            DropShadowEffect shadowEffect = (DropShadowEffect)WindowBorder.Effect;

            WindowBorder.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                {
                    new((Color)ColorConverter.ConvertFromString("#23272C"), -0.1),
                    new((Color)ColorConverter.ConvertFromString("#2A2E33"), 0.15)
                }
            };
            WindowTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#768594"));
            shadowEffect.Opacity = 0.2;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Set up tray icon
            _tbIcon = new TaskbarIcon
                { Name = "VROrganizer", ToolTipText = "VROrganizer", Icon = Properties.Resources.indexReady };

            // Hide the window on startup
            ShowInTaskbar = false;
            Hide();

            // Set up Context Menu
            TrayMenu = new TrayMenu(this); // Init
            _tbIcon.TrayRightMouseUp += delegate
            {
                if (!TrayMenu.IsLoaded)
                    TrayMenu.ForceLoad();

                TrayMenu.SetCenter();
                TrayMenu.Show();
                TrayMenu.Activate();
            };
            _tbIcon.TrayMouseDoubleClick += delegate
            {
                Show();
                ShowInTaskbar = true;
            };


            // Add elements to ComboBoxes
            ComboBox[] comboBoxes =
                { SteamVRComboBox, WMRComboBox, OculusComboBox, SlimeVRComboBox, K2VRComboBox, OVRToolkitComboBox };

            foreach (var comboBox in comboBoxes)
            {
                comboBox.Items.Add("No Action");
                comboBox.Items.Add("Auto Hide");
                comboBox.Items.Add("Hide on Minimize");
            }

            // Load settings
            if (File.Exists(_settingsFilePath))
            {
                if (Settings.ReadSettings(_settingsFilePath, out Settings settings)) // Read settings
                {
                    Settings = settings;

                    // Set dock option
                    if (Settings.DockToTray)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual;
                        DockToTray();
                    }
                    else
                    {
                        UndockFromTray();
                    }
                }
                else
                {
                    MessageBox.Show("Corrupted Settings File!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                Settings = new Settings();
                if (!Settings.WriteSettings(_settingsFilePath, Settings))
                {
                    MessageBox.Show("Couldn't Write Settings!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }

            // Set up Watchers
            SlimeVr = new SlimeVR(this);
            WmrPortal = new WMRPortal(VirtualDesktop.Current, this);
            SteamVr = new SteamVR(this);
            Oculus = new Oculus(this);
            OvrToolkit = new OVRToolkit(this);
            K2VR = new K2VR(this);

            // Update other Setting stuff
            UpdateUIFromSettings();
            UpdateStartup();
        }

        private void UpdateSettings()
        {
            // Set General Settings
            Settings.RunOnStartup = StartupChkBox.IsChecked ?? false;
            Settings.DockToTray = DockChkBox.IsChecked ?? false;
            Settings.CloseAllWithSteamVR = SteamVRClsChkBox.IsChecked ?? false;

            // Set ComboBox Selections
            Settings.SteamVrActionSetting = (Settings.ActionSetting)SteamVRComboBox.SelectedIndex;
            Settings.WmrPortalActionSetting = (Settings.ActionSetting)WMRComboBox.SelectedIndex;
            Settings.OculusActionSetting = (Settings.ActionSetting)OculusComboBox.SelectedIndex;
            Settings.SlimeVrActionSetting = (Settings.ActionSetting)SlimeVRComboBox.SelectedIndex;
            Settings.K2VrActionSetting = (Settings.ActionSetting)K2VRComboBox.SelectedIndex;
            Settings.OvrToolkitActionSetting = (Settings.ActionSetting)OVRToolkitComboBox.SelectedIndex;

            // Write Settings
            Settings.WriteSettings(_settingsFilePath, Settings);

            // Set startup option
            UpdateStartup();

            // Set dock option
            if (Settings.DockToTray)
                DockToTray();
            else
                UndockFromTray();
        }

        private void UpdateStartup()
        {
            if (Settings.RunOnStartup)
            {
                if (!Registry.SetStartup())
                    MessageBox.Show("Couldn't create Registry Key for Program run on Startup","Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            else
            {
                if (!Registry.RemoveStartup())
                    MessageBox.Show("Couldn't remove Registry Key for Program run on Startup", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUIFromSettings()
        {
            // Load General Settings
            StartupChkBox.IsChecked = Settings.RunOnStartup;
            DockChkBox.IsChecked = Settings.DockToTray;
            SteamVRClsChkBox.IsChecked = Settings.CloseAllWithSteamVR;

            // Load ComboBox Selections
            SteamVRComboBox.SelectedIndex = (int)Settings.SteamVrActionSetting;
            WMRComboBox.SelectedIndex = (int)Settings.WmrPortalActionSetting;
            OculusComboBox.SelectedIndex = (int)Settings.OculusActionSetting;
            SlimeVRComboBox.SelectedIndex = (int)Settings.SlimeVrActionSetting;
            K2VRComboBox.SelectedIndex = (int)Settings.K2VrActionSetting;
            OVRToolkitComboBox.SelectedIndex = (int)Settings.OvrToolkitActionSetting;
        }

        private void OkBtn_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = false;
            Hide();
            UpdateSettings(); // Write to settings
        }

        private void CancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = false;
            Hide();
            UpdateUIFromSettings(); // Revert changes if any
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if(_tbIcon != null)
                _tbIcon.Visibility = Visibility.Hidden; // Properly hides tray icon on exit
            
            Process.GetCurrentProcess()
                .Kill(); // Force close again (something isn't letting it close sometimes ;-;)
        }
    }
}