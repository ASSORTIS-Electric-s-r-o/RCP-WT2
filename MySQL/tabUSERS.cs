using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka uživatelů
    // ==========================================
    internal static class tabUSERS
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tableName = "tabUSERS";

        public static string viewName = "hmiUSERS";

        public static string tableNameRole = "tabROLES";

        public static string logFile =
            new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";


        // ==========================================
        // Třída jednoho řádku uživatele
        // ==========================================
        public class UserRow
        {
            public int IDuser { get; set; }

            public string Username { get; set; } = "";

            public string PasswordHash { get; set; } = "";

            public int IDrole { get; set; }

            public int IsDeleted { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public UserRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public UserRow(DataRow row)
            {
                IDuser = MySQL.CInt(row, "IDuser");

                Username = MySQL.CStr(row, "Username");

                PasswordHash = MySQL.CStr(row, "PasswordHash");

                IDrole = MySQL.CInt(row, "IDrole");

                IsDeleted = MySQL.CInt(row, "IsDeleted");
            }
        }


        // ==========================================
        // Třída jednoho řádku uživatele z view
        // ==========================================
        public class UserViewRow
        {
            public int IDuser { get; set; }

            public string Username { get; set; } = "";

            public string PasswordHash { get; set; } = "";

            public int IDrole { get; set; }

            public string RoleName { get; set; } = "";

            public int IsDeleted { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public UserViewRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public UserViewRow(DataRow row)
            {
                IDuser = MySQL.CInt(row, "IDuser");

                Username = MySQL.CStr(row, "Username");

                PasswordHash = MySQL.CStr(row, "PasswordHash");

                IDrole = MySQL.CInt(row, "IDrole");

                RoleName = MySQL.CStr(row, "RoleName");

                IsDeleted = MySQL.CInt(row, "IsDeleted");
            }
        }


        // ==========================================
        // Třída jednoho řádku role
        // ==========================================
        public class RoleRow
        {
            public int IDrole { get; set; }

            public string Name { get; set; } = "";


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public RoleRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public RoleRow(DataRow row)
            {
                IDrole = MySQL.CInt(row, "IDrole");

                Name = MySQL.CStr(row, "NAME");
            }
        }


        // ==========================================
        // Načtení všech aktivních uživatelů
        // ==========================================
        public static (List<UserRow>, int) GetAllUsers()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {tableName} " +
                    $"WHERE IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<UserRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new UserRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<UserRow>(), 0);
            }
        }


        // ==========================================
        // Načtení všech aktivních uživatelů z view
        // ==========================================
        public static (List<UserViewRow>, int) GetAllUserViews()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<UserViewRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new UserViewRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<UserViewRow>(), 0);
            }
        }


        // ==========================================
        // Načtení uživatele podle jména
        // ==========================================
        public static UserViewRow? GetUserByUsername(string username)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string safeUsername =
                    (username ?? "").Replace("'", "''");

                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsDeleted = 0 " +
                    $"AND Username = '{safeUsername}' " +
                    $"LIMIT 1";

                var (_, rows) = MySQL.RunSelectCmd(cmd);

                foreach (DataRow row in rows.Rows)
                {
                    return new UserViewRow(row);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return null;
            }
        }


        // ==========================================
        // Ověření přihlášení uživatele
        // ==========================================
        public static UserRow? Authenticate(string username, string passwordHash)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string safeUsername =
                    (username ?? "").Replace("'", "''");

                string safePasswordHash =
                    (passwordHash ?? "").Replace("'", "''");

                string cmd =
                    $"SELECT * FROM {tableName} " +
                    $"WHERE Username = '{safeUsername}' " +
                    $"AND PasswordHash = '{safePasswordHash}' " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0 && rows.Rows.Count > 0)
                {
                    return new UserRow(rows.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
            }

            return null;
        }


        // ==========================================
        // Vložení nového uživatele
        // ==========================================
        public static int InsertUser(UserRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string usernameSafe =
                    (row.Username ?? "").Replace("'", "''");

                string hashSafe =
                    (row.PasswordHash ?? "").Replace("'", "''");

                string cmd =
                    $"INSERT INTO {tableName} " +
                    $"(Username, PasswordHash, IDrole, IsDeleted) " +
                    $"VALUES " +
                    $"('{usernameSafe}', '{hashSafe}', {row.IDrole}, 0)";

                return MySQL.RunInsertCmd(cmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return 0;
            }
        }


        // ==========================================
        // Aktualizace uživatele
        // ==========================================
        public static bool UpdateUser(UserRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string usernameSafe =
                    (row.Username ?? "").Replace("'", "''");

                string hashSafe =
                    (row.PasswordHash ?? "").Replace("'", "''");

                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET Username = '{usernameSafe}', " +
                    $"PasswordHash = '{hashSafe}', " +
                    $"IDrole = {row.IDrole} " +
                    $"WHERE IDuser = {row.IDuser}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Logické smazání uživatele
        // ==========================================
        public static bool DeleteUser(int iduser)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsDeleted = 1 " +
                    $"WHERE IDuser = {iduser}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Načtení všech rolí
        // ==========================================
        public static (List<RoleRow>, int) GetAllRoles()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {tableNameRole}";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RoleRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RoleRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RoleRow>(), 0);
            }
        }
    }
}