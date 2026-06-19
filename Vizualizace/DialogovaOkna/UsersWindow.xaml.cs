using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using RCP_WT1.MySQL;
using RCP_WT1.Vizualizace.Klavesnice;
using System.Linq;
using Windows.Graphics;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class UsersWindow : Window
    {
        public bool Ulozeno { get; private set; } = false;

        private readonly tabUSERS.UserViewRow? _editUser;

        internal UsersWindow(tabUSERS.UserViewRow? editUser = null)
        {
            InitializeComponent();

            _editUser = editUser;

            Title = editUser == null ? "Nový uživatel" : "Upravit uživatele";
            TxtTitle.Text = editUser == null ? "Nový uživatel" : "Upravit uživatele";
            TxtDescription.Text = editUser == null
                ? "Vytvoření nového operátora aplikace"
                : "Úprava uloženého operátora aplikace";

            AppWindow.Resize(new SizeInt32(500, 460));

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            VycentrujOkno();

            RootGrid.Loaded += UsersWindow_Loaded;

            NactiRole();
            NactiEditovanehoUzivatele();
        }

        private void UsersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DotykovaKlavesniceService.Pripoj(
                RootGrid,
                ownerWindow: this);
        }

        private void VycentrujOkno()
        {
            DisplayArea displayArea =
                DisplayArea.GetFromWindowId(
                    AppWindow.Id,
                    DisplayAreaFallback.Primary);

            RectInt32 area = displayArea.WorkArea;
            SizeInt32 size = AppWindow.Size;

            int x = area.X + (area.Width - size.Width) / 2;
            int y = area.Y + (area.Height - size.Height) / 2;

            AppWindow.Move(new PointInt32(x, y));
        }

        private void NactiRole()
        {
            var (roles, _) = tabUSERS.GetAllRoles();

            CmbRole.ItemsSource = roles;

            if (roles.Count > 0)
                CmbRole.SelectedIndex = 0;
        }

        private void NactiEditovanehoUzivatele()
        {
            if (_editUser == null)
                return;

            TxtUsername.Text = _editUser.Username;
            TxtPassword.Password = _editUser.PasswordHash;

            if (CmbRole.ItemsSource is System.Collections.Generic.List<tabUSERS.RoleRow> roles)
            {
                CmbRole.SelectedItem = roles.FirstOrDefault(r => r.IDrole == _editUser.IDrole);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                ZobrazInfo(InfoBarSeverity.Warning, "Uživatel", "Jméno uživatele nesmí být prázdné.");
                return;
            }

            if (CmbRole.SelectedItem is not tabUSERS.RoleRow role)
            {
                ZobrazInfo(InfoBarSeverity.Warning, "Uživatel", "Vyber roli uživatele.");
                return;
            }

            if (_editUser == null)
                UlozNovehoUzivatele(role);
            else
                UlozUpravenehoUzivatele(role);
        }

        private void UlozNovehoUzivatele(tabUSERS.RoleRow role)
        {
            int id = tabUSERS.InsertUser(new tabUSERS.UserRow
            {
                Username = TxtUsername.Text.Trim(),
                PasswordHash = TxtPassword.Password ?? "",
                IDrole = role.IDrole,
                IsDeleted = 0
            });

            if (id <= 0)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Uživatel", "Uživatele se nepodařilo přidat.");
                return;
            }

            Ulozeno = true;
            Close();
        }

        private void UlozUpravenehoUzivatele(tabUSERS.RoleRow role)
        {
            bool ok = tabUSERS.UpdateUser(new tabUSERS.UserRow
            {
                IDuser = _editUser!.IDuser,
                Username = TxtUsername.Text.Trim(),
                PasswordHash = TxtPassword.Password ?? "",
                IDrole = role.IDrole,
                IsDeleted = _editUser.IsDeleted
            });

            if (!ok)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Uživatel", "Uživatele se nepodařilo upravit.");
                return;
            }

            Ulozeno = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Ulozeno = false;
            Close();
        }

        private void ZobrazInfo(InfoBarSeverity severity, string title, string message)
        {
            InfoStatus.Severity = severity;
            InfoStatus.Title = title;
            InfoStatus.Message = message;
            InfoStatus.IsOpen = true;
        }
    }
}