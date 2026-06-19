using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.SerialComm;
using RCP_WT1.Vizualizace.DialogovaOkna;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class RecipeActualPage : Page
    {
        // ==========================================
        // Základní proměnné stránky
        // ==========================================

        // První karta, která je aktuálně viditelná vlevo v carouselu.
        private int _carouselFirstVisibleIndex = 0;

        private int _jobID;
        private int _batchNoIndex;
        private int _jobNumberBatch;
        private int _selectedIndex = 0;
        private int _selectedTenzoIndex = 0;

        private RecipeCard? _activeCard;
        private string? _pdfProcedurePath;
        private bool _carouselPointerDown = false;
        private double _carouselPointerStartX = 0;

        private SerialScaleClient? _attachedScale;

        private readonly ObservableCollection<RecipeCard> _cards = new();
        private DateTime _lastRenderTime = DateTime.MinValue;

        // ==========================================
        // Systémové WinUI barvy
        // ==========================================
        private Brush SystemTextBrush =>
            (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

        private Brush SystemSubTextBrush =>
            (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

        private Brush SystemBorderBrush =>
            (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        // ==========================================
        // Jednotky
        // ==========================================

        private string JednotkaText =>
            Settings.Param_Units == 0 ? "kg" : "ks";

        // ==========================================
        // Konstruktor
        // ==========================================
        public RecipeActualPage()
        {
            InitializeComponent();

            Loaded += RecipeActual_Loaded;
            Unloaded += RecipeActual_Unloaded;
        }

        // ==========================================
        // Převzetí parametrů z navigace
        // ==========================================
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RecipeActualArgs args)
            {
                _jobID = args.IDjob;
                _batchNoIndex = args.BatchIndex <= 0 ? 1 : args.BatchIndex;
                return;
            }

            if (e.Parameter is int idJob)
            {
                _jobID = idJob;
                _batchNoIndex = 1;
                return;
            }

            _jobID = 0;
            _batchNoIndex = 1;
        }

        // ==========================================
        // Loaded stránky
        // =========================================
        private void RecipeActual_Loaded(object sender, RoutedEventArgs e)
        {
            AktualizujOperatora();

            int storedIndex = Settings.Param_ScaleIndex;

            if (storedIndex < 1 || storedIndex > 5)
                storedIndex = 1;

            _selectedTenzoIndex = storedIndex - 1;

            HighlightSelectedWeightButton();

            if (Application.Current is App app)
            {
                app.ScaleChanged += OnScaleChanged;
                AttachScale(app.Scale);
            }

            SetWeightButtonsVisibility();
            ApplyScaleNamesToButtons();

            if (_jobID > 0)
            {
                LoadJobData(_jobID, _batchNoIndex);
            }
            else
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Vážení",
                    "Nebyla předána platná zakázka.");
            }

            DispatcherQueue.TryEnqueue(SafeRenderCarousel);
        }

        // ==========================================
        // Unloaded stránky – odpojení událostí
        // ==========================================
        private void RecipeActual_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current is App app)
                    app.ScaleChanged -= OnScaleChanged;

                if (_attachedScale != null)
                    _attachedScale.Updated -= ScaleUpdated;
            }
            catch
            {
                // Odpojení událostí nesmí shodit stránku při navigaci.
            }
        }

        // ==========================================
        // Načtení dat zakázky a vytvoření karet
        // ==========================================
        private void LoadJobData(int jobId, int batchNoIndex)
        {
            var (rows, count) = tabPRODUCTION.GetProductionViewByID_IDX(jobId, batchNoIndex);

            _cards.Clear();

            if (rows == null || rows.Count == 0)
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Zakázka",
                    "Pro vybranou zakázku nebyly nalezeny žádné řádky výroby.");

                RenderCarousel();
                return;
            }

            var first = rows.First();

            _jobID = first.IDjob;
            _batchNoIndex = first.BatchNoIndex;
            _jobNumberBatch = first.JobNumberBatch;

            RecipeName_Label.Text = first.IDmrg > 0
                ? "Sloučená receptura"
                : string.IsNullOrWhiteSpace(first.RecipeCislo)
                    ? first.RecipeName
                    : $"{first.RecipeCislo} - {first.RecipeName}";

            VelikostVarky_Label.Text = $"{first.JobAmountPcs:0.###} {JednotkaText}";
            CisloVarky_Label.Text = $"{first.BatchNoIndex} / {first.JobNumberBatch}";
            JobNo_Label.Text = first.JobNo;
            BatchNo_Label.Text = first.BatchNo;

            _pdfProcedurePath = first.PdfProcedurePath;

            foreach (var row in rows)
            {
                _cards.Add(new RecipeCard
                {
                    IDprod = row.IDprod,
                    IDjob = row.IDjob,
                    IDrcp = row.IDrcp,
                    IDmat = row.IDmat,
                    IDpc = row.IDpc,
                    MaterialCislo = row.MaterialCislo,
                    MaterialDesc = row.MaterialName,
                    RecipeMatCislo = row.RecipeMatCislo,
                    BatchNo = row.BatchNo,
                    BatchNoIdx = row.BatchNoIndex,
                    Navazeno = 0,
                    Zbyva = row.HmotnostPozadovana,
                    HmotnostPozadovana = row.HmotnostPozadovana,
                    HmotnostNavazena = row.HmotnostNavazena,
                    Tolerance = row.Tolerance,
                    Vazit = row.MaterialVazit,
                    Status = row.Status,
                    StatusName = row.StatusName,
                    IsActive = false
                });
            }

            int foundIndex = _cards.ToList().FindIndex(c => c.Status == 1);

            if (foundIndex == -1)
                foundIndex = _cards.ToList().FindIndex(c => c.Status == 0);

            if (foundIndex != -1)
            {
                for (int i = 0; i < _cards.Count; i++)
                    _cards[i].IsActive = i == foundIndex;

                _selectedIndex = foundIndex;
                _activeCard = _cards[foundIndex];
                _carouselFirstVisibleIndex = Math.Min(_carouselFirstVisibleIndex, _selectedIndex);

                if (Settings.Param_AutoRecipeStart && _activeCard.Status == 0)
                {
                    tabPRODUCTION.UpdateProductionStatus(1, _activeCard.IDprod);

                    if (_activeCard.Vazit != 0)
                        _attachedScale?.Dosing.Start();

                    _activeCard.Status = 1;
                }
            }
            else if (_cards.Count > 0)
            {
                _selectedIndex = 0;
                _cards[0].IsActive = true;
                _activeCard = _cards[0];
            }

            bool vseDokonceno = _cards.All(c => c.Status == 10 || c.Status == 3);
            BtnNovaVarka.Visibility = vseDokonceno ? Visibility.Visible : Visibility.Collapsed;

            SetWeightButtonsVisibility();
            UpdateDumpResetVisibility();
            UpdateTareVisibility();
            AktualizujLevyPanel();
            RenderCarousel();

        }

        // ==========================================
        // Aktualizace levého panelu
        // ==========================================
        private void AktualizujLevyPanel()
        {
            bool maPdf =
                !string.IsNullOrWhiteSpace(_pdfProcedurePath) &&
                File.Exists(_pdfProcedurePath);

            BtnPdf.Visibility = maPdf ? Visibility.Visible : Visibility.Collapsed;

            bool celaZakazkaHotova = JeCelaZakazkaDokoncena();

            BtnUkoncit.Content = celaZakazkaHotova
                ? "Dokončit zakázku"
                : "Ukončit zakázku";

            BtnUkoncit.Icon = new ImageIcon
            {
                Source = new SvgImageSource(
                    new Uri(
                        celaZakazkaHotova
                            ? "ms-appx:///Assets/MenuIcons/ic_fluent_complete_job_28_color.svg"
                            : "ms-appx:///Assets/MenuIcons/ic_fluent_stop_job_28_color.svg"))
            };
        }

        // ==========================================
        // Připojení aktuální váhy
        // ==========================================
        private void AttachScale(SerialScaleClient? scale)
        {
            if (_attachedScale != null)
                _attachedScale.Updated -= ScaleUpdated;

            _attachedScale = scale;

            if (_attachedScale != null)
            {
                _attachedScale.Updated += ScaleUpdated;
                UpdateWeightUI();
                return;
            }

            AktualniVahaLabel.Text = "--- kg";
        }

        // ==========================================
        // Změna váhy v App
        // ==========================================
        private void OnScaleChanged(SerialScaleClient? newScale)
        {
            AttachScale(newScale);
        }

        // ==========================================
        // Událost nové hodnoty z váhy
        // ==========================================
        private void ScaleUpdated(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(UpdateWeightUI);
        }

        // ==========================================
        // Aktualizace hodnoty z váhy a aktivní karty
        // ==========================================
        private void UpdateWeightUI()
        {
            SerialScaleClient? scale = _attachedScale;

            if (scale == null)
            {
                AktualniVahaLabel.Text = "--- kg";
                UpdateDumpResetVisibility();
                UpdateTareVisibility();
                return;
            }

            string unit = string.IsNullOrWhiteSpace(scale.Units)
                ? "kg"
                : scale.Units;

            if (string.Equals(scale.StatusCode, "NV", StringComparison.OrdinalIgnoreCase))
            {
                AktualniVahaLabel.Text = $"--- {unit}";
                UpdateDumpResetVisibility();
                UpdateTareVisibility();
                return;
            }

            if (!string.IsNullOrWhiteSpace(scale.WeightText))
            {
                AktualniVahaLabel.Text = $"{scale.WeightText.Trim()} {unit}";
            }
            else if (scale.WeightDisplay.HasValue)
            {
                AktualniVahaLabel.Text =
                    $"{scale.WeightDisplay.Value.ToString(CultureInfo.CurrentCulture)} {unit}";
            }
            else
            {
                AktualniVahaLabel.Text = $"--- {unit}";
            }

            RecipeCard? current = _cards.FirstOrDefault(c => c.IsActive);

            if (current == null)
            {
                // Překreslení carouselu maximálně 4x za sekundu.
                // Hodnoty z váhy se aktualizují průběžně,
                // ale celé karty se nevytvářejí znovu při každém přijatém znaku
                // z komunikace s váhou, což eliminuje blikání tlačítek.
                if ((DateTime.Now - _lastRenderTime).TotalMilliseconds > 1000)
                {
                    _lastRenderTime = DateTime.Now;
                    RenderCarousel();
                }
                UpdateDumpResetVisibility();
                UpdateTareVisibility();
                return;
            }

            double diff = scale.Dosing.Difference;

            current.Navazeno = diff;
            current.Zbyva = current.HmotnostPozadovana - diff;

            double ratio = current.HmotnostPozadovana <= 0
                ? 0
                : diff / current.HmotnostPozadovana;

            current.ProgresWidth = 200 * Math.Clamp(ratio, 0, 1);

            RenderCarousel();
            HighlightSelectedWeightButton();
            UpdateDumpResetVisibility();
            UpdateTareVisibility();
        }

        // ==========================================
        // Viditelnost tlačítek vah
        // ==========================================
        private void SetWeightButtonsVisibility()
        {
            bool vseDokonceno = _cards.Count > 0 && _cards.All(c => c.Status == 10 || c.Status == 3);

            Button[] buttons =
            {
                BtnWeight1,
                BtnWeight2,
                BtnWeight3,
                BtnWeight4,
                BtnWeight5
            };

            if (vseDokonceno)
            {
                foreach (Button button in buttons)
                    button.Visibility = Visibility.Collapsed;

                return;
            }

            BtnWeight1.Visibility = Settings.Param_ScaleEnabled1 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight2.Visibility = Settings.Param_ScaleEnabled2 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight3.Visibility = Settings.Param_ScaleEnabled3 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight4.Visibility = Settings.Param_ScaleEnabled4 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight5.Visibility = Settings.Param_ScaleEnabled5 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ==========================================
        // Názvy tlačítek vah
        // ==========================================
        private void ApplyScaleNamesToButtons()
        {
            BtnWeight1.Content = string.IsNullOrWhiteSpace(Settings.Param_Scale_Name1) ? "Váha 1" : Settings.Param_Scale_Name1;
            BtnWeight2.Content = string.IsNullOrWhiteSpace(Settings.Param_Scale_Name2) ? "Váha 2" : Settings.Param_Scale_Name2;
            BtnWeight3.Content = string.IsNullOrWhiteSpace(Settings.Param_Scale_Name3) ? "Váha 3" : Settings.Param_Scale_Name3;
            BtnWeight4.Content = string.IsNullOrWhiteSpace(Settings.Param_Scale_Name4) ? "Váha 4" : Settings.Param_Scale_Name4;
            BtnWeight5.Content = string.IsNullOrWhiteSpace(Settings.Param_Scale_Name5) ? "Váha 5" : Settings.Param_Scale_Name5;
        }

        // ==========================================
        // Zvýraznění vybrané váhy
        // ==========================================
        private void HighlightSelectedWeightButton()
        {
            Button[] buttons =
            {
                BtnWeight1,
                BtnWeight2,
                BtnWeight3,
                BtnWeight4,
                BtnWeight5
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == _selectedTenzoIndex)
                {
                    // Vybraná váha má výrazné modré pozadí a bílý text.
                    buttons[i].Background = new SolidColorBrush(ColorHelper.FromArgb(255, 66, 133, 244));
                    buttons[i].Foreground = new SolidColorBrush(Colors.White);
                    buttons[i].BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 30, 95, 190));
                    buttons[i].BorderThickness = new Thickness(1.5);
                }
                else
                {
                    // Nevybraná váha musí být čitelná i ve světlém režimu Windows.
                    // Proto se nepoužívá bílý text na světlém podkladu.
                    buttons[i].Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"];
                    buttons[i].Foreground = SystemTextBrush;
                    buttons[i].BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];
                    buttons[i].BorderThickness = new Thickness(1.5);
                }
            }
        }


        // ==========================================
        // Vykreslení carouselu
        // ==========================================
        private void SafeRenderCarousel()
        {
            if (CarouselCanvas.ActualWidth <= 0)
            {
                DispatcherQueue.TryEnqueue(SafeRenderCarousel);
                return;
            }

            RenderCarousel();
        }

        private void RenderCarousel()
        {
            CarouselCanvas.Children.Clear();

            if (CarouselCanvas.ActualWidth <= 0 || CarouselCanvas.ActualHeight <= 0 || _cards.Count == 0)
                return;

            _selectedIndex = _cards.ToList().FindIndex(c => c.IsActive);

            if (_selectedIndex < 0)
                _selectedIndex = 0;

            double areaWidth = CarouselCanvas.ActualWidth;
            double areaHeight = CarouselCanvas.ActualHeight;

            double spacing = 12;
            double leftSafeMargin = 18;
            double rightSafeMargin = 18;

            double activeWidth = Math.Clamp(areaWidth * 0.34, 260, 330);
            double activeHeight = Math.Clamp(areaHeight * 0.68, 300, 370);

            double smallWidth = Math.Clamp(areaWidth * 0.10, 92, 126);
            double smallHeight = activeHeight * 0.90;

            double centerY = areaHeight / 2;
            double smallTop = centerY - smallHeight / 2;
            double activeTop = centerY - activeHeight / 2;

            double usableWidth = areaWidth - leftSafeMargin - rightSafeMargin;

            int activeSlotCount = Math.Max(
                1,
                (int)Math.Ceiling(activeWidth / (smallWidth + spacing)));

            int maxVisibleSlots = Math.Max(
                1,
                (int)Math.Floor((usableWidth + spacing) / (smallWidth + spacing)));

            int activeSlotPosition = _selectedIndex - _carouselFirstVisibleIndex;

            if (activeSlotPosition < 0)
            {
                _carouselFirstVisibleIndex = _selectedIndex;
                activeSlotPosition = 0;
            }

            if (activeSlotPosition + activeSlotCount > maxVisibleSlots)
            {
                _carouselFirstVisibleIndex = _selectedIndex - maxVisibleSlots + activeSlotCount;
                activeSlotPosition = _selectedIndex - _carouselFirstVisibleIndex;
            }

            if (_carouselFirstVisibleIndex < 0)
                _carouselFirstVisibleIndex = 0;

            double x = leftSafeMargin;

            for (int i = _carouselFirstVisibleIndex; i < _cards.Count; i++)
            {
                bool isActive = i == _selectedIndex;

                double cardWidth = isActive ? activeWidth : smallWidth;

                if (x + cardWidth > areaWidth - rightSafeMargin)
                    break;

                Border cardControl;

                if (isActive)
                {
                    cardControl = CreateCardUI(_cards[i], true, 1.0, activeWidth, activeHeight);
                    cardControl.Width = activeWidth;
                    cardControl.Height = activeHeight;

                    Canvas.SetTop(cardControl, activeTop);
                    Canvas.SetZIndex(cardControl, 200);
                }
                else
                {
                    cardControl = CreateSmallCardUI(_cards[i], i);
                    cardControl.Width = smallWidth;
                    cardControl.Height = smallHeight;

                    Canvas.SetTop(cardControl, smallTop);
                    Canvas.SetZIndex(cardControl, 100);
                }

                Canvas.SetLeft(cardControl, x);
                CarouselCanvas.Children.Add(cardControl);

                x += cardWidth + spacing;
            }

            CreateCarouselDots(areaWidth, activeTop + activeHeight + 16);

            UpdateArrowButtons();
        }

        // ==========================================
        // Vytvoření malé karty carouselu
        // ==========================================
        private Border CreateSmallCardUI(RecipeCard card, int index)
        {
            int dec = ZiskejPocetDesetinnychMistProZobrazeni(card);

            string unit = string.IsNullOrWhiteSpace(_attachedScale?.Units)
                ? "kg"
                : _attachedScale!.Units!;

            bool hotovo = card.Status == 10 || card.Status == 3;

            string popisek = hotovo
                ? card.Status == 3 ? "NEVÁŽENO" : "NAVÁŽENO"
                : "POŽADOVÁNO";

            double hodnota = hotovo
                ? card.Status == 3 ? card.HmotnostPozadovana : card.HmotnostNavazena
                : card.HmotnostPozadovana;

            string materialText = string.IsNullOrWhiteSpace(card.RecipeMatCislo)
                ? card.MaterialDesc
                : $"{card.RecipeMatCislo} - {card.MaterialDesc}";

            Grid grid = new()
            {
                Padding = new Thickness(8, 10, 8, 10),
                RowSpacing = 5
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border numberBadge = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(160, 255, 255, 255)),
                Background = new SolidColorBrush(ColorHelper.FromArgb(45, 255, 255, 255)),
                Padding = new Thickness(9, 2, 9, 2),
                Child = new TextBlock
                {
                    Text = card.IDmat > 0 ? card.IDmat.ToString() : (index + 1).ToString(),
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = SystemTextBrush
                }
            };

            Grid.SetRow(numberBadge, 0);
            grid.Children.Add(numberBadge);

            TextBlock material = new()
            {
                Text = materialText.ToUpper(),
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 8,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = SystemTextBrush
            };

            Grid.SetRow(material, 1);
            grid.Children.Add(material);

            TextBlock caption = new()
            {
                Text = popisek,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Opacity = 0.9,
                Foreground = SystemTextBrush
            };

            Grid.SetRow(caption, 2);
            grid.Children.Add(caption);

            TextBlock value = new()
            {
                Text = $"{hodnota.ToString("F" + dec, CultureInfo.CurrentCulture)} {unit}",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = SystemTextBrush
            };

            Grid.SetRow(value, 3);
            grid.Children.Add(value);

            Border iconCircle = new()
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(17),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(
                    hotovo
                        ? ColorHelper.FromArgb(220, 95, 220, 120)
                        : ColorHelper.FromArgb(220, 75, 180, 255)),
                Background = new SolidColorBrush(ColorHelper.FromArgb(25, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
                Child = new FontIcon
                {
                    Glyph = hotovo ? "\uE73E" : "\uE823",
                    FontSize = 19,
                    Foreground = SystemTextBrush
                }
            };

            Grid.SetRow(iconCircle, 4);
            grid.Children.Add(iconCircle);

            return new Border
            {
                Background = GetBrushForStatus(card.Status),
                CornerRadius = new CornerRadius(14),

                // U malých karet není použitý rámeček.
                // Oddělení od pozadí řeší tmavší výchozí šedá barva ve funkci GetBrushForStatus().
                BorderBrush = null,
                BorderThickness = new Thickness(0),

                Opacity = 0.92,
                Child = grid
            };
        }

        private void CreateCarouselDots(double areaWidth, double top)
        {
            if (_cards.Count <= 1)
                return;

            StackPanel dots = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Brush activeBrush =
                (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

            Brush inactiveBrush =
                (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            for (int i = 0; i < _cards.Count; i++)
            {
                Border dot = new()
                {
                    Width = i == _selectedIndex ? 18 : 10,
                    Height = 6,
                    CornerRadius = new CornerRadius(6),
                    Background = i == _selectedIndex
                        ? activeBrush
                        : inactiveBrush,
                    Opacity = i == _selectedIndex ? 1.0 : 0.45
                };

                dots.Children.Add(dot);
            }

            dots.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(dots, (areaWidth - dots.DesiredSize.Width) / 2);
            Canvas.SetTop(dots, top);
            Canvas.SetZIndex(dots, 300);

            CarouselCanvas.Children.Add(dots);
        }

        // ==========================================
        // Vytvoření jedné karty carouselu
        // ==========================================
        private Border CreateCardUI(RecipeCard card, bool isActive, double scale, double cardWidth, double cardHeight)
        {
            Brush background = GetBrushForStatus(card.Status);

            bool bezVazeni = card.Vazit == 0;
            bool bezTolerance = card.Tolerance == 0;
            int dec = ZiskejPocetDesetinnychMistProZobrazeni(card);

            string unit = string.IsNullOrWhiteSpace(_attachedScale?.Units)
                ? "kg"
                : _attachedScale!.Units!;

            string materialText = string.IsNullOrWhiteSpace(card.RecipeMatCislo)
                ? card.MaterialDesc
                : $"{card.RecipeMatCislo} - {card.MaterialDesc}";

            Grid backgroundGrid = new();

            if (!bezVazeni && isActive && card.Status == 1)
            {
                double ratio = card.HmotnostPozadovana <= 0
                    ? 0
                    : card.Navazeno / card.HmotnostPozadovana;

                ratio = Math.Clamp(ratio, 0, 1);

                Rectangle progressRect = new()
                {
                    Fill = card.Navazeno > card.HmotnostPozadovana + card.Tolerance
                        ? new SolidColorBrush(ColorHelper.FromArgb(130, 255, 80, 60))
                        : card.Navazeno < card.HmotnostPozadovana - card.Tolerance
                            ? new SolidColorBrush(ColorHelper.FromArgb(120, 70, 170, 255))
                            : new SolidColorBrush(ColorHelper.FromArgb(130, 90, 230, 140)),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = cardHeight * ratio,
                    RadiusX = 16,
                    RadiusY = 16,
                    Opacity = 0.45
                };

                backgroundGrid.Children.Add(progressRect);
            }

            Grid contentGrid = new()
            {
                Padding = new Thickness(14, 10, 14, 12)
            };

            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel headerPanel = new()
            {
                Spacing = 6
            };

            Border numberBadge = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(170, 255, 255, 255)),
                Background = new SolidColorBrush(ColorHelper.FromArgb(45, 255, 255, 255)),
                Padding = new Thickness(12, 3, 12, 3),
                Child = new TextBlock
                {
                    Text = card.IDmat > 0 ? card.IDmat.ToString() : card.MaterialCislo,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = SystemTextBrush
                }
            };

            headerPanel.Children.Add(numberBadge);

            headerPanel.Children.Add(new TextBlock
            {
                Text = materialText.ToUpper(),
                FontSize = cardWidth < 285 ? 15 : 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 3,
                Foreground = SystemTextBrush
            });

            Grid.SetRow(headerPanel, 0);
            contentGrid.Children.Add(headerPanel);

            StackPanel middlePanel = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 6,
                Margin = new Thickness(0, 2, 0, 2)
            };

            middlePanel.Children.Add(new TextBlock
            {
                Text = "POŽADOVANÁ HMOTNOST",
                FontSize = cardWidth < 285 ? 12 : 14,
                Opacity = 0.9,
                TextAlignment = TextAlignment.Center,
                Foreground = SystemTextBrush
            });

            middlePanel.Children.Add(new TextBlock
            {
                Text = $"{card.HmotnostPozadovana.ToString("F" + dec, CultureInfo.CurrentCulture)} {unit}",
                FontSize = cardWidth < 285 ? 24 : 30,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Foreground = SystemTextBrush
            });

            if (card.Status == 1 && !bezVazeni)
            {
                double progressWidth = Math.Clamp(cardWidth - 52, 150, 230);
                double progressHeight = 12;
                double lineWidth = 3;

                double maxHodnota = bezTolerance
                    ? card.HmotnostPozadovana
                    : card.HmotnostPozadovana + card.Tolerance;

                if (maxHodnota <= 0)
                    maxHodnota = card.HmotnostPozadovana;

                double ratio = card.HmotnostPozadovana <= 0
                    ? 0
                    : Math.Clamp(card.Navazeno / maxHodnota, 0, 1);

                double minTolerance = Math.Max(0, card.HmotnostPozadovana - card.Tolerance);
                double maxTolerance = card.HmotnostPozadovana + card.Tolerance;

                double minX = bezTolerance || maxHodnota <= 0
                    ? 0
                    : Math.Clamp(minTolerance / maxHodnota, 0, 1) * progressWidth;

                double maxX = bezTolerance || maxHodnota <= 0
                    ? progressWidth
                    : Math.Clamp(maxTolerance / maxHodnota, 0, 1) * progressWidth;

                minX = Math.Clamp(minX - (lineWidth / 2), 0, progressWidth - lineWidth);
                maxX = Math.Clamp(maxX - lineWidth, 0, progressWidth - lineWidth);

                Canvas progressCanvas = new()
                {
                    Width = progressWidth,
                    Height = 34,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                Border progressBack = new()
                {
                    Width = progressWidth,
                    Height = progressHeight,
                    CornerRadius = new CornerRadius(progressHeight / 2),
                    Background = new SolidColorBrush(ColorHelper.FromArgb(80, 255, 255, 255))
                };

                Canvas.SetLeft(progressBack, 0);
                Canvas.SetTop(progressBack, 11);
                progressCanvas.Children.Add(progressBack);

                Border progressFront = new()
                {
                    Width = progressWidth * ratio,
                    Height = progressHeight,
                    CornerRadius = new CornerRadius(progressHeight / 2),
                    Background = new SolidColorBrush(ColorHelper.FromArgb(230, 40, 130, 255))
                };

                Canvas.SetLeft(progressFront, 0);
                Canvas.SetTop(progressFront, 11);
                progressCanvas.Children.Add(progressFront);

                if (!bezTolerance && card.HmotnostPozadovana > 0)
                {
                    Border minLine = new()
                    {
                        Width = lineWidth,
                        Height = 28,
                        CornerRadius = new CornerRadius(2),
                        Background = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 230, 80))
                    };

                    Border maxLine = new()
                    {
                        Width = lineWidth,
                        Height = 28,
                        CornerRadius = new CornerRadius(2),
                        Background = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 230, 80))
                    };

                    Canvas.SetLeft(minLine, minX);
                    Canvas.SetTop(minLine, 3);

                    Canvas.SetLeft(maxLine, maxX);
                    Canvas.SetTop(maxLine, 3);

                    Canvas.SetZIndex(minLine, 10);
                    Canvas.SetZIndex(maxLine, 10);

                    progressCanvas.Children.Add(minLine);
                    progressCanvas.Children.Add(maxLine);
                }

                middlePanel.Children.Add(progressCanvas);

                Grid scaleLabels = new()
                {
                    Width = progressWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, -4, 0, 0)
                };

                scaleLabels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                scaleLabels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                TextBlock minText = new()
                {
                    Text = $"0 {unit}",
                    FontSize = 10,
                    TextAlignment = TextAlignment.Left,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = SystemTextBrush
                };

                TextBlock maxText = new()
                {
                    Text = $"{maxHodnota.ToString("F" + dec, CultureInfo.CurrentCulture)} {unit}",
                    FontSize = 10,
                    TextAlignment = TextAlignment.Right,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = SystemTextBrush
                };

                Grid.SetColumn(minText, 0);
                Grid.SetColumn(maxText, 1);

                scaleLabels.Children.Add(minText);
                scaleLabels.Children.Add(maxText);

                middlePanel.Children.Add(scaleLabels);
            }

            Grid.SetRow(middlePanel, 1);
            contentGrid.Children.Add(middlePanel);

            StackPanel bottomPanel = new()
            {
                Spacing = 4,
                Margin = new Thickness(0, 0, 0, 6)
            };

            if (card.Status != 0)
            {
                double navazeno = card.Status == 10
                    ? card.HmotnostNavazena
                    : bezVazeni
                        ? card.HmotnostPozadovana
                        : card.Navazeno;

                bottomPanel.Children.Add(new TextBlock
                {
                    Text = card.Status == 3 ? "NEVÁŽENO" : "NAVÁŽENO",
                    FontSize = cardWidth < 285 ? 12 : 14,
                    TextAlignment = TextAlignment.Center,
                    Foreground = SystemTextBrush
                });

                bottomPanel.Children.Add(new TextBlock
                {
                    Text = $"{navazeno.ToString("F" + dec, CultureInfo.CurrentCulture)} {unit}",
                    FontSize = cardWidth < 285 ? 26 : 34,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = SystemTextBrush
                });
            }

            Grid.SetRow(bottomPanel, 2);
            contentGrid.Children.Add(bottomPanel);

            StackPanel actionPanel = new()
            {
                Spacing = 5
            };

            bool jinaMaStatus1 = _cards.Any(c => c != card && c.Status == 1);
            double diff = _attachedScale?.Dosing.Difference ?? 0;

            bool vToleranciNow =
                diff >= card.HmotnostPozadovana - card.Tolerance &&
                diff <= card.HmotnostPozadovana + card.Tolerance;

            if (isActive)
            {
                if (card.Status == 0 && !jinaMaStatus1)
                {
                    CreateActionButton("VÁŽIT", async () =>
                    {
                        RootNav.IsPaneOpen = false;

                        tabPRODUCTION.UpdateProductionStatus(1, card.IDprod);
                        tabPRODUCTION.SetActionReq(card.IDprod, 1);

                        if (!bezVazeni)
                            _attachedScale?.Dosing.Start();

                        card.Status = 1;

                        await Task.Delay(300);

                        RenderCarousel();
                        UpdateTareVisibility();
                        UpdateDumpResetVisibility();
                        SetWeightButtonsVisibility();
                    }, actionPanel);

                    if (Settings.Param_PovolitPreskoceni)
                    {
                        CreateActionButton("NEVÁŽÍ SE", async () =>
                        {
                            ConfirmWindow confirm = new ConfirmWindow(
                                "Nevážený materiál",
                                $"Označit jako nevážený materiál?\n\n{card.MaterialDesc}");

                            confirm.Closed += async (_, _) =>
                            {
                                if (!confirm.Potvrzeno)
                                    return;

                                tabPRODUCTION.UpdateProductionStatus(3, card.IDprod);
                                tabPRODUCTION.UpdateProductionAmount(card.IDprod, 0);
                                tabPRODUCTION.SetActionReq(card.IDprod, 1);

                                _attachedScale?.Dosing.Stop();

                                _batchNoIndex = tabPRODUCTION.GetMaxBatchNoIndex(_jobID);

                                await Task.Delay(300);

                                LoadJobData(_jobID, _batchNoIndex);

                                if (Settings.Param_ActPageOpen)
                                    tabSIGNAL.SetIDprod(card.IDprod);
                            };

                            ModalWindowService.Otevri(confirm);
                        }, actionPanel);
                    }
                }
                else if (card.Status == 1 && (vToleranciNow || bezTolerance || bezVazeni))
                {
                    CreateActionButton("DOKONČIT", async () =>
                    {
                        float hmotnost = bezVazeni
                            ? (float)card.HmotnostPozadovana
                            : (float)card.Navazeno;

                        tabPRODUCTION.UpdateProductionStatus(10, card.IDprod);
                        tabPRODUCTION.UpdateProductionAmount(card.IDprod, hmotnost);
                        tabPRODUCTION.SetActionReq(card.IDprod, 1);

                        _batchNoIndex = tabPRODUCTION.GetMaxBatchNoIndex(_jobID);

                        await Task.Delay(1000);

                        _attachedScale?.Dosing.Stop();

                        LoadJobData(_jobID, _batchNoIndex);

                        if (Settings.Param_ActPageOpen)
                            tabSIGNAL.SetIDprod(card.IDprod);
                    }, actionPanel);
                }
            }

            Grid.SetRow(actionPanel, 3);
            contentGrid.Children.Add(actionPanel);

            backgroundGrid.Children.Add(contentGrid);

            return new Border
            {
                Background = background,
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(180, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Child = backgroundGrid
            };
        }

        private void CarouselCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderCarousel();
        }
        private void CarouselCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _carouselPointerDown = true;
            _carouselPointerStartX = e.GetCurrentPoint(CarouselCanvas).Position.X;
            CarouselCanvas.CapturePointer(e.Pointer);
        }

        private void CarouselCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_carouselPointerDown)
                return;

            _carouselPointerDown = false;

            double endX = e.GetCurrentPoint(CarouselCanvas).Position.X;
            double deltaX = endX - _carouselPointerStartX;

            CarouselCanvas.ReleasePointerCapture(e.Pointer);

            if (Math.Abs(deltaX) < 45)
                return;

            if (deltaX < 0)
                BtnRight_Click(sender, new RoutedEventArgs());
            else
                BtnLeft_Click(sender, new RoutedEventArgs());
        }

        private void CarouselCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _carouselPointerDown = false;
            CarouselCanvas.ReleasePointerCapture(e.Pointer);
        }

        // ==========================================
        // Pomocné vytvoření akčního tlačítka v kartě
        // ==========================================
        private void CreateActionButton(string text, Func<Task> onClick, Panel parent)
        {
            Button button = new()
            {
                Content = text,
                Height = 46,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(12, 8, 12, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,

                // Akční tlačítka mají barevné pozadí, proto je text vždy bílý.
                Foreground = new SolidColorBrush(Colors.White),

                Background = text switch
                {
                    // Modrá WinUI
                    "VÁŽIT" => new SolidColorBrush(ColorHelper.FromArgb(255, 66, 133, 244)),

                    // Zelená WinUI
                    "DOKONČIT" => new SolidColorBrush(ColorHelper.FromArgb(255, 82, 183, 136)),

                    // Oranžová WinUI
                    "NEVÁŽÍ SE" => new SolidColorBrush(ColorHelper.FromArgb(255, 230, 149, 74)),

                    // Výchozí
                    _ => new SolidColorBrush(ColorHelper.FromArgb(255, 110, 118, 130))
                },
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(180, 255, 255, 255)),
                Opacity = 0.98,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10)
            };

            button.Click += async (_, _) => await onClick();

            parent.Children.Add(button);
        }

        // ==========================================
        // Barva karty podle statusu
        // ==========================================
        private Brush GetBrushForStatus(int status)
        {
            return status switch
            {
                // Čeká na vážení
                // Výchozí šedá je záměrně tmavší, aby malé karty ve světlém režimu nesplývaly s pozadím.
                0 => new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = ColorHelper.FromArgb(235, 220, 225, 235), Offset = 0 },
                new GradientStop { Color = ColorHelper.FromArgb(220, 185, 195, 210), Offset = 1 }
            }
                },

                // Aktivně se váží
                1 => new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = ColorHelper.FromArgb(225, 40, 125, 255), Offset = 0 },
                new GradientStop { Color = ColorHelper.FromArgb(195, 70, 190, 255), Offset = 1 }
            }
                },

                // Pozastaveno / upozornění
                2 => new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = ColorHelper.FromArgb(220, 255, 190, 80), Offset = 0 },
                new GradientStop { Color = ColorHelper.FromArgb(180, 255, 145, 45), Offset = 1 }
            }
                },

                // Neváženo
                3 => new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = ColorHelper.FromArgb(215, 255, 120, 120), Offset = 0 },
                new GradientStop { Color = ColorHelper.FromArgb(175, 210, 70, 95), Offset = 1 }
            }
                },

                // Hotovo
                10 => new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
            {
                new GradientStop { Color = ColorHelper.FromArgb(220, 70, 210, 140), Offset = 0 },
                new GradientStop { Color = ColorHelper.FromArgb(180, 30, 150, 105), Offset = 1 }
            }
                },

                _ => new SolidColorBrush(ColorHelper.FromArgb(220, 185, 195, 210))
            };
        }


        private void RecipeCardsList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderCarousel();
        }

        // ==========================================
        // Posun carouselu vlevo
        // ==========================================
        private void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex <= 0)
                return;

            _cards[_selectedIndex].IsActive = false;

            _selectedIndex--;

            _cards[_selectedIndex].IsActive = true;
            _activeCard = _cards[_selectedIndex];

            RenderCarousel();
            UpdateWeightUI();
            UpdateDumpResetVisibility();
            UpdateTareVisibility();
        }

        // ==========================================
        // Posun carouselu vpravo
        // ==========================================
        private void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex >= _cards.Count - 1)
                return;

            _cards[_selectedIndex].IsActive = false;

            _selectedIndex++;

            _cards[_selectedIndex].IsActive = true;
            _activeCard = _cards[_selectedIndex];

            RenderCarousel();
            UpdateWeightUI();
            UpdateDumpResetVisibility();
            UpdateTareVisibility();
        }

        // ==========================================
        // Aktualizace šipek
        // ==========================================
        private void UpdateArrowButtons()
        {
            BtnLeft.Visibility = _selectedIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
            BtnRight.Visibility = _selectedIndex < _cards.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            UpdateDumpResetVisibility();
        }

        // ==========================================
        // Nová várka
        // ==========================================
        private void BtnNovaVarka_Click(object sender, RoutedEventArgs e)
        {
            if (_cards.Count == 0)
                return;

            RecipeCard first = _cards.First();
            int jobID = first.IDjob;

            // Označení dokončené dávky pro ERP
            RecipeCard? lastCardCurrentBatch = _cards
                .OrderByDescending(x => x.IDprod)
                .FirstOrDefault();

            if (lastCardCurrentBatch != null)
            {
                tabPRODUCTION.SetActionReq(lastCardCurrentBatch.IDprod, 2);
            }

            var (rows, _) = tabPRODUCTION.GetProductionViewByID_IDX(jobID, 0);

            if (rows == null || rows.Count == 0)
                return;

            string lastBatchNo = rows.First().BatchNo;
            string[] parts = lastBatchNo.Split('-');
            string prefix = parts[0];

            int nextBatch =
                parts.Length > 1 && int.TryParse(parts[1], out int parsed)
                    ? parsed + 1
                    : 1;

            string newBatchNo = $"{prefix}-{nextBatch}";

            foreach (var row in rows)
            {
                tabPRODUCTION.InsertProductionFromRecipe(new tabPRODUCTION.tabProduction
                {
                    IDjob = row.IDjob,
                    IDrcp = row.IDrcp,
                    IDmrg = row.IDmrg,
                    IDmat = row.IDmat,
                    IDzaklad = row.IDzaklad,
                    BatchNo = newBatchNo,
                    HmotnostNavazena = 0,
                    HmotnostPozadovana = (float)Math.Round(row.HmotnostPozadovana, 3),
                    Tolerance = (float)Math.Round(row.Tolerance, 3),
                    Status = 0
                });
            }

            ZobrazInfo(
                InfoBarSeverity.Success,
                "Nová várka",
                $"Várka {newBatchNo} byla vytvořena.");

            LoadJobData(jobID, nextBatch);
        }

        // ==========================================
        // Ukončení nebo dokončení zakázky
        // ==========================================
        private void BtnUkoncitVazeni_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool celaZakazkaHotova = JeCelaZakazkaDokoncena();

            if (celaZakazkaHotova)
            {
                RecipeCard? lastCardCurrentBatch = _cards
                    .OrderByDescending(x => x.IDprod)
                    .FirstOrDefault();

                if (lastCardCurrentBatch != null)
                {
                    tabPRODUCTION.SetActionReq(lastCardCurrentBatch.IDprod, 2);
                }

                tabJOB_LIST.UpdateJobStatus(10, _jobID);
                tabJOB_LIST.SetActionReq(_jobID, 1);

                _attachedScale?.Dosing.Stop();
                Frame?.Navigate(typeof(JobPage));
                return;
            }

            bool unfinishedExists = _cards.Any(c => c.Status != 10 && c.Status != 3);
            bool existujeDalsiVarka = _batchNoIndex < _jobNumberBatch;

            string zprava;

            if (unfinishedExists && existujeDalsiVarka)
                zprava = "Některé materiály nejsou naváženy a tato várka není poslední.\nChcete zakázku přerušit?";
            else if (unfinishedExists)
                zprava = "Některé materiály nejsou naváženy.\nChcete zakázku přerušit?";
            else if (existujeDalsiVarka)
                zprava = "Tato várka není poslední v zakázce.\nChcete přesto zakázku přerušit?";
            else
                zprava = "Chcete zakázku přerušit?";

            ConfirmWindow confirm = new ConfirmWindow("Přerušení zakázky", zprava);

            confirm.Closed += (_, _) =>
            {
                if (!confirm.Potvrzeno)
                    return;

                tabJOB_LIST.UpdateJobStatus(2, _jobID);
                tabJOB_LIST.SetActionReq(_jobID, 1);

                RecipeCard? activeCard = _cards.FirstOrDefault(c => c.Status == 1);

                if (activeCard != null)
                {
                    tabPRODUCTION.UpdateProductionStatus(0, activeCard.IDprod);
                    tabPRODUCTION.SetActionReq(activeCard.IDprod, 1);
                }

                _attachedScale?.Dosing.Stop();
                Frame?.Navigate(typeof(JobPage));
            };

            ModalWindowService.Otevri(confirm);
        }

        // ==========================================
        // Přerušení zakázky
        // ==========================================
        private void BtnPrerusitVazeni_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ConfirmWindow confirm = new ConfirmWindow(
                "Přerušení zakázky",
                "Chcete zakázku přerušit?");

            confirm.Closed += (_, _) =>
            {
                if (!confirm.Potvrzeno)
                    return;

                tabJOB_LIST.UpdateJobStatus(3, _jobID);
                tabJOB_LIST.SetActionReq(_jobID, 1);
                _attachedScale?.Dosing.Stop();
                Frame?.Navigate(typeof(JobPage));
            };

            ModalWindowService.Otevri(confirm);
        }

        // ==========================================
        // Reset aktivního vážení
        // ==========================================
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _attachedScale?.Dosing.Reset();

            if (_activeCard != null)
            {
                tabPRODUCTION.UpdateProductionStatus(0, _activeCard.IDprod);
                tabPRODUCTION.SetActionReq(_activeCard.IDprod, 1);
            }

            LoadJobData(_jobID, _batchNoIndex);
        }

        // ==========================================
        // Vysypání váhy
        // ==========================================
        private void BtnDumpScale_Click(object sender, RoutedEventArgs e)
        {
            _attachedScale?.Dosing.SetHold(true);

            InfoWindow infoWindow = new InfoWindow(
                "Vysypání díže",
                "Po vysypání díže potvrďte pokračování vážení.",
                "Pokračovat");

            infoWindow.Closed += (_, _) =>
            {
                if (!infoWindow.Potvrzeno)
                    return;

                _attachedScale?.Dosing.SetHold(false);

                UpdateWeightUI();
                UpdateDumpResetVisibility();
                UpdateTareVisibility();
            };

            ModalWindowService.Otevri(infoWindow);
        }

        // ==========================================
        // Viditelnost vysypání a resetu
        // ==========================================
        private void UpdateDumpResetVisibility()
        {
            RecipeCard? active = _cards.FirstOrDefault(c => c.IsActive);

            if (active == null)
            {
                BtnDumpScale.Visibility = Visibility.Collapsed;
                BtnResetVazeni.Visibility = Visibility.Collapsed;
                return;
            }

            bool isActiveStatus = active.Status == 1;

            BtnDumpScale.Visibility = isActiveStatus ? Visibility.Visible : Visibility.Collapsed;
            BtnResetVazeni.Visibility = isActiveStatus ? Visibility.Visible : Visibility.Collapsed;
        }

        // ==========================================
        // Přepnutí váhy
        // ==========================================
        private void WeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            int idx0 = Convert.ToInt32(button.Tag);
            int idx1 = idx0 + 1;

            Debug.WriteLine($"Vybraná váha: {idx1}");

            _selectedTenzoIndex = idx0;
            Settings.Param_ScaleIndex = idx1;

            HighlightSelectedWeightButton();

            if (Application.Current is App app)
                app.SwitchScale(idx1);
        }

        // ==========================================
        // Zpět na přehled zakázek
        // ==========================================
        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(JobPage));
        }

        // ==========================================
        // Zobrazení PDF technologického postupu
        // ==========================================
        private void BtnPDF_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pdfProcedurePath) || !File.Exists(_pdfProcedurePath))
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "PDF",
                    "PDF soubor nebyl nalezen nebo cesta není nastavena.");

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _pdfProcedurePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "PDF",
                    $"PDF se nepodařilo otevřít: {ex.Message}");
            }
        }

        // ==========================================
        // Tara
        // ==========================================
        private async void BtnTare_Click(object sender, RoutedEventArgs e)
        {
            SerialScaleClient? scale = _attachedScale;

            if (scale == null)
            {
                ZobrazInfo(
                    InfoBarSeverity.Warning,
                    "Váha",
                    "Není připojena žádná váha.");

                return;
            }

            BtnTare.IsEnabled = false;

            try
            {
                bool ok = await scale.SendTareAsync();

                ZobrazInfo(
                    ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning,
                    "Tara",
                    ok ? "Tara provedena." : "Váha nepotvrdila taru.");
            }
            catch (Exception ex)
            {
                ZobrazInfo(
                    InfoBarSeverity.Error,
                    "Tara",
                    $"Chyba při tarování: {ex.Message}");
            }
            finally
            {
                BtnTare.IsEnabled = true;
            }
        }

        // ==========================================
        // Viditelnost tary
        // ==========================================
        private void UpdateTareVisibility()
        {
            bool probihaVazeni = _cards.Any(c => c.Status == 1);
            BtnTare.Visibility = probihaVazeni ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==========================================
        // Dokončení celé zakázky
        // ==========================================
        private bool JeCelaZakazkaDokoncena()
        {
            if (_cards.Count == 0)
                return false;

            bool aktualniVarkaHotova = _cards.All(c => c.Status == 10 || c.Status == 3);
            bool jePosledniVarka = _batchNoIndex >= _jobNumberBatch;

            return aktualniVarkaHotova && jePosledniVarka;
        }

        // ==========================================
        // Počet desetinných míst
        // ==========================================
        private int ZjistiPocetDesetinnychMist(double hodnota)
        {
            string text = hodnota.ToString("0.###", CultureInfo.InvariantCulture);
            int idx = text.IndexOf('.');

            if (idx < 0)
                return 0;

            return Math.Min(3, text.Length - idx - 1);
        }

        private int ZiskejPocetDesetinnychMistProZobrazeni(RecipeCard card)
        {
            int dec = 0;

            dec = Math.Max(dec, ZjistiPocetDesetinnychMist(card.HmotnostPozadovana));
            dec = Math.Max(dec, ZjistiPocetDesetinnychMist(card.HmotnostNavazena));
            dec = Math.Max(dec, ZjistiPocetDesetinnychMist(card.Tolerance));
            dec = Math.Max(dec, ZjistiPocetDesetinnychMist(card.Navazeno));
            dec = Math.Max(dec, ZjistiPocetDesetinnychMist(card.Zbyva));

            string? weightText = _attachedScale?.WeightText?.Trim();

            if (!string.IsNullOrWhiteSpace(weightText))
            {
                int dot = weightText.IndexOf('.');
                int comma = weightText.IndexOf(',');
                int idx = dot >= 0 ? dot : comma;

                if (idx >= 0 && idx < weightText.Length - 1)
                {
                    int decScale = Math.Min(3, weightText.Length - idx - 1);
                    dec = Math.Max(dec, decScale);
                }
            }

            return Math.Min(3, dec);
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
        // Stavový řádek
        // ==========================================
        private async void ZobrazInfo(
            InfoBarSeverity severity,
            string title,
            string message)
        {
            InfoStatus.Severity = severity;
            InfoStatus.Title = title;
            InfoStatus.Message = message;
            InfoStatus.IsOpen = true;

            await Task.Delay(3000);

            InfoStatus.IsOpen = false;
        }
    }


    // ==========================================
    // Jedna karta carouselu
    // ==========================================

    public sealed class RecipeCard : INotifyPropertyChanged
    {
        public int IDprod { get; set; }
        public int IDjob { get; set; }
        public int IDrcp { get; set; }
        public int IDmat { get; set; }
        public int IDpc { get; set; }
        public string BatchNo { get; set; } = "";
        public int BatchNoIdx { get; set; }
        public string MaterialCislo { get; set; } = "";
        public string MaterialDesc { get; set; } = "";
        public string RecipeMatCislo { get; set; } = "";
        public double Navazeno { get; set; }
        public double Zbyva { get; set; }
        public double HmotnostPozadovana { get; set; }
        public double HmotnostNavazena { get; set; }
        public double Tolerance { get; set; }
        public int Vazit { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = "";

        private bool _isActive;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        private double _progresWidth;

        public double ProgresWidth
        {
            get => _progresWidth;
            set
            {
                if (Math.Abs(_progresWidth - value) < 0.001)
                    return;

                _progresWidth = value;
                OnPropertyChanged(nameof(ProgresWidth));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
