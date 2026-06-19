using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RCP_WT1.PomocneTridy;
using System;
using System.Collections.Generic;

namespace RCP_WT1.Vizualizace.Klavesnice
{
    // =========================================================
    // Služba pro automatické otevírání dotykové klávesnice
    // =========================================================
    public static class DotykovaKlavesniceService
    {
        private static readonly HashSet<Control> _registrovanePrvky = new();

        private static bool _klavesniceOtevrena = false;
        private static TextBox? _aktivniTextBox;
        private static PasswordBox? _aktivniPasswordBox;

        // =========================================================
        // Připojení klávesnice k zadanému vizuálnímu stromu
        // =========================================================
        public static void Pripoj(
            DependencyObject root,
            Window? ownerWindow = null,
            Func<Control, VirtualKeyboard.KeyboardMode>? zjistiRezim = null)
        {
            foreach (TextBox tb in NajdiPrvky<TextBox>(root))
            {
                if (_registrovanePrvky.Add(tb))
                {
                    tb.AddHandler(
                        UIElement.PointerPressedEvent,
                        new PointerEventHandler((s, e) =>
                        {
                            e.Handled = true;
                            OtevriProTextBox(tb, ownerWindow, zjistiRezim);
                        }),
                        true);
                }
            }

            foreach (PasswordBox pb in NajdiPrvky<PasswordBox>(root))
            {
                if (_registrovanePrvky.Add(pb))
                {
                    pb.AddHandler(
                        UIElement.PointerPressedEvent,
                        new PointerEventHandler((s, e) =>
                        {
                            e.Handled = true;
                            OtevriProPasswordBox(pb, ownerWindow);
                        }),
                        true);
                }
            }
        }

        // =========================================================
        // Otevření klávesnice pro TextBox
        // =========================================================
        private static void OtevriProTextBox(
            TextBox tb,
            Window? ownerWindow,
            Func<Control, VirtualKeyboard.KeyboardMode>? zjistiRezim)
        {
            if (_klavesniceOtevrena || !tb.IsEnabled)
                return;

            _klavesniceOtevrena = true;
            _aktivniTextBox = tb;
            _aktivniPasswordBox = null;

            VirtualKeyboard.KeyboardMode rezim =
                zjistiRezim?.Invoke(tb) ?? VirtualKeyboard.KeyboardMode.Str;

            string titulek = VratPopisPole(tb);

            VirtualKeyboard keyboard = new VirtualKeyboard(
                rezim,
                tb.Text ?? "",
                titulek);

            OtevriModalne(keyboard, ownerWindow);
        }

        // =========================================================
        // Otevření klávesnice pro PasswordBox
        // =========================================================
        private static void OtevriProPasswordBox(
            PasswordBox pb,
            Window? ownerWindow)
        {
            if (_klavesniceOtevrena || !pb.IsEnabled)
                return;

            _klavesniceOtevrena = true;
            _aktivniTextBox = null;
            _aktivniPasswordBox = pb;

            string titulek = VratPopisPole(pb);

            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Pwd,
                pb.Password ?? "",
                titulek);

            OtevriModalne(keyboard, ownerWindow);
        }

        // =========================================================
        // Otevření klávesnice přes ModalWindowService
        // =========================================================
        private static void OtevriModalne(
            VirtualKeyboard keyboard,
            Window? ownerWindow)
        {
            keyboard.Closed += (_, _) =>
            {
                if (keyboard.Potvrzeno)
                {
                    if (_aktivniTextBox != null)
                        _aktivniTextBox.Text = keyboard.Vysledek;

                    if (_aktivniPasswordBox != null)
                        _aktivniPasswordBox.Password = keyboard.Vysledek;
                }

                _aktivniTextBox = null;
                _aktivniPasswordBox = null;
                _klavesniceOtevrena = false;
            };

            ModalWindowService.Otevri(
                keyboard,
                ownerWindow ?? App.MainWindow);
        }

        // =========================================================
        // Popis pole pro titulek klávesnice
        // =========================================================
        private static string VratPopisPole(Control control)
        {
            if (control.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                return tag;

            if (!string.IsNullOrWhiteSpace(control.Name))
                return control.Name;

            return "Zadání hodnoty";
        }

        // =========================================================
        // Rekurzivní hledání prvků ve vizuálním stromu
        // =========================================================
        private static IEnumerable<T> NajdiPrvky<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);

                if (child is T nalezenyPrvek)
                    yield return nalezenyPrvek;

                foreach (T vnorenyPrvek in NajdiPrvky<T>(child))
                    yield return vnorenyPrvek;
            }
        }
    }
}