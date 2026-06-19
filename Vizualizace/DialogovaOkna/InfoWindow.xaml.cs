using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using RCP_WT1.PomocneTridy;
using System;
using Windows.Graphics;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class InfoWindow : Window
    {
        public bool Potvrzeno { get; private set; }

        public InfoWindow(
            string title,
            string message,
            string buttonText = "OK")
        {
            InitializeComponent();

            Potvrzeno = false;

            TxtTitle.Text = title;
            TxtMessage.Text = message;
            BtnOk.Content = buttonText;

            NastavVelikostOknaPodleTextu(title, message);

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            VycentrujOkno();
        }

        private void NastavVelikostOknaPodleTextu(
            string title,
            string message)
        {
            int pocetRadku = message.Split('\n').Length;

            int zalomeni =
                Math.Max(
                    title.Length / 40,
                    message.Length / 55);

            int vyskaTextu =
                (pocetRadku + zalomeni) * 24;

            int sirka =
                title.Length > 35 || message.Length > 90
                    ? 620
                    : 520;

            int vyska =
                210 + vyskaTextu;

            sirka = Math.Clamp(sirka, 460, 650);
            vyska = Math.Clamp(vyska, 240, 520);

            AppWindow.Resize(
                new SizeInt32(
                    sirka,
                    vyska));
        }

        private void VycentrujOkno()
        {
            DisplayArea displayArea =
                DisplayArea.GetFromWindowId(
                    AppWindow.Id,
                    DisplayAreaFallback.Primary);

            RectInt32 area = displayArea.WorkArea;
            SizeInt32 size = AppWindow.Size;

            int x =
                area.X +
                (area.Width - size.Width) / 2;

            int y =
                area.Y +
                (area.Height - size.Height) / 2;

            AppWindow.Move(
                new PointInt32(x, y));
        }

        private void BtnOk_Click(
            object sender,
            RoutedEventArgs e)
        {
            Potvrzeno = true;
            ModalWindowService.Zavri(this);
        }
    }
}