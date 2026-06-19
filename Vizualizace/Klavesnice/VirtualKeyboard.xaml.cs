using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using Windows.Graphics;
using WinRT.Interop;

namespace RCP_WT1.Vizualizace.Klavesnice
{
    public sealed partial class VirtualKeyboard : Window
    {
        public enum KeyboardMode
        {
            Str,
            Int,
            Float,
            Pwd
        }

        public string Vysledek { get; private set; } = "";
        public bool Potvrzeno { get; private set; } = false;

        private readonly KeyboardMode _rezim;

        private bool _capsLock = false;
        private bool _shift = false;
        private bool _diakritika = false;
        private bool _symboly = false;

        // Při podržení prstu nad klávesou se může po zavření nabídky vyvolat i běžný Click.
        // Tento příznak zajistí, že se po výběru diakritiky nevloží ještě základní písmeno.
        private bool _ignorovatDalsiKlik = false;

        private string _skutecneHeslo = "";

        private int _sirkaOkna = 950;
        private int _vyskaOkna = 520;

        private readonly List<Button> _textovaTlacitka = new();

        private readonly Dictionary<string, string> _diaMapa = new(StringComparer.OrdinalIgnoreCase)
        {
            { "a", "á" },
            { "c", "č" },
            { "d", "ď" },
            { "e", "ě" },
            { "i", "í" },
            { "n", "ň" },
            { "o", "ó" },
            { "r", "ř" },
            { "s", "š" },
            { "t", "ť" },
            { "u", "ú" },
            { "y", "ý" },
            { "z", "ž" }
        };

        private readonly Dictionary<string, string> _symMapa = new(StringComparer.OrdinalIgnoreCase)
        {
            { "q", "@" },
            { "w", "#" },
            { "e", "€" },
            { "r", "$" },
            { "t", "%" },
            { "z", "^" },
            { "u", "&" },
            { "i", "*" },
            { "o", "(" },
            { "p", ")" },
            { "a", "!" },
            { "s", "?" },
            { "d", "/" },
            { "f", "\\" },
            { "g", "|" },
            { "h", "[" },
            { "j", "]" },
            { "k", "{" },
            { "l", "}" },
            { "y", "<" },
            { "x", ">" },
            { "c", ":" },
            { "v", ";" },
            { "b", "\"" },
            { "n", "'" },
            { "m", "_" }
        };

        // Alternativní české znaky pro podržení prstu nebo pravé tlačítko myši.
        // Základní klávesnice tak zůstává přehledná, ale operátor může rychle vložit diakritiku.
        private readonly Dictionary<string, string[]> _alternativniZnaky = new(StringComparer.OrdinalIgnoreCase)
        {
            { "a", new[] { "á" } },
            { "c", new[] { "č" } },
            { "d", new[] { "ď" } },
            { "e", new[] { "é", "ě" } },
            { "i", new[] { "í" } },
            { "n", new[] { "ň" } },
            { "o", new[] { "ó" } },
            { "r", new[] { "ř" } },
            { "s", new[] { "š" } },
            { "t", new[] { "ť" } },
            { "u", new[] { "ú", "ů" } },
            { "y", new[] { "ý" } },
            { "z", new[] { "ž" } }
        };

        public VirtualKeyboard(
            KeyboardMode rezim = KeyboardMode.Str,
            string puvodniText = "",
            string titulek = "")
        {
            InitializeComponent();

            nint hwndKeyboard = WindowNative.GetWindowHandle(this);

            WindowId keyboardWindowId = Win32Interop.GetWindowIdFromWindow(hwndKeyboard);
            AppWindow keyboardAppWindow = AppWindow.GetFromWindowId(keyboardWindowId);

            if (keyboardAppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragArea);

            _rezim = rezim;

            TxtKeyboardTitle.Text = titulek ?? "";
            TxtKeyboardTitle.Visibility =
                string.IsNullOrWhiteSpace(titulek)
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            NastavRozlozeniKlavesnice();

            if (_rezim == KeyboardMode.Pwd)
            {
                _skutecneHeslo = puvodniText ?? "";

                TxtText.Visibility = Visibility.Collapsed;
                PwdGrid.Visibility = Visibility.Visible;

                AktualizujMaskuHesla();

            }
            else
            {
                TxtText.Text = puvodniText ?? "";

            }

            NactiTextovaTlacitka(KlavesnicePanel);
            AktualizujKlavesy();

            AppWindow.Resize(new SizeInt32(_sirkaOkna, _vyskaOkna));
            VycentrujNaHlavniOkno();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (_rezim == KeyboardMode.Pwd)
                {
                    TxtPassword.Focus(FocusState.Programmatic);
                    TxtPassword.SelectAll();
                }
                else
                {
                    TxtText.Focus(FocusState.Programmatic);
                    TxtText.SelectAll();
                }
            });


        }

        private void NastavRozlozeniKlavesnice()
        {
            bool numerickyRezim =
                _rezim == KeyboardMode.Int ||
                _rezim == KeyboardMode.Float;

            TextKeyboardView.Visibility =
                numerickyRezim ? Visibility.Collapsed : Visibility.Visible;

            NumericKeyboardGrid.Visibility =
                numerickyRezim ? Visibility.Visible : Visibility.Collapsed;

            BtnDecimalDot.Visibility =
                _rezim == KeyboardMode.Float ? Visibility.Visible : Visibility.Collapsed;

            if (numerickyRezim)
            {
                _sirkaOkna = 420;
                _vyskaOkna = 500;
            }
            else
            {
                _sirkaOkna = 900;
                _vyskaOkna = 500;
            }
        }

        private void VycentrujNaHlavniOkno()
        {
            if (App.MainWindow == null)
                return;

            nint hwndMain =
                WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);

            WindowId mainWindowId =
                Win32Interop.GetWindowIdFromWindow(hwndMain);

            AppWindow mainWindow =
                AppWindow.GetFromWindowId(mainWindowId);

            PointInt32 pozice = mainWindow.Position;
            SizeInt32 velikost = mainWindow.Size;

            int x = pozice.X + ((velikost.Width - _sirkaOkna) / 2);
            int y = pozice.Y + ((velikost.Height - _vyskaOkna) / 2);

            AppWindow.Move(new PointInt32(x, y));
        }

        private void VirtualKeyboard_Loaded(object sender, RoutedEventArgs e)
        {
            if (_rezim == KeyboardMode.Pwd)
            {
                TxtPassword.Focus(FocusState.Programmatic);
                TxtPassword.SelectAll();
            }
            else
            {
                TxtText.Focus(FocusState.Programmatic);
                TxtText.SelectAll();
            }
        }

        private void NactiTextovaTlacitka(DependencyObject parent)
        {
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is Button button && button.Tag != null)
                {
                    string hodnota = button.Tag.ToString() ?? "";

                    if (hodnota.Length == 1 && char.IsLetter(hodnota[0]))
                    {
                        _textovaTlacitka.Add(button);

                        // Podržení prstu otevře nabídku českých znaků.
                        button.Holding += Pismeno_Holding;

                        // Pravé tlačítko myši otevře stejnou nabídku jako podržení prstu.
                        button.RightTapped += Pismeno_RightTapped;
                    }
                }

                NactiTextovaTlacitka(child);
            }
        }

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (_ignorovatDalsiKlik)
            {
                _ignorovatDalsiKlik = false;
                return;
            }

            if (sender is not Button button)
                return;

            string znak = button.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(znak))
                return;

            if (znak.Length == 1 && char.IsLetter(znak[0]))
                znak = PrevedPismeno(znak);

            if (!JeZnakPovolenyPodleRezimu(znak))
                return;

            VlozText(znak);

            ZrusJednorazovyCapsLockPokudJeAktivni();
        }

        private async void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            TxtPassword.Text = _skutecneHeslo;
            TxtPassword.SelectionStart = TxtPassword.Text.Length;

            await System.Threading.Tasks.Task.Delay(1200);

            AktualizujMaskuHesla();
            TxtPassword.SelectionStart = TxtPassword.Text.Length;
        }

        private string PrevedPismeno(string znak)
        {
            string vysledek = znak.ToLower();

            if (_symboly && _symMapa.TryGetValue(vysledek, out string? symbol))
                return symbol;

            if (_diakritika && _diaMapa.TryGetValue(vysledek, out string? dia))
                vysledek = dia;

            if (JeVelkePismenoAktivni())
                vysledek = vysledek.ToUpper();

            return vysledek;
        }

        // ==========================================
        // Nabídka českých znaků přes dotyk / myš
        // ==========================================
        private void Pismeno_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (sender is Button btn)
                ZobrazAlternativniZnaky(btn, true);
        }

        private void Pismeno_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            ZobrazAlternativniZnaky(button, false);
        }

        private void ZobrazAlternativniZnaky(Button zdroj, bool blokovatNasledujiciKlik)
        {
            // Nabídka diakritiky se nepoužívá v režimu symbolů, protože klávesy právě zobrazují znaky jako @, %, &, atd.
            if (_symboly)
                return;

            string zakladniZnak = zdroj.Tag?.ToString()?.ToLower() ?? "";

            if (!_alternativniZnaky.TryGetValue(zakladniZnak, out string[]? varianty))
                return;

            if (varianty.Length == 0)
                return;

            MenuFlyout flyout = new();

            foreach (string varianta in varianty)
            {
                string vlozenyZnak = JeVelkePismenoAktivni()
                    ? varianta.ToUpper()
                    : varianta;

                MenuFlyoutItem item = new()
                {
                    Text = vlozenyZnak
                };

                item.Click += (_, _) =>
                {
                    VlozText(vlozenyZnak);
                    ZrusJednorazovyCapsLockPokudJeAktivni();
                };

                flyout.Items.Add(item);
            }

            _ignorovatDalsiKlik = blokovatNasledujiciKlik;

            flyout.ShowAt(zdroj);
        }

        private bool JeVelkePismenoAktivni()
        {
            return _capsLock || _shift;
        }

        private void ZrusJednorazovyCapsLockPokudJeAktivni()
        {
            // Zachování původního chování: CapsLock bez Shiftu funguje jako jedno velké písmeno.
            // Pokud je současně aktivní Shift, velká písmena zůstávají zapnutá.
            if (_capsLock && !_shift)
            {
                _capsLock = false;
                AktualizujKlavesy();
            }
        }

        private bool JeZnakPovolenyPodleRezimu(string znak)
        {
            if (_rezim == KeyboardMode.Str || _rezim == KeyboardMode.Pwd)
                return true;

            if (_rezim == KeyboardMode.Int)
            {
                if (znak.Length == 1 && char.IsDigit(znak[0]))
                    return true;

                if (znak == "-" && VratSkutecnyText().Length == 0)
                    return true;

                return false;
            }

            if (_rezim == KeyboardMode.Float)
            {
                if (znak.Length == 1 && char.IsDigit(znak[0]))
                    return true;

                if (znak == "-" && VratSkutecnyText().Length == 0)
                    return true;

                if (znak == "," || znak == ".")
                {
                    string text = VratSkutecnyText();

                    if (text.Contains(",") || text.Contains("."))
                        return false;

                    return true;
                }

                return false;
            }

            return true;
        }

        private void VlozText(string znak)
        {
            if (_rezim == KeyboardMode.Pwd)
            {
                int pozice = TxtPassword.SelectionStart;
                int delkaVyberu = TxtPassword.SelectionLength;

                if (pozice < 0)
                    pozice = 0;

                if (pozice > _skutecneHeslo.Length)
                    pozice = _skutecneHeslo.Length;

                if (delkaVyberu > 0)
                {
                    if (pozice + delkaVyberu > _skutecneHeslo.Length)
                        delkaVyberu = _skutecneHeslo.Length - pozice;

                    _skutecneHeslo = _skutecneHeslo.Remove(pozice, delkaVyberu);
                }

                _skutecneHeslo = _skutecneHeslo.Insert(pozice, znak);
                AktualizujMaskuHesla();

                TxtPassword.SelectionStart = pozice + znak.Length;
                TxtPassword.SelectionLength = 0;
                TxtPassword.Focus(FocusState.Programmatic);
                return;
            }

            string text = TxtText.Text ?? "";
            int poziceTextu = TxtText.SelectionStart;
            int delkaVyberuTextu = TxtText.SelectionLength;

            if (poziceTextu < 0)
                poziceTextu = 0;

            if (poziceTextu > text.Length)
                poziceTextu = text.Length;

            if (delkaVyberuTextu > 0)
            {
                if (poziceTextu + delkaVyberuTextu > text.Length)
                    delkaVyberuTextu = text.Length - poziceTextu;

                text = text.Remove(poziceTextu, delkaVyberuTextu);
            }

            if (_rezim == KeyboardMode.Float &&
                (znak == "," || znak == ".") &&
                string.IsNullOrEmpty(text))
            {
                znak = "0" + znak;
            }

            TxtText.Text = text.Insert(poziceTextu, znak);
            TxtText.SelectionStart = poziceTextu + znak.Length;
            TxtText.SelectionLength = 0;
            TxtText.Focus(FocusState.Programmatic);
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (_rezim == KeyboardMode.Pwd)
            {
                int pozice = TxtPassword.SelectionStart;

                if (pozice <= 0 || _skutecneHeslo.Length == 0)
                    return;

                if (pozice > _skutecneHeslo.Length)
                    pozice = _skutecneHeslo.Length;

                _skutecneHeslo = _skutecneHeslo.Remove(pozice - 1, 1);
                AktualizujMaskuHesla();

                TxtPassword.SelectionStart = pozice - 1;
                TxtPassword.Focus(FocusState.Programmatic);
                return;
            }

            string text = TxtText.Text ?? "";
            int poziceTextu = TxtText.SelectionStart;

            if (poziceTextu <= 0 || text.Length == 0)
                return;

            if (poziceTextu > text.Length)
                poziceTextu = text.Length;

            TxtText.Text = text.Remove(poziceTextu - 1, 1);
            TxtText.SelectionStart = poziceTextu - 1;
            TxtText.Focus(FocusState.Programmatic);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (_rezim == KeyboardMode.Pwd)
            {
                _skutecneHeslo = "";
                AktualizujMaskuHesla();

                TxtPassword.SelectionStart = 0;
                TxtPassword.Focus(FocusState.Programmatic);
                return;
            }

            TxtText.Text = "";
            TxtText.SelectionStart = 0;
            TxtText.Focus(FocusState.Programmatic);
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            if (_rezim == KeyboardMode.Str || _rezim == KeyboardMode.Pwd)
                VlozText(Environment.NewLine);
        }

        private void Shift_Click(object sender, RoutedEventArgs e)
        {
            _shift = !_shift;
            AktualizujKlavesy();
        }

        private void CapsLock_Click(object sender, RoutedEventArgs e)
        {
            _capsLock = !_capsLock;
            AktualizujKlavesy();
        }

        private void Dia_Click(object sender, RoutedEventArgs e)
        {
            _diakritika = !_diakritika;
            AktualizujKlavesy();
        }

        private void Sym_Click(object sender, RoutedEventArgs e)
        {
            _symboly = !_symboly;
            AktualizujKlavesy();
        }

        private void AktualizujKlavesy()
        {
            bool velke = JeVelkePismenoAktivni();

            foreach (Button button in _textovaTlacitka)
            {
                string hodnota = button.Tag?.ToString() ?? "";

                if (hodnota.Length == 1 && char.IsLetter(hodnota[0]))
                {
                    string text = hodnota.ToLower();

                    if (_symboly && _symMapa.TryGetValue(text, out string? symbol))
                        text = symbol;
                    else if (_diakritika && _diaMapa.TryGetValue(text, out string? dia))
                        text = dia;
                    else if (velke)
                        text = text.ToUpper();

                    button.Content = text;
                }
            }

            BtnCapsLock.Style =
                (Style)RootGrid.Resources[_capsLock
                    ? "KeyboardActiveButtonStyle"
                    : "KeyboardSpecialButtonStyle"];

            BtnShiftLeft.Style =
                (Style)RootGrid.Resources[_shift
                    ? "KeyboardActiveButtonStyle"
                    : "KeyboardSpecialButtonStyle"];

            BtnShiftRight.Style =
                (Style)RootGrid.Resources[_shift
                    ? "KeyboardActiveButtonStyle"
                    : "KeyboardSpecialButtonStyle"];

            BtnDia.Style =
                (Style)RootGrid.Resources[_diakritika
                    ? "KeyboardActiveButtonStyle"
                    : "KeyboardSpecialButtonStyle"];

            BtnSym.Style =
                (Style)RootGrid.Resources[_symboly
                    ? "KeyboardActiveButtonStyle"
                    : "KeyboardSpecialButtonStyle"];

            BtnCapsLock.Content = "Caps Lock";
            BtnShiftLeft.Content = "Shift";
            BtnShiftRight.Content = "Shift";
            BtnDia.Content = "DIA";
            BtnSym.Content = "SYM";
        }

        private string VratSkutecnyText()
        {
            return _rezim == KeyboardMode.Pwd
                ? _skutecneHeslo
                : TxtText.Text ?? "";
        }

        private void AktualizujMaskuHesla()
        {
            TxtPassword.Text = new string('*', _skutecneHeslo.Length);
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            Vysledek = _rezim == KeyboardMode.Pwd
                ? _skutecneHeslo
                : TxtText.Text ?? "";

            Potvrzeno = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Vysledek = "";
            Potvrzeno = false;
            Close();
        }
    }
}