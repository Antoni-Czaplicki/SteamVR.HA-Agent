using Microsoft.UI.Xaml;
using WinUIEx;
using System;
using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;
using Sentry;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Home_Assistant_Agent_for_SteamVR
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public MainWindow MWindow;
        public readonly StatusViewModel StatusViewModel = new StatusViewModel();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            SentrySdk.Init(options =>
            {
                // A Sentry Data Source Name (DSN) is required.
                // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
                // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
                options.Dsn =
                    "https://156fc5390520df6bdcc809c72fdf8f0b@o4506773109669888.ingest.sentry.io/4506773120483328";

                // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
                // This might be helpful, or might interfere with the normal operation of your application.
                // We enable it here for demonstration purposes when first trying Sentry.
                // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
                options.Debug = false;

                // This option is recommended. It enables Sentry's "Release Health" feature.
                options.AutoSessionTracking = true;

                // This option will enable Sentry's tracing features. You still need to start transactions and spans.
                options.EnableTracing = true;
            });
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        // App.xaml.cs
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var mainInstance = AppInstance.FindOrRegisterForKey("haasvr");
            if (!mainInstance.IsCurrent)
            {
                var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                await mainInstance.RedirectActivationToAsync(activatedEventArgs);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            AppInstance.GetCurrent().Activated += AppActivated;

            MWindow = new MainWindow();
            MWindow.Activate();
            MWindow.SetWindowSize(680, 480);
            MWindow.CenterOnScreen();
            var manager = WindowManager.Get(MWindow);
            manager.MinWidth = 560;
            manager.MinHeight = 360;
            if (Settings.Default.AlwaysOnTop)
            {
                MWindow.SetIsAlwaysOnTop(true);
            }

            if (!Settings.Default.LaunchMinimized) return;
            if (Settings.Default.EnableTray)
            {
                MWindow.Hide();
            }
            else
            {
                MWindow.Minimize();
            }
        }

        private void AppActivated(object sender, AppActivationArguments args)
        {
            if (args.Kind == ExtendedActivationKind.Protocol)
            {
                var eventArgs = args.Data as ProtocolActivatedEventArgs;
                if (eventArgs != null)
                {
                    var uri = eventArgs.Uri;
                    if (uri != null)
                    {
                        var query = uri.Query;
                        if (query.Contains("launch_type=steamvr"))
                        {
                            return;
                        }
                    }
                }
            }

            if (MWindow.WindowState == WindowState.Minimized)
            {
                MWindow.DispatcherQueue.TryEnqueue(() => MWindow.Restore());
            }

            MWindow.BringToFront();
        }
    }
}