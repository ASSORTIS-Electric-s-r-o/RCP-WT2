using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka materiálů
    // ==========================================
    internal static class tabMATERIAL
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tableName = "tabMATERIAL";

        public static string viewName = "hmiMATERIAL";

        public static string logFile =
            new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";


        // ==========================================
        // Třída jednoho řádku materiálu
        // ==========================================
        public class MaterialRow
        {
            public int IDjob { get; set; }

            public int IDmat { get; set; }

            public string Cislo { get; set; } = "";

            public string Name { get; set; } = "";

            public string MpImage { get; set; } = "";

            public int IsDeleted { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public MaterialRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public MaterialRow(DataRow row)
            {
                IDmat = MySQL.CInt(row, "IDmat");

                Cislo = row.Table.Columns.Contains("Cislo")
                    ? MySQL.CStr(row, "Cislo")
                    : "";

                Name = MySQL.CStr(row, "Name");

                MpImage = row.Table.Columns.Contains("MpImage")
                    ? MySQL.CStr(row, "MpImage")
                    : "";

                IsDeleted = MySQL.CInt(row, "IsDeleted");
            }
        }


        // ==========================================
        // Načtení všech materiálů z view
        // ==========================================
        public static (List<MaterialRow>, int) GetMaterialsAll()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<MaterialRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new MaterialRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<MaterialRow>(), 0);
            }
        }


        // ==========================================
        // Načtení materiálu podle ID
        // ==========================================
        public static (MaterialRow?, bool) GetMaterialByID(int idmat)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDmat = {idmat} " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0)
                    return (new MaterialRow(rows.Rows[0]), true);

                return (null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (null, false);
            }
        }


        // ==========================================
        // Získání dalšího volného ID
        // ==========================================
        public static int GetNextID()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT IFNULL(MAX(IDmat), 0) + 1 AS NextID " +
                    $"FROM {tableName}";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0 && table.Rows.Count > 0)
                {
                    return MySQL.CInt(table.Rows[0], "NextID");
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return 1;
            }
        }


        // ==========================================
        // Vložení nového materiálu
        // ==========================================
        public static int InsertMaterial(string name, string cislo = "", string image = "")
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                name = (name ?? "").Replace("'", "''");
                cislo = (cislo ?? "").Replace("'", "''");
                image = (image ?? "").Replace("\\", "\\\\").Replace("'", "''");

                int newID = GetNextID();

                string cmd =
                    $"INSERT INTO {tableName} " +
                    $"(IDmat, Cislo, `Name`, MpImage) " +
                    $"VALUES " +
                    $"({newID}, '{cislo}', '{name}', '{image}')";

                int affectedRows = MySQL.RunNonQuery(cmd);

                return affectedRows > 0 ? newID : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return 0;
            }
        }


        // ==========================================
        // Úprava materiálu
        // ==========================================
        public static bool UpdateMaterial(
            int idmat,
            string newName,
            string newCislo = "",
            string? newImage = null)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                newName = (newName ?? "").Replace("'", "''");
                newCislo = (newCislo ?? "").Replace("'", "''");

                string cmd;

                if (newImage == null)
                {
                    cmd =
                        $"UPDATE {tableName} " +
                        $"SET `Name` = '{newName}', " +
                        $"Cislo = '{newCislo}' " +
                        $"WHERE IDmat = {idmat}";
                }
                else
                {
                    newImage = newImage.Replace("\\", "\\\\").Replace("'", "''");

                    cmd =
                        $"UPDATE {tableName} " +
                        $"SET `Name` = '{newName}', " +
                        $"Cislo = '{newCislo}', " +
                        $"MpImage = '{newImage}' " +
                        $"WHERE IDmat = {idmat}";
                }

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Logické smazání materiálu
        // ==========================================
        public static bool DeleteMaterial(int idmat)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsDeleted = 1 " +
                    $"WHERE IDmat = {idmat}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Kontrola existence materiálu
        // ==========================================
        public static bool ExistsMaterial(int idmat)
        {
            try
            {
                string cmd =
                    $"SELECT COUNT(*) AS Pocet " +
                    $"FROM {tableName} " +
                    $"WHERE IDmat = {idmat}";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                return rowcount > 0 &&
                       table.Rows.Count > 0 &&
                       MySQL.CInt(table.Rows[0], "Pocet") > 0;
            }
            catch
            {
                return false;
            }
        }


        // ==========================================
        // Vložení nebo aktualizace materiálu
        // ==========================================
        public static bool UpsertMaterial(MaterialRow row)
        {
            try
            {
                if (row == null || row.IDmat <= 0)
                    return false;

                string name = (row.Name ?? "").Replace("'", "''");
                string cislo = (row.Cislo ?? "").Replace("'", "''");
                string image = (row.MpImage ?? "").Replace("\\", "\\\\").Replace("'", "''");

                // ==========================================
                // Existuje podle ID - UPDATE
                // ==========================================
                if (ExistsMaterial(row.IDmat))
                {
                    string cmd =
                        $"UPDATE {tableName} " +
                        $"SET Cislo = '{cislo}', " +
                        $"`Name` = '{name}', " +
                        $"MpImage = '{image}', " +
                        $"IsDeleted = {row.IsDeleted} " +
                        $"WHERE IDmat = {row.IDmat}";

                    return MySQL.RunNonQuery(cmd) > 0;
                }


                // ==========================================
                // Existuje podle názvu - UPDATE
                // ==========================================
                string findCmd =
                    $"SELECT IDmat " +
                    $"FROM {tableName} " +
                    $"WHERE `Name` = '{name}' " +
                    $"LIMIT 1";

                var (rowcount, table) = MySQL.RunSelectCmd(findCmd);

                if (rowcount > 0 && table.Rows.Count > 0)
                {
                    int existID = MySQL.CInt(table.Rows[0], "IDmat");

                    string cmd =
                        $"UPDATE {tableName} " +
                        $"SET Cislo = '{cislo}', " +
                        $"MpImage = '{image}', " +
                        $"IsDeleted = {row.IsDeleted} " +
                        $"WHERE IDmat = {existID}";

                    return MySQL.RunNonQuery(cmd) > 0;
                }


                // ==========================================
                // Neexistuje - INSERT
                // ==========================================
                string insertCmd =
                    $"INSERT INTO {tableName} " +
                    $"(IDmat, Cislo, `Name`, MpImage, IsDeleted) " +
                    $"VALUES " +
                    $"({row.IDmat}, '{cislo}', '{name}', '{image}', {row.IsDeleted})";

                return MySQL.RunNonQuery(insertCmd) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}