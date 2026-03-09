using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.ApplicationModel;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Home_Assistant_Agent_for_SteamVR
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            DataContext = (Application.Current as App)?.StatusViewModel;
            InitializeAutoStartInfoBar();
        }

        private async void InitializeAutoStartInfoBar()
        {
            var isStartWithWindowsEnabled = false;

            try
            {
                var startupTask = await StartupTask.GetAsync("HASteamvrAgentStartup");
                isStartWithWindowsEnabled = startupTask.State == StartupTaskState.Enabled ||
                                            startupTask.State == StartupTaskState.EnabledByPolicy;
            }
            catch (Exception)
            {
            }

            if (string.IsNullOrWhiteSpace(AppSettings.ManifestFilePath))
            {
                AutoStartInfoBar.IsOpen = !isStartWithWindowsEnabled;
                return;
            }

            if (!File.Exists(AppSettings.ManifestFilePath))
            {
                AutoStartInfoBar.IsOpen = true;
                AutoStartInfoBar.Message = "The manifest file does not exist. Please reconfigure the settings.";
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}