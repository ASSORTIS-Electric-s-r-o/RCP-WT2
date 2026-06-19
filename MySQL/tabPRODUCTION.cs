using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Windows.Networking.NetworkOperators;
using RCP_WT1.PomocneTridy;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka produkce
    // ==========================================
    internal static class tabPRODUCTION
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tabProductionTable = "tabPRODUCTION";

        public static string tabStatusTable = "tabStatus";

        public static string viewPRODUCTION = "viewPRODUCTION";

        public static string viewRecipesAll = "hmiRecipesAll";


        // ==========================================
        // Třída jednoho řádku statusu
        // ==========================================
        public class tabStatus
        {
            public int ID { get; set; }

            public string Name { get; set; } = "";


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public tabStatus(DataRow row)
            {
                ID = MySQL.CInt(row, "ID");
                Name = MySQL.CStr(row, "Name");
            }
        }


        // ==========================================
        // Třída jednoho řádku produkce
        // ==========================================
        public class tabProduction
        {
            public int ID { get; set; }

            public string DTstart { get; set; } = "";

            public string DTend { get; set; } = "";

            public int IDjob { get; set; }

            public int IDrcp { get; set; }

            public int IDmrg { get; set; }

            public int IDmat { get; set; }

            public int IDzaklad { get; set; }

            public int IDpc { get; set; }

            public string BatchNo { get; set; } = "";

            public float HmotnostNavazena { get; set; }

            public float HmotnostPozadovana { get; set; }

            public float Tolerance { get; set; }

            public int Status { get; set; }

            public int IDuser { get; set; }
            public string UUID { get; set; } = "";

            public int ActionReq { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public tabProduction()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public tabProduction(DataRow row)
            {
                ID = MySQL.CInt(row, "ID");

                DTstart = MySQL.CDT(row, "DTstart");
                DTend = MySQL.CDT(row, "DTend");

                IDjob = MySQL.CInt(row, "IDjob");
                IDrcp = MySQL.CInt(row, "IDrcp");

                IDmrg = row.Table.Columns.Contains("IDmrg")
                    ? MySQL.CInt(row, "IDmrg")
                    : 0;

                IDmat = MySQL.CInt(row, "IDmat");
                IDzaklad = MySQL.CInt(row, "IDzaklad");
                IDpc = MySQL.CInt(row, "IDpc");

                BatchNo = MySQL.CStr(row, "BatchNo");

                HmotnostNavazena = MySQL.CFloat(row, "HmotnostNavazena");
                HmotnostPozadovana = MySQL.CFloat(row, "HmotnostPozadovana");
                Tolerance = MySQL.CFloat(row, "Tolerance");

                Status = MySQL.CInt(row, "Status");
                IDuser = MySQL.CInt(row, "IDuser");
                UUID = row.Table.Columns.Contains("UUID")
                    ? MySQL.CStr(row, "UUID")
                    : "";

                ActionReq = row.Table.Columns.Contains("ActionReq")
                    ? MySQL.CInt(row, "ActionReq")
                    : 0;
            }
        }


        // ==========================================
        // Třída jednoho řádku z viewPRODUCTION
        // ==========================================
        public class viewProduction
        {
            public int IDprod { get; set; }

            public string DTstart { get; set; } = "";

            public string DTend { get; set; } = "";

            public int IDjob { get; set; }

            public int IDrcp { get; set; }

            public int IDmrg { get; set; }

            public int IDmat { get; set; }

            public int IDzaklad { get; set; }

            public int IDpc { get; set; }

            public string JobNo { get; set; } = "";

            public string BatchNo { get; set; } = "";

            public int BatchNoIndex { get; set; }

            public float HmotnostNavazena { get; set; }

            public float HmotnostPozadovana { get; set; }

            public float Tolerance { get; set; }

            public int Status { get; set; }

            public float JobAmountPcs { get; set; }

            public int JobNumberBatch { get; set; }

            public string StatusName { get; set; } = "";

            public string RecipeName { get; set; } = "";

            public string RecipeMatCislo { get; set; } = "";

            public string RecipeCislo { get; set; } = "";

            public int RecipeAmountPcs { get; set; }

            public string MaterialName { get; set; } = "";

            public string MaterialCislo { get; set; } = "";

            public float MaterialDavka { get; set; }

            public float MaterialTolerance { get; set; }

            public int MaterialRow_no { get; set; }

            public int MaterialVazit { get; set; }

            public string MaterialVazitText { get; set; } = "";

            public string PdfProcedurePath { get; set; } = "";

            public string UserName { get; set; } = "";


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public viewProduction(DataRow row)
            {
                IDprod = MySQL.CInt(row, "IDprod");

                DTstart = MySQL.CDT(row, "DTstart");
                DTend = MySQL.CDT(row, "DTend");

                IDjob = MySQL.CInt(row, "IDjob");
                IDrcp = MySQL.CInt(row, "IDrcp");

                IDmrg = row.Table.Columns.Contains("IDmrg")
                    ? MySQL.CInt(row, "IDmrg")
                    : 0;

                IDmat = MySQL.CInt(row, "IDmat");
                IDzaklad = MySQL.CInt(row, "IDzaklad");
                IDpc = MySQL.CInt(row, "IDpc");

                JobNo = MySQL.CStr(row, "JobNo");
                BatchNo = MySQL.CStr(row, "BatchNo");
                BatchNoIndex = MySQL.CInt(row, "BatchNoIndex");

                HmotnostNavazena = MySQL.CFloat(row, "HmotnostNavazena");
                HmotnostPozadovana = MySQL.CFloat(row, "HmotnostPozadovana");
                Tolerance = MySQL.CFloat(row, "Tolerance");

                Status = MySQL.CInt(row, "Status");

                JobAmountPcs = MySQL.CFloat(row, "JobAmountPcs");
                JobNumberBatch = MySQL.CInt(row, "JobNumberBatch");

                StatusName = MySQL.CStr(row, "StatusName");
                RecipeName = MySQL.CStr(row, "RecipeName");
                RecipeMatCislo = MySQL.CStr(row, "RecipeMatCislo");
                RecipeCislo = MySQL.CStr(row, "RecipeCislo");
                RecipeAmountPcs = MySQL.CInt(row, "RecipeAmountPcs");

                MaterialName = MySQL.CStr(row, "MaterialName");
                MaterialCislo = MySQL.CStr(row, "MaterialCislo");
                MaterialDavka = MySQL.CFloat(row, "MaterialDavka");
                MaterialTolerance = MySQL.CFloat(row, "MaterialTolerance");
                MaterialRow_no = MySQL.CInt(row, "MaterialRow_no");
                MaterialVazit = MySQL.CInt(row, "MaterialVazit");
                MaterialVazitText = MySQL.CStr(row, "MaterialVazitText");

                PdfProcedurePath = MySQL.CStr(row, "PdfProcedurePath");
                UserName = MySQL.CStr(row, "UserName");
            }
        }


        // ==========================================
        // Souhrnný řádek produkce
        // ==========================================
        public class ProductionSumRow
        {
            public int IDjob { get; set; }

            public int Status { get; set; }

            public string Name { get; set; } = "";

            public double Poz { get; set; }

            public double Nav { get; set; }

            public double Diff { get; set; }

            public double Accuracy { get; set; }
        }


        // ==========================================
        // Souhrnný řádek materiálu
        // ==========================================
        public class MaterialSumRow
        {
            public int IDjob { get; set; }

            public int Status { get; set; }

            public string Nazev { get; set; } = "";

            public double NavazenoKg { get; set; }

            public string NavazenoText => $"{NavazenoKg:0.000} kg";
        }


        // ==========================================
        // Načtení seznamu statusů
        // ==========================================
        public static (List<tabStatus>, int) GetStatusList()
        {
            return FetchRows<tabStatus>(tabStatusTable);
        }


        // ==========================================
        // Načtení všech řádků produkce
        // ==========================================
        public static (List<tabProduction>, int) GetProduction()
        {
            return FetchRows<tabProduction>(tabProductionTable);
        }


        // ==========================================
        // Načtení produkce podle zakázky a indexu šarže
        // ==========================================
        public static (List<viewProduction>, int) GetProductionViewByID_IDX(int idJob, int batchNoIndex)
        {
            string cmd;

            if (batchNoIndex == 0)
            {
                cmd =
                    $"SELECT * FROM {viewPRODUCTION} " +
                    $"WHERE IDjob = {idJob} " +
                    $"AND BatchNoIndex = " +
                    $"(SELECT MAX(BatchNoIndex) FROM {viewPRODUCTION} WHERE IDjob = {idJob}) " +
                    $"ORDER BY MaterialRow_no ASC, IDprod ASC";
            }
            else
            {
                cmd =
                    $"SELECT * FROM {viewPRODUCTION} " +
                    $"WHERE IDjob = {idJob} " +
                    $"AND BatchNoIndex = {batchNoIndex} " +
                    $"ORDER BY MaterialRow_no ASC, IDprod ASC";
            }

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            if (table == null || table.Rows.Count == 0)
                return (new List<viewProduction>(), 0);

            return (
                table.AsEnumerable().Select(row => new viewProduction(row)).ToList(),
                rowcount
            );
        }


        // ==========================================
        // Získání nejvyššího indexu šarže
        // ==========================================
        public static int GetMaxBatchNoIndex(int idJob)
        {
            string cmd =
                $"SELECT MAX(BatchNoIndex) AS MaxBatchNoIndex " +
                $"FROM {viewPRODUCTION} " +
                $"WHERE IDjob = {idJob}";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            if (table != null && table.Rows.Count > 0)
                return MySQL.CInt(table.Rows[0], "MaxBatchNoIndex");

            return 0;
        }


        // ==========================================
        // Univerzální načtení řádků z tabulky
        // ==========================================
        private static (List<T>, int) FetchRows<T>(string tableName) where T : class
        {
            try
            {
                string cmd =
                    $"SELECT * FROM {tableName}";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rows == null || rows.Rows.Count == 0)
                    return (new List<T>(), 0);

                List<T> result = new();

                foreach (DataRow row in rows.Rows)
                {
                    object? instance = Activator.CreateInstance(typeof(T), row);

                    if (instance is T item)
                    {
                        result.Add(item);
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při načítání tabulky '{tableName}': {ex.Message}");
                return (new List<T>(), 0);
            }
        }


        // ==========================================
        // Načtení celé view produkce
        // ==========================================
        public static (List<viewProduction>, int) GetProductionViewAll()
        {
            string cmd =
                $"SELECT * FROM {viewPRODUCTION}";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            if (table == null || table.Rows.Count == 0)
                return (new List<viewProduction>(), 0);

            return (
                table.AsEnumerable().Select(row => new viewProduction(row)).ToList(),
                rowcount
            );
        }


        // ==========================================
        // Načtení seznamu šarží podle zakázky
        // ==========================================
        public static List<string> GetBatchNoList(int idJob)
        {
            string cmd =
                $"SELECT DISTINCT BatchNo " +
                $"FROM {viewPRODUCTION} " +
                $"WHERE IDjob = {idJob} " +
                $"ORDER BY BatchNo";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            List<string> result = new();

            if (table == null || table.Rows.Count == 0)
                return result;

            foreach (DataRow row in table.Rows)
            {
                string batchNo = MySQL.CStr(row, "BatchNo");

                if (!string.IsNullOrEmpty(batchNo))
                {
                    result.Add(batchNo);
                }
            }

            return result;
        }

        // ==========================================
        // Načtení roků výroby
        // ==========================================
        public static List<int> SelectYearsVyroba(int modeKey)
        {
            string cmd;

            if (modeKey == 2)
            {
                cmd =
                    $"SELECT DISTINCT YEAR(p.DTstart) AS Y " +
                    $"FROM {viewPRODUCTION} p " +
                    $"WHERE p.IDzaklad = 0 " +
                    $"ORDER BY Y DESC";
            }
            else
            {
                cmd =
                    $"SELECT DISTINCT YEAR(p.DTstart) AS Y " +
                    $"FROM {viewPRODUCTION} p " +
                    $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                    $"WHERE j.RecipeIsZaklad = {modeKey} " +
                    $"ORDER BY Y DESC";
            }

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<int>();

            return table.AsEnumerable()
                .Select(row => MySQL.CInt(row, "Y"))
                .Where(year => year > 0)
                .ToList();
        }


        // ==========================================
        // Načtení měsíců výroby
        // ==========================================
        public static List<int> SelectMonthsVyroba(int modeKey, int year)
        {
            string whereYear = year > 0
                ? $" AND YEAR(p.DTstart) = {year}"
                : "";

            string cmd;

            if (modeKey == 2)
            {
                cmd =
                    $"SELECT DISTINCT MONTH(p.DTstart) AS M " +
                    $"FROM {viewPRODUCTION} p " +
                    $"WHERE p.IDzaklad = 0 " +
                    $"{whereYear} " +
                    $"ORDER BY M DESC";
            }
            else
            {
                cmd =
                    $"SELECT DISTINCT MONTH(p.DTstart) AS M " +
                    $"FROM {viewPRODUCTION} p " +
                    $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                    $"WHERE j.RecipeIsZaklad = {modeKey} " +
                    $"{whereYear} " +
                    $"ORDER BY M DESC";
            }

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<int>();

            return table.AsEnumerable()
                .Select(row => MySQL.CInt(row, "M"))
                .Where(month => month >= 1 && month <= 12)
                .ToList();
        }


        // ==========================================
        // Načtení týdnů výroby
        // ==========================================
        public static List<int> SelectWeeksVyroba(int modeKey, int year, int month)
        {
            string where = "";

            if (year > 0)
                where += $" AND YEAR(p.DTstart) = {year}";

            if (month > 0)
                where += $" AND MONTH(p.DTstart) = {month}";

            string cmd;

            if (modeKey == 2)
            {
                cmd =
                    $"SELECT DISTINCT WEEK(p.DTstart, 3) AS W " +
                    $"FROM {viewPRODUCTION} p " +
                    $"WHERE p.IDzaklad = 0 " +
                    $"{where} " +
                    $"ORDER BY W DESC";
            }
            else
            {
                cmd =
                    $"SELECT DISTINCT WEEK(p.DTstart, 3) AS W " +
                    $"FROM {viewPRODUCTION} p " +
                    $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                    $"WHERE j.RecipeIsZaklad = {modeKey} " +
                    $"{where} " +
                    $"ORDER BY W DESC";
            }

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<int>();

            return table.AsEnumerable()
                .Select(row => MySQL.CInt(row, "W"))
                .Where(week => week > 0)
                .ToList();
        }


        // ==========================================
        // Načtení dnů výroby
        // ==========================================
        public static List<int> SelectDaysVyroba(int modeKey, int year, int month, int week)
        {
            string where = "";

            if (year > 0)
                where += $" AND YEAR(p.DTstart) = {year}";

            if (month > 0)
                where += $" AND MONTH(p.DTstart) = {month}";

            if (week > 0)
                where += $" AND WEEK(p.DTstart, 3) = {week}";

            string cmd;

            if (modeKey == 2)
            {
                cmd =
                    $"SELECT DISTINCT DAY(p.DTstart) AS D " +
                    $"FROM {viewPRODUCTION} p " +
                    $"WHERE p.IDzaklad = 0 " +
                    $"{where} " +
                    $"ORDER BY D DESC";
            }
            else
            {
                cmd =
                    $"SELECT DISTINCT DAY(p.DTstart) AS D " +
                    $"FROM {viewPRODUCTION} p " +
                    $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                    $"WHERE j.RecipeIsZaklad = {modeKey} " +
                    $"{where} " +
                    $"ORDER BY D DESC";
            }

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<int>();

            return table.AsEnumerable()
                .Select(row => MySQL.CInt(row, "D"))
                .Where(day => day >= 1 && day <= 31)
                .ToList();
        }


        // ==========================================
        // Načtení operátorů výroby
        // ==========================================
        public static List<string> SelectOperatorsVyroba(int modeKey,int year,int month,int week,int day)
        {
            string where = "";

            if (year > 0)
                where += $" AND YEAR(p.DTstart) = {year}";

            if (month > 0)
                where += $" AND MONTH(p.DTstart) = {month}";

            if (week > 0)
                where += $" AND WEEK(p.DTstart, 3) = {week}";

            if (day > 0)
                where += $" AND DAY(p.DTstart) = {day}";

            string cmd;

            if (modeKey == 2)
            {
                cmd =
                    $"SELECT DISTINCT p.UserName " +
                    $"FROM {viewPRODUCTION} p " +
                    $"WHERE p.IDzaklad = 0 " +
                    $"AND p.UserName IS NOT NULL " +
                    $"{where} " +
                    $"ORDER BY p.UserName";
            }
            else
            {
                cmd =
                    $"SELECT DISTINCT p.UserName " +
                    $"FROM {viewPRODUCTION} p " +
                    $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                    $"WHERE j.RecipeIsZaklad = {modeKey} " +
                    $"AND p.UserName IS NOT NULL " +
                    $"{where} " +
                    $"ORDER BY p.UserName";
            }

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<string>();

            return table.AsEnumerable()
                .Select(row => MySQL.CStr(row, "UserName"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }

        // ==========================================
        // Souhrn receptur a základů
        // ==========================================
        public static List<ProductionSumRow> SelectSummaryVyroba(int modeKey, int year,int month,int week,int day,string userName)
        {
            string where = "";

            if (year > 0)
                where += $" AND YEAR(p.DTstart) = {year}";

            if (month > 0)
                where += $" AND MONTH(p.DTstart) = {month}";

            if (week > 0)
                where += $" AND WEEK(p.DTstart, 3) = {week}";

            if (day > 0)
                where += $" AND DAY(p.DTstart) = {day}";

            if (!string.IsNullOrWhiteSpace(userName) &&
                userName != "-Vše-")
            {
                where += $" AND p.UserName = '{userName}'";
            }

            string cmd =
                $"SELECT " +
                $"j.RecipeName AS Name, " +
                $"SUM(p.HmotnostPozadovana) AS Poz, " +
                $"SUM(p.HmotnostNavazena) AS Nav " +
                $"FROM {viewPRODUCTION} p " +
                $"JOIN {tabJOB_LIST.viewJOB_LIST} j ON j.IDjob = p.IDjob " +
                $"WHERE j.RecipeIsZaklad = {modeKey} " +
                $"{where} " +
                $"GROUP BY j.RecipeName " +
                $"ORDER BY j.RecipeName";

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null)
                return new List<ProductionSumRow>();

            return table.AsEnumerable()
                .Select(row =>
                {
                    double poz = MySQL.CFloat(row, "Poz");
                    double nav = MySQL.CFloat(row, "Nav");

                    return new ProductionSumRow
                    {
                        Name = MySQL.CStr(row, "Name"),
                        Poz = Math.Round(poz, 3),
                        Nav = Math.Round(nav, 3),
                        Diff = Math.Round(nav - poz, 3),
                        Accuracy = poz > 0
                            ? Math.Round((nav / poz) * 100.0, 2)
                            : 0
                    };
                })
                .ToList();
        }


        // ==========================================
        // Souhrn materiálů
        // ==========================================
        public static List<MaterialSumRow> SelectSummaryMaterialyVyroba(int year,int month,int week,int day,string userName)
        {
            string where = "WHERE p.IDzaklad = 0";

            if (year > 0)
                where += $" AND YEAR(p.DTstart) = {year}";

            if (month > 0)
                where += $" AND MONTH(p.DTstart) = {month}";

            if (week > 0)
                where += $" AND WEEK(p.DTstart, 3) = {week}";

            if (day > 0)
                where += $" AND DAY(p.DTstart) = {day}";

            if (!string.IsNullOrWhiteSpace(userName) &&
                userName != "-Vše-")
            {
                where += $" AND p.UserName = '{userName}'";
            }

            string cmd =
                $"SELECT " +
                $"p.MaterialName AS Nazev, " +
                $"SUM(p.HmotnostNavazena) AS Navazeno " +
                $"FROM {viewPRODUCTION} p " +
                $"{where} " +
                $"GROUP BY p.MaterialName " +
                $"ORDER BY p.MaterialName";

            var (_, table) = MySQL.RunSelectCmd(cmd);

            List<MaterialSumRow> result = new();

            if (table == null)
                return result;

            foreach (DataRow row in table.Rows)
            {
                result.Add(new MaterialSumRow
                {
                    Nazev = MySQL.CStr(row, "Nazev"),
                    NavazenoKg = Math.Round(
                        MySQL.CFloat(row, "Navazeno"),
                        3)
                });
            }

            return result;
        }


        // ==========================================
        // Kontrola existence řádků produkce podle zakázky
        // ==========================================
        public static bool HasProductionRows(int idjob)
        {
            if (idjob <= 0)
                return false;

            string cmd =
                $"SELECT COUNT(*) AS Pocet " +
                $"FROM {tabProductionTable} " +
                $"WHERE IDjob = {idjob}";

            var (_, table) = MySQL.RunSelectCmd(cmd);

            if (table == null || table.Rows.Count == 0)
                return false;

            return MySQL.CInt(table.Rows[0], "Pocet") > 0;
        }


        // ==========================================
        // Aktualizace statusu produkce
        // ==========================================
        public static void UpdateProductionStatus(int status, int idprod)
        {
            string timeUpdate = status switch
            {
                1 => "DTstart = NOW(),",
                10 => "DTend = NOW(),",
                _ => ""
            };

            string idpcUpdate =
                status == 1
                    ? $"IDpc = {Settings.Param_PC_ID},"
                    : "IDpc = 0,";

            string cmd =
                $"UPDATE {tabProductionTable} " +
                $"SET {timeUpdate} " +
                $"{idpcUpdate} " +
                $"Status = {status} " +
                $"WHERE ID = {idprod}";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Aktualizace navážené hmotnosti
        // ==========================================
        public static void UpdateProductionAmount(int idprod, double hmotnost)
        {
            int? idUser = UserSession.CurrentUser?.IDuser;

            string idUserValue =
                idUser.HasValue
                    ? idUser.Value.ToString()
                    : "NULL";

            string hmotnostSql =
                hmotnost.ToString("0.############", CultureInfo.InvariantCulture);

            string cmd =
                $"UPDATE {tabProductionTable} " +
                $"SET HmotnostNavazena = {hmotnostSql}, " +
                $"IDuser = {idUserValue}, " +
                $"DTend = NOW() " +
                $"WHERE ID = {idprod}";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Vložení produkce z receptury
        // ==========================================
        public static void InsertProductionFromRecipe(tabProduction data)
        {
            string batchNo =
                (data.BatchNo ?? "").Replace("'", "''");

            string cmd =
                $"INSERT INTO {tabProductionTable} " +
                $"(IDjob, IDrcp, IDmrg, IDmat, IDzaklad, BatchNo, HmotnostNavazena, HmotnostPozadovana, Tolerance, Status) " +
                $"VALUES " +
                $"({data.IDjob}, {data.IDrcp}, {data.IDmrg}, {data.IDmat}, {data.IDzaklad}, '{batchNo}', " +
                $"{data.HmotnostNavazena.ToString(CultureInfo.InvariantCulture)}, " +
                $"{data.HmotnostPozadovana.ToString(CultureInfo.InvariantCulture)}, " +
                $"{data.Tolerance.ToString(CultureInfo.InvariantCulture)}, " +
                $"{data.Status})";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Smazání produkce podle zakázky
        // ==========================================
        public static void DeleteByJob(int idjob)
        {
            if (idjob <= 0)
                return;

            string cmd =
                $"DELETE FROM {tabProductionTable} " +
                $"WHERE IDjob = {idjob}";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Nastavení ActionReq pouze pokud má zakázka UUID
        // ==========================================
        public static void SetActionReq(int idprod, int actionReq)
        {
            if (idprod <= 0)
                return;

            string cmd =
                $"UPDATE {tabProductionTable} p " +
                $"INNER JOIN {tabJOB_LIST.tabJobListTable} j ON p.IDjob = j.ID " +
                $"SET p.ActionReq = {actionReq} " +
                $"WHERE p.ID = {idprod} " +
                $"AND j.UUID IS NOT NULL " +
                $"AND j.UUID <> ''";

            MySQL.RunNonQuery(cmd);
        }

    }




}