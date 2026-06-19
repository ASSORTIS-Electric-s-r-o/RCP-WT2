using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        public static Frame? AppFrame { get; private set; }

        public string? CurrentPageName { get; private set; }

        public MainWindow(bool otevritNastaveni = false)
        {
            InitializeComponent();

            Instance = this;
            AppFrame = MainFrame;

            NastavCeloobrazovkovyRezim();

            MainFrame.Navigated += MainFrame_Navigated;
            MainFrame.NavigationFailed += MainFrame_NavigationFailed;

            if (otevritNastaveni)
                OpenSettingsPage();
            else
                MainFrame.Navigate(typeof(JobPage));
        }

        public void OpenSettingsPage()
        {
            MainFrame.Navigate(typeof(SettingPage));
        }

        private void NastavCeloobrazovkovyRezim()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
            }

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            CurrentPageName = e.SourcePageType?.Name;
        }

        private void MainFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception(
                $"Nepodařilo se načíst stránku: {e.SourcePageType.FullName}");
        }
    }
}