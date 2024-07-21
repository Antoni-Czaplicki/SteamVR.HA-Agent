using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace Home_Assistant_Agent_for_SteamVR
{
    public enum SaveManifestResult
    {
        Success,
        SteamVRNotRunning,
        FailedToRegister,
        FailedToSave,
    }

    public sealed partial class SettingsPage : Page
    {
        private bool _isInitializing;
        private bool _ignoreAutoStartToggleThisTime;

        public string Version
        {
            get
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;

                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }

        public SettingsPage()
        {
            InitializeComponent();

            _isInitializing = true;

            PortBox.Text = AppSettings.Port.ToString();
            LaunchMinimizedToggle.IsOn = AppSettings.LaunchMinimized;
            EnableTrayToggle.IsOn = AppSettings.EnableTray;
            ExitWithSteamVRToggle.IsOn = AppSettings.ExitWithSteamVR;
            AlwaysOnTopToggle.IsOn = AppSettings.AlwaysOnTop;
            EnableNotifyPluginToggle.IsOn = AppSettings.EnableNotifyPlugin;

            if (AppSettings.ManifestFilePath != "")
            {
                AutoStartToggle.IsOn = true;
                AutoStartInfoBar.Title = "Manifest file path: " + AppSettings.ManifestFilePath;
                AutoStartInfoBar.Severity = InfoBarSeverity.Success;
                AutoStartInfoBar.IsOpen = true;
                SaveManifestButton.Content = "Change";
            }
            else
            {
                AutoStartToggle.IsOn = false;
                AutoStartInfoBar.Title = "Manifest file path is not set, auto start is disabled";
                AutoStartInfoBar.Severity = InfoBarSeverity.Warning;
                AutoStartInfoBar.IsOpen = true;
                ExportManifestCard.IsEnabled = false;
            }

            _isInitializing = false;
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
            if (_isInitializing) return;
            AppSettings.LaunchMinimized = LaunchMinimizedToggle.IsOn;
        }

        private void EnableTray_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.EnableTray = EnableTrayToggle.IsOn;
            (Application.Current as App)?.MWindow.SetTrayIconVisibility(AppSettings.EnableTray);
        }

        private void AlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.AlwaysOnTop = AlwaysOnTopToggle.IsOn;
            (Application.Current as App)?.MWindow.SetIsAlwaysOnTop(AppSettings.AlwaysOnTop);
        }

        private void ExitWithSteamVR_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.ExitWithSteamVR = ExitWithSteamVRToggle.IsOn;
        }

        private void EnableNotifyPlugin_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.EnableNotifyPlugin = EnableNotifyPluginToggle.IsOn;
            if (EnableNotifyPluginToggle.IsOn)
            {
                if (Process.GetProcessesByName("SteamVR.NotifyPlugin").Length > 0)
                {
                    Debug.WriteLine("SteamVR.NotifyPlugin is already running");
                }
                else
                {
                    var startInfo = new ProcessStartInfo("SteamVR.NotifyPlugin.exe");
                    startInfo.WorkingDirectory = Package.Current.InstalledPath + "\\NotifyPlugin";
                    startInfo.UseShellExecute = true;
                    Process.Start(startInfo);
                }
            }
            else
            {
                if (Process.GetProcessesByName("SteamVR.NotifyPlugin").Length > 0)
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
            var oldPort = AppSettings.Port;
            AppSettings.Port = int.Parse(PortBox.Text);
            (Application.Current as App)?.MWindow.SetWebsocketPort(AppSettings.Port, oldPort);
            SavePortButton.Content = "Saved!";
        }

        private async Task<SaveManifestResult> SaveManifest()
        {
            // Check if SteamVR is running
            if ((Application.Current as App)?.MWindow.IsSteamVRRunning == false)
            {
                return SaveManifestResult.SteamVRNotRunning;
            }

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            Window window = (Application.Current as App)?.MWindow;
            if (window == null)
            {
                return SaveManifestResult.FailedToSave;
            }

            nint windowHandle = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(savePicker, windowHandle);

            savePicker.FileTypeChoices.Add("VR manifest", new List<string>() { ".vrmanifest" });
            savePicker.SuggestedFileName = "steamvr_ha_agent.vrmanifest";

            var file = await savePicker.PickSaveFileAsync();

            if (file == null) return SaveManifestResult.FailedToSave;

            // Save the chosen file path to your settings
            AppSettings.ManifestFilePath = file.Path;

            // Write the manifest file
            var manifestFile = await Package.Current.InstalledLocation.GetFileAsync("app.vrmanifest");
            var manifestContent = await FileIO.ReadTextAsync(manifestFile);
            await FileIO.WriteTextAsync(file, manifestContent);

            // Update the UI
            AutoStartInfoBar.Title = "Manifest file path: " + AppSettings.ManifestFilePath;
            AutoStartInfoBar.Severity = InfoBarSeverity.Success;
            SaveManifestButton.Content = "Change";

            return (Application.Current as App)?.MWindow.RegisterManifest() != false
                ? SaveManifestResult.Success
                : SaveManifestResult.FailedToRegister;
        }

        private async void SaveManifestAndUpdateUI()
        {
            switch (await SaveManifest())
            {
                case SaveManifestResult.Success:
                    _ignoreAutoStartToggleThisTime = !AutoStartToggle.IsOn;
                    AutoStartToggle.IsOn = true;
                    AutoStartExpander.IsExpanded = true;
                    ExportManifestCard.IsEnabled = true;
                    AutoStartInfoBar.IsOpen = true;
                    AutoStartInfoBarActionButton.Visibility = Visibility.Collapsed;
                    break;
                case SaveManifestResult.SteamVRNotRunning:
                    _ignoreAutoStartToggleThisTime = !AutoStartToggle.IsOn;
                    AutoStartToggle.IsOn = true;
                    AutoStartExpander.IsExpanded = true;
                    ExportManifestCard.IsEnabled = false;
                    AutoStartInfoBar.Title = "Failed to set up auto start, SteamVR is not running";
                    AutoStartInfoBar.Severity = InfoBarSeverity.Error;
                    AutoStartInfoBar.IsOpen = true;
                    AutoStartInfoBarActionButton.Visibility = Visibility.Visible;
                    break;
                case SaveManifestResult.FailedToRegister:
                    _ignoreAutoStartToggleThisTime = !AutoStartToggle.IsOn;
                    AutoStartToggle.IsOn = true;
                    AutoStartExpander.IsExpanded = true;
                    ExportManifestCard.IsEnabled = false;
                    AutoStartInfoBar.Title =
                        "Failed to register the manifest, please try again or try to edit SteamVR's settings manually. The app will try to register the manifest again when it starts next time.";
                    AutoStartInfoBar.Severity = InfoBarSeverity.Error;
                    AutoStartInfoBar.IsOpen = true;
                    AutoStartInfoBarActionButton.Visibility = Visibility.Visible;
                    break;
                case SaveManifestResult.FailedToSave:
                    _ignoreAutoStartToggleThisTime = !AutoStartToggle.IsOn;
                    AutoStartToggle.IsOn = true;
                    AutoStartExpander.IsExpanded = true;
                    ExportManifestCard.IsEnabled = false;
                    AutoStartInfoBar.Title = "Failed to save the manifest file, please try again";
                    AutoStartInfoBar.Severity = InfoBarSeverity.Error;
                    AutoStartInfoBar.IsOpen = true;
                    AutoStartInfoBarActionButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private async Task DeleteManifest()
        {
            // Check if the manifest file path is set
            if (AppSettings.ManifestFilePath != "")
            {
                // Unregister the manifest
                (Application.Current as App)?.MWindow.UnregisterManifest(AppSettings.ManifestFilePath);

                // Delete the manifest file
                var file = await StorageFile.GetFileFromPathAsync(AppSettings.ManifestFilePath);
                await file.DeleteAsync();
                AppSettings.ManifestFilePath = "";
            }

            // Update the UI
            AutoStartInfoBar.Title = "Manifest file path is not set, auto start is disabled";
            AutoStartInfoBar.Severity = InfoBarSeverity.Warning;
            SaveManifestButton.Content = "Save";
            AutoStartInfoBarActionButton.Visibility = Visibility.Collapsed;
        }

        private async void SaveManifestButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.ManifestFilePath != "")
            {
                await DeleteManifest();
            }

            AutoStartInfoBar.Title = "Setting up auto start...";
            AutoStartInfoBar.Severity = InfoBarSeverity.Informational;
            AutoStartInfoBar.IsOpen = true;
            SaveManifestButton.Content = "Saving...";

            SaveManifestAndUpdateUI();
        }

        private async void AutoStart_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (_ignoreAutoStartToggleThisTime)
            {
                _ignoreAutoStartToggleThisTime = false;
                return;
            }

            AutoStartExpander.IsExpanded = AutoStartToggle.IsOn;
            ExportManifestCard.IsEnabled = AutoStartToggle.IsOn;
            if (AutoStartToggle.IsOn)
            {
                await DeleteManifest();

                AutoStartInfoBar.Title = "Setting up auto start...";
                AutoStartInfoBar.Severity = InfoBarSeverity.Informational;
                AutoStartInfoBar.IsOpen = true;
                SaveManifestButton.Content = "Saving...";


                SaveManifestAndUpdateUI();
            }
            else
            {
                await DeleteManifest();
            }
        }

        private async void AutoStartInfoBar_ActionButton_Click(object sender, RoutedEventArgs e)
        {
            await DeleteManifest();

            AutoStartInfoBar.Title = "Setting up auto start...";
            AutoStartInfoBar.Severity = InfoBarSeverity.Informational;
            AutoStartInfoBar.IsOpen = true;
            SaveManifestButton.Content = "Saving...";


            SaveManifestAndUpdateUI();
        }
    }
}