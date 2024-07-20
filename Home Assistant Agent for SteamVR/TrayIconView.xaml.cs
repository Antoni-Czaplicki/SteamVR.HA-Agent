using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace Home_Assistant_Agent_for_SteamVR
{
    [ObservableObject]
    public sealed partial class TrayIconView : UserControl
    {
        public TrayIconView()
        {
            InitializeComponent();

            TrayIcon.Visibility = AppSettings.EnableTray ? Visibility.Visible : Visibility.Collapsed;
        }
        
        public void SetTrayIconVisibility(bool visible)
        {
            TrayIcon.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
        
        public void Dispose()
        {
            TrayIcon.Dispose();
        }

        [RelayCommand]
        private static void ShowWindow()
        {
            var window = ((App)Application.Current).MWindow;
            if (window == null)
            {
                return;
            }

            if (!window.Visible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.Restore();
            }

            window.BringToFront();
        }

        [RelayCommand]
        private void ExitApplication()
        {
            TrayIcon.Dispose();
            ((App)Application.Current).MWindow?.Exit();
        }
    }
}