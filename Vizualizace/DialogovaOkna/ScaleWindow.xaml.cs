using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RCP_WT1.SerialComm;
using System;
using System.Globalization;
using Windows.Graphics;

namespace RCP_WT1.Vizualizace.DialogovaOkna
{
    public sealed partial class ScaleWindow : Window
    {
        private int _selectedScaleIndex = 0;
        private SerialScaleClient? _attachedScale;

        public ScaleWindow()
        {
            InitializeComponent();

            AppWindow.Resize(new SizeInt32(680, 420));

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.IsResizable = true;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            VycentrujOkno();

            Activated += ScaleWindow_Activated;
            Closed += ScaleWindow_Closed;
        }

        private void ScaleWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            ApplyScaleNamesToButtons();
            SetWeightButtonsVisibility();

            int storedIndex = Settings.Param_ScaleIndex;

            if (storedIndex < 1 || storedIndex > 5)
                storedIndex = 1;

            _selectedScaleIndex = storedIndex - 1;

            HighlightSelectedWeightButton();

            if (Application.Current is App app)
            {
                app.ScaleChanged += OnScaleChanged;
                AttachScale(app.Scale);
            }

            Activated -= ScaleWindow_Activated;
        }

        private void ScaleWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Application.Current is App app)
                app.ScaleChanged -= OnScaleChanged;

            AttachScale(null);
        }

        private void OnScaleChanged(SerialScaleClient? newScale)
        {
            AttachScale(newScale);
        }

        private void AttachScale(SerialScaleClient? scale)
        {
            if (_attachedScale != null)
                _attachedScale.Updated -= Scale_Updated;

            _attachedScale = scale;

            if (_attachedScale != null)
            {
                _attachedScale.Updated += Scale_Updated;
                UpdateWeightUI();
            }
            else
            {
                LblWeight.Text = "—";
                LblStatus.Text = "Váha nenalezena";
            }
        }

        private void Scale_Updated(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(UpdateWeightUI);
        }

        private void UpdateWeightUI()
        {
            var scale = _attachedScale;

            if (scale == null)
            {
                LblWeight.Text = "—";
                LblStatus.Text = "Váha nenalezena";
                return;
            }

            LblStatus.Text = scale.StatusText;

            if (string.Equals(scale.StatusCode, "NV", StringComparison.OrdinalIgnoreCase))
            {
                LblWeight.Text = "—";
                return;
            }

            string unit = string.IsNullOrWhiteSpace(scale.Units)
                ? "kg"
                : scale.Units;

            if (!string.IsNullOrWhiteSpace(scale.WeightText))
            {
                LblWeight.Text = $"{scale.WeightText.Trim()} {unit}";
                return;
            }

            if (scale.WeightDisplay.HasValue)
            {
                LblWeight.Text =
                    $"{scale.WeightDisplay.Value.ToString(CultureInfo.CurrentCulture)} {unit}";
                return;
            }

            LblWeight.Text = "—";
        }

        private void WeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            int idx0 = Convert.ToInt32(button.Tag);
            int idx1 = idx0 + 1;

            _selectedScaleIndex = idx0;
            Settings.Param_ScaleIndex = idx1;

            HighlightSelectedWeightButton();

            if (Application.Current is App app)
                app.SwitchScale(idx1);
        }

        private void ApplyScaleNamesToButtons()
        {
            BtnWeight1.Content = Settings.Param_Scale_Name1;
            BtnWeight2.Content = Settings.Param_Scale_Name2;
            BtnWeight3.Content = Settings.Param_Scale_Name3;
            BtnWeight4.Content = Settings.Param_Scale_Name4;
            BtnWeight5.Content = Settings.Param_Scale_Name5;
        }

        private void SetWeightButtonsVisibility()
        {
            BtnWeight1.Visibility = Settings.Param_ScaleEnabled1 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight2.Visibility = Settings.Param_ScaleEnabled2 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight3.Visibility = Settings.Param_ScaleEnabled3 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight4.Visibility = Settings.Param_ScaleEnabled4 ? Visibility.Visible : Visibility.Collapsed;
            BtnWeight5.Visibility = Settings.Param_ScaleEnabled5 ? Visibility.Visible : Visibility.Collapsed;
        }

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
                if (i == _selectedScaleIndex)
                {
                    buttons[i].Background =
                        new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(80, 80, 160, 255));

                    buttons[i].Foreground =
                        new SolidColorBrush(Microsoft.UI.Colors.White);
                }
                else
                {
                    buttons[i].Background =
                        new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 255, 255, 255));

                    buttons[i].Foreground =
                        new SolidColorBrush(Microsoft.UI.Colors.White);
                }
            }
        }

        private async void BtnTare_Click(object sender, RoutedEventArgs e)
        {
            var scale = _attachedScale;

            if (scale == null)
            {
                LblStatus.Text = "Není připojena žádná váha.";
                return;
            }

            BtnTare.IsEnabled = false;

            try
            {
                bool ok = await scale.SendTareAsync();

                LblStatus.Text = ok
                    ? "Tara provedena."
                    : "Váha nepotvrdila taru.";
            }
            catch (Exception ex)
            {
                LblStatus.Text = $"Chyba při tarování: {ex.Message}";
            }
            finally
            {
                BtnTare.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
    }
}