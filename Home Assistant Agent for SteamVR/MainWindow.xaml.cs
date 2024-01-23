using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Home_Assistant_Agent_for_SteamVR
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_AppWindow;
        private readonly MainController _controller;


        public MainWindow()
        {
            this.InitializeComponent();

            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();

            ContentFrame.Navigate(
                typeof(MainPage),
                null,
                new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo()
            );
            

            SystemBackdrop = new MicaBackdrop()
            { Kind = MicaKind.Base };
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.Title = GetAppTitleFromSystem();
            m_AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            // Controller
            _controller = new MainController(
                ((App)Application.Current).StatusViewModel,
                (session, sessionCount) => {
                    Debug.WriteLine($"MainController: Session: {sessionCount}");
                }, 
                (status) => { 
                    Debug.WriteLine($"MainController: Status: {status}");
                    ((App)Application.Current).StatusViewModel.SteamVRStatus = status;
                    if (status)
                    {
                    }
                    else
                    {
                        if (Settings.Default.ExitWithSteamVR)
                        {
                            _controller?.Shutdown();
                            Application.Current.Exit();
                        }
                    }
                }
                ); 
            _controller.Start();
            
            m_AppWindow.Closing += AppWindow_Closing;

        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
        
        private void AppWindow_Closing(object sender, AppWindowClosingEventArgs e)
        {
            Shutdown();
        }

        public string GetAppTitleFromSystem()
        {
            return Windows.ApplicationModel.Package.Current.DisplayName;
        }

        public void Shutdown()
        {
            _controller.Shutdown();
        }
        
        public void SetWebsocketPort(int port)
        {
            _controller.SetPort(port);
        }

        private void NavigationViewControl_SelectionChanged(NavigationView sender,
                      NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null && (args.SelectedItemContainer.Tag != null))
            {
                Type newPage = Type.GetType(args.SelectedItemContainer.Tag.ToString());
                ContentFrame.Navigate(
                       newPage,
                       null,
                       args.RecommendedNavigationTransitionInfo
                       );
            }
        }

        private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavigationViewControl.SelectedItem = (NavigationViewItem)NavigationViewControl.SettingsItem;
            }
            else if (ContentFrame.SourcePageType != null)
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems
                    .OfType<NavigationViewItem>()
                    .First(n => n.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));
            }

        }
    }
}
