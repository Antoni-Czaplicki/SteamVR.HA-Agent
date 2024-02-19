using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Home_Assistant_Agent_for_SteamVR
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public string Version
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            PortBox.Text = Settings.Default.Port.ToString();
            LaunchMinimizedToggle.IsOn = Settings.Default.LaunchMinimized;
            EnableTrayToggle.IsOn = Settings.Default.EnableTray;
            ExitWithSteamVRToggle.IsOn = Settings.Default.ExitWithSteamVR;
            AlwaysOnTopToggle.IsOn = Settings.Default.AlwaysOnTop;
            EnableNotifyPluginToggle.IsOn = Settings.Default.EnableNotifyPlugin;
        }

        private void PortBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(SavePortButton);
                if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProv) invokeProv.Invoke();
                VisualStateManager.GoToState(SavePortButton, "Pressed", true);
            }
            else
            {
                VisualStateManager.GoToState(SavePortButton, "Normal", true);
                SavePortButton.Content = "Save";
            }
        }

        private void LaunchMinimized_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.LaunchMinimized = LaunchMinimizedToggle.IsOn;
            Settings.Default.Save();
        }

        private void EnableTray_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableTray = EnableTrayToggle.IsOn;
            Settings.Default.Save();
            (Application.Current as App)?.MWindow.SetTrayIconVisibility(Settings.Default.EnableTray);
        }

        private void AlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.AlwaysOnTop = AlwaysOnTopToggle.IsOn;
            Settings.Default.Save();
            (Application.Current as App)?.MWindow.SetIsAlwaysOnTop(Settings.Default.AlwaysOnTop);
        }

        private void ExitWithSteamVR_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.ExitWithSteamVR = ExitWithSteamVRToggle.IsOn;
            Settings.Default.Save();
        }

        private void EnableNotifyPlugin_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableNotifyPlugin = EnableNotifyPluginToggle.IsOn;
            Settings.Default.Save();
            if (EnableNotifyPluginToggle.IsOn)
            {
                if (Process.GetProcessesByName("SteamVR.NotifyPlugin").Count() > 0)
                {
                    Debug.WriteLine("SteamVR.NotifyPlugin is already running");
                }
                else
                {
                    var startInfo = new ProcessStartInfo("SteamVR.NotifyPlugin.exe");
                    startInfo.WorkingDirectory =
                        Windows.ApplicationModel.Package.Current.InstalledPath + "\\NotifyPlugin";
                    startInfo.UseShellExecute = true;
                    Process.Start(startInfo);
                }
            }
            else
            {
                if (Process.GetProcessesByName("SteamVR.NotifyPlugin").Count() > 0)
                {
                    foreach (var process in Process.GetProcessesByName("SteamVR.NotifyPlugin"))
                    {
                        process.Kill();
                    }
                }
            }
        }

        private void SavePortButton_Click(object sender, RoutedEventArgs e)
        {
            var oldPort = Settings.Default.Port;
            Settings.Default.Port = int.Parse(PortBox.Text);
            Settings.Default.Save();
            (Application.Current as App)?.MWindow.SetWebsocketPort(Settings.Default.Port, oldPort);
            SavePortButton.Content = "Saved!";
        }
    }
}