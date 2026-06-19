using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace RCP_WT1.PomocneTridy
{
    public static class ModalWindowService
    {
        public static void Otevri(
            Window dialog,
            Window? ownerWindow = null,
            bool vratitFokusPoZavreni = true)
        {
            Window vlastnik = ownerWindow ?? App.MainWindow;

            nint hwndOwner = WindowNative.GetWindowHandle(vlastnik);
            nint hwndDialog = WindowNative.GetWindowHandle(dialog);

            NastavVlastnikaOkna(hwndDialog, hwndOwner);

            if (hwndOwner != 0)
                EnableWindow(hwndOwner, false);

            dialog.Activate();

            if (hwndDialog != 0)
                SetForegroundWindow(hwndDialog);

            dialog.Closed += (_, _) =>
            {
                if (hwndOwner != 0)
                    EnableWindow(hwndOwner, true);

                if (vratitFokusPoZavreni && hwndOwner != 0)
                {
                    SetForegroundWindow(hwndOwner);
                    vlastnik.Activate();
                }
            };
        }

        public static void Zavri(Window dialog)
        {
            dialog.DispatcherQueue.TryEnqueue(() =>
            {
                dialog.Close();
            });
        }

        private static void NastavVlastnikaOkna(
            nint hwndOkno,
            nint hwndVlastnik)
        {
            if (hwndOkno == 0 || hwndVlastnik == 0)
                return;

            if (IntPtr.Size == 8)
                SetWindowLongPtr(hwndOkno, GWLP_HWNDPARENT, hwndVlastnik);
            else
                SetWindowLong(hwndOkno, GWLP_HWNDPARENT, hwndVlastnik);
        }

        private const int GWLP_HWNDPARENT = -8;

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(nint hWnd, bool bEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern nint SetWindowLongPtr(
            nint hWnd,
            int nIndex,
            nint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(
            nint hWnd,
            int nIndex,
            nint dwNewLong);
    }
}