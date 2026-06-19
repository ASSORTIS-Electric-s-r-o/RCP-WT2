using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Linq;
using Windows.Graphics;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class LoginWindow : Window
    {
        // ==========================================
        // Stav úspěšného přihlášení
        // ==========================================
        public bool Prihlaseno { get; private set; } = false;

        // ==========================================
        // Timer pro automatické skrytí informační lišty
        // ==========================================
        private DispatcherQueueTimer? _infoTimer;

        // ==========================================
        // Konstruktor přihlašovacího okna
        // ==========================================
        public LoginWindow(string? preselectedUsername = null)
        {
            InitializeComponent();

            // Výchozí menší velikost bez informační lišty.
            AppWindow.Resize(new SizeInt32(460, 390));

            // Zakázání minimalizace, maximalizace a změny velikosti okna.
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
            }

            // Použití vlastního horního pruhu pro přesun okna.
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            VycentrujOkno();
            NactiUzivatele(preselectedUsername);

            // Připojení dotykové klávesnice ke všem vstupním prvkům v okně.
            DotykovaKlavesniceService.Pripoj(Content, ownerWindow: this);
        }

        // ==========================================
        // Vycentrování okna na aktuální pracovní plochu
        // ==========================================
        private void VycentrujOkno()
        {
            DisplayArea displayArea = DisplayArea.GetFromWindowId(
                AppWindow.Id,
                DisplayAreaFallback.Primary);

            RectInt32 area = displayArea.WorkArea;
            SizeInt32 size = AppWindow.Size;

            int x = area.X + (area.Width - size.Width) / 2;
            int y = area.Y + (area.Height - size.Height) / 2;

            AppWindow.Move(new PointInt32(x, y));
        }

        // ==========================================
        // Načtení seznamu uživatelů do ComboBoxu
        // ==========================================
        private void NactiUzivatele(string? preselectedUsername)
        {
            var (users, _) = tabUSERS.GetAllUserViews();

            var usernames = users
                .Select(u => u.Username)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            UsernameComboBox.ItemsSource = usernames;

            // Pokud je předán konkrétní uživatel, pokusíme se ho rovnou vybrat.
            if (!string.IsNullOrWhiteSpace(preselectedUsername) &&
                usernames.Contains(preselectedUsername))
            {
                UsernameComboBox.SelectedItem = preselectedUsername;
                return;
            }

            // Pokud není předán konkrétní uživatel, vybere se první dostupný.
            if (usernames.Count > 0)
                UsernameComboBox.SelectedIndex = 0;
        }

        // ==========================================
        // Kliknutí na tlačítko Přihlásit
        // ==========================================
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameComboBox.SelectedItem?.ToString() ?? "";
            string password = PasswordBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Přihlášení",
                    "Vyberte operátora.");

                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Přihlášení",
                    "Zadejte heslo.");

                return;
            }

            var user = tabUSERS.Authenticate(username, password);

            if (user == null)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Přihlášení",
                    "Neplatné jméno nebo heslo.");

                return;
            }

            UserSession.Login(user);

            Prihlaseno = true;
            Close();
        }

        // ==========================================
        // Kliknutí na tlačítko Zrušit
        // ==========================================
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Prihlaseno = false;
            Close();
        }

        // ==========================================
        // Zobrazení informační lišty na omezený čas
        // Po zobrazení se okno zvětší.
        // Po skrytí se opět vrátí na původní velikost.
        // ==========================================
        private void ZobrazInfo(
            InfoBarSeverity severity,
            string title,
            string message)
        {
            InfoStatus.Severity = severity;
            InfoStatus.Title = title;
            InfoStatus.Message = message;
            InfoStatus.IsOpen = true;

            // Zvětšení okna, aby se informační lišta vešla
            // a neposouvala formulář mimo viditelnou část.
            AppWindow.Resize(new SizeInt32(460, 460));
            VycentrujOkno();

            _infoTimer?.Stop();

            _infoTimer = DispatcherQueue.CreateTimer();
            _infoTimer.Interval = TimeSpan.FromSeconds(4);

            _infoTimer.Tick += (_, _) =>
            {
                _infoTimer?.Stop();

                InfoStatus.IsOpen = false;

                // Návrat na výchozí menší velikost bez informační lišty.
                AppWindow.Resize(new SizeInt32(460, 390));
                VycentrujOkno();
            };

            _infoTimer.Start();
        }
    }
}