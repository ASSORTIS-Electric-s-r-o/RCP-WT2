using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class InitWindow : Window
    {
        private readonly AppWindow _appWindow;

        public InitWindow()
        {
            InitializeComponent();

            _appWindow = ZiskejAppWindow();

            NastavOkno();
            NastavCas();
        }

        public void SetStatus(string text)
        {
            TxtStatus.Text = string.IsNullOrWhiteSpace(text)
                ? ""
                : text;

            NastavCas();
        }

        public void SetDetail(string text)
        {
            TxtDetail.Text = string.IsNullOrWhiteSpace(text)
                ? ""
                : text;

            NastavCas();
        }

        private void NastavCas()
        {
            TxtCas.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void NastavOkno()
        {
            _appWindow.Resize(new Windows.Graphics.SizeInt32(520, 280));

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            VycentrujOkno();
        }

        private void VycentrujOkno()
        {
            DisplayArea displayArea = DisplayArea.GetFromWindowId(
                _appWindow.Id,
                DisplayAreaFallback.Primary);

            Windows.Graphics.RectInt32 plocha = displayArea.WorkArea;
            Windows.Graphics.SizeInt32 velikost = _appWindow.Size;

            int x = plocha.X + (plocha.Width - velikost.Width) / 2;
            int y = plocha.Y + (plocha.Height - velikost.Height) / 2;

            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        private AppWindow ZiskejAppWindow()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            return AppWindow.GetFromWindowId(windowId);
        }
    }
}