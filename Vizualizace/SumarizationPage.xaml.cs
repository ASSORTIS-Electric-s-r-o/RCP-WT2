using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class SumarizationPage : Page
    {
        // ==========================================
        // Režim zobrazení sumarizace
        // ==========================================
        private enum ViewMode
        {
            Zaklad,
            Klasika,
            Materialy
        }

        // ==========================================
        // Aktuální stav filtrů
        // ==========================================
        private ViewMode _mode = ViewMode.Zaklad;

        private int _year = 0;
        private int _month = 0;
        private int _week = 0;
        private int _day = 0;

        private bool _blockFilter = false;

        // ==========================================
        // Krátké zobrazení horního vysouvacího informačního panelu
        // ==========================================
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;

        // ==========================================
        // Konstruktor
        // ==========================================
        public SumarizationPage()
        {
            InitializeComponent();

            Loaded += SumarizationPage_Loaded;
        }

        // ==========================================
        // Loaded stránky
        // ==========================================
        private void SumarizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            AktualizujOperatora();
            SetMode(ViewMode.Zaklad);
        }

        // ==========================================
        // Přepnutí na sumarizaci základů
        // ==========================================
        private void BtnZaklad_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetMode(ViewMode.Zaklad);
        }

        // ==========================================
        // Přepnutí na sumarizaci receptur
        // ==========================================
        private void BtnKlasika_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetMode(ViewMode.Klasika);
        }

        // ==========================================
        // Přepnutí na sumarizaci materiálů
        // ==========================================
        private void BtnMaterialy_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetMode(ViewMode.Materialy);
        }

        // ==========================================
        // Nastavení aktuálního režimu
        // ==========================================
        private void SetMode(ViewMode mode)
        {
            _mode = mode;

            RootNav.SelectedItem = mode switch
            {
                ViewMode.Zaklad => BtnZaklad,
                ViewMode.Klasika => BtnKlasika,
                ViewMode.Materialy => BtnMaterialy,
                _ => BtnZaklad
            };

            TxtTableTitle.Text = mode switch
            {
                ViewMode.Zaklad => "Suma základů",
                ViewMode.Klasika => "Suma receptů",
                ViewMode.Materialy => "Suma materiálů",
                _ => "Sumarizace"
            };

            TxtPageDescription.Text = mode switch
            {
                ViewMode.Zaklad => "Přehled navážených základů podle období a operátora",
                ViewMode.Klasika => "Přehled navážených receptur podle období a operátora",
                ViewMode.Materialy => "Přehled navážených materiálů podle období a operátora",
                _ => "Sumarizace výroby"
            };

            UpdateGridColumns();
            InitDateCombosForMode();
            InitOperatorCombo(false);
            ReloadTable();
        }

        // ==========================================
        // Klíč režimu pro SQL dotazy
        // 0 = receptury, 1 = základy, 2 = materiály
        // ==========================================
        private int GetModeKey()
        {
            return _mode switch
            {
                ViewMode.Klasika => 0,
                ViewMode.Zaklad => 1,
                ViewMode.Materialy => 2,
                _ => 0
            };
        }

        // ==========================================
        // Inicializace datumových filtrů
        // ==========================================
        private void InitDateCombosForMode()
        {
            _blockFilter = true;

            try
            {
                int modeKey = GetModeKey();

                List<int> years = tabPRODUCTION.SelectYearsVyroba(modeKey) ?? new List<int>();

                if (years.Count == 0)
                {
                    NastavPrazdneFiltry();
                    return;
                }

                List<object> yearSource = years.Cast<object>().ToList();
                yearSource.Insert(0, "-Vše-");

                CbYear.ItemsSource = yearSource;

                _year = years[0];
                CbYear.SelectedIndex = 1;

                RebuildMonthCombo(modeKey, false);
                RebuildWeekCombo(modeKey, false);
                RebuildDayCombo(modeKey, false);
            }
            finally
            {
                _blockFilter = false;
            }
        }

        // ==========================================
        // Nastavení filtrů při prázdných datech
        // ==========================================
        private void NastavPrazdneFiltry()
        {
            object[] emptySource = { "-Vše-" };

            CbYear.ItemsSource = emptySource;
            CbMonth.ItemsSource = emptySource;
            CbWeek.ItemsSource = emptySource;
            CbDay.ItemsSource = emptySource;
            CbOperator.ItemsSource = emptySource;

            CbYear.SelectedIndex = 0;
            CbMonth.SelectedIndex = 0;
            CbWeek.SelectedIndex = 0;
            CbDay.SelectedIndex = 0;
            CbOperator.SelectedIndex = 0;

            _year = 0;
            _month = 0;
            _week = 0;
            _day = 0;

            AktualizujSouhrnneKarty(null);
        }

        // ==========================================
        // Obnova filtru měsíců
        // ==========================================
        private void RebuildMonthCombo(int modeKey, bool keepSelection)
        {
            int previousValue = keepSelection && CbMonth.SelectedItem is int selectedMonth
                ? selectedMonth
                : 0;

            List<int> months = tabPRODUCTION.SelectMonthsVyroba(modeKey, _year) ?? new List<int>();

            List<object> source = months.Cast<object>().ToList();
            source.Insert(0, "-Vše-");

            CbMonth.ItemsSource = source;

            if (keepSelection && previousValue != 0 && months.Contains(previousValue))
            {
                _month = previousValue;
                CbMonth.SelectedItem = previousValue;
            }
            else
            {
                _month = months.Count > 0 ? months[0] : 0;
                CbMonth.SelectedIndex = months.Count > 0 ? 1 : 0;
            }
        }

        // ==========================================
        // Obnova filtru týdnů
        // ==========================================
        private void RebuildWeekCombo(int modeKey, bool keepSelection)
        {
            int previousValue = keepSelection && CbWeek.SelectedItem is int selectedWeek
                ? selectedWeek
                : 0;

            List<int> weeks = tabPRODUCTION.SelectWeeksVyroba(modeKey, _year, _month) ?? new List<int>();

            List<object> source = weeks.Cast<object>().ToList();
            source.Insert(0, "-Vše-");

            CbWeek.ItemsSource = source;

            if (keepSelection && previousValue != 0 && weeks.Contains(previousValue))
            {
                _week = previousValue;
                CbWeek.SelectedItem = previousValue;
            }
            else
            {
                _week = weeks.Count > 0 ? weeks[0] : 0;
                CbWeek.SelectedIndex = weeks.Count > 0 ? 1 : 0;
            }
        }

        // ==========================================
        // Obnova filtru dnů
        // ==========================================
        private void RebuildDayCombo(int modeKey, bool keepSelection)
        {
            int previousValue = keepSelection && CbDay.SelectedItem is int selectedDay
                ? selectedDay
                : 0;

            List<int> days = tabPRODUCTION.SelectDaysVyroba(modeKey, _year, _month, _week) ?? new List<int>();

            List<object> source = days.Cast<object>().ToList();
            source.Insert(0, "-Vše-");

            CbDay.ItemsSource = source;

            if (keepSelection && previousValue != 0 && days.Contains(previousValue))
            {
                _day = previousValue;
                CbDay.SelectedItem = previousValue;
            }
            else
            {
                _day = days.Count > 0 ? days[0] : 0;
                CbDay.SelectedIndex = days.Count > 0 ? 1 : 0;
            }
        }

        // ==========================================
        // Inicializace operátorů
        // ==========================================
        private void InitOperatorCombo(bool keepSelection)
        {
            _blockFilter = true;

            try
            {
                string? previousOperator = keepSelection
                    ? CbOperator.SelectedItem as string
                    : null;

                int modeKey = GetModeKey();

                List<string> operators = tabPRODUCTION.SelectOperatorsVyroba(
                    modeKey,
                    _year,
                    _month,
                    _week,
                    _day) ?? new List<string>();

                operators.Insert(0, "-Vše-");

                CbOperator.ItemsSource = operators;

                if (keepSelection &&
                    !string.IsNullOrWhiteSpace(previousOperator) &&
                    operators.Contains(previousOperator))
                {
                    CbOperator.SelectedItem = previousOperator;
                }
                else
                {
                    CbOperator.SelectedIndex = 0;
                }
            }
            finally
            {
                _blockFilter = false;
            }
        }

        // ==========================================
        // Změna filtru
        // ==========================================
        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_blockFilter)
                return;

            int modeKey = GetModeKey();

            _blockFilter = true;

            try
            {
                if (sender == CbYear)
                {
                    _year = CbYear.SelectedItem is int selectedYear ? selectedYear : 0;

                    RebuildMonthCombo(modeKey, false);
                    RebuildWeekCombo(modeKey, false);
                    RebuildDayCombo(modeKey, false);
                }
                else if (sender == CbMonth)
                {
                    _month = CbMonth.SelectedItem is int selectedMonth ? selectedMonth : 0;

                    RebuildWeekCombo(modeKey, false);
                    RebuildDayCombo(modeKey, false);
                }
                else if (sender == CbWeek)
                {
                    _week = CbWeek.SelectedItem is int selectedWeek ? selectedWeek : 0;

                    RebuildDayCombo(modeKey, false);
                }
                else if (sender == CbDay)
                {
                    _day = CbDay.SelectedItem is int selectedDay ? selectedDay : 0;
                }
            }
            finally
            {
                _blockFilter = false;
            }

            InitOperatorCombo(true);
            ReloadTable();
        }

        // ==========================================
        // Načtení tabulky
        // ==========================================
        private void ReloadTable()
        {
            string? selectedOperator = CbOperator.SelectedItem as string;
            int modeKey = GetModeKey();

            if (_mode == ViewMode.Materialy)
            {
                var rows = tabPRODUCTION.SelectSummaryMaterialyVyroba(
                    _year,
                    _month,
                    _week,
                    _day,
                    selectedOperator);

                GridMaterials.ItemsSource = rows;
                GridRecipes.ItemsSource = null;

                AktualizujSouhrnneKarty(rows);

                ZobrazInfo(
                    InfoBarSeverity.Informational,
                    "Suma materiálů",
                    "Zobrazen souhrn navážených materiálů.");

                return;
            }

            var recipeRows = tabPRODUCTION.SelectSummaryVyroba(
                modeKey,
                _year,
                _month,
                _week,
                _day,
                selectedOperator);

            GridRecipes.ItemsSource = recipeRows;
            GridMaterials.ItemsSource = null;

            AktualizujSouhrnneKarty(recipeRows);

            ZobrazInfo(
                InfoBarSeverity.Informational,
                _mode == ViewMode.Zaklad ? "Suma základů" : "Suma receptů",
                "Zobrazen souhrn výroby podle zvolených filtrů.");
        }

        // ==========================================
        // Přepnutí hlaviček a tabulek
        // ==========================================
        private void UpdateGridColumns()
        {
            bool isMaterialy = _mode == ViewMode.Materialy;

            HeaderRecipes.Visibility = isMaterialy ? Visibility.Collapsed : Visibility.Visible;
            GridRecipes.Visibility = isMaterialy ? Visibility.Collapsed : Visibility.Visible;

            HeaderMaterials.Visibility = isMaterialy ? Visibility.Visible : Visibility.Collapsed;
            GridMaterials.Visibility = isMaterialy ? Visibility.Visible : Visibility.Collapsed;

            TxtPozadovanoCaption.Text = isMaterialy ? "Požadováno" : "Požadováno";
            TxtNavazenoCaption.Text = isMaterialy ? "Naváženo" : "Naváženo";
            TxtPresnostCaption.Text = isMaterialy ? "Položek" : "Průměrná přesnost";
        }

        // ==========================================
        // Aktualizace souhrnných karet nad tabulkou
        // ==========================================
        private void AktualizujSouhrnneKarty(IEnumerable? rows)
        {
            List<object> data = rows?.Cast<object>().ToList() ?? new List<object>();

            double pozadovano = 0;
            double navazeno = 0;
            double presnostSuma = 0;
            int presnostPocet = 0;

            foreach (object row in data)
            {
                pozadovano += ZiskejCislo(row, "Poz", "Pozadovano", "PozadovanoKg", "Required", "RequiredKg");
                navazeno += ZiskejCislo(row, "Nav", "Navazeno", "NavazenoKg", "NavazenoText", "Actual", "ActualKg");

                double accuracy = ZiskejCislo(row, "Accuracy", "Presnost", "AccuracyPercent", "PresnostText");

                if (accuracy > 0)
                {
                    presnostSuma += accuracy;
                    presnostPocet++;
                }
            }

            TxtPocetPolozek.Text = data.Count.ToString(CultureInfo.InvariantCulture);
            TxtPozadovano.Text = FormatKg(pozadovano);
            TxtNavazeno.Text = FormatKg(navazeno);

            if (_mode == ViewMode.Materialy)
            {
                TxtPresnost.Text = data.Count.ToString(CultureInfo.InvariantCulture);
                return;
            }

            TxtPresnost.Text = presnostPocet > 0
                ? $"{presnostSuma / presnostPocet:0.##} %"
                : "—";
        }

        private static double ZiskejCislo(object row, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                PropertyInfo? property = row.GetType().GetProperty(propertyName);

                if (property == null)
                    continue;

                object? value = property.GetValue(row);

                if (value == null)
                    continue;

                if (value is double doubleValue)
                    return doubleValue;

                if (value is float floatValue)
                    return floatValue;

                if (value is decimal decimalValue)
                    return (double)decimalValue;

                if (value is int intValue)
                    return intValue;

                string text = value.ToString() ?? "";
                text = text
                    .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("%", "")
                    .Replace(" ", "")
                    .Replace(",", ".")
                    .Trim();

                if (double.TryParse(
                        text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double parsed))
                {
                    return parsed;
                }
            }

            return 0;
        }

        private static string FormatKg(double value)
        {
            return value > 0
                ? $"{value:0.###} kg"
                : "—";
        }

        // ==========================================
        // Ruční obnovení aktuálního přehledu
        // ==========================================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitOperatorCombo(true);
            ReloadTable();
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

        // ==========================================
        // Aktualizace zobrazení operátora
        // ==========================================
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
        // Zpět
        // ==========================================
        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(JobPage));
        }

        // ==========================================
        // Stavový řádek
        // ==========================================
        private void ZobrazInfo(InfoBarSeverity severity, string title, string message)
        {
            ZobrazInfoStatus(title, message, severity);
        }

        private void ZobrazInfoStatus(string title, string message, InfoBarSeverity severity)
        {
            InfoToastTitle.Text = title;
            InfoToastMessage.Text = message;

            InfoToastIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                new Uri(VratInfoIkonu(severity)));

            NastavInfoToastStyl(severity);

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
    }
}
