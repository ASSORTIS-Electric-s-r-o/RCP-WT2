using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;
using Microsoft.UI.Xaml.Input;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using System.Collections.Generic;
using System.Linq;
using RCP_WT1.Vizualizace;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class JobPage : Page
    {
        // ==========================================
        // Testovací datová struktura zakázky
        // ==========================================
        private class JobRow
        {
            public int IDjob { get; set; }
            public int Status { get; set; }
            public string JobNo { get; set; } = "";
            public string BatchNo { get; set; } = "";
            public string RecipeName { get; set; } = "";
            public double ReqAmountPcs { get; set; }
            public int ReqNumberBatch { get; set; }
        }

        // ==========================================
        // Testovací datová struktura statusu
        // ==========================================
        private class StatusRow
        {
            public int ID { get; set; }
            public string Name { get; set; } = "";
        }

        // ==========================================
        // Datová struktura směny pro ComboBox
        // ==========================================
        private class ShiftRow
        {
            public int ID { get; set; }
            public string Name { get; set; } = "";
        }

        // ==========================================
        // Vrácení data zakázky pro filtrování dnů
        // Priorita:
        // 1) DeliveryDate
        // 2) DT
        // ==========================================
        private DateTime? GetJobDate(tabJOB_LIST.viewJobList job)
        {
            if (!string.IsNullOrWhiteSpace(job.DeliveryDate) &&
                DateTime.TryParse(job.DeliveryDate, out DateTime deliveryDate))
            {
                return deliveryDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(job.DT) &&
                DateTime.TryParse(job.DT, out DateTime dt))
            {
                return dt.Date;
            }

            return null;
        }

        // ==========================================
        // Stav přihlášení pro testovací obrazovku1
        // ==========================================
        private bool _operatorPrihlasen = false;
        private string _operatorJmeno = "";

        // ==========================================
        // Seznam všech načtených zakázek podle statusu
        // ==========================================
        private List<tabJOB_LIST.viewJobList> _allJobs = new();

        // ==========================================
        // Aktuálně vybraný den dodání
        // ==========================================
        private DateTime? _vybranyDen = null;

        // ==========================================
        // Aktuálně vybraná směna
        // 0 = všechny směny
        // 1 = ranní směna
        // 2 = odpolední směna
        // ==========================================
        private int _vybranaSmena = 0;

        // ==========================================
        // Aktuálně vybraný status zakázek
        // 0 = plánované / aktivní
        // 10 = dokončené / historie
        // ==========================================
        private int _aktualniStatusID = 0;

        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _refreshTimer;

        // ==========================================
        // Viditelnost ikony smazání v řádku tabulky.
        // Ikona je dostupná pouze pro přihlášeného uživatele
        // s vyššími právy než běžný operátor.
        // ==========================================
        public Visibility DeleteColumnVisibility
        {
            get
            {
                if (!UserSession.IsLoggedIn || UserSession.CurrentUser == null)
                    return Visibility.Collapsed;

                int role = UserSession.CurrentUser.IDrole;

                return role == 1 || role == 2
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        // ==========================================
        // Konstruktor stránky
        // ==========================================
        public JobPage()
        {
            InitializeComponent();

            // Datový kontext tabulky se používá pro řízení viditelnosti tlačítka smazání v každém řádku.
            JobsTable.DataContext = this;

            NactiShiftComboBox();
            NactiStatusComboBox();

            AktualizujOperatora();
            UpdateSettingsButtonVisibility();

            // ==========================================
            // Automatický refresh zakázek každou minutu
            // ==========================================
            _refreshTimer = DispatcherQueue.CreateTimer();
            _refreshTimer.Interval = TimeSpan.FromMinutes(1);

            _refreshTimer.Tick += (_, _) =>
            {
                try
                {
                    NactiZakazky(_aktualniStatusID);
                }
                catch
                {
                    // Ignorováno
                }
            };

            _refreshTimer.Start();
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnNavigatedFrom(e);
        }

        // ==========================================
        // Krátké zobrazení informačního panelu
        // ==========================================
        //timer pro info lištu
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;
        private void ZobrazInfoStatus(string title, string message, InfoBarSeverity severity)
        {
            // Naplnění obsahu horního vysouvacího panelu.
            InfoToastTitle.Text = title;
            InfoToastMessage.Text = message;
            InfoToastIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(VratInfoIkonu(severity)));

            // Nastavení barev podle typu hlášení a aktuálního světlého / tmavého režimu.
            NastavInfoToastStyl(severity);

            // Krátké skrytí před opětovným zobrazením zajistí,
            // že se EntranceThemeTransition spustí i při rychlém opakovaném hlášení.
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

            InfoToast.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(border.a, border.r, border.g, border.b));
            InfoToast.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(background.a, background.r, background.g, background.b));
        }

        // ==========================================
        // Načtení dat pro zobrazení
        // ==========================================
        private void NactiZakazky(int statusID)
        {
            _aktualniStatusID = statusID;

            var (jobs, _) = tabJOB_LIST.GetJobList();

            _allJobs = jobs ?? new List<tabJOB_LIST.viewJobList>();

            if (statusID == 0)
            {
                _allJobs = _allJobs
                    .Where(j => j.Status == 0 ||
                                j.Status == 1 ||
                                j.Status == 3)
                    .ToList();
            }
            else if (statusID == 10)
            {
                _allJobs = _allJobs
                    .Where(j => j.Status == 10 ||
                                j.Status == 2)
                    .ToList();
            }

            NaplnPrepinacDnu();
            AktualizujZakazkyPodleFiltru();
            CheckZakladZakazky();
        }

        // ==========================================
        // Naplnění přepínače dnů podle DeliveryDate.
        // Pokud DeliveryDate není k dispozici,
        // použije se původní datum DT.
        // ==========================================
        private void NaplnPrepinacDnu()
        {
            DaysPanel.Children.Clear();

            var dny = _allJobs
                .Select(j => GetJobDate(j))
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (dny.Count == 0)
            {
                _vybranyDen = null;
                JobsTable.ItemsSource = new List<tabJOB_LIST.viewJobList>();
                return;
            }

            if (_vybranyDen == null || !dny.Contains(_vybranyDen.Value.Date))
                _vybranyDen = dny.First();

            foreach (DateTime den in dny)
            {
                int pocetCelkem = _allJobs.Count(j =>
                {
                    DateTime? datum = GetJobDate(j);

                    return datum.HasValue &&
                           datum.Value.Date == den.Date;
                });

                int pocetRanni = _allJobs.Count(j =>
                {
                    DateTime? datum = GetJobDate(j);

                    return datum.HasValue &&
                           datum.Value.Date == den.Date &&
                           j.DeliveryShift == 1;
                });

                int pocetOdpoledni = _allJobs.Count(j =>
                {
                    DateTime? datum = GetJobDate(j);

                    return datum.HasValue &&
                           datum.Value.Date == den.Date &&
                           j.DeliveryShift == 2;
                });

                bool jeVybrany =
                    _vybranyDen.HasValue &&
                    _vybranyDen.Value.Date == den.Date;

                Button button = new Button
                {
                    Tag = den,
                    MinWidth = 154,
                    Height = 64,
                    Padding = new Thickness(10, 6, 10, 6),
                    Background = jeVybrany
                    ? (Brush)Application.Current.Resources["AccentButtonBackground"]
                    : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],

                    Foreground = jeVybrany
                    ? (Brush)Application.Current.Resources["AccentButtonForeground"]
                    : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                };

                StackPanel obsah = new StackPanel
                {
                    Spacing = 1
                };

                obsah.Children.Add(new TextBlock
                {
                    Text = den.ToString("dddd dd.MM.", new CultureInfo("cs-CZ")),
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                obsah.Children.Add(new TextBlock
                {
                    Text = $"Celkem {pocetCelkem} | R {pocetRanni} | O {pocetOdpoledni}",
                    FontSize = 12,
                    Opacity = 0.85,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                button.Content = obsah;
                button.Click += DayButton_Click;

                DaysPanel.Children.Add(button);
            }
        }

        // ==========================================
        // Kliknutí na den v přepínači dnů
        // ==========================================
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not DateTime den)
                return;

            _vybranyDen = den.Date;

            NaplnPrepinacDnu();
            AktualizujZakazkyPodleFiltru();

            BtnDetail.Visibility = Visibility.Collapsed;
            BtnOpenRecipe.Visibility = Visibility.Collapsed;
        }

        // ==========================================
        // Filtrování tabulky podle vybraného dne a směny.
        // Datum se bere z DeliveryDate,
        // pokud není k dispozici, použije se DT.
        // Aktivní zakázka je vždy první v tabulce.
        // ==========================================
        private void AktualizujZakazkyPodleFiltru()
        {
            if (_vybranyDen == null)
            {
                JobsTable.ItemsSource = new List<tabJOB_LIST.viewJobList>();

                return;
            }

            DateTime vybranyDen = _vybranyDen.Value.Date;

            var filtrovaneZakazky = _allJobs
                .Where(j =>
                {
                    DateTime? datum = GetJobDate(j);

                    return datum.HasValue &&
                           datum.Value.Date == vybranyDen;
                })
                .Where(j =>
                {
                    if (j.DeliveryShift <= 0)
                        return true;

                    return _vybranaSmena == 0 ||
                           j.DeliveryShift == _vybranaSmena;
                })
                .OrderBy(j => j.Status == 1 ? 0 : 1)
                .ThenBy(j => j.DeliveryShift)
                .ThenBy(j => j.JobNo)
                .ToList();

            JobsTable.ItemsSource = filtrovaneZakazky;

            string smenaText = _vybranaSmena switch
            {
                1 => "Ranní",
                2 => "Odpolední",
                _ => "Všechny směny"
            };

        }

        // ==========================================
        // Zobrazení tlačítka pro výpočet základu zakázky, pokud je potřeba
        // ==========================================
        private void CheckZakladZakazky()
        {
            BtnVypocetZakladu.Visibility = Visibility.Collapsed;

            if (!Settings.Param_ZakladActive)
                return;

            var (jobs, _) = tabJOB_LIST.GetJobListZaklad();

            var cekajici = jobs.Where(j => j.Status == 0).ToList();

            foreach (var job in cekajici)
            {
                if (job.IDrcp <= 0 || job.ReqAmountPcs <= 0)
                    continue;

                var (mat, _) = tabRECIPES_MAT.GetByRecipe(job.IDrcp);

                bool maZaklad =
                    mat.Any(m => m.IDzaklad > 0 && m.Davka > 0);

                if (maZaklad)
                {
                    BtnVypocetZakladu.Visibility = Visibility.Visible;
                    return;
                }
            }
        }

        // ==========================================
        // Kliknutí na přihlášení / odhlášení
        // ==========================================
        private void UserPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (UserSession.IsLoggedIn)
            {
                UserSession.Logout();
                AktualizujOperatora();
                UpdateSettingsButtonVisibility();
                return;
            }

            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Closed += (_, _) =>
            {
                AktualizujOperatora();
                UpdateSettingsButtonVisibility();
            };

            ModalWindowService.Otevri(loginWindow);
        }

        // ==========================================
        // Aktualizace viditelnosti administračních tlačítek
        // ==========================================
        private void UpdateSettingsButtonVisibility()
        {
            if (!UserSession.IsLoggedIn || UserSession.CurrentUser == null)
            {
                BtnSettings.Visibility = Visibility.Collapsed;
                ObnovViditelnostMazaniVRadku();
                return;
            }

            int role = UserSession.CurrentUser.IDrole;

            Visibility visibility =
                role == 1 || role == 2
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BtnSettings.Visibility = visibility;
            ObnovViditelnostMazaniVRadku();
        }

        // ==========================================
        // Obnovení šablony řádků tabulky kvůli viditelnosti
        // tlačítka pro smazání zakázky.
        // ==========================================
        private void ObnovViditelnostMazaniVRadku()
        {
            if (JobsTable == null)
                return;

            object? aktualniZdroj = JobsTable.ItemsSource;
            JobsTable.ItemsSource = null;
            JobsTable.ItemsSource = aktualniZdroj;
        }

        // ==========================================
        // Aktualizace zobrazení operátora
        // ==========================================
        private void AktualizujOperatora()
        {
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                _operatorPrihlasen = true;
                _operatorJmeno = UserSession.CurrentUser.Username;

                TxtLoginCaption.Text = "Odhlásit";
                TxtLoginUser.Text = _operatorJmeno;
                TxtLoginUser.Visibility = Visibility.Visible;
            }
            else
            {
                _operatorPrihlasen = false;
                _operatorJmeno = "";

                TxtLoginCaption.Text = "Přihlášení";
                TxtLoginUser.Text = "";
                TxtLoginUser.Visibility = Visibility.Collapsed;
            }
        }

        // ==========================================
        // Načtení výběru režimu zakázek
        // ==========================================
        private void NactiStatusComboBox()
        {
            StatusComboBox.ItemsSource = new List<StatusRow>
            {
                new StatusRow { ID = 0, Name = "Plánované" },
                new StatusRow { ID = 10, Name = "Dokončené" }
            };

            int statusID = Settings.LastSelectedStatusID;

            if (statusID != 0 && statusID != 10)
                statusID = 0;

            StatusComboBox.SelectedValue = statusID;

            NactiZakazky(statusID);
        }

        private void NactiShiftComboBox()
        {
            ShiftComboBox.ItemsSource = new List<ShiftRow>
            {
                new ShiftRow { ID = 0, Name = "Všechny směny" },
                new ShiftRow { ID = 1, Name = "Ranní" },
                new ShiftRow { ID = 2, Name = "Odpolední" }
            };

            ShiftComboBox.SelectedValue = _vybranaSmena;
        }

        private void ShiftComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShiftComboBox.SelectedValue is not int smena)
                return;

            _vybranaSmena = smena;

            AktualizujZakazkyPodleFiltru();

            BtnDetail.Visibility = Visibility.Collapsed;
            BtnOpenRecipe.Visibility = Visibility.Collapsed;
        }

        // ==========================================
        // Navigace na receptury
        // ==========================================
        private void BtnRecipesLeft_Click(object sender, TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(RecipePage));
        }

        private ScaleWindow? _scaleWindow;

        // ==========================================
        // Otevření pomocného okna váhy
        // ==========================================
        private void OpenScale_Click(object sender, TappedRoutedEventArgs e)
        {
            if (_scaleWindow != null)
                return;

            _scaleWindow = new ScaleWindow();

            _scaleWindow.Closed += (_, _) =>
            {
                _scaleWindow = null;
            };

            ModalWindowService.Otevri(_scaleWindow);
        }

        // ==========================================
        // Navigace na sumarizaci
        // ==========================================
        private void OpenSums_Click(object sender, TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(SumarizationPage));
        }

        // ==========================================
        // Výpočet požadovaných základů z naplánovaných zakázek
        // ==========================================
        private void VypocetZakladu_Click(object sender, TappedRoutedEventArgs e)
        {
            var (jobs, _) = tabJOB_LIST.GetJobListZaklad();

            var cekajici = jobs
                .Where(j => j.Status == 0)
                .ToList();

            Dictionary<int, float> suma = new();
            HashSet<int> pouzite = new();

            foreach (var job in cekajici)
            {
                int idrcp = job.IDrcp;
                float req = job.ReqAmountPcs;

                if (idrcp <= 0 || req <= 0)
                    continue;

                var (rec, ok) = tabRECIPES.GetRecipeRowByIDrcp(idrcp);

                if (!ok || rec == null)
                    continue;

                float receptPcs = rec.AmountPcs > 0 ? rec.AmountPcs : 1;

                var (matRows, _) = tabRECIPES_MAT.GetByRecipe(idrcp);

                var zaklady = matRows
                    .Where(m => m.IDzaklad > 0 && m.Davka > 0)
                    .ToList();

                if (zaklady.Count == 0)
                    continue;

                foreach (var m in zaklady)
                {
                    float hmotnost = m.Davka * req / receptPcs;

                    if (!suma.ContainsKey(m.IDzaklad))
                        suma[m.IDzaklad] = 0;

                    suma[m.IDzaklad] += hmotnost;
                }

                pouzite.Add(job.IDjob);
            }

            int counter = 1;
            int created = 0;

            foreach (var kvp in suma)
            {
                int idzaklad = kvp.Key;
                float cilHmotnost = kvp.Value;

                var (zrec, ok) = tabRECIPES.GetRecipeRowByIDrcp(idzaklad);

                if (!ok || zrec == null)
                    continue;

                float rpcs = zrec.AmountPcs > 0 ? zrec.AmountPcs : 1;

                var (mrows, _) = tabRECIPES_MAT.GetByRecipe(idzaklad);

                float davkaKg = mrows.Sum(m => m.Davka);

                if (davkaKg <= 0)
                    continue;

                float kgNa1 = davkaKg / rpcs;
                float pocetKs = cilHmotnost / kgNa1;

                tabJOB_LIST.InsertNewJob(new tabJOB_LIST.tabJobListInsert
                {
                    JobNo = $"ZAKLAD-{counter}",
                    BatchNo = $"{DateTime.Now:yyMMdd}",
                    IDrcp = zrec.IDrcp,
                    IDgrp = zrec.IDgrp,
                    Status = 0,
                    AmountPcs = pocetKs,
                    PlannedBatch = 1,
                    StationIdx = 0,
                    DT = DateTime.Now,
                    ZakladVypocten = 1
                });

                counter++;
                created++;
            }

            foreach (int id in pouzite)
            {
                var (list, count) = tabJOB_LIST.GetJobByID(id);

                if (count > 0)
                {
                    var job = list.First();
                    job.ZakladVypocten = 1;
                    tabJOB_LIST.UpdateJob(job);
                }
            }

            int statusID = StatusComboBox.SelectedValue is int s
                ? s
                : Settings.LastSelectedStatusID;

            NactiZakazky(statusID);
            CheckZakladZakazky();

            ZobrazInfoStatus(
                "Výpočet základu",
                $"Výpočet dokončen. Vytvořeno zakázek: {created}. Označeno původních zakázek: {pouzite.Count}.",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Smazání zakázky přímo z řádku tabulky.
        // Tlačítko je v XAML viditelné pouze pro přihlášeného
        // uživatele s vyššími právy než běžný operátor.
        // ==========================================
        private void DeleteJobRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not tabJOB_LIST.viewJobList job)
            {
                ZobrazInfoStatus(
                    "Informace",
                    "Zakázku se nepodařilo určit.",
                    InfoBarSeverity.Informational);

                return;
            }

            ConfirmWindow confirm = new ConfirmWindow(
                "Smazání zakázky",
                $"Opravdu chcete smazat zakázku?\n\nZakázka: {job.JobNo}\nŠarže: {job.BatchNo}\nReceptura: {job.RecipeName}");

            confirm.Closed += (_, _) =>
            {
                if (!confirm.Potvrzeno)
                    return;

                try
                {
                    int statusID = StatusComboBox.SelectedValue is int s
                        ? s
                        : Settings.LastSelectedStatusID;

                    tabPRODUCTION.DeleteByJob(job.IDjob);
                    tabJOB_LIST.DeleteJob(job.IDjob);

                    NactiZakazky(statusID);

                    BtnDetail.Visibility = Visibility.Collapsed;
                    BtnOpenRecipe.Visibility = Visibility.Collapsed;

                    ZobrazInfoStatus(
                        "Smazáno",
                        $"Zakázka: {job.JobNo} | Šarže: {job.BatchNo} | Receptura: {job.RecipeName} byla smazána.",
                        InfoBarSeverity.Success);
                }
                catch
                {
                    ZobrazInfoStatus(
                        "Chyba",
                        "Zakázku se nepodařilo smazat.",
                        InfoBarSeverity.Error);
                }
            };

            ModalWindowService.Otevri(confirm);
        }

        // ==========================================
        // Navigace na nastavení
        // ==========================================
        private void BtnSettingsLeft_Click(object sender, TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(SettingPage));
        }

        // ==========================================
        // Otevření aktivního vážení z levého menu
        // ==========================================
        private void OpenRecipe_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenRecipe_Click(sender, e);
        }

        // ==========================================
        // Otevření detailu zakázky z levého menu
        // ==========================================
        private void DetailPage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DetailPage_Click(sender, e);
        }

        // ==========================================
        // Otevření aktivního vážení vybrané zakázky
        // ==========================================
        private void OpenRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (JobsTable.SelectedItem is not tabJOB_LIST.viewJobList selected)
            {
                ZobrazInfoStatus(
                    "Informace",
                    "Nejprve vyberte zakázku.",
                    InfoBarSeverity.Informational);

                return;
            }

            int jobID = selected.IDjob;
            int batchIndex = tabPRODUCTION.GetMaxBatchNoIndex(jobID);

            if (batchIndex <= 0)
                batchIndex = 1;

            if (selected.Status == 3)
            {
                var (activeJobs, _) = tabJOB_LIST.GetJobListViewByStatus(1);

                bool existujeJinaAktivni =
                    activeJobs.Any(j => j.IDjob != jobID);

                if (existujeJinaAktivni)
                {
                    ZobrazInfoStatus(
                        "Informace",
                        "Jiná zakázka je právě aktivní. Tuto zakázku nelze spustit.",
                        InfoBarSeverity.Informational);

                    Frame?.Navigate(
                        typeof(RecipeDetailPage),
                        new RecipeDetailArgs
                        {
                            Mode = RecipeDetailPage.EditMode.Planned,
                            Id = jobID,
                            BatchIndex = 1
                        });

                    return;
                }

                tabJOB_LIST.UpdateJobStatus(1, jobID);
            }

            Frame?.Navigate(
                typeof(RecipeActualPage),
                new RecipeActualArgs
                {
                    IDjob = jobID,
                    BatchIndex = batchIndex
                });
        }

        // ==========================================
        // Otevření detailu vybrané zakázky
        // ==========================================
        private void DetailPage_Click(object sender, RoutedEventArgs e)
        {
            if (JobsTable.SelectedItem is not tabJOB_LIST.viewJobList selectedJob)
            {
                ZobrazInfoStatus(
                    "Informace",
                    "Nejprve vyberte zakázku z tabulky.",
                    InfoBarSeverity.Informational);

                return;
            }

            if (selectedJob.IDmrg > 0)
            {
                if (selectedJob.Status == 2 || selectedJob.Status == 10)
                {
                    Frame?.Navigate(
                        typeof(RecipeDetailPage),
                        new RecipeDetailArgs
                        {
                            Mode = RecipeDetailPage.EditMode.History,
                            Id = selectedJob.IDjob,
                            BatchIndex = 1
                        });

                    return;
                }

                Frame?.Navigate(
                    typeof(RecipeDetailPage),
                    new RecipeDetailArgs
                    {
                        Mode = RecipeDetailPage.EditMode.MergeRecipes,
                        Id = selectedJob.IDjob,
                        BatchIndex = 1
                    });

                return;
            }

            if (selectedJob.Status == 0 || selectedJob.Status == 3)
            {
                Frame?.Navigate(
                    typeof(RecipeDetailPage),
                    new RecipeDetailArgs
                    {
                        Mode = RecipeDetailPage.EditMode.Planned,
                        Id = selectedJob.IDjob,
                        BatchIndex = 1
                    });

                return;
            }

            if (selectedJob.Status == 2 || selectedJob.Status == 10)
            {
                Frame?.Navigate(
                    typeof(RecipeDetailPage),
                    new RecipeDetailArgs
                    {
                        Mode = RecipeDetailPage.EditMode.History,
                        Id = selectedJob.IDjob,
                        BatchIndex = 1
                    });
            }
        }

        // ==========================================
        // Ruční obnovení zakázek z databáze
        // ==========================================
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            int statusID = StatusComboBox.SelectedValue is int s
                ? s
                : Settings.LastSelectedStatusID;

            NactiZakazky(statusID);

            BtnDetail.Visibility = Visibility.Collapsed;
            BtnOpenRecipe.Visibility = Visibility.Collapsed;

            ZobrazInfoStatus(
                "Obnoveno",
                "Zakázky byly znovu načteny z databáze.",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Změna režimu zobrazení zakázek
        // ==========================================
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusComboBox.SelectedValue is not int statusID)
                return;

            Settings.LastSelectedStatusID = statusID;

            _vybranyDen = null;
            _vybranaSmena = 0;

            if (ShiftComboBox != null)
                ShiftComboBox.SelectedValue = _vybranaSmena;

            NactiZakazky(statusID);

            if (statusID == 10)
            {


                ZobrazInfoStatus(
                    "Dokončené",
                    "Zobrazeny jsou dokončené zakázky.",
                    InfoBarSeverity.Success);
            }
            else
            {


                ZobrazInfoStatus(
                    "Plánované",
                    "Zobrazeny jsou plánované a aktivní zakázky.",
                    InfoBarSeverity.Success);
            }
        }

        // ==========================================
        // Změna výběru řádku v tabulce zakázek
        // ==========================================
        private void JobsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (JobsTable.SelectedItem is not tabJOB_LIST.viewJobList job)
            {
                BtnDetail.Visibility = Visibility.Collapsed;
                BtnOpenRecipe.Visibility = Visibility.Collapsed;
                return;
            }

            BtnDetail.Visibility =
                job.Status == 0 || job.Status == 10 || job.Status == 2
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BtnOpenRecipe.Visibility =
                job.Status == 1 || job.Status == 3
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ZobrazInfoStatus(
                job.StatusName,
                $"{job.JobNo} | {job.BatchNo} | {job.RecipeName} | {job.DeliveryDateName} | {job.DeliveryShiftName}",
                InfoBarSeverity.Informational);
        }
    }

    // ==========================================
    // Konvertor barvy pozadí podle statusu zakázky
    // ==========================================
    public sealed class JobStatusBackgroundConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        // ==========================================
        // Převod statusu na barvu pozadí řádku
        // ==========================================
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int status = value is int s ? s : -1;

            return status switch
            {
                1 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 76, 175, 80)),     // zelená
                2 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 255, 193, 7)),     // žlutá
                3 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 255, 152, 0)),     // oranžová
                10 => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(35, 76, 175, 80)),    // zelená
                _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent)                        // default
            };
        }

        // ==========================================
        // Zpětný převod není potřeba
        // ==========================================
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
