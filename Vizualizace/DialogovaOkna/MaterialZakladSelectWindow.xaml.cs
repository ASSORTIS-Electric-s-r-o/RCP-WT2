using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.Klavesnice;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.Graphics;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class MaterialZakladSelectWindow : Window
    {
        private readonly List<MaterialZakladSelectRow> _vsechnyPolozky = new();

        public ObservableCollection<MaterialZakladSelectRow> ZobrazenePolozky { get; } = new();

        public List<MaterialZakladSelectRow> VybranePolozky =>
            _vsechnyPolozky
                .Where(x => x.JeVybrano)
                .ToList();

        public bool Potvrzeno { get; private set; } = false;

        public MaterialZakladSelectWindow(
            string titulek,
            IEnumerable<MaterialZakladSelectRow> polozky)
        {
            InitializeComponent();

            TxtTitle.Text = string.IsNullOrWhiteSpace(titulek)
                ? "Výběr položek"
                : titulek;

            _vsechnyPolozky = polozky?.ToList() ?? new List<MaterialZakladSelectRow>();

            TableItems.ItemsSource = ZobrazenePolozky;

            NaplnTabulku(_vsechnyPolozky);
            AktualizujVyber();
            AktualizujPlaceholder();

            NastavOkno();
        }

        private void NastavOkno()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            AppWindow.Resize(new SizeInt32(820, 560));
            VycentrujNaHlavniOkno();

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }

        private void VycentrujNaHlavniOkno()
        {
            if (App.MainWindow == null)
                return;

            nint hwndMain = WindowNative.GetWindowHandle(App.MainWindow);
            WindowId mainWindowId = Win32Interop.GetWindowIdFromWindow(hwndMain);
            AppWindow mainWindow = AppWindow.GetFromWindowId(mainWindowId);

            SizeInt32 mainSize = mainWindow.Size;
            PointInt32 mainPosition = mainWindow.Position;
            SizeInt32 dialogSize = AppWindow.Size;

            int x = mainPosition.X + ((mainSize.Width - dialogSize.Width) / 2);
            int y = mainPosition.Y + ((mainSize.Height - dialogSize.Height) / 2);

            AppWindow.Move(new PointInt32(x, y));
        }

        private void NaplnTabulku(IEnumerable<MaterialZakladSelectRow> polozky)
        {
            ZobrazenePolozky.Clear();

            foreach (MaterialZakladSelectRow polozka in polozky)
                ZobrazenePolozky.Add(polozka);
        }

        private void TableItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not MaterialZakladSelectRow row)
                return;

            PrepnoutVyber(row);
        }

        private void PrepnoutVyber(MaterialZakladSelectRow row)
        {
            row.JeVybrano = !row.JeVybrano;
            AktualizujVyber();
        }

        private void AktualizujVyber()
        {
            int pocet = _vsechnyPolozky.Count(x => x.JeVybrano);

            TxtVybrano.Text = $"Vybráno: {pocet}";
            BtnPokracovat.IsEnabled = pocet > 0;
        }

        private void Search_Tapped(object sender, TappedRoutedEventArgs e)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Str,
                SearchBox.Text ?? "",
                "Zadejte hledaný text");

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                SearchBox.Text = keyboard.Vysledek ?? "";

                AktualizujPlaceholder();
                PouzijFiltr();
            };

            ModalWindowService.Otevri(keyboard);
        }

        private void SearchReset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";

            AktualizujPlaceholder();
            NaplnTabulku(_vsechnyPolozky);
        }

        private void AktualizujPlaceholder()
        {
            SearchPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(SearchBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void PouzijFiltr()
        {
            string hledat = (SearchBox.Text ?? "")
                .Trim()
                .ToLower();

            if (string.IsNullOrWhiteSpace(hledat))
            {
                NaplnTabulku(_vsechnyPolozky);
                return;
            }

            List<MaterialZakladSelectRow> vysledek = _vsechnyPolozky
                .Where(x =>
                    (x.Kod ?? "").ToLower().Contains(hledat) ||
                    (x.Name ?? "").ToLower().Contains(hledat))
                .ToList();

            NaplnTabulku(vysledek);
        }

        private void BtnPokracovat_Click(object sender, RoutedEventArgs e)
        {
            if (!VybranePolozky.Any())
                return;

            Potvrzeno = true;
            Close();
        }

        private void BtnZavrit_Click(object sender, RoutedEventArgs e)
        {
            Potvrzeno = false;
            Close();
        }
    }

    public sealed class MaterialZakladSelectRow : INotifyPropertyChanged
    {
        private bool _jeVybrano;

        public int IDjob { get; set; } = 0;
        public int Status { get; set; } = 0;
        public int ID { get; set; }
        public string Kod { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsZaklad { get; set; }

        public bool JeVybrano
        {
            get => _jeVybrano;
            set
            {
                if (_jeVybrano == value)
                    return;

                _jeVybrano = value;

                OnPropertyChanged(nameof(JeVybrano));
                OnPropertyChanged(nameof(IkonaVyberu));
            }
        }

        public string IkonaVyberu => JeVybrano ? "✓" : "+";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}