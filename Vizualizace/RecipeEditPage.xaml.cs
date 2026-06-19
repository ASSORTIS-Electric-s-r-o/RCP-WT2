using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.System;
using Windows.Foundation;


namespace RCP_WT1.Vizualizace
{
    public sealed partial class RecipeEditPage : Page
    {
        // ==========================================
        // Data stránky
        // ==========================================

        private int? _idRcp;
        private tabRECIPES.RecipeRow? _selectedRecipe;

        private List<tabGROUPS.GroupRow> _allGroups = new();
        private List<tabRECIPES_MAT.RecipeMaterialViewRow> _recipeMaterialRows = new();
        private bool _nacitamSkupiny = false;

        // Slovník uchovává běžící animace posuvných textů v levém panelu.
        // Díky tomu lze starou animaci bezpečně zastavit před spuštěním nové,
        // například po změně názvu receptury, skupiny nebo PDF souboru.
        private readonly Dictionary<TextBlock, Storyboard> _posuvneTextyAnimace = new();

        // Pevná šířka hodnot v levém panelu. Stejná hodnota je použita i v XAML
        // u ořezového Gridu jednotlivých posuvných TextBlocků.
        private const double SirkaTextuLevehoPanelu = 135;

        private string JednotkaText => Settings.Param_Units == 1 ? "ks" : "kg";

        // ==========================================
        // Konstruktor stránky
        // ==========================================
        public RecipeEditPage()
        {
            InitializeComponent();

            Loaded += RecipePreSelect_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            int idrcp = 0;

            if (e.Parameter is int id)
                idrcp = id;

            InicializujStranku(idrcp);
        }


        // ==========================================
        // Inicializace stránky
        // ==========================================
        private void InicializujStranku(int? idrcp)
        {
            _idRcp = idrcp;

            LoadGroups();

            if (_idRcp.HasValue && _idRcp.Value > 0)
            {
                LoadRecipe(_idRcp.Value);
                LoadRecipeMaterials(_idRcp.Value);
            }
            else
            {
                _selectedRecipe = new tabRECIPES.RecipeRow
                {
                    IDjob = 0,
                    IDrcp = 0,
                    Name = "Nový recept",
                    Cislo = "",
                    AmountPcs = 0,
                    IDgrp = _allGroups.FirstOrDefault()?.IDgrp ?? 0,
                    IsDeleted = 0,
                    RecipeName = "",
                    PdfProcedurePath = ""
                };

                RefreshRecipeTable();
            }

            BtnMoveUp.Visibility = Visibility.Collapsed;
            BtnMoveDown.Visibility = Visibility.Collapsed;
            BtnDelete.Visibility = Visibility.Collapsed;

            NastavViditelnostZakladu();
            ObnovLevyPanel();
            AktualizujOperatora();
            AktualizujTlacitka();
        }

        // ==========================================
        // Automatické navázání dotykové klávesnice
        // ==========================================
        private void RecipePreSelect_Loaded(object sender, RoutedEventArgs e)
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
            return VirtualKeyboard.KeyboardMode.Str;
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
                return;
            }

            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Closed += (_, _) =>
            {
                AktualizujOperatora();
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
        // Načtení skupin receptur
        // ==========================================
        private void LoadGroups()
        {
            _nacitamSkupiny = true;

            var (groups, _) = tabGROUPS.GetGroupsAll();

            _allGroups = groups ?? new List<tabGROUPS.GroupRow>();

            CmbRecipeGroup.ItemsSource = null;
            CmbRecipeGroup.DisplayMemberPath = "Name";
            CmbRecipeGroup.SelectedValuePath = "IDgrp";
            CmbRecipeGroup.ItemsSource = _allGroups;

            _nacitamSkupiny = false;
        }

        // ==========================================
        // Změna vybrané skupiny receptury
        // ==========================================
        private void CmbRecipeGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_nacitamSkupiny)
                return;

            if (_selectedRecipe == null)
                return;

            if (CmbRecipeGroup.SelectedValue is not int idgrp)
                return;

            _selectedRecipe.IDgrp = idgrp;
            _selectedRecipe.IsZaklad = tabGROUPS.GetIsZakladByGroupID(idgrp);

            AktualizujTextSkupiny();
            ObnovPosuvneTextyLevehoPanelu();
            NastavViditelnostZakladu();

            CmbRecipeGroup.IsHitTestVisible = false;
        }

        // ==========================================
        // Načtení receptury
        // ==========================================
        private void LoadRecipe(int idrcp)
        {
            var (recipes, _) = tabRECIPES.GetRecipeRows();

            _selectedRecipe = recipes
                .FirstOrDefault(r => r.IDrcp == idrcp);

            if (_selectedRecipe == null)
            {
                ZobrazInfoStatus(
                    "Receptura",
                    "Receptura nebyla nalezena.",
                    InfoBarSeverity.Error);
                return;
            }

            _idRcp = _selectedRecipe.IDrcp;
        }

        // ==========================================
        // Načtení materiálů receptury
        // ==========================================
        private void LoadRecipeMaterials(int idrcp)
        {
            var (rows, _) = tabRECIPES_MAT.GetByRecipe(idrcp);

            _recipeMaterialRows = rows ?? new List<tabRECIPES_MAT.RecipeMaterialViewRow>();

            RefreshRecipeTable();
        }

        // ==========================================
        // Obnovení levého panelu
        // ==========================================
        private void ObnovLevyPanel()
        {
            if (_selectedRecipe == null)
                return;

            string nazev = !string.IsNullOrWhiteSpace(_selectedRecipe.Name)
                ? _selectedRecipe.Name
                : _selectedRecipe.RecipeName ?? "";

            TxtRecipeName.Text = nazev;
            TxtRecipeCode.Text = _selectedRecipe.Cislo ?? "";
            TxtRecipeAmount.Text = $"{_selectedRecipe.AmountPcs:0.###} {JednotkaText}";

            if (_allGroups.Count > 0)
            {
                bool skupinaExistuje = _allGroups.Any(x => x.IDgrp == _selectedRecipe.IDgrp);

                if (!skupinaExistuje)
                    _selectedRecipe.IDgrp = _allGroups.First().IDgrp;

                _nacitamSkupiny = true;
                CmbRecipeGroup.SelectedValue = _selectedRecipe.IDgrp;
                _nacitamSkupiny = false;

                AktualizujTextSkupiny();
            }

            if (!string.IsNullOrWhiteSpace(_selectedRecipe.PdfProcedurePath))
            {
                string path = NormalizujCestu(_selectedRecipe.PdfProcedurePath);
                TxtRecipePdf.Text = Path.GetFileName(path);
            }
            else
            {
                TxtRecipePdf.Text = "Vyber PDF soubor";
            }

            TxtPageTitle.Text = _selectedRecipe.IDrcp > 0
                ? "Editace receptury"
                : "Nová receptura";

            TxtPageDescription.Text = nazev;

            // Po naplnění hodnot v levém panelu se znovu vyhodnotí délka textů.
            // Krátké texty zůstanou stát, dlouhé texty se začnou plynule posouvat.
            ObnovPosuvneTextyLevehoPanelu();
        }

        // ==========================================
        // Obnovení tabulky receptury
        // ==========================================
        private void RefreshRecipeTable()
        {
            Table.ItemsSource = null;

            Table.ItemsSource = _recipeMaterialRows
                .Select(m => new RecipeMaterialDisplayRow
                {
                    Source = m,
                    RowNoText = m.row_no.ToString(),
                    MaterialName = m.MaterialName ?? "",
                    DavkaText = m.Davka.ToString("0.###", CultureInfo.InvariantCulture),
                    ToleranceText = m.Tolerance.ToString("0.###", CultureInfo.InvariantCulture),
                    VazitText = m.Vazit == 1 ? "Ano" : "Ne"
                })
                .ToList();
        }

        // ==========================================
        // Výběr řádku
        // ==========================================
        private void Table_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool showButtons =
                Table.SelectedIndex >= 0 &&
                Table.SelectedIndex < _recipeMaterialRows.Count;

            BtnMoveUp.Visibility = showButtons ? Visibility.Visible : Visibility.Collapsed;
            BtnMoveDown.Visibility = showButtons ? Visibility.Visible : Visibility.Collapsed;
            BtnDelete.Visibility = showButtons ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Table_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
            {
                BtnDelete_Click(sender, e);
                e.Handled = true;
            }
        }

        // ==========================================
        // Editace hodnot řádku
        // ==========================================
        private void BtnEditDavka_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not RecipeMaterialDisplayRow displayRow)
                return;

            OtevriEditaciFloat(
                "Dávka",
                displayRow.Source.Davka,
                value =>
                {
                    displayRow.Source.Davka = value;
                    RefreshRecipeTable();
                });
        }

        private void BtnEditTolerance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not RecipeMaterialDisplayRow displayRow)
                return;

            OtevriEditaciFloat(
                "Tolerance",
                displayRow.Source.Tolerance,
                value =>
                {
                    displayRow.Source.Tolerance = value;
                    RefreshRecipeTable();
                });
        }

        private void BtnToggleVazit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not RecipeMaterialDisplayRow displayRow)
                return;

            displayRow.Source.Vazit = displayRow.Source.Vazit == 1 ? 0 : 1;

            RefreshRecipeTable();
        }

        // ==========================================
        // Editace hlavičky receptury
        // ==========================================
        private void AktualizujTextSkupiny()
        {
            if (_selectedRecipe == null)
                return;

            tabGROUPS.GroupRow? group = _allGroups
                .FirstOrDefault(x => x.IDgrp == _selectedRecipe.IDgrp);

            TxtRecipeGroup.Text = group?.Name ?? "";
        }

        // ==========================================
        // Posuvné texty v levém panelu
        // ==========================================
        private void ObnovPosuvneTextyLevehoPanelu()
        {
            // Měření TextBlocku je spolehlivé až po překreslení layoutu.
            // Proto se spuštění animací odloží do fronty UI vlákna.
            DispatcherQueue.TryEnqueue(() =>
            {
                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipeName,
                    TxtRecipeNameTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipeCode,
                    TxtRecipeCodeTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipeGroup,
                    TxtRecipeGroupTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipeAmount,
                    TxtRecipeAmountTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipePdf,
                    TxtRecipePdfTransform,
                    SirkaTextuLevehoPanelu);
            });
        }

        private void SpustPosunTextuPokudJeDlouhy(
            TextBlock textBlock,
            TranslateTransform transform,
            double dostupnaSirka)
        {
            // Při každém obnovení textu se nejdříve zastaví původní animace.
            // Zabrání se tím vrstvení více animací na stejném TextBlocku.
            if (_posuvneTextyAnimace.TryGetValue(textBlock, out Storyboard? puvodniAnimace))
            {
                puvodniAnimace.Stop();
                _posuvneTextyAnimace.Remove(textBlock);
            }

            transform.X = 0;

            if (string.IsNullOrWhiteSpace(textBlock.Text))
                return;

            // Text se změří bez omezení šířky, aby šlo zjistit,
            // zda se do pevné šířky levého panelu opravdu nevejde.
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double skutecnaSirkaTextu = textBlock.DesiredSize.Width;

            if (skutecnaSirkaTextu <= dostupnaSirka)
                return;

            // Malá rezerva na konci zajistí, že se při posunu zobrazí celý text
            // a poslední znak nebude přilepený přímo na hranu výřezu.
            double cilovyPosun = dostupnaSirka - skutecnaSirkaTextu - 18;

            Storyboard animace = new Storyboard
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            DoubleAnimation posunTextu = new DoubleAnimation
            {
                From = 0,
                To = cilovyPosun,
                BeginTime = TimeSpan.FromSeconds(1),
                Duration = new Duration(TimeSpan.FromSeconds(4))
            };

            Storyboard.SetTarget(posunTextu, transform);
            Storyboard.SetTargetProperty(posunTextu, "X");

            animace.Children.Add(posunTextu);
            _posuvneTextyAnimace[textBlock] = animace;

            animace.Begin();
        }

        private void BtnRecipeName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            OtevriEditaciTextu(
                "Název receptury",
                _selectedRecipe.Name ?? "",
                value =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    _selectedRecipe.Name = value;
                    ObnovLevyPanel();
                });
        }

        private void BtnRecipeCode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            OtevriEditaciTextu(
                "Kódové označení",
                _selectedRecipe.Cislo ?? "",
                value =>
                {
                    _selectedRecipe.Cislo = value ?? "";
                    ObnovLevyPanel();
                });
        }

        // ==========================================
        // Otevření výběru skupiny z celého řádku
        // ==========================================
        private void BtnRecipeGroup_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_allGroups.Count == 0)
                LoadGroups();

            CmbRecipeGroup.IsHitTestVisible = true;
            CmbRecipeGroup.Focus(FocusState.Programmatic);
            CmbRecipeGroup.IsDropDownOpen = true;
        }

        private void BtnRecipeAmount_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            OtevriEditaciCisla(
                "Velikost várky",
                _selectedRecipe.AmountPcs,
                value =>
                {
                    _selectedRecipe.AmountPcs = value;
                    ObnovLevyPanel();
                });
        }

        private void BtnRecipePdf_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            FileSelectDialog dialog = new FileSelectDialog(
                "Vyber technologický postup PDF:",
                _selectedRecipe.PdfProcedurePath ?? "",
                "Výběr PDF souboru",
                ".pdf");

            dialog.Closed += (_, _) =>
            {
                if (!dialog.Potvrzeno)
                    return;

                _selectedRecipe.PdfProcedurePath = dialog.SelectedPath ?? "";

                ObnovLevyPanel();
                AktualizujTlacitka();

                ZobrazInfoStatus(
                    "PDF",
                    string.IsNullOrWhiteSpace(_selectedRecipe.PdfProcedurePath)
                        ? "PDF soubor byl odebrán."
                        : "PDF soubor byl vybrán.",
                    InfoBarSeverity.Success);
            };

            ModalWindowService.Otevri(dialog);
        }

        // ==========================================
        // PDF
        // ==========================================

        private void BtnShowPdf_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            string path = NormalizujCestu(_selectedRecipe.PdfProcedurePath);

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ZobrazInfoStatus(
                    "PDF",
                    "PDF soubor nebyl nalezen.",
                    InfoBarSeverity.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                ZobrazInfoStatus(
                    "PDF",
                    "PDF soubor se nepodařilo otevřít.",
                    InfoBarSeverity.Error);
            }
        }

        // ==========================================
        // Režimy přidání položek
        // ==========================================

        private void BtnEditMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            var (materials, _) = tabMATERIAL.GetMaterialsAll();

            List<MaterialZakladSelectRow> polozky = materials
                .Select(m => new MaterialZakladSelectRow
                {
                    ID = m.IDmat,
                    Kod = m.Cislo ?? "",
                    Name = m.Name ?? "",
                    IsZaklad = false
                })
                .ToList();

            OtevriVyberMaterialuNeboZakladu(
                "Výběr surovin",
                polozky);
        }

        private void BtnEditZaklad_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipe == null)
                return;

            var (recipes, _) = tabRECIPES.GetRecipesIsZaklad();

            List<MaterialZakladSelectRow> polozky = recipes
                .Where(r => r.IDrcp != _selectedRecipe.IDrcp)
                .Select(r => new MaterialZakladSelectRow
                {
                    ID = r.IDrcp,
                    Kod = r.Cislo ?? "",
                    Name = r.Name ?? "",
                    IsZaklad = true
                })
                .ToList();

            OtevriVyberMaterialuNeboZakladu(
                "Výběr základů",
                polozky);
        }

        private void OtevriVyberMaterialuNeboZakladu(string titulek, List<MaterialZakladSelectRow> polozky)
        {
            if (polozky.Count == 0)
            {
                ZobrazInfoStatus(
                    titulek,
                    "Nejsou dostupné žádné položky pro výběr.",
                    InfoBarSeverity.Warning);
                return;
            }

            MaterialZakladSelectWindow dialog =
                new MaterialZakladSelectWindow(titulek, polozky);

            dialog.Closed += (_, _) =>
            {
                if (!dialog.Potvrzeno)
                    return;

                foreach (MaterialZakladSelectRow item in dialog.VybranePolozky)
                    PridejPolozkuDoReceptury(item);

                ZobrazInfoStatus(
                    titulek,
                    $"Přidáno položek: {dialog.VybranePolozky.Count}",
                    InfoBarSeverity.Success);
            };

            ModalWindowService.Otevri(dialog);
        }

        private void PridejPolozkuDoReceptury(MaterialZakladSelectRow item)
        {
            int idmat = item.IsZaklad ? 0 : item.ID;
            int idzaklad = item.IsZaklad ? item.ID : 0;

            bool uzExistuje = _recipeMaterialRows.Any(r =>
                r.IDmat == idmat &&
                r.IDzaklad == idzaklad);

            if (uzExistuje)
            {
                ZobrazInfoStatus(
                    "Receptura",
                    "Tato položka už je v receptu použita.",
                    InfoBarSeverity.Warning);
                return;
            }

            tabRECIPES_MAT.RecipeMaterialViewRow newRow;

            var (deleted, count) =
                tabRECIPES_MAT.GetDeletedRowByRecipeAndMaterial(_idRcp ?? 0, idmat, idzaklad);

            if (count > 0 && deleted != null)
            {
                newRow = deleted;
                newRow.IsDeleted = 0;
                newRow.MaterialName = item.Name;
            }
            else
            {
                newRow = new tabRECIPES_MAT.RecipeMaterialViewRow
                {
                    IDrcp = _idRcp ?? 0,
                    IDmat = idmat,
                    IDzaklad = idzaklad,
                    RecipeMatCislo = item.Kod,
                    MaterialName = item.Name,
                    Davka = 0,
                    Tolerance = 0.2f,
                    Vazit = 1,
                    IsDeleted = 0
                };
            }

            _recipeMaterialRows.Add(newRow);

            UpdateRowNumbers();
            RefreshRecipeTable();

            Table.SelectedIndex = _recipeMaterialRows.Count - 1;
        }

        // ==========================================
        // Řazení a mazání
        // ==========================================

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int index = Table.SelectedIndex;

            if (index > 0 && index < _recipeMaterialRows.Count)
            {
                tabRECIPES_MAT.RecipeMaterialViewRow item = _recipeMaterialRows[index];

                _recipeMaterialRows.RemoveAt(index);
                _recipeMaterialRows.Insert(index - 1, item);

                UpdateRowNumbers();
                RefreshRecipeTable();

                Table.SelectedIndex = index - 1;
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int index = Table.SelectedIndex;

            if (index >= 0 && index < _recipeMaterialRows.Count - 1)
            {
                tabRECIPES_MAT.RecipeMaterialViewRow item = _recipeMaterialRows[index];

                _recipeMaterialRows.RemoveAt(index);
                _recipeMaterialRows.Insert(index + 1, item);

                UpdateRowNumbers();
                RefreshRecipeTable();

                Table.SelectedIndex = index + 1;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            int index = Table.SelectedIndex;

            if (index >= 0 && index < _recipeMaterialRows.Count)
            {
                _recipeMaterialRows.RemoveAt(index);

                UpdateRowNumbers();
                RefreshRecipeTable();

                if (_recipeMaterialRows.Count > 0)
                    Table.SelectedIndex = Math.Min(index, _recipeMaterialRows.Count - 1);
            }
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < _recipeMaterialRows.Count; i++)
                _recipeMaterialRows[i].row_no = i + 1;
        }

        // ==========================================
        // Uložení receptury
        // ==========================================

        private void BtnSave_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UlozRecepturu();
        }

        private void UlozRecepturu()
        {
            try
            {
                if (_selectedRecipe == null)
                    return;

                _selectedRecipe.IsZaklad =
                    tabGROUPS.GetIsZakladByGroupID(_selectedRecipe.IDgrp);

                bool isNew = _selectedRecipe.IDrcp <= 0;

                if (isNew)
                {
                    int newId = tabRECIPES.InsertRecipe(_selectedRecipe);

                    if (newId <= 0)
                    {
                        ZobrazChybuUlozeni();
                        return;
                    }

                    _idRcp = newId;
                    _selectedRecipe.IDrcp = newId;
                }
                else
                {
                    if (!tabRECIPES.UpdateRecipe(_selectedRecipe))
                    {
                        ZobrazChybuUlozeni();
                        return;
                    }

                    _idRcp = _selectedRecipe.IDrcp;
                }

                if (!_idRcp.HasValue || _idRcp.Value <= 0)
                {
                    ZobrazChybuUlozeni();
                    return;
                }

                List<tabRECIPES_MAT.RecipeMaterialViewRow> dbRows =
                    tabRECIPES_MAT.GetByRecipe(_idRcp.Value).Item1;

                Dictionary<(int IDmat, int IDzaklad), tabRECIPES_MAT.RecipeMaterialViewRow> dbMap =
                    dbRows.ToDictionary(r => (r.IDmat, r.IDzaklad));

                List<tabRECIPES_MAT.RecipeMaterialViewRow> allRows =
                    tabRECIPES_MAT.GetAllByRecipe(_idRcp.Value).Item1;

                Dictionary<(int IDmat, int IDzaklad), tabRECIPES_MAT.RecipeMaterialViewRow> deletedMap =
                    allRows
                        .Where(r => r.IsDeleted == 1)
                        .ToDictionary(r => (r.IDmat, r.IDzaklad));

                foreach (tabRECIPES_MAT.RecipeMaterialViewRow row in _recipeMaterialRows)
                {
                    row.IDrcp = _idRcp.Value;

                    var key = (row.IDmat, row.IDzaklad);

                    if (dbMap.TryGetValue(key, out tabRECIPES_MAT.RecipeMaterialViewRow? dbRow))
                    {
                        dbRow.Davka = row.Davka;
                        dbRow.Tolerance = row.Tolerance;
                        dbRow.row_no = row.row_no;
                        dbRow.Vazit = row.Vazit;
                        dbRow.IsDeleted = 0;

                        tabRECIPES_MAT.UpdateMaterialRow(ToMatRow(dbRow));
                    }
                    else if (deletedMap.TryGetValue(key, out tabRECIPES_MAT.RecipeMaterialViewRow? deletedRow))
                    {
                        deletedRow.Davka = row.Davka;
                        deletedRow.Tolerance = row.Tolerance;
                        deletedRow.row_no = row.row_no;
                        deletedRow.Vazit = row.Vazit;
                        deletedRow.IsDeleted = 0;

                        tabRECIPES_MAT.UpdateMaterialRow(ToMatRow(deletedRow));
                    }
                    else
                    {
                        tabRECIPES_MAT.RecipeMaterialRow newRow = ToMatRow(row);
                        newRow.IsDeleted = 0;

                        tabRECIPES_MAT.InsertRecipeMaterial(newRow);
                    }
                }

                foreach (tabRECIPES_MAT.RecipeMaterialViewRow dbRow in dbRows)
                {
                    bool exists = _recipeMaterialRows.Any(v =>
                        v.IDmat == dbRow.IDmat &&
                        v.IDzaklad == dbRow.IDzaklad);

                    if (!exists)
                    {
                        dbRow.IsDeleted = 1;
                        tabRECIPES_MAT.UpdateMaterialRow(ToMatRow(dbRow));
                    }
                }

                ZobrazInfoStatus(
                    "Uloženo",
                    "Receptura a materiály byly uloženy.",
                    InfoBarSeverity.Success);

                AktualizujTlacitka();
                ObnovLevyPanel();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Chyba při ukládání receptury:");
                Debug.WriteLine(ex.ToString());

                ZobrazChybuUlozeni();
            }
        }

        private void ZobrazChybuUlozeni()
        {
            ZobrazInfoStatus(
                "Chyba",
                "Recepturu se nepodařilo uložit.",
                InfoBarSeverity.Error);
        }

        // ==========================================
        // Převod view řádku na DB řádek
        // ==========================================

        private tabRECIPES_MAT.RecipeMaterialRow ToMatRow(
            tabRECIPES_MAT.RecipeMaterialViewRow view)
        {
            return new tabRECIPES_MAT.RecipeMaterialRow
            {
                IDjob = 0,
                IDrcp = view.IDrcp,
                IDmat = view.IDzaklad > 0 ? 0 : view.IDmat,
                IDzaklad = view.IDzaklad > 0 ? view.IDzaklad : 0,
                Davka = view.Davka,
                Tolerance = view.Tolerance,
                row_no = view.row_no,
                Vazit = view.Vazit,
                IsDeleted = view.IsDeleted
            };
        }

        // ==========================================
        // Obnovení
        // ==========================================

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_idRcp.HasValue && _idRcp.Value > 0)
            {
                LoadRecipe(_idRcp.Value);
                LoadRecipeMaterials(_idRcp.Value);
            }

            ObnovLevyPanel();
            AktualizujTlacitka();

            ZobrazInfoStatus(
                "Obnoveno",
                "Receptura byla znovu načtena.",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Návrat
        // ==========================================

        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Zpet();
        }

        private void Zpet()
        {
            Frame?.Navigate(typeof(RecipePage));
        }

        // ==========================================
        // Viditelnost tlačítek
        // ==========================================

        private void AktualizujTlacitka()
        {
            bool pdfOk =
                !string.IsNullOrWhiteSpace(_selectedRecipe?.PdfProcedurePath) &&
                File.Exists(NormalizujCestu(_selectedRecipe.PdfProcedurePath));

            BtnShowPdf.Visibility = pdfOk
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void NastavViditelnostZakladu()
        {
            BtnEditZaklad.Visibility =
                Settings.Param_ZakladActive &&
                _selectedRecipe?.IsZaklad == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // ==========================================
        // Připravené editace přes klávesnici
        // ==========================================
        private void OtevriEditaciTextu(string titulek, string hodnota, Action<string> poPotvrzeni)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Str,
                hodnota ?? "");

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                poPotvrzeni.Invoke(keyboard.Vysledek ?? "");
            };

            ModalWindowService.Otevri(keyboard);
        }

        private void OtevriEditaciCisla(string titulek, float hodnota, Action<float> poPotvrzeni)
        {
            VirtualKeyboard.KeyboardMode mode =
                Settings.Param_Units == 1
                    ? VirtualKeyboard.KeyboardMode.Int
                    : VirtualKeyboard.KeyboardMode.Float;

            VirtualKeyboard keyboard = new VirtualKeyboard(
                mode,
                hodnota.ToString("0.###", CultureInfo.InvariantCulture));

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                string text = (keyboard.Vysledek ?? "")
                    .Replace(",", ".");

                if (!float.TryParse(
                        text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out float value))
                {
                    ZobrazInfoStatus(
                        titulek,
                        "Zadaná hodnota není platné číslo.",
                        InfoBarSeverity.Warning);
                    return;
                }

                poPotvrzeni.Invoke(value);
            };

            ModalWindowService.Otevri(keyboard);
        }

        private void OtevriEditaciFloat(string titulek, float hodnota, Action<float> poPotvrzeni)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Float,
                hodnota.ToString("0.###", CultureInfo.InvariantCulture));

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                string text = (keyboard.Vysledek ?? "")
                    .Replace(",", ".");

                if (!float.TryParse(
                        text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out float value))
                {
                    ZobrazInfoStatus(
                        titulek,
                        "Zadaná hodnota není platné číslo.",
                        InfoBarSeverity.Warning);
                    return;
                }

                poPotvrzeni.Invoke(value);
            };

            ModalWindowService.Otevri(keyboard);
        }

        // ==========================================
        // Pomocné funkce
        // ==========================================

        private static string NormalizujCestu(string? path)
        {
            return (path ?? "").Replace("\\\\", "\\").Trim();
        }
    }

    // ==========================================
    // Zobrazený řádek receptury
    // ==========================================

    internal sealed class RecipeMaterialDisplayRow
    {
        public tabRECIPES_MAT.RecipeMaterialViewRow Source { get; set; } = new();

        public string RowNoText { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public string DavkaText { get; set; } = "";

        public string ToleranceText { get; set; } = "";

        public string VazitText { get; set; } = "";
    }

}