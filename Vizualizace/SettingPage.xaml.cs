using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MySql.Data.MySqlClient;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using RCP_WT1.PomocneTridy.CSV;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.Win32;
using System.Reflection;


namespace RCP_WT1.Vizualizace
{
    public sealed partial class SettingPage : Page
    {
        private int _selectedScaleIndex = 1;
        private string _currentSection = "MYSQL";

        // =========================================================
        // Timer pro krátké zobrazení informační lišty
        // =========================================================
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;

        public SettingPage()
        {
            InitializeComponent();

            // Stejný princip jako ve starším WPF řešení:
            // po načtení stránky najdeme všechny vstupní prvky a automaticky jim navážeme otevření klávesnice.
            Loaded += SettingPage_Loaded;

            NaplnSeznamVah();
            NactiVsechnaNastaveni();
            ZobrazPanel("MYSQL");
        }

        // =========================================================
        // Automatické navázání dotykové klávesnice
        // =========================================================

        private void SettingPage_Loaded(object sender, RoutedEventArgs e)
        {
            DotykovaKlavesniceService.Pripoj(this, zjistiRezim: ZjistiRezimKlavesnice);
        }

        private static VirtualKeyboard.KeyboardMode ZjistiRezimKlavesnice(Control control)
        {
            return control.Name switch
            {
                "TxtPcId" or
                "TxtBaud" or
                "TxtDataBits" or
                "TxtTcpPort" or
                "TxtPollMs" or
                "TxtTimeoutMs"
                    => VirtualKeyboard.KeyboardMode.Int,

                "TxtDisplayDivisor" or
                "TxtMaxBatchSize"
                    => VirtualKeyboard.KeyboardMode.Float,

                _ => VirtualKeyboard.KeyboardMode.Str
            };
        }

        // =========================================================
        // Navigace v levém panelu
        // =========================================================

        private void BtnMySql_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazPanel("MYSQL");
        }

        private void BtnComm_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazPanel("COMM");
        }

        private void BtnUsers_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazPanel("USERS");
        }

        private void BtnSystem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazPanel("SYSTEM");
        }

        private void BtnParameters_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ZobrazPanel("PARAMETERS");
        }

        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Frame != null && Frame.CanGoBack)
                Frame.GoBack();
        }

        private void ZobrazPanel(string section)
        {
            _currentSection = section;

            PanelMySql.Visibility = Visibility.Collapsed;
            PanelComm.Visibility = Visibility.Collapsed;
            PanelUsers.Visibility = Visibility.Collapsed;
            PanelSystem.Visibility = Visibility.Collapsed;
            PanelParameters.Visibility = Visibility.Collapsed;

            BtnTestMySql.Visibility = Visibility.Collapsed;
            BtnTestScale.Visibility = Visibility.Collapsed;
            SepTestComm.Visibility = Visibility.Collapsed;
            BtnTestAlibi.Visibility = Visibility.Collapsed;

            BtnUserAdd.Visibility = Visibility.Collapsed;
            BtnUserEdit.Visibility = Visibility.Collapsed;
            BtnUserDelete.Visibility = Visibility.Collapsed;
            SepUsers1.Visibility = Visibility.Collapsed;
            SepUsers2.Visibility = Visibility.Collapsed;

            BtnExportRecipes.Visibility = Visibility.Collapsed;
            BtnImportRecipes.Visibility = Visibility.Collapsed;
            SepSystemImportExport.Visibility = Visibility.Collapsed;

            switch (section)
            {
                case "MYSQL":
                    PanelMySql.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "MySQL";
                    TxtPageDescription.Text = "Nastavení připojení k databázi";
                    ZobrazInfo(
                        InfoBarSeverity.Informational,
                        "MySQL",
                        "Zde nastavíte připojení k MySQL databázi.");

                    BtnTestMySql.Visibility = Visibility.Visible;
                    break;

                case "COMM":
                    PanelComm.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "COMM";
                    TxtPageDescription.Text = "Nastavení komunikace s váhou";
                    ZobrazInfo(
                        InfoBarSeverity.Informational,
                        "COMM",
                        "Zde nastavíte COM nebo ETH komunikaci.");

                    BtnTestScale.Visibility = Visibility.Visible;
                    SepTestComm.Visibility = Visibility.Visible;
                    BtnTestAlibi.Visibility = Visibility.Visible;
                    break;

                case "USERS":
                    PanelUsers.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "Uživatelé";
                    TxtPageDescription.Text = "Správa uživatelů aplikace";

                    BtnUserAdd.Visibility = Visibility.Visible;
                    BtnUserEdit.Visibility = Visibility.Visible;
                    BtnUserDelete.Visibility = Visibility.Visible;
                    SepUsers1.Visibility = Visibility.Visible;
                    SepUsers2.Visibility = Visibility.Visible;

                    NactiUzivatele();
                    break;

                case "SYSTEM":
                    PanelSystem.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "Systém";
                    TxtPageDescription.Text = "Systémové nastavení aplikace";
                    ZobrazInfo(
                        InfoBarSeverity.Informational,
                        "Systém",
                        "Zde nastavíte ID panelu, autostart a import/export receptur.");

                    BtnExportRecipes.Visibility = Visibility.Visible;
                    BtnImportRecipes.Visibility = Visibility.Visible;
                    SepSystemImportExport.Visibility = Visibility.Visible;
                    break;

                case "PARAMETERS":
                    PanelParameters.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "Parametry";
                    TxtPageDescription.Text = "Parametry chování aplikace";
                    ZobrazInfo(
                        InfoBarSeverity.Informational,
                        "Parametry",
                        "Zde nastavíte provozní parametry aplikace.");
                    break;
            }
        }

        private void ZobrazInfo(InfoBarSeverity severity, string title, string message)
        {
            // Naplnění obsahu horního vysouvacího panelu.
            InfoToastTitle.Text = title;
            InfoToastMessage.Text = message;
            InfoToastIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(VratInfoIkonu(severity)));

            // Nastavení barev podle typu hlášení a aktuálního světlého / tmavého režimu.
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

            InfoToast.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(border.a, border.r, border.g, border.b));
            InfoToast.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(background.a, background.r, background.g, background.b));
        }

        // =========================================================
        // Horní lišta
        // =========================================================

        private void SaveCurrent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_currentSection)
                {
                    case "MYSQL":
                        UlozMySql();
                        break;

                    case "COMM":
                        UlozComm(_selectedScaleIndex);
                        break;

                    case "SYSTEM":
                        UlozSystem();
                        break;

                    case "PARAMETERS":
                        UlozParametry();
                        break;
                }

                ZobrazInfo(
                    InfoBarSeverity.Success,
                    "Uloženo",
                    "Nastavení bylo uloženo.");
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Chyba uložení",
                    ex.Message);
            }
        }

        private void ReloadCurrent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NactiVsechnaNastaveni();

                ZobrazInfo(
                    InfoBarSeverity.Informational,
                    "Načteno",
                    "Nastavení bylo znovu načteno.");
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Chyba načtení",
                    ex.Message);
            }
        }

        // =========================================================
        // Načtení všech nastavení
        // =========================================================

        private void NactiVsechnaNastaveni()
        {
            TxtSqlIp.Text = Settings.Param_SQL_IP;
            TxtSqlDb.Text = Settings.Param_SQL_DB;
            TxtSqlUser.Text = Settings.Param_SQL_USER;
            TxtSqlPassword.Password = Settings.Param_SQL_PASSWORD;

            TxtPcId.Text = Settings.Param_PC_ID.ToString();
            ChkAutoStartWindows.IsChecked = Settings.Param_AutoStartWindows;

            ChkAutoRecipeStart.IsChecked = Settings.Param_AutoRecipeStart;
            ChkAutoTare.IsChecked = Settings.Param_AutoTare;
            ChkLoginRequired.IsChecked = Settings.Param_LoginRequired;
            ChkZakladActive.IsChecked = Settings.Param_ZakladActive;
            ChkVypocetVarky.IsChecked = Settings.Param_VypocetVarky;
            ChkVypocetVarky_Checked(null!, null!);

            CmbVypocetVarkyMode.SelectedIndex = Settings.Param_VypocetVarkyMode;
            TxtMaxBatchSize.Text = Settings.Param_MaxBatchSize.ToString(CultureInfo.InvariantCulture);
            CmbUnits.SelectedIndex = Settings.Param_Units;

            ChkZobrazSkupiny.IsChecked = Settings.Param_ZobrazSkupiny;
            ChkPovolitPreskoceni.IsChecked = Settings.Param_PovolitPreskoceni;

            _selectedScaleIndex = Settings.Param_ScaleIndex;

            if (_selectedScaleIndex < 1 || _selectedScaleIndex > 5)
                _selectedScaleIndex = 1;

            CmbScaleIndex.SelectedIndex = _selectedScaleIndex - 1;
            NactiComm(_selectedScaleIndex);
        }

        // =========================================================
        // Uložení MySQL
        // =========================================================

        private void UlozMySql()
        {
            Settings.Param_SQL_IP = TxtSqlIp.Text ?? "";
            Settings.Param_SQL_DB = TxtSqlDb.Text ?? "";
            Settings.Param_SQL_USER = TxtSqlUser.Text ?? "";
            Settings.Param_SQL_PASSWORD = TxtSqlPassword.Password ?? "";
        }

        // =========================================================
        // Uložení systému
        // =========================================================

        private void UlozSystem()
        {
            Settings.Param_PC_ID = ParseInt(TxtPcId.Text, 0);
            Settings.Param_AutoStartWindows = ChkAutoStartWindows.IsChecked == true;

            NastavAutostartWindows(Settings.Param_AutoStartWindows);
        }

        // =========================================================
        // Nastavení automatického spuštění aplikace po startu Windows
        // =========================================================
        private void NastavAutostartWindows(bool povolit)
        {
            const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "RCP_WT1";

            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(runKeyPath, true);

            if (runKey == null)
                throw new InvalidOperationException("Nepodařilo se otevřít registr Windows pro autostart aplikace.");

            if (!povolit)
            {
                runKey.DeleteValue(appName, false);
                return;
            }

            string? exePath = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(exePath))
                exePath = Assembly.GetEntryAssembly()?.Location;

            if (string.IsNullOrWhiteSpace(exePath))
                exePath = Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrWhiteSpace(exePath))
                throw new InvalidOperationException("Nepodařilo se zjistit cestu ke spuštěnému programu.");

            runKey.SetValue(appName, $"\"{exePath}\"");
        }

        // =========================================================
        // Uložení parametrů
        // =========================================================

        private void UlozParametry()
        {
            Settings.Param_AutoRecipeStart = ChkAutoRecipeStart.IsChecked == true;
            Settings.Param_AutoTare = ChkAutoTare.IsChecked == true;
            Settings.Param_LoginRequired = ChkLoginRequired.IsChecked == true;
            Settings.Param_ZakladActive = ChkZakladActive.IsChecked == true;
            Settings.Param_VypocetVarky = ChkVypocetVarky.IsChecked == true;

            Settings.Param_VypocetVarkyMode = CmbVypocetVarkyMode.SelectedIndex < 0
                ? 0
                : CmbVypocetVarkyMode.SelectedIndex;

            Settings.Param_MaxBatchSize = ParseDouble(TxtMaxBatchSize.Text, 2.0);

            Settings.Param_Units = CmbUnits.SelectedIndex < 0
                ? 0
                : CmbUnits.SelectedIndex;

            Settings.Param_ZobrazSkupiny = ChkZobrazSkupiny.IsChecked == true;
            Settings.Param_PovolitPreskoceni = ChkPovolitPreskoceni.IsChecked == true;
        }

        private void ChkVypocetVarky_Checked(object sender, RoutedEventArgs e)
        {
            bool visible = ChkVypocetVarky.IsChecked == true;

            TxtVypocetPodle.Visibility =
                visible ? Visibility.Visible : Visibility.Collapsed;

            CmbVypocetVarkyMode.Visibility =
                visible ? Visibility.Visible : Visibility.Collapsed;

            RowVypocetPodle.Height =
                visible
                    ? new GridLength(64)
                    : new GridLength(0);
        }

        // =========================================================
        // COMM - načtení
        // =========================================================

        private void NaplnSeznamVah()
        {
            CmbScaleIndex.Items.Clear();

            for (int i = 1; i <= 5; i++)
            {
                CmbScaleIndex.Items.Add(new ComboBoxItem
                {
                    Content = NactiNazevVahy(i),
                    Tag = i
                });
            }
        }

        private void CmbScaleIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbScaleIndex.SelectedItem is ComboBoxItem item && item.Tag is int index)
            {
                _selectedScaleIndex = index;
                NactiComm(index);
            }
        }

        private void NactiComm(int index)
        {
            ChkScaleEnabled.IsChecked = NactiPovoleniVahy(index);
            TxtScaleName.Text = NactiNazevVahy(index);

            string typ = NactiCommTyp(index);
            CmbCommType.SelectedIndex = typ == "ETH" ? 1 : 0;

            NaplnComPorty(NactiSerialPort(index));
            TxtBaud.Text = NactiSerialBaud(index).ToString();
            TxtParity.Text = NactiSerialParity(index);
            TxtDataBits.Text = NactiSerialDataBits(index).ToString();
            TxtStopBits.Text = NactiSerialStopBits(index);
            TxtHandshake.Text = NactiSerialHandshake(index);

            TxtHost.Text = NactiSerialHost(index);
            TxtTcpPort.Text = NactiSerialTcpPort(index).ToString();

            TxtScaleAddress.Text = NactiScaleAddress(index);
            TxtReadCommand.Text = NactiReadCommand(index);
            TxtNewline.Text = NactiNewline(index);

            ChkScaleAsync.IsChecked = NactiScaleAsync(index);

            TxtPollMs.Text = NactiPollMs(index).ToString();
            TxtTimeoutMs.Text = NactiTimeoutMs(index).ToString();

            TxtScaleFormat.Text = NactiScaleFormat(index);
            TxtTokenDefs.Text = NactiTokenDefs(index);
            TxtFieldMap.Text = NactiFieldMap(index);
            TxtDisplayDivisor.Text = NactiDisplayDivisor(index).ToString(CultureInfo.InvariantCulture);
            TxtUnitsFallback.Text = NactiUnitsFallback(index);

            TxtStatusMap.Text = NactiStatusMap(index);
            TxtStatusTextMap.Text = NactiStatusTextMap(index);

            ChkEnableAlibi.IsChecked = NactiEnableAlibi(index);
            TxtAlibiStoreCommand.Text = NactiAlibiStoreCommand(index);
            TxtAlibiSuccessFormat.Text = NactiAlibiSuccessFormat(index);
            TxtAlibiEchoFormat.Text = NactiAlibiEchoFormat(index);

            NaplnKopirovaniVah(index);

            NastavViditelnostComm(typ);
        }

        private void CmbCommType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string typ = ((CmbCommType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM").Trim();
            NastavViditelnostComm(typ);
        }

        private void NastavViditelnostComm(string typ)
        {
            bool jeCom = typ != "ETH";

            LblComPort.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            CmbComPort.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblBaud.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            TxtBaud.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblParity.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            TxtParity.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblDataBits.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            TxtDataBits.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblStopBits.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            TxtStopBits.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblHandshake.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;
            TxtHandshake.Visibility =
                jeCom ? Visibility.Visible : Visibility.Collapsed;

            LblHost.Visibility =
                jeCom ? Visibility.Collapsed : Visibility.Visible;
            TxtHost.Visibility =
                jeCom ? Visibility.Collapsed : Visibility.Visible;

            LblTcpPort.Visibility =
                jeCom ? Visibility.Collapsed : Visibility.Visible;
            TxtTcpPort.Visibility =
                jeCom ? Visibility.Collapsed : Visibility.Visible;
        }

        // =========================================================
        // Kopirovani nastaveni vah
        // =========================================================
        private void NaplnKopirovaniVah(int aktualniIndex)
        {
            CmbCopyFromScale.Items.Clear();

            for (int i = 1; i <= 5; i++)
            {
                if (i == aktualniIndex)
                    continue;

                CmbCopyFromScale.Items.Add(
                    new ComboBoxItem
                    {
                        Content = NactiNazevVahy(i),
                        Tag = i
                    });
            }

            if (CmbCopyFromScale.Items.Count > 0)
                CmbCopyFromScale.SelectedIndex = 0;
        }

        private void BtnCopyFromScale_Click(object sender, RoutedEventArgs e)
        {
            if (CmbCopyFromScale.SelectedItem is not ComboBoxItem item)
                return;

            int zdroj = (int)item.Tag;
            int cil = _selectedScaleIndex;

            KopirujNastaveniVahyBezComPortu(zdroj, cil);

            NactiComm(cil);

            ZobrazInfo(
                InfoBarSeverity.Success,
                "Kopírování",
                "Nastavení bylo zkopírováno.");
        }

        private void KopirujNastaveniVahyBezComPortu(
            int zdroj,
            int cil)
        {
            string comPort = NactiSerialPort(cil);

            UlozCommTyp(cil, NactiCommTyp(zdroj));

            UlozSerialBaud(cil, NactiSerialBaud(zdroj));
            UlozSerialParity(cil, NactiSerialParity(zdroj));
            UlozSerialDataBits(cil, NactiSerialDataBits(zdroj));
            UlozSerialStopBits(cil, NactiSerialStopBits(zdroj));
            UlozSerialHandshake(cil, NactiSerialHandshake(zdroj));

            UlozSerialHost(cil, NactiSerialHost(zdroj));
            UlozSerialTcpPort(cil, NactiSerialTcpPort(zdroj));

            UlozScaleAddress(cil, NactiScaleAddress(zdroj));
            UlozReadCommand(cil, NactiReadCommand(zdroj));
            UlozNewline(cil, NactiNewline(zdroj));

            UlozScaleAsync(cil, NactiScaleAsync(zdroj));

            UlozPollMs(cil, NactiPollMs(zdroj));
            UlozTimeoutMs(cil, NactiTimeoutMs(zdroj));

            UlozScaleFormat(cil, NactiScaleFormat(zdroj));
            UlozTokenDefs(cil, NactiTokenDefs(zdroj));
            UlozFieldMap(cil, NactiFieldMap(zdroj));

            UlozDisplayDivisor(
                cil,
                NactiDisplayDivisor(zdroj));

            UlozUnitsFallback(
                cil,
                NactiUnitsFallback(zdroj));

            UlozStatusMap(cil, NactiStatusMap(zdroj));
            UlozStatusTextMap(cil, NactiStatusTextMap(zdroj));

            UlozEnableAlibi(cil, NactiEnableAlibi(zdroj));

            UlozAlibiStoreCommand(
                cil,
                NactiAlibiStoreCommand(zdroj));

            UlozAlibiSuccessFormat(
                cil,
                NactiAlibiSuccessFormat(zdroj));

            UlozAlibiEchoFormat(
                cil,
                NactiAlibiEchoFormat(zdroj));

            UlozSerialPort(cil, comPort);
        }

        // =========================================================
        // COMM - uložení
        // =========================================================
        private void UlozComm(int index)
        {
            Settings.Param_ScaleIndex = index;

            UlozPovoleniVahy(index, ChkScaleEnabled.IsChecked == true);
            UlozNazevVahy(index, TxtScaleName.Text ?? $"Váha {index}");

            string typ =
                ((CmbCommType.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "COM").Trim();

            UlozCommTyp(index, typ);

            UlozSerialPort(index, CmbComPort.SelectedItem?.ToString() ?? "");
            UlozSerialBaud(index, ParseInt(TxtBaud.Text, 9600));
            UlozSerialParity(index, TxtParity.Text ?? "None");
            UlozSerialDataBits(index, ParseInt(TxtDataBits.Text, 8));
            UlozSerialStopBits(index, TxtStopBits.Text ?? "One");
            UlozSerialHandshake(index, TxtHandshake.Text ?? "None");

            UlozSerialHost(index, TxtHost.Text ?? "127.0.0.1");
            UlozSerialTcpPort(index, ParseInt(TxtTcpPort.Text, 502));

            UlozScaleAddress(index, TxtScaleAddress.Text ?? "");
            UlozReadCommand(index, TxtReadCommand.Text ?? "");
            UlozNewline(index, TxtNewline.Text ?? "");

            UlozScaleAsync(index, ChkScaleAsync.IsChecked == true);

            UlozPollMs(index, ParseInt(TxtPollMs.Text, 300));
            UlozTimeoutMs(index, ParseInt(TxtTimeoutMs.Text, 2000));

            UlozScaleFormat(index, TxtScaleFormat.Text ?? "");
            UlozTokenDefs(index, TxtTokenDefs.Text ?? "");
            UlozFieldMap(index, TxtFieldMap.Text ?? "");

            UlozDisplayDivisor(
                index,
                ParseDouble(TxtDisplayDivisor.Text, 1));

            UlozUnitsFallback(index, TxtUnitsFallback.Text ?? "kg");

            UlozStatusMap(index, TxtStatusMap.Text ?? "");
            UlozStatusTextMap(index, TxtStatusTextMap.Text ?? "");

            UlozEnableAlibi(index, ChkEnableAlibi.IsChecked == true);
            UlozAlibiStoreCommand(index, TxtAlibiStoreCommand.Text ?? "");
            UlozAlibiSuccessFormat(index, TxtAlibiSuccessFormat.Text ?? "");
            UlozAlibiEchoFormat(index, TxtAlibiEchoFormat.Text ?? "");

            NaplnSeznamVah();
            CmbScaleIndex.SelectedIndex = index - 1;
        }

        // =========================================================
        // UŽIVATELE
        // =========================================================
        private sealed class UserViewItem
        {
            public int ID { get; set; }
            public string Name { get; set; } = "";
            public string RoleName { get; set; } = "";
        }

        private void NactiUzivatele()
        {
            var (users, _) = tabUSERS.GetAllUserViews();

            UsersTable.ItemsSource = users
                .Where(u => u.IDuser != 1)
                .ToList();

            ZobrazInfo(
                InfoBarSeverity.Success,
                "Uživatelé",
                "Uživatelé byli načteni.");
        }

        private void BtnUserDelete_Click(object sender, RoutedEventArgs e)
        {
            if (UsersTable.SelectedItem is not tabUSERS.UserViewRow user)
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Uživatelé",
                    "Nejdříve označ uživatele v tabulce.");
                return;
            }

            bool ok = tabUSERS.DeleteUser(user.IDuser);

            if (ok)
            {
                NactiUzivatele();

                ZobrazInfo(
                    InfoBarSeverity.Success,
                    "Uživatelé",
                    $"Uživatel '{user.Username}' byl smazán.");
            }
            else
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Uživatelé",
                    "Uživatele se nepodařilo smazat.");
            }
        }

        private void BtnUserAdd_Click(object sender, RoutedEventArgs e)
        {
            UsersWindow window = new UsersWindow();

            window.Closed += UserWindow_Closed;

            ModalWindowService.Otevri(
                window,
                App.MainWindow);
        }

        private void BtnUserEdit_Click(object sender, RoutedEventArgs e)
        {
            if (UsersTable.SelectedItem is not tabUSERS.UserViewRow user)
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Uživatelé",
                    "Nejdříve označ uživatele v tabulce.");

                return;
            }

            UsersWindow window = new UsersWindow(user);

            window.Closed += UserWindow_Closed;

            ModalWindowService.Otevri(
                window,
                App.MainWindow);
        }

        private void UserWindow_Closed(object sender, WindowEventArgs args)
        {
            if (sender is UsersWindow window && window.Ulozeno)
            {
                NactiUzivatele();

                ZobrazInfo(
                    InfoBarSeverity.Success,
                    "Uživatelé",
                    "Změny uživatele byly uloženy.");
            }
        }


        // =========================================================
        // Pomocné převody
        // =========================================================

        private static int ParseInt(string text, int def)
        {
            return int.TryParse(text, out int value) ? value : def;
        }

        private static double ParseDouble(string text, double def)
        {
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
                ? value
                : def;
        }

        // =========================================================
        // COMM - načtení dostupných COM portů z Windows
        // =========================================================
        private void NaplnComPorty(string? vybranyPort)
        {
            CmbComPort.Items.Clear();

            string[] porty = SerialPort
                .GetPortNames()
                .OrderBy(p => p)
                .ToArray();

            foreach (string port in porty)
                CmbComPort.Items.Add(port);

            if (!string.IsNullOrWhiteSpace(vybranyPort) &&
                !CmbComPort.Items.Contains(vybranyPort))
            {
                CmbComPort.Items.Add(vybranyPort);
            }

            if (!string.IsNullOrWhiteSpace(vybranyPort))
                CmbComPort.SelectedItem = vybranyPort;
            else if (CmbComPort.Items.Count > 0)
                CmbComPort.SelectedIndex = 0;
        }


        // =========================================================
        // TESTOVACÍ TLAČÍTKA
        // =========================================================
        private void BtnTestMySql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Nejprve uložíme hodnoty z formuláře do Settings,
                // protože MySQL.CheckConnection() čte přímo ze Settings.
                UlozMySql();

                bool ok = MySQL.MySQL.CheckConnection();

                if (ok)
                {
                    ZobrazInfo(
                        InfoBarSeverity.Success,
                        "MySQL",
                        "Test spojení proběhl úspěšně.");
                }
                else
                {
                    ZobrazInfo(
                        InfoBarSeverity.Error,
                        "MySQL",
                        "Spojení s databází se nepodařilo navázat.");
                }
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "MySQL",
                    ex.Message);
            }
        }

        private async void BtnTestScale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string typ = ((CmbCommType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM").Trim();

                if (typ == "ETH")
                {
                    using TcpClient client = new TcpClient();

                    Task connectTask = client.ConnectAsync(
                        TxtHost.Text,
                        ParseInt(TxtTcpPort.Text, 502));

                    Task timeoutTask = Task.Delay(2000);

                    Task finishedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (finishedTask == timeoutTask)
                        throw new TimeoutException("Vypršel časový limit pro připojení k ETH váze.");

                    ZobrazInfo(
                        InfoBarSeverity.Success,
                        "Váha OK",
                        "ETH spojení s váhou bylo úspěšné.");
                }
                else
                {
                    using SerialPort sp = new SerialPort(
                        CmbComPort.SelectedItem?.ToString() ?? "",
                        ParseInt(TxtBaud.Text, 9600),
                        Enum.Parse<Parity>(TxtParity.Text),
                        ParseInt(TxtDataBits.Text, 8),
                        Enum.Parse<StopBits>(TxtStopBits.Text))
                    {
                        Handshake = Enum.Parse<Handshake>(TxtHandshake.Text),
                        ReadTimeout = 1000,
                        WriteTimeout = 1000
                    };

                    sp.Open();

                    if (sp.IsOpen)
                        sp.Close();

                    ZobrazInfo(
                        InfoBarSeverity.Success,
                        "Váha OK",
                        "COM port váhy byl úspěšně otevřen.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Chyba váhy",
                    "Přístup k COM portu byl odepřen. Port je pravděpodobně otevřen jinou aplikací.");
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Chyba váhy",
                    ex.Message);
            }
        }

        private void BtnTestAlibi_Click(object sender, RoutedEventArgs e)
        {
            ZobrazInfo(
                InfoBarSeverity.Informational,
                "Test alibi",
                "Test alibi bude napojený na komunikační klient váhy.");
        }


        // =========================================================
        // SYSTEM - EXPORT / IMPORT RECEPTUR
        // =========================================================
        private void BtnExportRecipes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool ok = RecipeExport.ExportRecipesAllDoCsv(
                    out string cesta,
                    out string chyba);

                if (ok)
                {
                    ZobrazInfo(
                        InfoBarSeverity.Success,
                        "Export receptur",
                        $"Export receptur byl úspěšně vytvořen.\n{cesta}");
                }
                else
                {
                    ZobrazInfo(
                        InfoBarSeverity.Error,
                        "Export receptur",
                        chyba);
                }
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Export receptur",
                    ex.Message);
            }
        }

        private async void BtnImportRecipes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileOpenPicker picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    ViewMode = PickerViewMode.List
                };

                picker.FileTypeFilter.Add(".csv");

                Window? window = App.MainWindow;

                if (window == null)
                {
                    ZobrazInfo(
                        InfoBarSeverity.Error,
                        "Import receptur",
                        "Nepodařilo se získat hlavní okno aplikace.");
                    return;
                }

                IntPtr hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile? file = await picker.PickSingleFileAsync();

                if (file == null)
                    return;

                ZobrazInfo(
                    InfoBarSeverity.Informational,
                    "Import receptur",
                    "Import receptur byl spuštěn.");

                string chyba = "";

                bool ok = await Task.Run(() =>
                {
                    return RecipeImport.ImportRecipesAllZCsv(
                        file.Path,
                        out chyba,
                        (procenta, text) =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                ZobrazInfo(
                                    InfoBarSeverity.Informational,
                                    $"Import receptur {procenta} %",
                                    text);
                            });
                        });
                });

                if (ok)
                {
                    ZobrazInfo(
                        InfoBarSeverity.Success,
                        "Import receptur",
                        $"Import receptur byl úspěšně dokončen.\n{file.Path}");
                }
                else
                {
                    ZobrazInfo(
                        InfoBarSeverity.Error,
                        "Import receptur",
                        chyba);
                }
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Import receptur",
                    ex.Message);
            }
        }

        // =========================================================
        // Ukončení programu
        // =========================================================
        private void BtnExit_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ConfirmWindow confirmWindow = new ConfirmWindow(
                "Ukončení programu",
                "Opravdu chcete ukončit aplikaci?");

            confirmWindow.Closed += (_, _) =>
            {
                if (!confirmWindow.Potvrzeno)
                    return;

                DispatcherQueue.TryEnqueue(() =>
                {
                    Application.Current.Exit();
                });
            };

            ModalWindowService.Otevri(
                confirmWindow,
                App.MainWindow);
        }

        // =========================================================
        // Pomocné metody - váhy
        // =========================================================

        private static bool NactiPovoleniVahy(int i) => i switch
        {
            1 => Settings.Param_ScaleEnabled1,
            2 => Settings.Param_ScaleEnabled2,
            3 => Settings.Param_ScaleEnabled3,
            4 => Settings.Param_ScaleEnabled4,
            5 => Settings.Param_ScaleEnabled5,
            _ => true
        };

        private static void UlozPovoleniVahy(int i, bool value)
        {
            switch (i)
            {
                case 1: Settings.Param_ScaleEnabled1 = value; break;
                case 2: Settings.Param_ScaleEnabled2 = value; break;
                case 3: Settings.Param_ScaleEnabled3 = value; break;
                case 4: Settings.Param_ScaleEnabled4 = value; break;
                case 5: Settings.Param_ScaleEnabled5 = value; break;
            }
        }

        private static string NactiNazevVahy(int i) => i switch
        {
            1 => Settings.Param_Scale_Name1,
            2 => Settings.Param_Scale_Name2,
            3 => Settings.Param_Scale_Name3,
            4 => Settings.Param_Scale_Name4,
            5 => Settings.Param_Scale_Name5,
            _ => $"Váha {i}"
        };

        private static void UlozNazevVahy(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_Name1 = value; break;
                case 2: Settings.Param_Scale_Name2 = value; break;
                case 3: Settings.Param_Scale_Name3 = value; break;
                case 4: Settings.Param_Scale_Name4 = value; break;
                case 5: Settings.Param_Scale_Name5 = value; break;
            }
        }

        private static string NactiCommTyp(int i) => i switch
        {
            1 => Settings.Param_Comm_Type1,
            2 => Settings.Param_Comm_Type2,
            3 => Settings.Param_Comm_Type3,
            4 => Settings.Param_Comm_Type4,
            5 => Settings.Param_Comm_Type5,
            _ => "COM"
        };

        private static void UlozCommTyp(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Comm_Type1 = value; break;
                case 2: Settings.Param_Comm_Type2 = value; break;
                case 3: Settings.Param_Comm_Type3 = value; break;
                case 4: Settings.Param_Comm_Type4 = value; break;
                case 5: Settings.Param_Comm_Type5 = value; break;
            }
        }

        private static string NactiSerialPort(int i) => i switch
        {
            1 => Settings.Param_Serial_Port1,
            2 => Settings.Param_Serial_Port2,
            3 => Settings.Param_Serial_Port3,
            4 => Settings.Param_Serial_Port4,
            5 => Settings.Param_Serial_Port5,
            _ => "COM1"
        };

        private static void UlozSerialPort(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_Port1 = value; break;
                case 2: Settings.Param_Serial_Port2 = value; break;
                case 3: Settings.Param_Serial_Port3 = value; break;
                case 4: Settings.Param_Serial_Port4 = value; break;
                case 5: Settings.Param_Serial_Port5 = value; break;
            }
        }

        private static int NactiSerialBaud(int i) => i switch
        {
            1 => Settings.Param_Serial_Baud1,
            2 => Settings.Param_Serial_Baud2,
            3 => Settings.Param_Serial_Baud3,
            4 => Settings.Param_Serial_Baud4,
            5 => Settings.Param_Serial_Baud5,
            _ => 9600
        };

        private static void UlozSerialBaud(int i, int value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_Baud1 = value; break;
                case 2: Settings.Param_Serial_Baud2 = value; break;
                case 3: Settings.Param_Serial_Baud3 = value; break;
                case 4: Settings.Param_Serial_Baud4 = value; break;
                case 5: Settings.Param_Serial_Baud5 = value; break;
            }
        }

        private static string NactiSerialParity(int i) => i switch
        {
            1 => Settings.Param_Serial_Parity1,
            2 => Settings.Param_Serial_Parity2,
            3 => Settings.Param_Serial_Parity3,
            4 => Settings.Param_Serial_Parity4,
            5 => Settings.Param_Serial_Parity5,
            _ => "None"
        };

        private static void UlozSerialParity(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_Parity1 = value; break;
                case 2: Settings.Param_Serial_Parity2 = value; break;
                case 3: Settings.Param_Serial_Parity3 = value; break;
                case 4: Settings.Param_Serial_Parity4 = value; break;
                case 5: Settings.Param_Serial_Parity5 = value; break;
            }
        }

        private static int NactiSerialDataBits(int i) => i switch
        {
            1 => Settings.Param_Serial_DataBits1,
            2 => Settings.Param_Serial_DataBits2,
            3 => Settings.Param_Serial_DataBits3,
            4 => Settings.Param_Serial_DataBits4,
            5 => Settings.Param_Serial_DataBits5,
            _ => 8
        };

        private static void UlozSerialDataBits(int i, int value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_DataBits1 = value; break;
                case 2: Settings.Param_Serial_DataBits2 = value; break;
                case 3: Settings.Param_Serial_DataBits3 = value; break;
                case 4: Settings.Param_Serial_DataBits4 = value; break;
                case 5: Settings.Param_Serial_DataBits5 = value; break;
            }
        }

        private static string NactiSerialStopBits(int i) => i switch
        {
            1 => Settings.Param_Serial_StopBits1,
            2 => Settings.Param_Serial_StopBits2,
            3 => Settings.Param_Serial_StopBits3,
            4 => Settings.Param_Serial_StopBits4,
            5 => Settings.Param_Serial_StopBits5,
            _ => "One"
        };

        private static void UlozSerialStopBits(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_StopBits1 = value; break;
                case 2: Settings.Param_Serial_StopBits2 = value; break;
                case 3: Settings.Param_Serial_StopBits3 = value; break;
                case 4: Settings.Param_Serial_StopBits4 = value; break;
                case 5: Settings.Param_Serial_StopBits5 = value; break;
            }
        }

        private static string NactiSerialHost(int i) => i switch
        {
            1 => Settings.Param_Serial_Host1,
            2 => Settings.Param_Serial_Host2,
            3 => Settings.Param_Serial_Host3,
            4 => Settings.Param_Serial_Host4,
            5 => Settings.Param_Serial_Host5,
            _ => "127.0.0.1"
        };

        private static void UlozSerialHost(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_Host1 = value; break;
                case 2: Settings.Param_Serial_Host2 = value; break;
                case 3: Settings.Param_Serial_Host3 = value; break;
                case 4: Settings.Param_Serial_Host4 = value; break;
                case 5: Settings.Param_Serial_Host5 = value; break;
            }
        }

        private static int NactiSerialTcpPort(int i) => i switch
        {
            1 => Settings.Param_Serial_TcpPort1,
            2 => Settings.Param_Serial_TcpPort2,
            3 => Settings.Param_Serial_TcpPort3,
            4 => Settings.Param_Serial_TcpPort4,
            5 => Settings.Param_Serial_TcpPort5,
            _ => 502
        };

        private static void UlozSerialTcpPort(int i, int value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_TcpPort1 = value; break;
                case 2: Settings.Param_Serial_TcpPort2 = value; break;
                case 3: Settings.Param_Serial_TcpPort3 = value; break;
                case 4: Settings.Param_Serial_TcpPort4 = value; break;
                case 5: Settings.Param_Serial_TcpPort5 = value; break;
            }
        }

        private static string NactiScaleAddress(int i) => i switch
        {
            1 => Settings.Param_Scale_Address1,
            2 => Settings.Param_Scale_Address2,
            3 => Settings.Param_Scale_Address3,
            4 => Settings.Param_Scale_Address4,
            5 => Settings.Param_Scale_Address5,
            _ => "01"
        };

        private static void UlozScaleAddress(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_Address1 = value; break;
                case 2: Settings.Param_Scale_Address2 = value; break;
                case 3: Settings.Param_Scale_Address3 = value; break;
                case 4: Settings.Param_Scale_Address4 = value; break;
                case 5: Settings.Param_Scale_Address5 = value; break;
            }
        }

        private static string NactiReadCommand(int i) => i switch
        {
            1 => Settings.Param_Scale_ReadCommand1,
            2 => Settings.Param_Scale_ReadCommand2,
            3 => Settings.Param_Scale_ReadCommand3,
            4 => Settings.Param_Scale_ReadCommand4,
            5 => Settings.Param_Scale_ReadCommand5,
            _ => "READ"
        };

        private static void UlozReadCommand(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_ReadCommand1 = value; break;
                case 2: Settings.Param_Scale_ReadCommand2 = value; break;
                case 3: Settings.Param_Scale_ReadCommand3 = value; break;
                case 4: Settings.Param_Scale_ReadCommand4 = value; break;
                case 5: Settings.Param_Scale_ReadCommand5 = value; break;
            }
        }

        private static string NactiNewline(int i) => i switch
        {
            1 => Settings.Param_Scale_Newline1,
            2 => Settings.Param_Scale_Newline2,
            3 => Settings.Param_Scale_Newline3,
            4 => Settings.Param_Scale_Newline4,
            5 => Settings.Param_Scale_Newline5,
            _ => "\\r\\n"
        };

        private static void UlozNewline(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_Newline1 = value; break;
                case 2: Settings.Param_Scale_Newline2 = value; break;
                case 3: Settings.Param_Scale_Newline3 = value; break;
                case 4: Settings.Param_Scale_Newline4 = value; break;
                case 5: Settings.Param_Scale_Newline5 = value; break;
            }
        }

        private static int NactiPollMs(int i) => i switch
        {
            1 => Settings.Param_Scale_PollMs1,
            2 => Settings.Param_Scale_PollMs2,
            3 => Settings.Param_Scale_PollMs3,
            4 => Settings.Param_Scale_PollMs4,
            5 => Settings.Param_Scale_PollMs5,
            _ => 300
        };

        private static void UlozPollMs(int i, int value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_PollMs1 = value; break;
                case 2: Settings.Param_Scale_PollMs2 = value; break;
                case 3: Settings.Param_Scale_PollMs3 = value; break;
                case 4: Settings.Param_Scale_PollMs4 = value; break;
                case 5: Settings.Param_Scale_PollMs5 = value; break;
            }
        }

        private static int NactiTimeoutMs(int i) => i switch
        {
            1 => Settings.Param_Scale_TimeoutMs1,
            2 => Settings.Param_Scale_TimeoutMs2,
            3 => Settings.Param_Scale_TimeoutMs3,
            4 => Settings.Param_Scale_TimeoutMs4,
            5 => Settings.Param_Scale_TimeoutMs5,
            _ => 2000
        };

        private static void UlozTimeoutMs(int i, int value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_TimeoutMs1 = value; break;
                case 2: Settings.Param_Scale_TimeoutMs2 = value; break;
                case 3: Settings.Param_Scale_TimeoutMs3 = value; break;
                case 4: Settings.Param_Scale_TimeoutMs4 = value; break;
                case 5: Settings.Param_Scale_TimeoutMs5 = value; break;
            }
        }

        private static string NactiScaleFormat(int i) => i switch
        {
            1 => Settings.Param_Scale_Format1,
            2 => Settings.Param_Scale_Format2,
            3 => Settings.Param_Scale_Format3,
            4 => Settings.Param_Scale_Format4,
            5 => Settings.Param_Scale_Format5,
            _ => ""
        };

        private static void UlozScaleFormat(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_Format1 = value; break;
                case 2: Settings.Param_Scale_Format2 = value; break;
                case 3: Settings.Param_Scale_Format3 = value; break;
                case 4: Settings.Param_Scale_Format4 = value; break;
                case 5: Settings.Param_Scale_Format5 = value; break;
            }
        }

        private static string NactiTokenDefs(int i) => i switch
        {
            1 => Settings.Param_Scale_TokenDefs1,
            2 => Settings.Param_Scale_TokenDefs2,
            3 => Settings.Param_Scale_TokenDefs3,
            4 => Settings.Param_Scale_TokenDefs4,
            5 => Settings.Param_Scale_TokenDefs5,
            _ => ""
        };

        private static void UlozTokenDefs(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_TokenDefs1 = value; break;
                case 2: Settings.Param_Scale_TokenDefs2 = value; break;
                case 3: Settings.Param_Scale_TokenDefs3 = value; break;
                case 4: Settings.Param_Scale_TokenDefs4 = value; break;
                case 5: Settings.Param_Scale_TokenDefs5 = value; break;
            }
        }

        private static string NactiFieldMap(int i) => i switch
        {
            1 => Settings.Param_Scale_FieldMap1,
            2 => Settings.Param_Scale_FieldMap2,
            3 => Settings.Param_Scale_FieldMap3,
            4 => Settings.Param_Scale_FieldMap4,
            5 => Settings.Param_Scale_FieldMap5,
            _ => ""
        };

        private static void UlozFieldMap(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_FieldMap1 = value; break;
                case 2: Settings.Param_Scale_FieldMap2 = value; break;
                case 3: Settings.Param_Scale_FieldMap3 = value; break;
                case 4: Settings.Param_Scale_FieldMap4 = value; break;
                case 5: Settings.Param_Scale_FieldMap5 = value; break;
            }
        }

        private static double NactiDisplayDivisor(int i) => i switch
        {
            1 => Settings.Param_Scale_DisplayDivisor1,
            2 => Settings.Param_Scale_DisplayDivisor2,
            3 => Settings.Param_Scale_DisplayDivisor3,
            4 => Settings.Param_Scale_DisplayDivisor4,
            5 => Settings.Param_Scale_DisplayDivisor5,
            _ => 1.0
        };

        private static void UlozDisplayDivisor(int i, double value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_DisplayDivisor1 = value; break;
                case 2: Settings.Param_Scale_DisplayDivisor2 = value; break;
                case 3: Settings.Param_Scale_DisplayDivisor3 = value; break;
                case 4: Settings.Param_Scale_DisplayDivisor4 = value; break;
                case 5: Settings.Param_Scale_DisplayDivisor5 = value; break;
            }
        }

        private static string NactiUnitsFallback(int i) => i switch
        {
            1 => Settings.Param_Scale_UnitsFallback1,
            2 => Settings.Param_Scale_UnitsFallback2,
            3 => Settings.Param_Scale_UnitsFallback3,
            4 => Settings.Param_Scale_UnitsFallback4,
            5 => Settings.Param_Scale_UnitsFallback5,
            _ => "kg"
        };

        private static void UlozUnitsFallback(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_UnitsFallback1 = value; break;
                case 2: Settings.Param_Scale_UnitsFallback2 = value; break;
                case 3: Settings.Param_Scale_UnitsFallback3 = value; break;
                case 4: Settings.Param_Scale_UnitsFallback4 = value; break;
                case 5: Settings.Param_Scale_UnitsFallback5 = value; break;
            }
        }

        private static string NactiStatusMap(int i) => i switch
        {
            1 => Settings.Param_Scale_StatusMap1,
            2 => Settings.Param_Scale_StatusMap2,
            3 => Settings.Param_Scale_StatusMap3,
            4 => Settings.Param_Scale_StatusMap4,
            5 => Settings.Param_Scale_StatusMap5,
            _ => ""
        };

        private static void UlozStatusMap(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_StatusMap1 = value; break;
                case 2: Settings.Param_Scale_StatusMap2 = value; break;
                case 3: Settings.Param_Scale_StatusMap3 = value; break;
                case 4: Settings.Param_Scale_StatusMap4 = value; break;
                case 5: Settings.Param_Scale_StatusMap5 = value; break;
            }
        }

        private static string NactiStatusTextMap(int i) => i switch
        {
            1 => Settings.Param_Scale_StatusTextMap1,
            2 => Settings.Param_Scale_StatusTextMap2,
            3 => Settings.Param_Scale_StatusTextMap3,
            4 => Settings.Param_Scale_StatusTextMap4,
            5 => Settings.Param_Scale_StatusTextMap5,
            _ => ""
        };

        private static void UlozStatusTextMap(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_StatusTextMap1 = value; break;
                case 2: Settings.Param_Scale_StatusTextMap2 = value; break;
                case 3: Settings.Param_Scale_StatusTextMap3 = value; break;
                case 4: Settings.Param_Scale_StatusTextMap4 = value; break;
                case 5: Settings.Param_Scale_StatusTextMap5 = value; break;
            }
        }

        private static bool NactiEnableAlibi(int i) => i switch
        {
            1 => Settings.Param_Scale_EnableAlibi1,
            2 => Settings.Param_Scale_EnableAlibi2,
            3 => Settings.Param_Scale_EnableAlibi3,
            4 => Settings.Param_Scale_EnableAlibi4,
            5 => Settings.Param_Scale_EnableAlibi5,
            _ => false
        };

        private static void UlozEnableAlibi(int i, bool value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_EnableAlibi1 = value; break;
                case 2: Settings.Param_Scale_EnableAlibi2 = value; break;
                case 3: Settings.Param_Scale_EnableAlibi3 = value; break;
                case 4: Settings.Param_Scale_EnableAlibi4 = value; break;
                case 5: Settings.Param_Scale_EnableAlibi5 = value; break;
            }
        }

        private static string NactiAlibiStoreCommand(int i) => i switch
        {
            1 => Settings.Param_Scale_AlibiStoreCommand1,
            2 => Settings.Param_Scale_AlibiStoreCommand2,
            3 => Settings.Param_Scale_AlibiStoreCommand3,
            4 => Settings.Param_Scale_AlibiStoreCommand4,
            5 => Settings.Param_Scale_AlibiStoreCommand5,
            _ => ""
        };

        private static void UlozAlibiStoreCommand(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_AlibiStoreCommand1 = value; break;
                case 2: Settings.Param_Scale_AlibiStoreCommand2 = value; break;
                case 3: Settings.Param_Scale_AlibiStoreCommand3 = value; break;
                case 4: Settings.Param_Scale_AlibiStoreCommand4 = value; break;
                case 5: Settings.Param_Scale_AlibiStoreCommand5 = value; break;
            }
        }

        private static string NactiAlibiSuccessFormat(int i) => i switch
        {
            1 => Settings.Param_Scale_AlibiSuccessFormat1,
            2 => Settings.Param_Scale_AlibiSuccessFormat2,
            3 => Settings.Param_Scale_AlibiSuccessFormat3,
            4 => Settings.Param_Scale_AlibiSuccessFormat4,
            5 => Settings.Param_Scale_AlibiSuccessFormat5,
            _ => ""
        };

        private static void UlozAlibiSuccessFormat(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_AlibiSuccessFormat1 = value; break;
                case 2: Settings.Param_Scale_AlibiSuccessFormat2 = value; break;
                case 3: Settings.Param_Scale_AlibiSuccessFormat3 = value; break;
                case 4: Settings.Param_Scale_AlibiSuccessFormat4 = value; break;
                case 5: Settings.Param_Scale_AlibiSuccessFormat5 = value; break;
            }
        }

        private static string NactiAlibiEchoFormat(int i) => i switch
        {
            1 => Settings.Param_Scale_AlibiEchoFormat1,
            2 => Settings.Param_Scale_AlibiEchoFormat2,
            3 => Settings.Param_Scale_AlibiEchoFormat3,
            4 => Settings.Param_Scale_AlibiEchoFormat4,
            5 => Settings.Param_Scale_AlibiEchoFormat5,
            _ => ""
        };

        private static void UlozAlibiEchoFormat(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_AlibiEchoFormat1 = value; break;
                case 2: Settings.Param_Scale_AlibiEchoFormat2 = value; break;
                case 3: Settings.Param_Scale_AlibiEchoFormat3 = value; break;
                case 4: Settings.Param_Scale_AlibiEchoFormat4 = value; break;
                case 5: Settings.Param_Scale_AlibiEchoFormat5 = value; break;
            }
        }

        private static string NactiSerialHandshake(int i) => i switch
        {
            1 => Settings.Param_Serial_Handshake1,
            2 => Settings.Param_Serial_Handshake2,
            3 => Settings.Param_Serial_Handshake3,
            4 => Settings.Param_Serial_Handshake4,
            5 => Settings.Param_Serial_Handshake5,
            _ => "None"
        };

        private static void UlozSerialHandshake(int i, string value)
        {
            switch (i)
            {
                case 1: Settings.Param_Serial_Handshake1 = value; break;
                case 2: Settings.Param_Serial_Handshake2 = value; break;
                case 3: Settings.Param_Serial_Handshake3 = value; break;
                case 4: Settings.Param_Serial_Handshake4 = value; break;
                case 5: Settings.Param_Serial_Handshake5 = value; break;
            }
        }

        private static bool NactiScaleAsync(int i) => i switch
        {
            1 => Settings.Param_Scale_Async1,
            2 => Settings.Param_Scale_Async2,
            3 => Settings.Param_Scale_Async3,
            4 => Settings.Param_Scale_Async4,
            5 => Settings.Param_Scale_Async5,
            _ => false
        };

        private static void UlozScaleAsync(int i, bool value)
        {
            switch (i)
            {
                case 1: Settings.Param_Scale_Async1 = value; break;
                case 2: Settings.Param_Scale_Async2 = value; break;
                case 3: Settings.Param_Scale_Async3 = value; break;
                case 4: Settings.Param_Scale_Async4 = value; break;
                case 5: Settings.Param_Scale_Async5 = value; break;
            }
        }



    }
}
