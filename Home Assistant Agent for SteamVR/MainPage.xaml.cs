using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


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
            if (AppSettings.ManifestFilePath == "")
            {
                AutoStartInfoBar.IsOpen = true;
            }
            else
            {
                // check if the manifest file exists
                if (!System.IO.File.Exists(AppSettings.ManifestFilePath))
                {
                    AutoStartInfoBar.IsOpen = true;
                    AutoStartInfoBar.Content = "The manifest file does not exist. Please reconfigure the settings.";
                }
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}