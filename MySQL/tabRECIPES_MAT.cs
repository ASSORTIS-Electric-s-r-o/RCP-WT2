using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka materiálů receptur
    // ==========================================
    internal static class tabRECIPES_MAT
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tableName = "tabRECIPES_MAT";

        public static string viewName = "hmiRECIPES_MAT";

        public static string logFile =
            new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";


        // ==========================================
        // Bezpečné ošetření textu pro SQL
        // ==========================================
        private static string Esc(string? value)
        {
            return (value ?? "").Replace("'", "''");
        }


        // ==========================================
        // Třída jednoho řádku tabRECIPES_MAT
        // ==========================================
        public class RecipeMaterialRow
        {
            public int IDjob { get; set; }

            public int IDrcp { get; set; }

            public int IDmat { get; set; }

            public int IDzaklad { get; set; }

            public string Cislo { get; set; } = "";

            public float Davka { get; set; }

            public string Jednotky { get; set; } = "";

            public float Tolerance { get; set; }

            public int row_no { get; set; }

            public int Vazit { get; set; }

            public int IsDeleted { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public RecipeMaterialRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public RecipeMaterialRow(DataRow row)
            {
                IDrcp = MySQL.CInt(row, "IDrcp");
                IDmat = MySQL.CInt(row, "IDmat");
                IDzaklad = MySQL.CInt(row, "IDzaklad");

                Cislo = MySQL.CStr(row, "Cislo");

                Davka = MySQL.CFloat(row, "Davka");

                Jednotky = MySQL.CStr(row, "Jednotky");

                Tolerance = MySQL.CFloat(row, "Tolerance");

                row_no = MySQL.CInt(row, "row_no");

                Vazit = MySQL.CInt(row, "Vazit");

                IsDeleted = MySQL.CInt(row, "IsDeleted");
            }
        }


        // ==========================================
        // Třída jednoho řádku view hmiRECIPES_MAT
        // ==========================================
        public class RecipeMaterialViewRow
        {
            public int IDjob { get; set; }

            public int IDrcp { get; set; }

            public string RecipeCislo { get; set; } = "";

            public string RecipeName { get; set; } = "";

            public int IDmat { get; set; }

            public int IDzaklad { get; set; }

            public string RecipeMatCislo { get; set; } = "";

            public string MaterialName { get; set; } = "";

            public string MaterialCislo { get; set; } = "";

            public int IsZaklad => IDzaklad > 0 ? 1 : 0;

            public float Davka { get; set; }

            public float Tolerance { get; set; }

            public int row_no { get; set; }

            public int Vazit { get; set; }

            public int IsDeleted { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public RecipeMaterialViewRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public RecipeMaterialViewRow(DataRow row)
            {
                IDrcp = MySQL.CInt(row, "IDrcp");

                RecipeCislo = MySQL.CStr(row, "RecipeCislo");

                RecipeName = MySQL.CStr(row, "RecipeName");

                IDmat = MySQL.CInt(row, "IDmat");

                IDzaklad = MySQL.CInt(row, "IDzaklad");

                RecipeMatCislo = MySQL.CStr(row, "RecipeMatCislo");

                MaterialName = MySQL.CStr(row, "MaterialName");

                MaterialCislo = MySQL.CStr(row, "MaterialCislo");

                Davka = MySQL.CFloat(row, "Davka");

                Tolerance = MySQL.CFloat(row, "Tolerance");

                row_no = MySQL.CInt(row, "row_no");

                Vazit = MySQL.CInt(row, "Vazit");

                IsDeleted = MySQL.CInt(row, "IsDeleted");
            }
        }


        // ==========================================
        // Načtení všech řádků z view
        // ==========================================
        public static (List<RecipeMaterialViewRow>, int) GetAllFromView()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeMaterialViewRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeMaterialViewRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeMaterialViewRow>(), 0);
            }
        }


        // ==========================================
        // Načtení řádků podle receptury
        // ==========================================
        public static (List<RecipeMaterialViewRow>, int) GetByRecipe(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IsDeleted = 0 " +
                    $"ORDER BY row_no ASC";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeMaterialViewRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeMaterialViewRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeMaterialViewRow>(), 0);
            }
        }


        // ==========================================
        // Načtení všech řádků podle receptury
        // ==========================================
        public static (List<RecipeMaterialViewRow>, int) GetAllByRecipe(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"ORDER BY row_no ASC";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeMaterialViewRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeMaterialViewRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeMaterialViewRow>(), 0);
            }
        }


        // ==========================================
        // Načtení materiálů receptury z tabulky
        // ==========================================
        public static (List<RecipeMaterialRow>, int) GetRecipeMaterials(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {tableName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeMaterialRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeMaterialRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeMaterialRow>(), 0);
            }
        }


        // ==========================================
        // Načtení smazaného řádku podle receptury a materiálu
        // ==========================================
        public static (RecipeMaterialViewRow? result, int rowcount) GetDeletedRowByRecipeAndMaterial(int idrcp,int idmat,int idzaklad)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IDmat = {idmat} " +
                    $"AND IDzaklad = {idzaklad} " +
                    $"AND IsDeleted = 1 " +
                    $"LIMIT 1";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0 && rows.Rows.Count > 0)
                {
                    RecipeMaterialViewRow result =
                        new RecipeMaterialViewRow(rows.Rows[0]);

                    return (result, rowcount);
                }

                return (null, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (null, 0);
            }
        }


        // ==========================================
        // Vložení materiálu do receptury
        // ==========================================
        public static bool InsertRecipeMaterial(RecipeMaterialRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"INSERT INTO {tableName} " +
                    $"(IDrcp, IDmat, IDzaklad, Davka, Jednotky, Tolerance, row_no, Vazit) " +
                    $"VALUES " +
                    $"({row.IDrcp}, " +
                    $"{row.IDmat}, " +
                    $"{row.IDzaklad}, " +
                    $"{row.Davka.ToString(CultureInfo.InvariantCulture)}, " +
                    $"'{Esc(row.Jednotky)}', " +
                    $"{row.Tolerance.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{row.row_no}, " +
                    $"{row.Vazit})";

                return MySQL.RunInsertCmd(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Aktualizace materiálu v receptuře
        // ==========================================
        public static bool UpdateMaterialRow(RecipeMaterialRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET Davka = {row.Davka.ToString(CultureInfo.InvariantCulture)}, " +
                    $"Jednotky = '{Esc(row.Jednotky)}', " +
                    $"Tolerance = {row.Tolerance.ToString(CultureInfo.InvariantCulture)}, " +
                    $"row_no = {row.row_no}, " +
                    $"Vazit = {row.Vazit}, " +
                    $"IsDeleted = {row.IsDeleted} " +
                    $"WHERE IDrcp = {row.IDrcp} " +
                    $"AND IDmat = {row.IDmat} " +
                    $"AND IDzaklad = {row.IDzaklad}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Logické smazání materiálu z receptury
        // ==========================================
        public static bool DeleteRecipeMaterial(int idrcp, int idmat, int idzaklad)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsDeleted = 1 " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IDmat = {idmat} " +
                    $"AND IDzaklad = {idzaklad}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Kontrola existence materiálu v receptuře
        // ==========================================
        public static bool ExistsRecipeMaterial(int idrcp, int idmat, int idzaklad)
        {
            try
            {
                string cmd =
                    $"SELECT COUNT(*) AS Pocet " +
                    $"FROM {tableName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IDmat = {idmat} " +
                    $"AND IDzaklad = {idzaklad}";

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
        // Vložení nebo aktualizace materiálu receptury
        // ==========================================
        public static bool UpsertRecipeMaterial(RecipeMaterialRow row)
        {
            try
            {
                if (row == null || row.IDrcp <= 0)
                    return false;

                if (ExistsRecipeMaterial(row.IDrcp, row.IDmat, row.IDzaklad))
                    return UpdateMaterialRow(row);

                return InsertRecipeMaterial(row);
            }
            catch
            {
                return false;
            }
        }


        // ==========================================
        // Synchronizace řádků receptury pro CSV import
        // ==========================================
        public static bool SyncRecipeMaterialsImport(int idrcp, List<RecipeMaterialRow> csvRows)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                if (idrcp <= 0)
                    return false;

                if (csvRows == null)
                    csvRows = new List<RecipeMaterialRow>();


                // ==========================================
                // Odstranění neplatných řádků
                // ==========================================
                csvRows = csvRows
                    .Where(row => row.IDmat > 0 || row.IDzaklad > 0)
                    .ToList();


                // ==========================================
                // Načtení všech řádků receptury z databáze
                // ==========================================
                string cmdSelect =
                    $"SELECT * FROM {tableName} " +
                    $"WHERE IDrcp = {idrcp}";

                var (rowcount, table) = MySQL.RunSelectCmd(cmdSelect);

                List<RecipeMaterialRow> dbRows = new();

                if (rowcount > 0 && table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        dbRows.Add(new RecipeMaterialRow(row));
                    }
                }


                // ==========================================
                // Aktualizace nebo vložení řádků z CSV
                // ==========================================
                foreach (RecipeMaterialRow csvRow in csvRows)
                {
                    csvRow.IDrcp = idrcp;
                    csvRow.IsDeleted = 0;

                    bool existujeVDb = dbRows.Any(dbRow =>
                        dbRow.IDrcp == csvRow.IDrcp &&
                        dbRow.IDmat == csvRow.IDmat &&
                        dbRow.IDzaklad == csvRow.IDzaklad);

                    if (existujeVDb)
                    {
                        string cmdUpdate =
                            $"UPDATE {tableName} " +
                            $"SET Davka = {csvRow.Davka.ToString(CultureInfo.InvariantCulture)}, " +
                            $"Jednotky = '{Esc(csvRow.Jednotky)}', " +
                            $"Tolerance = {csvRow.Tolerance.ToString(CultureInfo.InvariantCulture)}, " +
                            $"row_no = {csvRow.row_no}, " +
                            $"Vazit = {csvRow.Vazit}, " +
                            $"IsDeleted = 0 " +
                            $"WHERE IDrcp = {csvRow.IDrcp} " +
                            $"AND IDmat = {csvRow.IDmat} " +
                            $"AND IDzaklad = {csvRow.IDzaklad}";

                        MySQL.RunNonQuery(cmdUpdate);
                    }
                    else
                    {
                        string cmdInsert =
                            $"INSERT INTO {tableName} " +
                            $"(IDrcp, IDmat, IDzaklad, Davka, Jednotky, Tolerance, row_no, Vazit, IsDeleted) " +
                            $"VALUES " +
                            $"({csvRow.IDrcp}, " +
                            $"{csvRow.IDmat}, " +
                            $"{csvRow.IDzaklad}, " +
                            $"{csvRow.Davka.ToString(CultureInfo.InvariantCulture)}, " +
                            $"'{Esc(csvRow.Jednotky)}', " +
                            $"{csvRow.Tolerance.ToString(CultureInfo.InvariantCulture)}, " +
                            $"{csvRow.row_no}, " +
                            $"{csvRow.Vazit}, " +
                            $"0)";

                        if (MySQL.RunNonQuery(cmdInsert) <= 0)
                            return false;
                    }
                }


                // ==========================================
                // Označení chybějících řádků jako smazané
                // ==========================================
                foreach (RecipeMaterialRow dbRow in dbRows)
                {
                    bool existujeVCsv = csvRows.Any(csvRow =>
                        csvRow.IDrcp == dbRow.IDrcp &&
                        csvRow.IDmat == dbRow.IDmat &&
                        csvRow.IDzaklad == dbRow.IDzaklad);

                    if (!existujeVCsv && dbRow.IsDeleted == 0)
                    {
                        string cmdDelete =
                            $"UPDATE {tableName} " +
                            $"SET IsDeleted = 1 " +
                            $"WHERE IDrcp = {dbRow.IDrcp} " +
                            $"AND IDmat = {dbRow.IDmat} " +
                            $"AND IDzaklad = {dbRow.IDzaklad}";

                        MySQL.RunNonQuery(cmdDelete);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }
    }
}