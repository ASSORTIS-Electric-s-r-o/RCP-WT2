using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka receptur
    // ==========================================
    internal static class tabRECIPES
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tableName = "tabRECIPES";

        public static string viewName = "hmiRECIPES";

        public static string hmiRecipesAll = "hmiRecipesAll";

        public static string logFile =
            new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";


        // ==========================================
        // Třída jednoho řádku receptury
        // ==========================================
        public class RecipeRow
        {
            public int IDjob { get; set; }

            public int IDrcp { get; set; }

            public int IDgrp { get; set; }

            public string Cislo { get; set; } = "";

            public string Name { get; set; } = "";

            public float AmountPcs { get; set; }

            public int IsDeleted { get; set; }

            public int IsZaklad { get; set; }

            public string RecipeName { get; set; } = "";

            public string PdfProcedurePath { get; set; } = "";


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public RecipeRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public RecipeRow(DataRow row)
            {
                IDrcp = MySQL.CInt(row, "IDrcp");
                IDgrp = MySQL.CInt(row, "IDgrp");

                Cislo = row.Table.Columns.Contains("Cislo")
                    ? MySQL.CStr(row, "Cislo")
                    : "";

                Name = MySQL.CStr(row, "Name");
                AmountPcs = MySQL.CFloat(row, "AmountPcs");
                IsDeleted = MySQL.CInt(row, "IsDeleted");

                IsZaklad = row.Table.Columns.Contains("IsZaklad")
                    ? MySQL.CInt(row, "IsZaklad")
                    : 0;

                RecipeName = row.Table.Columns.Contains("RecipeName")
                    ? MySQL.CStr(row, "RecipeName")
                    : "";

                PdfProcedurePath = row.Table.Columns.Contains("PdfProcedurePath")
                    ? MySQL.CStr(row, "PdfProcedurePath")
                    : "";
            }
        }


        // ==========================================
        // Třída jednoho řádku detailu receptury
        // ==========================================
        public class RecipeDetailRow
        {
            public int IDjob { get; set; }

            public int IDgrp { get; set; }

            public string GrpType { get; set; } = "";

            public string GroupName { get; set; } = "";

            public string GroupImage { get; set; } = "";

            public int GroupIsZaklad { get; set; }

            public int MaterialIsZaklad { get; set; }

            public int IDzaklad { get; set; }

            public string RecipeMatCislo { get; set; } = "";

            public int IDrcp { get; set; }

            public string RecipeCislo { get; set; } = "";

            public string RecipeName { get; set; } = "";

            public int RecipeIsDeleted { get; set; }

            public float AmountPcs { get; set; }

            public string PdfProcedurePath { get; set; } = "";

            public int IDmat { get; set; }

            public string MaterialCislo { get; set; } = "";

            public string MaterialName { get; set; } = "";

            public string MaterialImage { get; set; } = "";

            public int MaterialIsDeleted { get; set; }

            public float Davka { get; set; }

            public float Tolerance { get; set; }

            public int row_no { get; set; }

            public int Vazit { get; set; }

            public int Status { get; set; }

            public float BaseDavka { get; set; }

            public float BaseTolerance { get; set; }

            public string UserName { get; set; } = "";


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public RecipeDetailRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public RecipeDetailRow(DataRow row)
            {
                IDgrp = MySQL.CInt(row, "IDgrp");

                GrpType = row.Table.Columns.Contains("GrpType") ? MySQL.CStr(row, "GrpType"): "";
                GroupName = row.Table.Columns.Contains("GroupName")? MySQL.CStr(row, "GroupName"): "";
                GroupImage = row.Table.Columns.Contains("GroupImage")? MySQL.CStr(row, "GroupImage"): "";
                GroupIsZaklad = row.Table.Columns.Contains("GroupIsZaklad")? MySQL.CInt(row, "GroupIsZaklad"): 0;
                RecipeMatCislo = row.Table.Columns.Contains("RecipeMatCislo")? MySQL.CStr(row, "RecipeMatCislo"): "";
                IDrcp = MySQL.CInt(row, "IDrcp");
                RecipeCislo = row.Table.Columns.Contains("RecipeCislo")? MySQL.CStr(row, "RecipeCislo"): "";
                RecipeName = row.Table.Columns.Contains("RecipeName")? MySQL.CStr(row, "RecipeName"): "";
                RecipeIsDeleted = row.Table.Columns.Contains("RecipeIsDeleted")? MySQL.CInt(row, "RecipeIsDeleted"): 0;
                AmountPcs = row.Table.Columns.Contains("AmountPcs")? MySQL.CFloat(row, "AmountPcs"): 0;
                PdfProcedurePath = row.Table.Columns.Contains("PdfProcedurePath")? MySQL.CStr(row, "PdfProcedurePath"): "";
                IDmat = row.Table.Columns.Contains("IDmat")? MySQL.CInt(row, "IDmat"): 0;
                IDzaklad = row.Table.Columns.Contains("IDzaklad")? MySQL.CInt(row, "IDzaklad"): 0;
                MaterialIsZaklad = row.Table.Columns.Contains("MaterialIsZaklad")? MySQL.CInt(row, "MaterialIsZaklad"): 0;
                MaterialCislo = row.Table.Columns.Contains("MaterialCislo")? MySQL.CStr(row, "MaterialCislo") : "";
                MaterialName = row.Table.Columns.Contains("MaterialName")? MySQL.CStr(row, "MaterialName"): "";
                MaterialImage = row.Table.Columns.Contains("MaterialImage")? MySQL.CStr(row, "MaterialImage"): "";
                MaterialIsDeleted = row.Table.Columns.Contains("MaterialIsDeleted")? MySQL.CInt(row, "MaterialIsDeleted"): 0;
                Davka = row.Table.Columns.Contains("Davka")? MySQL.CFloat(row, "Davka"): 0;
                Tolerance = row.Table.Columns.Contains("Tolerance")? MySQL.CFloat(row, "Tolerance"): 0;
                row_no = row.Table.Columns.Contains("row_no")? MySQL.CInt(row, "row_no"): 0;
                Vazit = row.Table.Columns.Contains("Vazit")? MySQL.CInt(row, "Vazit"): 0;
                Status = row.Table.Columns.Contains("Status")? MySQL.CInt(row, "Status"): 0;
                BaseDavka = row.Table.Columns.Contains("BaseDavka")? MySQL.CFloat(row, "BaseDavka"): 0;
                BaseTolerance = row.Table.Columns.Contains("BaseTolerance")? MySQL.CFloat(row, "BaseTolerance"): 0;
                UserName = row.Table.Columns.Contains("UserName")? MySQL.CStr(row, "UserName"): "";
            }

        }

        // ==========================================
        // Načtení všech receptur
        // ==========================================
        public static (List<RecipeRow>, int) GetRecipeRows()
            {
                string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

                try
                {
                    string cmd =
                        $"SELECT * FROM {viewName} " +
                        $"WHERE IsDeleted = 0";

                    var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                    List<RecipeRow> result = new();

                    if (rowcount > 0)
                    {
                        foreach (DataRow row in rows.Rows)
                        {
                            result.Add(new RecipeRow(row));
                        }
                    }

                    return (result, rowcount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                    return (new List<RecipeRow>(), 0);
                }
            }


        // ==========================================
        // Načtení receptur podle skupiny
        // ==========================================
        public static (List<RecipeRow>, int) GetRecipeRowsByID(int id)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDgrp = {id} " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeRow>(), 0);
            }
        }


        // ==========================================
        // Načtení všech detailních řádků receptur
        // ==========================================
        public static (List<RecipeDetailRow>, int) GetRecipesAll()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {hmiRecipesAll} " +
                    $"WHERE RecipeIsDeleted = 0 " +
                    $"AND MaterialIsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeDetailRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeDetailRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeDetailRow>(), 0);
            }
        }


        // ==========================================
        // Načtení detailu receptury podle ID receptury
        // ==========================================
        public static (List<RecipeDetailRow>, int) GetRecipesByID(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {hmiRecipesAll} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND RecipeIsDeleted = 0 " +
                    $"AND MaterialIsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeDetailRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeDetailRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeDetailRow>(), 0);
            }
        }


        // ==========================================
        // Načtení jedné receptury podle ID receptury
        // ==========================================
        public static (RecipeRow?, bool) GetRecipeRowByIDrcp(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IsDeleted = 0";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0 && table.Rows.Count > 0)
                {
                    return (new RecipeRow(table.Rows[0]), true);
                }

                return (null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (null, false);
            }
        }


        // ==========================================
        // Načtení seznamu receptur podle ID receptury
        // ==========================================
        public static (List<RecipeRow>, int) GetRecipeRowsByIDrcp(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDrcp = {idrcp} " +
                    $"AND IsDeleted = 0";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                List<RecipeRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        result.Add(new RecipeRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeRow>(), 0);
            }
        }


        // ==========================================
        // Načtení receptur označených jako základ
        // ==========================================
        public static (List<RecipeRow>, int) GetRecipesIsZaklad()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsZaklad = 1 " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<RecipeRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new RecipeRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<RecipeRow>(), 0);
            }
        }

        // ==========================================
        // Získání dalšího volného ID receptury
        // ==========================================
        public static int GetNextID()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT IFNULL(MAX(IDrcp), 0) + 1 AS NextID " +
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
        // Kontrola existence sloupce Cislo
        // ==========================================
        private static bool MaSloupecCislo()
        {
            try
            {
                string cmd =
                    "SELECT COUNT(*) AS Pocet " +
                    "FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = 'RCP' " +
                    "AND TABLE_NAME = 'tabRECIPES' " +
                    "AND COLUMN_NAME = 'Cislo'";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0 && table.Rows.Count > 0)
                {
                    return MySQL.CInt(table.Rows[0], "Pocet") > 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }


        // ==========================================
        // Vložení nové receptury
        // ==========================================
        public static int InsertRecipe(RecipeRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                int newID = GetNextID();

                string nameSafe =
                    (row.Name ?? "").Replace("'", "''");

                string cisloSafe =
                    (row.Cislo ?? "").Replace("'", "''");

                string amountSafe =
                    row.AmountPcs.ToString(CultureInfo.InvariantCulture);

                string pdfPath =
                    string.IsNullOrWhiteSpace(row.PdfProcedurePath)
                        ? ""
                        : row.PdfProcedurePath.Replace("\\", "\\\\").Replace("'", "''");

                string cmd;

                if (MaSloupecCislo())
                {
                    cmd =
                        $"INSERT INTO {tableName} " +
                        $"(IDrcp, IDgrp, Cislo, Name, AmountPcs, IsZaklad, PdfProcedurePath) " +
                        $"VALUES " +
                        $"({newID}, {row.IDgrp}, '{cisloSafe}', '{nameSafe}', {amountSafe}, {row.IsZaklad}, '{pdfPath}')";
                }
                else
                {
                    cmd =
                        $"INSERT INTO {tableName} " +
                        $"(IDrcp, IDgrp, Name, AmountPcs, IsZaklad, PdfProcedurePath) " +
                        $"VALUES " +
                        $"({newID}, {row.IDgrp}, '{nameSafe}', {amountSafe}, {row.IsZaklad}, '{pdfPath}')";
                }

                int result = MySQL.RunInsertCmd(cmd);

                return result >= 0 ? newID : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return 0;
            }
        }


        // ==========================================
        // Vložení receptury s předaným ID
        // ==========================================
        public static int InsertRecipeID(RecipeRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string nameSafe =
                    (row.Name ?? "").Replace("'", "''");

                string cisloSafe =
                    (row.Cislo ?? "").Replace("'", "''");

                string amountSafe =
                    row.AmountPcs.ToString(CultureInfo.InvariantCulture);

                string pdfPath =
                    string.IsNullOrWhiteSpace(row.PdfProcedurePath)
                        ? ""
                        : row.PdfProcedurePath.Replace("\\", "\\\\").Replace("'", "''");

                string cmd;

                if (MaSloupecCislo())
                {
                    cmd =
                        $"INSERT INTO {tableName} " +
                        $"(IDrcp, IDgrp, Cislo, Name, AmountPcs, IsZaklad, PdfProcedurePath) " +
                        $"VALUES " +
                        $"({row.IDrcp}, {row.IDgrp}, '{cisloSafe}', '{nameSafe}', {amountSafe}, {row.IsZaklad}, '{pdfPath}')";
                }
                else
                {
                    cmd =
                        $"INSERT INTO {tableName} " +
                        $"(IDrcp, IDgrp, Name, AmountPcs, IsZaklad, PdfProcedurePath) " +
                        $"VALUES " +
                        $"({row.IDrcp}, {row.IDgrp}, '{nameSafe}', {amountSafe}, {row.IsZaklad}, '{pdfPath}')";
                }

                int result = MySQL.RunInsertCmd(cmd);

                return result >= 0 ? row.IDrcp : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return 0;
            }
        }

        // ==========================================
        // Aktualizace receptury
        // ==========================================
        public static bool UpdateRecipe(RecipeRow row)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string nameSafe =
                    (row.Name ?? "").Replace("'", "''");

                string cisloSafe =
                    (row.Cislo ?? "").Replace("'", "''");

                string pathSafe =
                    (row.PdfProcedurePath ?? "").Replace("\\", "\\\\").Replace("'", "''");

                string amountSafe =
                    row.AmountPcs.ToString(CultureInfo.InvariantCulture);

                string cmd;

                if (MaSloupecCislo())
                {
                    cmd =
                        $"UPDATE {tableName} " +
                        $"SET IDgrp = {row.IDgrp}, " +
                        $"Cislo = '{cisloSafe}', " +
                        $"Name = '{nameSafe}', " +
                        $"AmountPcs = {amountSafe}, " +
                        $"IsZaklad = {row.IsZaklad}, " +
                        $"PdfProcedurePath = '{pathSafe}' " +
                        $"WHERE IDrcp = {row.IDrcp}";
                }
                else
                {
                    cmd =
                        $"UPDATE {tableName} " +
                        $"SET IDgrp = {row.IDgrp}, " +
                        $"Name = '{nameSafe}', " +
                        $"AmountPcs = {amountSafe}, " +
                        $"IsZaklad = {row.IsZaklad}, " +
                        $"PdfProcedurePath = '{pathSafe}' " +
                        $"WHERE IDrcp = {row.IDrcp}";
                }

                Debug.WriteLine("========================================");
                Debug.WriteLine("SAVE RECIPE");
                Debug.WriteLine($"Funkce    : {_name}");
                Debug.WriteLine($"IDrcp     : {row.IDrcp}");
                Debug.WriteLine($"IDgrp     : {row.IDgrp}");
                Debug.WriteLine($"IsZaklad  : {row.IsZaklad}");
                Debug.WriteLine($"Cislo     : {row.Cislo}");
                Debug.WriteLine($"Name      : {row.Name}");
                Debug.WriteLine($"AmountPcs : {amountSafe}");
                Debug.WriteLine("SQL:");
                Debug.WriteLine(cmd);
                Debug.WriteLine("========================================");

                int result = MySQL.RunNonQuery(cmd);

                Debug.WriteLine("========================================");
                Debug.WriteLine($"UPDATE RECEPT RESULT: {result}");
                Debug.WriteLine("========================================");

                return result >= 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("========================================");
                Debug.WriteLine("CHYBA UPDATE RECEPTU");
                Debug.WriteLine($"Funkce: {_name}");
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("========================================");

                return false;
            }
        }


        // ==========================================
        // Aktualizace příznaku základ pro skupinu
        // ==========================================
        public static bool UpdateIsZaklad(int idgrp, int isZaklad)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsZaklad = {isZaklad} " +
                    $"WHERE IDgrp = {idgrp}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Logické smazání receptury
        // ==========================================
        public static bool DeleteRecipe(int idrcp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsDeleted = 1 " +
                    $"WHERE IDrcp = {idrcp}";

                return MySQL.RunNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return false;
            }
        }


        // ==========================================
        // Kontrola existence receptury
        // ==========================================
        public static bool ExistsRecipe(int idrcp)
        {
            try
            {
                string cmd =
                    $"SELECT COUNT(*) AS Pocet " +
                    $"FROM {tableName} " +
                    $"WHERE IDrcp = {idrcp}";

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
        // Vložení nebo aktualizace receptury
        // ==========================================
        public static bool UpsertRecipe(RecipeRow row)
        {
            try
            {
                if (row == null || row.IDrcp <= 0)
                    return false;

                if (ExistsRecipe(row.IDrcp))
                    return UpdateRecipe(row);

                return InsertRecipeID(row) > 0;
            }
            catch
            {
                return false;
            }
        }



    }
}