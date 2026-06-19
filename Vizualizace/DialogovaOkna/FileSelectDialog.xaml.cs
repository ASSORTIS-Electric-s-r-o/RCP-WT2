using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class FileSelectDialog : Window
    {
        // ==========================================
        // Výsledek dialogu
        // ==========================================

        public string SelectedPath { get; private set; } = "";

        public bool Potvrzeno { get; private set; } = false;

        private readonly string _dialogTitle;
        private readonly string[] _extensions;

        // ==========================================
        // Konstruktor
        // ==========================================

        public FileSelectDialog(
            string prompt,
            string defaultPath = "",
            string dialogTitle = "Vyber soubor",
            params string[] extensions)
        {
            InitializeComponent();

            _dialogTitle = string.IsNullOrWhiteSpace(dialogTitle)
                ? "Vyber soubor"
                : dialogTitle;

            _extensions = extensions != null && extensions.Length > 0
                ? extensions
                : new[] { ".pdf" };

            PromptTextBlock.Text = string.IsNullOrWhiteSpace(prompt)
                ? "Vyber cestu k souboru:"
                : prompt;

            PathTextBox.Text = defaultPath ?? "";
            SelectedPath = defaultPath ?? "";

            NastavOkno();
        }

        // ==========================================
        // Nastavení okna
        // ==========================================

        private void NastavOkno()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            AppWindow.Resize(new SizeInt32(760, 300));
            VycentrujNaHlavniOkno();

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }

        // ==========================================
        // Vycentrování na hlavní okno
        // ==========================================

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

        // ==========================================
        // Výběr souboru
        // ==========================================

        private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };

            foreach (string ext in _extensions)
            {
                if (!string.IsNullOrWhiteSpace(ext))
                    picker.FileTypeFilter.Add(ext.StartsWith(".") ? ext : "." + ext);
            }

            nint hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();

            if (file == null)
                return;

            PathTextBox.Text = file.Path ?? "";
            SelectedPath = file.Path ?? "";
        }

        // ==========================================
        // Smazání vybrané cesty
        // ==========================================

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            PathTextBox.Text = "";
            SelectedPath = "";
        }

        // ==========================================
        // Přeskočení výběru
        // ==========================================

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            SelectedPath = "";
            Potvrzeno = true;
            Close();
        }

        // ==========================================
        // Potvrzení výběru
        // ==========================================

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedPath = PathTextBox.Text?.Trim() ?? "";
            Potvrzeno = true;
            Close();
        }
    }
}