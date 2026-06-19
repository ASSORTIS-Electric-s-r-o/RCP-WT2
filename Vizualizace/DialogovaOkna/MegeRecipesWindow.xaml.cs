using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Windows.Graphics;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class MegeRecipesWindow : Window
    {
        public ObservableCollection<MergeRecipeInputRow> Receptury { get; } = new();

        public bool Potvrzeno { get; private set; } = false;

        public string JednotkaText { get; private set; } = "ks";

        public List<MergeRecipeInputRow> Vysledek =>
            Receptury.ToList();

        internal MegeRecipesWindow(
            IEnumerable<tabRECIPES.RecipeDetailRow> vybraneReceptury)
        {
            InitializeComponent();

            List<tabRECIPES.RecipeDetailRow> receptury =
                vybraneReceptury?.ToList()
                ?? new List<tabRECIPES.RecipeDetailRow>();

            bool jeKg =
                Settings.Param_Units == 0
                || receptury.Any(x =>
                    x.GroupIsZaklad > 0
                    || x.MaterialIsZaklad > 0);

            JednotkaText = jeKg ? "kg" : "ks";

            JednotkaText = jeKg ? "kg" : "ks";

            TxtMnozstviHeader.Text = $"Množství receptu ({JednotkaText})";

            RecipesMergeTable.ItemsSource = Receptury;

            foreach (tabRECIPES.RecipeDetailRow r in receptury)
            {
                Receptury.Add(new MergeRecipeInputRow
                {
                    IDrcp = r.IDrcp,

                    RecipeName =
                        string.IsNullOrWhiteSpace(r.RecipeCislo)
                            ? r.RecipeName ?? ""
                            : $"{r.RecipeCislo}-{r.RecipeName}",

                    Velikost = "0",
                    Jednotka = JednotkaText
                });
            }

            AktualizujTlacitkoPokracovat();
            NastavOkno();
        }

        private void NastavOkno()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            AppWindow.Resize(new SizeInt32(760, 500));
            VycentrujNaHlavniOkno();

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
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

        private void RecipesMergeTable_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not MergeRecipeInputRow row)
                return;

            OtevriZadaniVelikosti(row);
        }

        private void OtevriZadaniVelikosti(MergeRecipeInputRow row)
        {
            VirtualKeyboard.KeyboardMode rezim =
                JednotkaText == "kg"
                    ? VirtualKeyboard.KeyboardMode.Float
                    : VirtualKeyboard.KeyboardMode.Int;

            VirtualKeyboard keyboard = new VirtualKeyboard(
                rezim,
                row.Velikost ?? "",
                $"Zadejte velikost receptury ({JednotkaText})");

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                row.Velikost =
                    string.IsNullOrWhiteSpace(keyboard.Vysledek)
                        ? "0"
                        : keyboard.Vysledek;

                AktualizujTlacitkoPokracovat();

                Activate();
            };

            ModalWindowService.Otevri(
                keyboard,
                this,
                false);
        }

        private async void OdebratRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not MergeRecipeInputRow row)
                return;

            ContentDialogResult result = await ZobrazPotvrzeniAsync(
                "Odebrat recepturu ze sloučení?",
                row.RecipeName);

            if (result != ContentDialogResult.Primary)
                return;

            Receptury.Remove(row);

            AktualizujTlacitkoPokracovat();

            if (Receptury.Count < 2)
            {
                await ZobrazZpravuAsync(
                    "Sloučení receptur",
                    "Pro sloučení musí zůstat alespoň dvě receptury.");
            }
        }

        private void AktualizujTlacitkoPokracovat()
        {
            bool vseVyplneno =
                Receptury.Count >= 2
                && Receptury.All(r =>
                {
                    string text = (r.Velikost ?? "0").Replace(',', '.');

                    return double.TryParse(
                               text,
                               NumberStyles.Any,
                               CultureInfo.InvariantCulture,
                               out double value)
                           && value > 0;
                });

            BtnPokracovat.IsEnabled = vseVyplneno;
        }

        private async void BtnPokracovat_Click(object sender, RoutedEventArgs e)
        {
            if (Receptury.Count < 2)
            {
                await ZobrazZpravuAsync(
                    "Sloučení receptur",
                    "Pro sloučení musí být vybrány alespoň dvě receptury.");

                return;
            }

            foreach (MergeRecipeInputRow r in Receptury)
            {
                string text = (r.Velikost ?? "0").Replace(',', '.');

                if (!double.TryParse(
                        text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double value)
                    || value <= 0)
                {
                    await ZobrazZpravuAsync(
                        "Sloučení receptur",
                        "Velikost receptury musí být větší než 0.");

                    return;
                }

                r.VelikostValue = value;
            }

            MainWindow.AppFrame?.Navigate(
                typeof(RecipeDetailPage),
                new RecipeDetailArgs
                {
                    Mode = RecipeDetailPage.EditMode.MergeRecipes,
                    MergeRecipes = Receptury.ToList()
                });

            Close();
        }

        private void BtnZrusit_Click(object sender, RoutedEventArgs e)
        {
            Potvrzeno = false;
            Close();
        }

        private async System.Threading.Tasks.Task<ContentDialogResult> ZobrazPotvrzeniAsync(
            string titulek,
            string zprava)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = titulek,
                Content = zprava,
                PrimaryButtonText = "Ano",
                CloseButtonText = "Ne",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = RootGrid.XamlRoot
            };

            return await dialog.ShowAsync();
        }

        private async System.Threading.Tasks.Task ZobrazZpravuAsync(
            string titulek,
            string zprava)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = titulek,
                Content = zprava,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = RootGrid.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    public sealed class MergeRecipeInputRow : INotifyPropertyChanged
    {
        private string _velikost = "0";

        public int IDrcp { get; set; }

        public int IDjob { get; set; } = 0;

        public int Status { get; set; } = 0;

        public string RecipeName { get; set; } = "";

        public string Velikost
        {
            get => _velikost;
            set
            {
                if (_velikost == value)
                    return;

                _velikost = value;

                OnPropertyChanged(nameof(Velikost));
            }
        }

        public string Jednotka { get; set; } = "ks";

        public double VelikostValue { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}