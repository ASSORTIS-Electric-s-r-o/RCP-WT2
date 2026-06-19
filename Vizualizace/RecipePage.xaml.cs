using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.System;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class RecipePage : Page
    {
        // ==========================================
        // Data stránky
        // ==========================================

        private List<tabRECIPES.RecipeDetailRow> _vsechnyReceptury = new();
        private List<tabRECIPES.RecipeDetailRow> _hlavickyReceptur = new();

        private readonly List<tabRECIPES.RecipeDetailRow> _vybraneRecepturyProSlouceni = new();

        public ObservableCollection<GroupTile> GroupTiles { get; } = new();

        private int _vybranaSkupinaId = 0;

        // ==========================================
        // Konstruktor stránky
        // ==========================================
        public RecipePage()
        {
            InitializeComponent();

            DataContext = this;

            Loaded += RecipePage_Loaded;

            NactiData();
            NastavVychoziZobrazeni();

            AktualizujOperatora();
            AktualizujViditelnostTlacitek();
        }

        // ==========================================
        // Krátké zobrazení horního vysouvacího informačního panelu
        // ==========================================
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;

        private void ZobrazInfoStatus(string title, string message, InfoBarSeverity severity)
        {
            // Naplnění textů horního toast panelu.
            InfoToastTitle.Text = title;
            InfoToastMessage.Text = message;

            // Nastavení barevné SVG ikony podle typu hlášení.
            InfoToastIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                new Uri(VratInfoIkonu(severity)));

            // Nastavení pozadí a rámečku podle typu hlášení a aktuálního režimu aplikace.
            NastavInfoToastStyl(severity);

            // Krátké skrytí před opětovným zobrazením zajistí,
            // že se animace vysunutí spustí i při rychlém opakovaném hlášení.
            InfoToast.Visibility = Visibility.Collapsed;
            InfoToast.HorizontalAlignment = HorizontalAlignment.Center;
            InfoToast.Visibility = Visibility.Visible;

            _infoStatusTimer?.Stop();

            _infoStatusTimer = DispatcherQueue.CreateTimer();
            _infoStatusTimer.Interval = TimeSpan.FromSeconds(4);

            _infoStatusTimer.Tick += (_, _) =>
            {
                _infoStatusTimer?.Stop();
                InfoToast.Visibility = Visibility.Collapsed;
            };

            _infoStatusTimer.Start();
        }

        private static string VratInfoIkonu(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => "ms-appx:///Assets/MenuIcons/ic_fluent_send_28_color.svg",
                InfoBarSeverity.Warning => "ms-appx:///Assets/MenuIcons/ic_fluent_warning_28_color.svg",
                InfoBarSeverity.Error => "ms-appx:///Assets/MenuIcons/ic_fluent_dismiss_circle_28_color.svg",
                _ => "ms-appx:///Assets/MenuIcons/ic_fluent_alert_28_color.svg"
            };
        }

        private void NastavInfoToastStyl(InfoBarSeverity severity)
        {
            bool darkTheme = ActualTheme == ElementTheme.Dark;

            (byte a, byte r, byte g, byte b) border = severity switch
            {
                InfoBarSeverity.Success => darkTheme ? ((byte)255, (byte)100, (byte)221, (byte)23) : ((byte)255, (byte)16, (byte)124, (byte)16),
                InfoBarSeverity.Warning => darkTheme ? ((byte)255, (byte)255, (byte)193, (byte)7) : ((byte)255, (byte)169, (byte)109, (byte)0),
                InfoBarSeverity.Error => darkTheme ? ((byte)255, (byte)255, (byte)82, (byte)82) : ((byte)255, (byte)176, (byte)0, (byte)32),
                _ => darkTheme ? ((byte)255, (byte)79, (byte)195, (byte)247) : ((byte)255, (byte)0, (byte)95, (byte)184)
            };

            (byte a, byte r, byte g, byte b) background = severity switch
            {
                InfoBarSeverity.Success => darkTheme ? ((byte)240, (byte)24, (byte)44, (byte)24) : ((byte)255, (byte)233, (byte)246, (byte)233),
                InfoBarSeverity.Warning => darkTheme ? ((byte)240, (byte)56, (byte)45, (byte)14) : ((byte)255, (byte)255, (byte)244, (byte)214),
                InfoBarSeverity.Error => darkTheme ? ((byte)240, (byte)58, (byte)22, (byte)30) : ((byte)255, (byte)253, (byte)236, (byte)238),
                _ => darkTheme ? ((byte)240, (byte)16, (byte)36, (byte)56) : ((byte)255, (byte)236, (byte)244, (byte)252)
            };

            InfoToast.BorderBrush = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(border.a, border.r, border.g, border.b));

            InfoToast.Background = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(background.a, background.r, background.g, background.b));
        }

        // ==========================================
        // Automatické navázání dotykové klávesnice
        // ==========================================
        private void RecipePage_Loaded(object sender, RoutedEventArgs e)
        {
            DotykovaKlavesniceService.Pripoj(
                this,
                App.MainWindow,
                ZjistiRezimKlavesnice);
        }

        // ==========================================
        // Režim dotykové klávesnice
        // ==========================================
        private static VirtualKeyboard.KeyboardMode ZjistiRezimKlavesnice(Control control)
        {
            return control.Name switch
            {
                "TxtSearch" => VirtualKeyboard.KeyboardMode.Str,
                _ => VirtualKeyboard.KeyboardMode.Str
            };
        }

        // ==========================================
        // Přepočet velikosti dlaždic skupin
        // Vždy se zobrazují 3 karty vedle sebe přes celou dostupnou šířku
        // ==========================================
        private void GroupsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GroupTiles.Count == 0)
                return;

            double sirka = e.NewSize.Width;
            double vyska = e.NewSize.Height;

            if (sirka <= 0 || vyska <= 0)
                return;

            int pocetSloupcu = 3;
            int pocetRadku = (int)Math.Ceiling(GroupTiles.Count / (double)pocetSloupcu);

            if (pocetRadku < 1)
                pocetRadku = 1;

            double mezeraMeziKartami = 8;
            double rezervaGridViewItem = 10;

            double sirkaDlazdice =
                (sirka - (pocetSloupcu * rezervaGridViewItem) - ((pocetSloupcu - 1) * mezeraMeziKartami)) / pocetSloupcu;

            double vyskaDlazdice =
                (vyska - (pocetRadku * rezervaGridViewItem) - ((pocetRadku - 1) * mezeraMeziKartami)) / pocetRadku;

            if (sirkaDlazdice < 220)
                sirkaDlazdice = 220;

            if (vyskaDlazdice < 160)
                vyskaDlazdice = 160;

            foreach (GroupTile tile in GroupTiles)
            {
                tile.TileWidth = sirkaDlazdice;
                tile.TileHeight = vyskaDlazdice;
            }
        }

        // ==========================================
        // Načtení dat 
        // ==========================================
        private void NactiData()
        {
            var (recipes, _) = tabRECIPES.GetRecipesAll();
            _vsechnyReceptury = recipes ?? new List<tabRECIPES.RecipeDetailRow>();

            GroupTiles.Clear();

            var (groupsRows, _) = tabGROUPS.GetGroupsAll_FromTable();
            List<tabGROUPS.GroupRow> skupiny = groupsRows ?? new List<tabGROUPS.GroupRow>();

            double sirka = GroupsGrid?.ActualWidth ?? 0;
            double vyska = GroupsGrid?.ActualHeight ?? 0;

            int pocetSloupcu = 3;
            int pocetRadku = (int)Math.Ceiling(skupiny.Count / (double)pocetSloupcu);

            if (pocetRadku < 1)
                pocetRadku = 1;

            double tileWidth = sirka > 0
                ? (sirka - 30) / pocetSloupcu
                : 300;

            double tileHeight = vyska > 0
                ? (vyska - 30) / pocetRadku
                : 210;

            foreach (tabGROUPS.GroupRow skupina in skupiny.OrderBy(x => x.Name))
            {
                int pocet = _vsechnyReceptury
                    .Where(r => r.IDgrp == skupina.IDgrp)
                    .Select(r => r.IDrcp)
                    .Distinct()
                    .Count();

                GroupTiles.Add(new GroupTile
                {
                    IDgrp = skupina.IDgrp,
                    Name = skupina.Name ?? "",
                    ImagePath = skupina.MpImage ?? "",
                    PocetReceptu = pocet,
                    TileWidth = tileWidth,
                    TileHeight = tileHeight
                });
            }
        }

        private void NastavVychoziZobrazeni()
        {
            _vybraneRecepturyProSlouceni.Clear();
            BtnMergeRecipes.Visibility = Visibility.Collapsed;

            if (Settings.Param_ZakladActive)
            {
                ColMergeHeader.Visibility = Visibility.Collapsed;
            }

            if (Settings.Param_ZobrazSkupiny)
            {
                ZobrazSkupiny();
                return;
            }

            _vybranaSkupinaId = 0;

            _hlavickyReceptur = _vsechnyReceptury
                .GroupBy(r => new { r.IDrcp, r.RecipeName })
                .Select(g => g.First())
                .OrderBy(r => r.RecipeName)
                .ToList();

            TxtTableTitle.Text = "Všechny receptury";
            TxtPageDescription.Text = "Přehled všech receptur";
            TxtSearch.Text = "";

            PouzijFiltrVyhledavani();

            GroupsCard.Visibility = Visibility.Collapsed;
            RecipesCard.Visibility = Visibility.Visible;
            BtnBackToGroups.Visibility = Visibility.Collapsed;

            ZobrazInfoStatus(
                "Receptury",
                "Zobrazeny jsou všechny receptury.",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Zobrazení skupin / receptur
        // ==========================================
        private void ZobrazSkupiny()
        {
            GroupsCard.Visibility = Visibility.Visible;
            RecipesCard.Visibility = Visibility.Collapsed;

            BtnBackToGroups.Visibility = Visibility.Collapsed;
            BtnMergeRecipes.Visibility = Visibility.Collapsed;

            _vybranaSkupinaId = 0;
            RecipesTable.ItemsSource = null;
            TxtSearch.Text = "";

            TxtPageDescription.Text = "Přehled skupin receptur";

            ZobrazInfoStatus(
                "Receptury",
                "Vyberte skupinu receptur.",
                InfoBarSeverity.Success);
        }

        private void ZobrazReceptury()
        {
            GroupsCard.Visibility = Visibility.Collapsed;
            RecipesCard.Visibility = Visibility.Visible;

            BtnBackToGroups.Visibility = Settings.Param_ZobrazSkupiny
                ? Visibility.Visible
                : Visibility.Collapsed;

            TxtPageDescription.Text = "Přehled receptur ve vybrané skupině";

            ZobrazInfoStatus(
                "Receptury",
                "Vyberte recepturu nebo použijte vyhledávání.",
                InfoBarSeverity.Success);
        }

        private void GroupsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not GroupTile skupina)
                return;

            _vybranaSkupinaId = skupina.IDgrp;

            TxtTableTitle.Text = $"Receptury – {skupina.Name}";

            _hlavickyReceptur = _vsechnyReceptury
                .Where(r => r.IDgrp == skupina.IDgrp)
                .GroupBy(r => new { r.IDrcp, r.RecipeName })
                .Select(g => g.First())
                .OrderBy(r => r.RecipeName)
                .ToList();

            TxtSearch.Text = "";
            PouzijFiltrVyhledavani();
            ZobrazReceptury();

            AktualizujViditelnostTlacitek();
        }

        // ==========================================
        // Přihlášení / odhlášení
        // ==========================================

        private void UserPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (UserSession.IsLoggedIn)
            {
                UserSession.Logout();
                AktualizujOperatora();
                AktualizujViditelnostTlacitek();
                return;
            }

            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Closed += (_, _) =>
            {
                AktualizujOperatora();
                AktualizujViditelnostTlacitek();
            };

            ModalWindowService.Otevri(loginWindow);
        }

        private void AktualizujOperatora()
        {
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                TxtLoginCaption.Text = "Odhlásit";
                TxtLoginUser.Text = UserSession.CurrentUser.Username;
                TxtLoginUser.Visibility = Visibility.Visible;
            }
            else
            {
                TxtLoginCaption.Text = "Přihlášení";
                TxtLoginUser.Text = "";
                TxtLoginUser.Visibility = Visibility.Collapsed;
            }
        }

        private void AktualizujViditelnostTlacitek()
        {
            bool loginRequired = Settings.Param_LoginRequired;

            bool maPrava =
                !loginRequired ||
                (
                    UserSession.IsLoggedIn &&
                    UserSession.CurrentUser != null &&
                    (UserSession.CurrentUser.IDrole == 1 || UserSession.CurrentUser.IDrole == 2)
                );

            Visibility adminVisibility = maPrava
                ? Visibility.Visible
                : Visibility.Collapsed;

            BtnAddGroup.Visibility =
                maPrava && Settings.Param_ZobrazSkupiny
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BtnAddRecipe.Visibility = adminVisibility;
            BtnAddMaterial.Visibility = adminVisibility;

            BtnBackToGroups.Visibility =
                Settings.Param_ZobrazSkupiny && RecipesCard.Visibility == Visibility.Visible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // ==========================================
        // Levý panel
        // ==========================================

        private void BtnBackToGroups_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazSkupiny();
        }

        private void BtnMergeRecipes_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OtevriSlouceniReceptur();
        }

        private void BtnAddGroup_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OtevriEditaciSkupiny();
        }

        private void BtnAddRecipe_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OtevriEditaciReceptury();
        }

        private void BtnAddMaterial_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OtevriEditaciMaterialu();
        }

        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Zpet();
        }

        // ==========================================
        // Tabulka receptur
        // ==========================================
        private void RecipesTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipesTable.SelectedItem is not tabRECIPES.RecipeDetailRow receptura)
                return;

            ZobrazInfoStatus(
                "Vybraná receptura",
                $"{receptura.RecipeCislo} | {receptura.RecipeName}",
                InfoBarSeverity.Informational);
        }

        private void RecipesTable_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            OtevriDetailVybraneReceptury();
        }

        private void RecipesTable_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            OtevriDetailVybraneReceptury();
            e.Handled = true;
        }

        private void BtnOpenDetail_Click(object sender, RoutedEventArgs e)
        {
            OtevriDetailVybraneReceptury();
        }

        // ==========================================
        // Tabulka receptur - otevření receptury jedním dotykem / kliknutím
        // ==========================================
        private void RecipesTable_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not tabRECIPES.RecipeDetailRow receptura)
                return;

            if (receptura.IDrcp <= 0)
                return;

            OtevriDetailReceptury(receptura.IDrcp);
        }

        private void OtevriDetailVybraneReceptury()
        {
            if (RecipesTable.SelectedItem is not tabRECIPES.RecipeDetailRow receptura)
            {
                ZobrazInfoStatus(
                    "Upozornění",
                    "Nejprve vyberte recepturu.",
                    InfoBarSeverity.Warning);

                return;
            }

            if (receptura.IDrcp <= 0)
                return;

            OtevriDetailReceptury(receptura.IDrcp);
        }

        // ==========================================
        // Sloučení receptur - výběr receptury pomocí SVG ikony bez pozadí
        // ==========================================
        private void BtnSelectMergeRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not tabRECIPES.RecipeDetailRow receptura)
                return;

            tabRECIPES.RecipeDetailRow? existujici = _vybraneRecepturyProSlouceni
                .FirstOrDefault(x => x.IDrcp == receptura.IDrcp);

            if (existujici == null)
            {
                _vybraneRecepturyProSlouceni.Add(receptura);

                button.Content = VytvorIkonuTlacitka(
                    "ms-appx:///Assets/MenuIcons/ic_fluent_send_28_color.svg");
            }
            else
            {
                _vybraneRecepturyProSlouceni.Remove(existujici);

                button.Content = VytvorIkonuTlacitka(
                    "ms-appx:///Assets/MenuIcons/ic_fluent_link_28_color.svg");
            }

            BtnMergeRecipes.Visibility =
                _vybraneRecepturyProSlouceni.Count > 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ZobrazInfoStatus(
                "Sloučení receptur",
                $"Vybráno receptur: {_vybraneRecepturyProSlouceni.Count}",
                InfoBarSeverity.Informational);
        }

        // ==========================================
        // Pomocné vytvoření SVG ikony do tlačítka.
        // Používá se ve sloupci pro sloučení receptur.
        // ==========================================
        private static Image VytvorIkonuTlacitka(string cestaIkony)
        {
            return new Image
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform,
                Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                    new Uri(cestaIkony))
            };
        }

        private void OtevriSlouceniReceptur()
        {
            if (_vybraneRecepturyProSlouceni.Count < 2)
            {
                ZobrazInfoStatus(
                    "Sloučení receptur",
                    "Vyberte alespoň dvě receptury.",
                    InfoBarSeverity.Warning);

                return;
            }

            MegeRecipesWindow dialog =
                new MegeRecipesWindow(_vybraneRecepturyProSlouceni);

            ModalWindowService.Otevri(dialog);
        }

        // ==========================================
        // Vyhledávání
        // ==========================================
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecipesCard.Visibility != Visibility.Visible)
                return;

            PouzijFiltrVyhledavani();
        }

        private void BtnSearchReset_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            PouzijFiltrVyhledavani();
        }

        private void PouzijFiltrVyhledavani()
        {
            string text = (TxtSearch.Text ?? "")
                .Trim()
                .ToLower();

            List<tabRECIPES.RecipeDetailRow> vysledek =
                string.IsNullOrWhiteSpace(text)
                    ? _hlavickyReceptur.ToList()
                    : _hlavickyReceptur
                        .Where(r =>
                            (r.RecipeName ?? "").ToLower().Contains(text) ||
                            (r.RecipeCislo ?? "").ToLower().Contains(text))
                        .ToList();

            RecipesTable.ItemsSource = vysledek;

            if (vysledek.Count == 0)
                RecipesTable.SelectedItem = null;
        }


        // ==========================================
        // Obnovení
        // ==========================================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            NactiData();
            NastavVychoziZobrazeni();

            AktualizujOperatora();
            AktualizujViditelnostTlacitek();

            ZobrazInfoStatus(
                "Obnoveno",
                "Receptury byly znovu načteny z databáze.",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Připravené funkce pro další okna
        // ==========================================
        private void OtevriDetailReceptury(int idReceptury)
        {
            Frame?.Navigate(
                typeof(RecipeDetailPage),
                new RecipeDetailArgs
                {
                    Mode = RecipeDetailPage.EditMode.Recipes,
                    Id = idReceptury,
                    BatchIndex = 1
                });
        }

        // ==========================================
        // Otevření editace skupin
        // ==========================================
        private void OtevriEditaciSkupiny()
        {
            Frame?.Navigate(
                typeof(RecipePreSelectPage),
                new GroupMaterialEditArgs
                {
                    Mode = RecipePreSelectPage.EditMode.Groups,
                    Id = 0
                });
        }

        // ==========================================
        // Otevření editace receptur
        // ==========================================
        private void OtevriEditaciReceptury()
        {
            Frame?.Navigate(
                typeof(RecipePreSelectPage),
                new GroupMaterialEditArgs
                {
                    Mode = RecipePreSelectPage.EditMode.Recipes,
                    Id = _vybranaSkupinaId
                });
        }

        // ==========================================
        // Otevření editace materiálů
        // ==========================================
        private void OtevriEditaciMaterialu()
        {
            Frame?.Navigate(
                typeof(RecipePreSelectPage),
                new GroupMaterialEditArgs
                {
                    Mode = RecipePreSelectPage.EditMode.Materials,
                    Id = 0
                });
        }

        private void Zpet()
        {
            Frame?.Navigate(typeof(JobPage));
        }
    }

    // ==========================================
    // Datová třída dlaždice skupiny
    // ==========================================

    public class GroupTile : INotifyPropertyChanged
    {
        private double _tileWidth = 300;
        private double _tileHeight = 210;

        public int IDgrp { get; set; }

        public string Name { get; set; } = "";

        public string ImagePath { get; set; } = "";

        public int PocetReceptu { get; set; }

        public string PocetReceptuText => $"Receptur: {PocetReceptu}";

        public double TileWidth
        {
            get => _tileWidth;
            set
            {
                _tileWidth = value;
                OnPropertyChanged();
            }
        }

        public double TileHeight
        {
            get => _tileHeight;
            set
            {
                _tileHeight = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}