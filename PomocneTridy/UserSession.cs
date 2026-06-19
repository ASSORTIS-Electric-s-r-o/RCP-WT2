using System;
using RCP_WT1.MySQL;

namespace RCP_WT1.PomocneTridy
{
    internal static class UserSession
    {
        // ==========================================
        // Aktuálně přihlášený uživatel
        // ==========================================
        public static tabUSERS.UserRow? CurrentUser { get; private set; }

        // ==========================================
        // Offline režim aplikace
        // ==========================================
        public static bool IsOfflineMode { get; private set; } = false;

        // ==========================================
        // Je přihlášen uživatel?
        // ==========================================
        public static bool IsLoggedIn => CurrentUser != null;

        // ==========================================
        // Interní seznam posluchačů změny uživatele
        // ==========================================
        private static Action? _userChangedHandlers;

        // ==========================================
        // Událost změny uživatele
        // ==========================================
        public static event Action UserChanged
        {
            add => _userChangedHandlers += value;
            remove => _userChangedHandlers -= value;
        }

        // ==========================================
        // Vyvolání události změny uživatele
        // ==========================================
        private static void RaiseUserChanged()
        {
            _userChangedHandlers?.Invoke();
        }

        // ==========================================
        // Odpojení všech posluchačů
        // ==========================================
        public static void ClearUserChangedListeners()
        {
            _userChangedHandlers = null;
        }

        // ==========================================
        // Přihlášení uživatele
        // ==========================================
        public static void Login(tabUSERS.UserRow user, bool offline = false)
        {
            CurrentUser = user;
            IsOfflineMode = offline;

            RaiseUserChanged();
        }

        // ==========================================
        // Odhlášení uživatele
        // ==========================================
        public static void Logout()
        {
            CurrentUser = null;
            IsOfflineMode = false;

            RaiseUserChanged();
        }
    }
}