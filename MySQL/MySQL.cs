using System;
using System.Data;
using Microsoft.UI.Dispatching;
using MySql.Data.MySqlClient;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // MySQL databáze
    // ==========================================
    internal static class MySQL
    {
        // ==========================================
        // Veřejné stavové proměnné
        // ==========================================
        public static bool IsOnline { get; private set; } = false;

        public static event Action? ConnectionStatusChanged;


        // ==========================================
        // Privátní proměnné
        // ==========================================
        private static DispatcherQueueTimer? connectionMonitorTimer;


        // ==========================================
        // Monitorování připojení k databázi
        // ==========================================
        public static void StartMonitoringConnection()
        {
            if (connectionMonitorTimer != null)
                return;

            connectionMonitorTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            connectionMonitorTimer.Interval = TimeSpan.FromSeconds(5);

            connectionMonitorTimer.Tick += (s, e) =>
            {
                _ = CheckConnection();
            };

            connectionMonitorTimer.Start();
        }


        // ==========================================
        // Sestavení connection stringu
        // ==========================================
        private static string GetConnectionString()
        {
            string ip = Settings.Param_SQL_IP;
            string db = Settings.Param_SQL_DB;
            string user = Settings.Param_SQL_USER;
            string password = Settings.Param_SQL_PASSWORD;

            return $"Server={ip};Database={db};Uid={user};Pwd={password};CharSet=utf8;";
        }


        // ==========================================
        // Ověření připojení k databázi
        // ==========================================
        public static bool CheckConnection()
        {
            try
            {
                string connString = GetConnectionString();

                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();

                    if (!IsOnline)
                    {
                        IsOnline = true;
                        ConnectionStatusChanged?.Invoke();
                    }

                    return true;
                }
            }
            catch
            {
                if (IsOnline)
                {
                    IsOnline = false;
                    ConnectionStatusChanged?.Invoke();
                }

                return false;
            }
        }


        // ==========================================
        // SELECT dotaz
        // ==========================================
        public static (int, DataTable) RunSelectCmd(string cmd)
        {
            try
            {
                if (!IsOnline || string.IsNullOrWhiteSpace(cmd))
                    return (0, new DataTable());

                string connString = GetConnectionString();

                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(cmd, conn))
                    using (var adapter = new MySqlDataAdapter(command))
                    {
                        var result = new DataTable();
                        adapter.Fill(result);

                        return (result.Rows.Count, result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba SELECT: {ex.Message}");
                return (0, new DataTable());
            }
        }


        // ==========================================
        // UPDATE / DELETE dotaz
        // ==========================================
        public static int RunNonQuery(string cmd)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmd))
                    return 0;

                string connString = GetConnectionString();

                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(cmd.Replace("'NULL'", "NULL"), conn))
                    {
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba UPDATE/DELETE: {ex.Message}");
                return 0;
            }
        }


        // ==========================================
        // INSERT dotaz s návratem ID
        // ==========================================
        public static int RunInsertCmd(string cmd)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmd))
                    return 0;

                string connString = GetConnectionString();

                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(cmd.Replace("'NULL'", "NULL"), conn))
                    {
                        command.ExecuteNonQuery();
                        return Convert.ToInt32(command.LastInsertedId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba INSERT: {ex.Message}");
                return 0;
            }
        }


        // ==========================================
        // Převod hodnoty z DataRow na int
        // ==========================================
        public static int CInt(DataRow row, string id, int defaultValue = 0)
        {
            try
            {
                if (row?[id] != DBNull.Value)
                    return Convert.ToInt32(row[id]);

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Převod hodnoty z DataRow na float
        // ==========================================
        public static float CFloat(DataRow row, string id, float defaultValue = 0.0f)
        {
            try
            {
                if (row?[id] != DBNull.Value)
                    return Convert.ToSingle(row[id]);

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Převod hodnoty z DataRow na string
        // ==========================================
        public static string CStr(DataRow row, string id, string defaultValue = "")
        {
            try
            {
                if (row?[id] != DBNull.Value)
                    return row[id]?.ToString() ?? defaultValue;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Převod hodnoty z DataRow na datum
        // ==========================================
        public static string CDT(DataRow row, string id = "DT", string format = "yyyy-MM-dd")
        {
            try
            {
                if (row?[id] != DBNull.Value && row[id] is DateTime dt)
                    return dt.ToString(format);

                return "NULL";
            }
            catch
            {
                return DateTime.Now.ToString(format);
            }
        }
    }
}