using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using System.Threading.Tasks;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class RecipePreSelectPage : Page
    {
        // ==========================================
        // Režim editace
        // ==========================================

        public enum EditMode
        {
            Groups,
            Materials,
            Recipes
        }

        // ==========================================
        // Data stránky
        // ==========================================

        private EditMode _currentMode = EditMode.Groups;
        private int _id = 0;

        private List<tabGROUPS.GroupRow> _allGroups = new();
        private List<tabRECIPES.RecipeRow> _recipeRows = new();


        // ==========================================
        // Konstruktor stránky
        // ==========================================

        public RecipePreSelectPage()
        {
            InitializeComponent();

            Loaded += GroupMaterialEditPage_Loaded;
        }

        public RecipePreSelectPage(EditMode mode, int id)
        {
            InitializeComponent();

            Loaded += GroupMaterialEditPage_Loaded;

            _currentMode = mode;
            _id = id;

            InicializujStranku();
        }

        // ==========================================
        // Načtení parametru z Frame.Navigate
        // ==========================================

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is GroupMaterialEditArgs args)
            {
                _currentMode = args.Mode;
                _id = args.Id;
            }

            InicializujStranku();
        }

        // ==========================================
        // Inicializace stránky
        // ==========================================

        private void InicializujStranku()
        {
            AktualizujCaption();
            NastavSloupceTabulky();
            LoadData();
            AktualizujOperatora();
            UpdateVisibility();
        }

        // ==========================================
        // Automatické navázání dotykové klávesnice
        // ==========================================

        private void GroupMaterialEditPage_Loaded(object sender, RoutedEventArgs e)
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
                UpdateVisibility();
                return;
            }

            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Closed += (_, _) =>
            {
                AktualizujOperatora();
                UpdateVisibility();
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
        // Viditelnost tlačítek
        // ==========================================

        private void UpdateVisibility()
        {
            bool loginRequired = Settings.Param_LoginRequired;

            bool maPrava =
                !loginRequired ||
                (
                    UserSession.IsLoggedIn &&
                    UserSession.CurrentUser != null &&
                    (UserSession.CurrentUser.IDrole == 1 || UserSession.CurrentUser.IDrole == 2)
                );

            Visibility editVisibility =
                maPrava ? Visibility.Visible : Visibility.Collapsed;

            BtnNew.Visibility = editVisibility;
            BtnEdit.Visibility = editVisibility;
            BtnDelete.Visibility = editVisibility;

            BtnZaklad.Visibility =
                maPrava &&
                Settings.Param_ZakladActive &&
                _currentMode == EditMode.Groups
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            AktualizujStavZakladuZeSelekce();
        }

        // ==========================================
        // Popisky podle režimu
        // ==========================================

        private void AktualizujCaption()
        {
            string title;
            string description;
            string caption;
            string message;

            switch (_currentMode)
            {
                case EditMode.Groups:
                    title = "Skupiny";
                    description = "Správa skupin receptur";
                    caption = "Seznam skupin";
                    message = "Vyberte skupinu v seznamu.";
                    break;

                case EditMode.Materials:
                    title = "Materiály";
                    description = "Správa materiálů a surovin";
                    caption = "Seznam materiálů";
                    message = "Vyberte materiál v seznamu.";
                    break;

                case EditMode.Recipes:
                    title = "Receptury";
                    description = "Správa receptur ve vybrané skupině";
                    caption = "Seznam receptů";
                    message = "Vyberte recepturu v seznamu.";
                    break;

                default:
                    title = "Editace";
                    description = "Správa položek";
                    caption = "Seznam";
                    message = "Vyberte položku v seznamu.";
                    break;
            }

            TxtPageTitle.Text = title;
            TxtPageDescription.Text = description;
            MainCaption.Text = caption;

            ZobrazInfo(
                InfoBarSeverity.Informational,
                title,
                message);
        }

        // ==========================================
        // Sloupce podle režimu
        // ==========================================

        private void NastavSloupceTabulky()
        {
            if (_currentMode == EditMode.Groups)
            {
                CodeColumnHeader.Width = new GridLength(0);
                TxtCodeHeader.Visibility = Visibility.Collapsed;
                return;
            }

            CodeColumnHeader.Width = new GridLength(220);
            TxtCodeHeader.Visibility = Visibility.Visible;
        }

        // ==========================================
        // Načtení dat
        // ==========================================

        private void LoadData()
        {
            Table.SelectedItem = null;

            switch (_currentMode)
            {
                case EditMode.Groups:
                    LoadGroups();
                    break;

                case EditMode.Materials:
                    LoadMaterials();
                    break;

                case EditMode.Recipes:
                    LoadRecipes();
                    break;
            }

            AktualizujStavZakladuZeSelekce();
        }

        private void LoadGroups()
        {
            var (groups, _) = tabGROUPS.GetGroupsAll_FromTable();
            _allGroups = groups ?? new List<tabGROUPS.GroupRow>();

            Table.ItemsSource = _allGroups
                .Select(g => new GroupMaterialListRow
                {
                    ID = g.IDgrp,
                    Code = "",
                    Name = g.Name ?? "",
                    MpImage = g.MpImage ?? "",
                    IsZaklad = g.IsZaklad,
                    CodeVisibility = Visibility.Collapsed
                })
                .ToList();
        }

        private void LoadMaterials()
        {
            var (materials, _) = tabMATERIAL.GetMaterialsAll();

            Table.ItemsSource = materials
                .Select(m => new GroupMaterialListRow
                {
                    ID = m.IDmat,
                    Code = m.Cislo ?? "",
                    Name = m.Name ?? "",
                    IsZaklad = 0,
                    CodeVisibility = Visibility.Collapsed
                })
                .ToList();
        }

        private void LoadRecipes()
        {
            if (_id > 0)
            {
                var (recipes, _) = tabRECIPES.GetRecipeRowsByID(_id);
                _recipeRows = recipes ?? new List<tabRECIPES.RecipeRow>();
            }
            else
            {
                var (recipes, _) = tabRECIPES.GetRecipesAll();

                _recipeRows = recipes?
                    .GroupBy(r => r.IDrcp)
                    .Select(g => g.First())
                    .Select(r => new tabRECIPES.RecipeRow
                    {
                        IDrcp = r.IDrcp,
                        IDgrp = r.IDgrp,
                        Cislo = r.RecipeCislo,
                        Name = r.RecipeName,
                        AmountPcs = r.AmountPcs,
                        IsDeleted = r.RecipeIsDeleted,
                        PdfProcedurePath = r.PdfProcedurePath
                    })
                    .ToList()
                    ?? new List<tabRECIPES.RecipeRow>();
            }

            Table.ItemsSource = _recipeRows
                .Select(r => new GroupMaterialListRow
                {
                    ID = r.IDrcp,
                    Code = r.Cislo ?? "",
                    Name = r.Name ?? "",
                    IsZaklad = r.IsZaklad
                })
                .ToList();
        }

        // ==========================================
        // Výběr řádku
        // ==========================================

        private void Table_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Kliknutí nebo dotyk na řádek pouze označí položku.
            // Samotná akce se provádí až tlačítky v levém panelu.
            AktualizujStavZakladuZeSelekce();

            if (Table.SelectedItem is not GroupMaterialListRow row)
                return;

            ZobrazInfo(
                InfoBarSeverity.Informational,
                MainCaption.Text,
                row.Name);
        }


        private void Table_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
            {
                SmazVybranyRadek();
                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.Enter)
            {
                OtevriEditaciVybranehoRadku();
                e.Handled = true;
            }
        }

        // ==========================================
        // Otevření editace vybraného řádku
        // ==========================================
        private void OtevriEditaciVybranehoRadku()
        {
            if (Table.SelectedItem is not GroupMaterialListRow row)
            {
                ZobrazInfo(InfoBarSeverity.Warning, "Upozornění", "Není vybraný žádný řádek.");
                return;
            }

            if (_currentMode == EditMode.Groups)
            {
                EditGroup(row);
                return;
            }

            if (_currentMode == EditMode.Materials)
            {
                EditMaterial(row);
                return;
            }

            if (_currentMode == EditMode.Recipes)
            {
                Frame?.Navigate(typeof(RecipeEditPage), row.ID);
            }
        }

        // ==========================================
        // Smazání vybraného řádku
        // ==========================================
        private void SmazVybranyRadek()
        {
            if (Table.SelectedItem is not GroupMaterialListRow row)
            {
                ZobrazInfo(InfoBarSeverity.Warning, "Upozornění", "Není vybraný žádný řádek.");
                return;
            }

            ConfirmWindow confirm = new ConfirmWindow(
                "Smazání položky",
                $"Opravdu chcete smazat vybranou položku?\n\n{row.Name}");

            confirm.Closed += (_, _) =>
            {
                if (!confirm.Potvrzeno)
                    return;

                try
                {
                    if (_currentMode == EditMode.Groups)
                        tabGROUPS.DeleteGroup(row.ID);
                    else if (_currentMode == EditMode.Materials)
                        tabMATERIAL.DeleteMaterial(row.ID);
                    else if (_currentMode == EditMode.Recipes)
                        tabRECIPES.DeleteRecipe(row.ID);

                    LoadData();
                    ZobrazInfo(InfoBarSeverity.Success, "Smazáno", "Záznam byl úspěšně smazán.");
                }
                catch (Exception ex)
                {
                    ZobrazInfo(InfoBarSeverity.Error, "Chyba", $"Záznam se nepodařilo smazat. {ex.Message}");
                }
            };

            ModalWindowService.Otevri(confirm);
        }

        // ==========================================
        // Aktualizace přepínače základ
        // ==========================================

        private void AktualizujStavZakladuZeSelekce()
        {
            if (_currentMode != EditMode.Groups || !Settings.Param_ZakladActive)
                return;

            if (Table.SelectedItem is not GroupMaterialListRow row)
            {
                BtnZaklad.Opacity = 1;
                return;
            }

            BtnZaklad.Opacity = row.IsZaklad == 1 ? 1 : 0.45;
        }

        // ==========================================
        // Nový záznam
        // ==========================================
        private void BtnNew_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentMode == EditMode.Recipes)
            {
                Frame?.Navigate(typeof(RecipeEditPage), 0);
                return;
            }

            OtevriEditaciTextu(
                "Vlož nový název",
                "",
                async value =>
                {
                    string newName = (value ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        ZobrazInfo(InfoBarSeverity.Warning, "Upozornění", "Název nesmí být prázdný.");
                        return;
                    }

                    try
                    {
                        bool ok = false;

                        if (_currentMode == EditMode.Materials)
                        {
                            int newId = tabMATERIAL.InsertMaterial(newName);
                            ok = newId > 0;
                        }
                        else if (_currentMode == EditMode.Groups)
                        {
                            string imagePath = await OtevriVyberObrazkuAsync(
                                "Vyber obrázek pro novou skupinu.",
                                "");

                            int newId = tabGROUPS.InsertGroup(newName, "R", imagePath);
                            ok = newId > 0;
                        }

                        if (!ok)
                        {
                            ZobrazInfo(InfoBarSeverity.Error, "Chyba", "Záznam se nepodařilo vytvořit.");
                            return;
                        }

                        LoadData();
                        ZobrazInfo(InfoBarSeverity.Success, "Vytvořeno", "Záznam byl úspěšně vytvořen.");
                    }
                    catch (Exception ex)
                    {
                        ZobrazInfo(InfoBarSeverity.Error, "Chyba", $"Záznam se nepodařilo vytvořit. {ex.Message}");
                    }
                });
        }

        // ==========================================
        // Editace záznamu
        // ==========================================
        private void BtnEdit_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OtevriEditaciVybranehoRadku();
        }

        private void EditGroup(GroupMaterialListRow row)
        {
            OtevriEditaciTextu(
                "Uprav název",
                row.Name,
                async value =>
                {
                    string newName = (value ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        ZobrazInfo(InfoBarSeverity.Warning, "Upozornění", "Název nesmí být prázdný.");
                        return;
                    }

                    try
                    {
                        string currentImage =
                            _allGroups.FirstOrDefault(g => g.IDgrp == row.ID)?.MpImage ?? "";

                        string imagePath = await OtevriVyberObrazkuAsync(
                            "Vyber nový obrázek pro skupinu, nebo výběr přeskoč.",
                            currentImage);

                        tabGROUPS.UpdateGroup(row.ID, newName, row.IsZaklad, imagePath);
                        tabRECIPES.UpdateIsZaklad(row.ID, row.IsZaklad);

                        LoadData();
                        ZobrazInfo(InfoBarSeverity.Success, "Upraveno", "Skupina byla úspěšně upravena.");
                    }
                    catch (Exception ex)
                    {
                        ZobrazInfo(InfoBarSeverity.Error, "Chyba", $"Skupinu se nepodařilo upravit. {ex.Message}");
                    }
                });
        }

        private void EditMaterial(GroupMaterialListRow row)
        {
            var (material, found) = tabMATERIAL.GetMaterialByID(row.ID);

            if (!found || material == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Chyba", "Materiál se nepodařilo načíst.");
                return;
            }

            OtevriEditaciTextu(
                "Uprav název",
                material.Name ?? "",
                value =>
                {
                    string newName = (value ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        ZobrazInfo(InfoBarSeverity.Warning, "Upozornění", "Název nesmí být prázdný.");
                        return;
                    }

                    try
                    {
                        bool ok = tabMATERIAL.UpdateMaterial(
                            row.ID,
                            newName,
                            material.Cislo ?? "");

                        if (!ok)
                        {
                            ZobrazInfo(InfoBarSeverity.Error, "Chyba", "Materiál se nepodařilo upravit.");
                            return;
                        }

                        LoadData();
                        ZobrazInfo(InfoBarSeverity.Success, "Upraveno", "Materiál byl úspěšně upraven.");
                    }
                    catch (Exception ex)
                    {
                        ZobrazInfo(InfoBarSeverity.Error, "Chyba", $"Materiál se nepodařilo upravit. {ex.Message}");
                    }
                });
        }

        // ==========================================
        // Smazání záznamu
        // ==========================================
        private void BtnDelete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SmazVybranyRadek();
        }

        // ==========================================
        // Přepnutí základu
        // ==========================================

        private void BtnZaklad_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentMode != EditMode.Groups)
                return;

            if (!Settings.Param_ZakladActive)
                return;

            if (Table.SelectedItem is not GroupMaterialListRow row)
            {
                ZobrazInfo(InfoBarSeverity.Warning, "Základ", "Není vybraná žádná skupina.");
                return;
            }

            int newIsZaklad = row.IsZaklad == 1 ? 0 : 1;

            tabGROUPS.UpdateGroup(row.ID, row.Name, newIsZaklad);
            tabRECIPES.UpdateIsZaklad(row.ID, newIsZaklad);

            LoadData();

            foreach (object item in Table.Items)
            {
                if (item is GroupMaterialListRow listRow && listRow.ID == row.ID)
                {
                    Table.SelectedItem = item;
                    break;
                }
            }

            ZobrazInfo(InfoBarSeverity.Success, "Základ", "Příznak základ byl změněn.");
        }

        // ==========================================
        // Obnovení
        // ==========================================

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ZobrazInfo(InfoBarSeverity.Success, "Obnoveno", "Data byla znovu načtena.");
        }

        // ==========================================
        // Návrat
        // ==========================================

        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(RecipePage));
        }

        // ==========================================
        // Pomocné zobrazení informace
        // ==========================================

        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;

        private void ZobrazInfo(
            InfoBarSeverity severity,
            string title,
            string message)
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
        // Editace textu přes dotykovou klávesnici
        // ==========================================

        private void OtevriEditaciTextu(
            string titulek,
            string hodnota,
            Action<string> poPotvrzeni)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Str,
                hodnota ?? "",
                titulek);

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                poPotvrzeni.Invoke(keyboard.Vysledek ?? "");
            };

            ModalWindowService.Otevri(keyboard);
        }

        private Task<string> OtevriVyberObrazkuAsync(string prompt, string defaultPath)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            ImageSelectDialog dialog = new ImageSelectDialog(
                prompt,
                defaultPath,
                "Výběr obrázku skupiny",
                ".png",
                ".jpg",
                ".jpeg",
                ".bmp",
                ".webp");

            dialog.Closed += (_, _) =>
            {
                if (dialog.Potvrzeno)
                    tcs.TrySetResult(dialog.SelectedPath ?? "");
                else
                    tcs.TrySetResult(defaultPath ?? "");
            };

            ModalWindowService.Otevri(dialog);

            return tcs.Task;
        }
    }

    // ==========================================
    // Parametry navigace stránky
    // ==========================================

    internal sealed class GroupMaterialEditArgs
    {
        public RecipePreSelectPage.EditMode Mode { get; set; }

        public int Id { get; set; }
    }

    // ==========================================
    // Řádek seznamu
    // ==========================================

    internal sealed class GroupMaterialListRow
    {
        public int ID { get; set; }

        public string Code { get; set; } = "";

        public string Name { get; set; } = "";

        public string MpImage { get; set; } = "";

        public int IsZaklad { get; set; }

        public double CodeColumnWidth { get; set; } = 220;
        public Visibility CodeVisibility { get; set; } = Visibility.Visible;
    }
}