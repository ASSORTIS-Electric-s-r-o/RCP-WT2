using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka zakázek
    // ==========================================
    internal static class tabJOB_LIST
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tabJobListTable = "tabJOB_LIST";

        public static string viewJOB_LIST = "viewJOB_LIST";


        // ==========================================
        // Třída jednoho řádku z tabJOB_LIST
        // ==========================================
        public class tabJobList
        {
            public int ID { get; set; }

            public string DT { get; set; } = "";

            public string JobNo { get; set; } = "";

            public string BatchNo { get; set; } = "";

            public int IDrcp { get; set; }

            public int IDgrp { get; set; }

            public int IDmrg { get; set; }

            public int Status { get; set; }

            public int ImportSource { get; set; }

            public float AmountPcs { get; set; }

            public int PlannedBatch { get; set; }

            public int StationIdx { get; set; }

            public int ZakladVypocten { get; set; }
            public string UUID { get; set; } = "";

            public int ActionReq { get; set; }



            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public tabJobList(DataRow row)
            {
                ID = MySQL.CInt(row, "ID");
                DT = MySQL.CDT(row, "DT");

                JobNo = MySQL.CStr(row, "JobNo");
                BatchNo = MySQL.CStr(row, "BatchNo");

                IDrcp = MySQL.CInt(row, "IDrcp");
                IDgrp = MySQL.CInt(row, "IDgrp");

                IDmrg = row.Table.Columns.Contains("IDmrg")
                    ? MySQL.CInt(row, "IDmrg")
                    : 0;

                Status = MySQL.CInt(row, "Status");
                ImportSource = MySQL.CInt(row, "ImportSource");
                AmountPcs = MySQL.CFloat(row, "AmountPcs");
                PlannedBatch = MySQL.CInt(row, "PlannedBatch");
                StationIdx = MySQL.CInt(row, "StationIdx");
                ZakladVypocten = MySQL.CInt(row, "ZakladVypocten");
                UUID = row.Table.Columns.Contains("UUID")
                    ? MySQL.CStr(row, "UUID")
                    : "";

                ActionReq = row.Table.Columns.Contains("ActionReq")
                    ? MySQL.CInt(row, "ActionReq")
                    : 0;
            }
        }


        // ==========================================
        // Třída pro vložení nové zakázky
        // ==========================================
        public class tabJobListInsert
        {
            public string JobNo { get; set; } = "";

            public string BatchNo { get; set; } = "";

            public int IDrcp { get; set; }

            public int IDgrp { get; set; }

            public int IDmrg { get; set; }

            public int Status { get; set; }

            public float AmountPcs { get; set; }

            public int PlannedBatch { get; set; }

            public int StationIdx { get; set; }

            public int ImportSource { get; set; } = 0;

            public DateTime? DT { get; set; } = null;

            public int ZakladVypocten { get; set; } = 0;

        }


        // ==========================================
        // Třída jednoho řádku z viewJOB_LIST
        // ==========================================
        public class viewJobList
        {
            public int IDjob { get; set; }

            public string DT { get; set; } = "";

            public string DTname { get; set; } = "";

            public string JobNo { get; set; } = "";

            public string BatchNo { get; set; } = "";

            public int IDrcp { get; set; }

            public int IDgrp { get; set; }

            public int IDmrg { get; set; }

            public int Status { get; set; }

            public string StatusName { get; set; } = "";

            public int ImportSource { get; set; }

            public string ImportSourceName { get; set; } = "";

            public float ReqAmountPcs { get; set; }

            public string ReqAmountPcsText =>
                RecipeIsZaklad > 0
                    ? $"{ReqAmountPcs:0.000} kg"
                    : $"{(int)Math.Round(ReqAmountPcs)} ks";

            public int ReqNumberBatch { get; set; }

            public int ZakladVypocten { get; set; }

            public string RecipeName { get; set; } = "";

            public int RecipeAmountPcs { get; set; }

            public int StationIdx { get; set; }

            public int RecipeIsZaklad { get; set; }
            public string DeliveryDate { get; set; } = "";

            public string DeliveryDateName { get; set; } = "";

            public int DeliveryShift { get; set; }

            public string DeliveryShiftName =>
                DeliveryShift switch
                {
                    1 => "Ranní",
                    2 => "Odpolední",
                    _ => "Neurčeno"
                };


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public viewJobList()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public viewJobList(DataRow row)
            {
                IDjob = MySQL.CInt(row, "IDjob");
                DT = MySQL.CDT(row, "DT");

                if (DateTime.TryParse(DT, out DateTime dt))
                {
                    DTname = dt.ToString("dddd dd.MM.yyyy", new CultureInfo("cs-CZ"));
                }
                else
                {
                    DTname = "";
                }

                JobNo = MySQL.CStr(row, "JobNo");
                BatchNo = MySQL.CStr(row, "BatchNo");

                IDrcp = MySQL.CInt(row, "IDrcp");

                IDmrg = row.Table.Columns.Contains("IDmrg")
                    ? MySQL.CInt(row, "IDmrg")
                    : 0;

                IDgrp = MySQL.CInt(row, "IDgrp");
                Status = MySQL.CInt(row, "Status");
                StatusName = MySQL.CStr(row, "StatusName");

                ImportSource = MySQL.CInt(row, "ImportSource");
                ImportSourceName = MySQL.CStr(row, "ImportSourceName");

                ReqAmountPcs = MySQL.CFloat(row, "ReqAmountPcs");
                ReqNumberBatch = MySQL.CInt(row, "ReqNumberBatch");

                RecipeName = MySQL.CStr(row, "RecipeName");
                RecipeAmountPcs = MySQL.CInt(row, "RecipeAmountPcs");

                StationIdx = MySQL.CInt(row, "StationIdx");
                ZakladVypocten = MySQL.CInt(row, "ZakladVypocten");
                RecipeIsZaklad = MySQL.CInt(row, "RecipeIsZaklad");

                DeliveryDate = row.Table.Columns.Contains("DeliveryDate")
                    ? MySQL.CDT(row, "DeliveryDate")
                    : "";

                if (DateTime.TryParse(DeliveryDate, out DateTime deliveryDate))
                {
                    DeliveryDateName = deliveryDate.ToString("dddd dd.MM.yyyy", new CultureInfo("cs-CZ"));
                }
                else
                {
                    DeliveryDateName = "";
                }

                DeliveryShift = row.Table.Columns.Contains("DeliveryShift")
                    ? MySQL.CInt(row, "DeliveryShift")
                    : 0;
            }
        }


        // ==========================================
        // Kontrola existence zakázky
        // ==========================================
        public static bool JobExists(int idjob)
        {
            if (idjob <= 0)
                return false;

            var (rows, count) = GetJobByID(idjob);

            return count > 0;
        }


        // ==========================================
        // Načtení posledních zakázek
        // ==========================================
        public static (List<viewJobList>, int) GetJobList()
        {
            int stationIdx = Settings.Param_PC_ID;

            string cmd =
                $"SELECT * FROM {viewJOB_LIST} " +
                $"WHERE (StationIdx = 0 OR StationIdx = {stationIdx}) " +
                $"ORDER BY IDjob DESC " +
                $"LIMIT 1000";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            return (table.AsEnumerable().Select(row => new viewJobList(row)).ToList(), rowcount);
        }


        // ==========================================
        // Načtení zakázek pro výpočet základu
        // ==========================================
        public static (List<viewJobList>, int) GetJobListZaklad()
        {
            int stationIdx = Settings.Param_PC_ID;

            string cmd =
                $"SELECT * FROM {viewJOB_LIST} " +
                $"WHERE (StationIdx = 0 OR StationIdx = {stationIdx}) " +
                $"AND ZakladVypocten = 0 " +
                $"AND RecipeIsZaklad = 0 " +
                $"ORDER BY IDjob DESC " +
                $"LIMIT 100";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            return (table.AsEnumerable().Select(row => new viewJobList(row)).ToList(), rowcount);
        }


        // ==========================================
        // Načtení zakázek podle statusu
        // ==========================================
        public static (List<viewJobList>, int) GetJobListViewByStatus(int status)
        {
            int stationIdx = Settings.Param_PC_ID;

            string cmd =
                $"SELECT * FROM {viewJOB_LIST} " +
                $"WHERE Status = {status} " +
                $"AND (StationIdx = 0 OR StationIdx = {stationIdx})";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            return (table.AsEnumerable().Select(row => new viewJobList(row)).ToList(), rowcount);
        }


        // ==========================================
        // Načtení zakázky podle ID
        // ==========================================
        public static (List<viewJobList>, int) GetJobByID(int idjob)
        {
            int stationIdx = Settings.Param_PC_ID;

            string cmd =
                $"SELECT * FROM {viewJOB_LIST} " +
                $"WHERE IDjob = {idjob} " +
                $"AND (StationIdx = 0 OR StationIdx = {stationIdx}) " +
                $"LIMIT 1";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            return (table.AsEnumerable().Select(row => new viewJobList(row)).ToList(), rowcount);
        }


        // ==========================================
        // Načtení posledního data podle zdroje importu
        // ==========================================
        public static (DateTime, int) GetLatestDateForImportSource(int importSource)
        {
            string cmd =
                $"SELECT MAX(DT) AS MaxDT " +
                $"FROM {tabJobListTable} " +
                $"WHERE ImportSource = {importSource}";

            var (rowcount, table) = MySQL.RunSelectCmd(cmd);

            if (rowcount > 0 &&
                table.Rows.Count > 0 &&
                DateTime.TryParse(table.Rows[0]["MaxDT"]?.ToString(), out DateTime dt))
            {
                return (dt, rowcount);
            }

            return (DateTime.MinValue, 0);
        }


        // ==========================================
        // Získání dalšího volného IDmrg
        // ==========================================
        public static int GetNextIDmrg()
        {
            try
            {
                string cmd =
                    $"SELECT IFNULL(MAX(IDmrg), 0) + 1 AS NextID " +
                    $"FROM {tabJobListTable}";

                var (rowcount, table) = MySQL.RunSelectCmd(cmd);

                if (table != null && table.Rows.Count > 0)
                {
                    return MySQL.CInt(table.Rows[0], "NextID");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetNextIDmrg ERROR: {ex}");
            }

            return 1;
        }


        // ==========================================
        // Vložení nové zakázky
        // ==========================================
        public static int InsertNewJob(tabJobListInsert job)
        {
            string dtStr =
                job.DT?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                ?? "CURRENT_TIMESTAMP";

            string jobNo = (job.JobNo ?? "").Replace("'", "''");
            string batchNo = (job.BatchNo ?? "").Replace("'", "''");

            string cmd =
                $"INSERT INTO {tabJobListTable} " +
                $"(JobNo, BatchNo, IDrcp, IDgrp, IDmrg, Status, AmountPcs, PlannedBatch, StationIdx, ImportSource, DT, ZakladVypocten) " +
                $"VALUES " +
                $"('{jobNo}', '{batchNo}', {job.IDrcp}, {job.IDgrp}, {job.IDmrg}, {job.Status}, " +
                $"{job.AmountPcs.ToString(CultureInfo.InvariantCulture)}, " +
                $"{job.PlannedBatch}, {job.StationIdx}, {job.ImportSource}, " +
                $"{(job.DT.HasValue ? $"'{dtStr}'" : "CURRENT_TIMESTAMP")}, {job.ZakladVypocten})";

            return MySQL.RunInsertCmd(cmd);
        }


        // ==========================================
        // Aktualizace zakázky
        // ==========================================
        public static void UpdateJob(viewJobList job)
        {
            Debug.WriteLine("UPDATE JOB:");

            string jobNo = (job.JobNo ?? "").Replace("'", "''");
            string batchNo = (job.BatchNo ?? "").Replace("'", "''");

            string cmd =
                $"UPDATE {tabJobListTable} " +
                $"SET JobNo = '{jobNo}', " +
                $"BatchNo = '{batchNo}', " +
                $"IDrcp = {job.IDrcp}, " +
                $"IDgrp = {job.IDgrp}, " +
                $"IDmrg = {job.IDmrg}, " +
                $"Status = {job.Status}, " +
                $"AmountPcs = {job.ReqAmountPcs.ToString(CultureInfo.InvariantCulture)}, " +
                $"PlannedBatch = {job.ReqNumberBatch}, " +
                $"ZakladVypocten = {job.ZakladVypocten} " +
                $"WHERE ID = {job.IDjob}";

            Debug.WriteLine(cmd);

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Aktualizace statusu zakázky
        // ==========================================
        public static void UpdateJobStatus(int status, int idjob)
        {
            string cmd =
                $"UPDATE {tabJobListTable} " +
                $"SET Status = {status} " +
                $"WHERE ID = {idjob}";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Smazání zakázky
        // ==========================================
        public static void DeleteJob(int idjob)
        {
            if (idjob <= 0)
                return;

            string cmd =
                $"DELETE FROM {tabJobListTable} " +
                $"WHERE ID = {idjob}";

            MySQL.RunNonQuery(cmd);
        }

        // ==========================================
        // Nastavení ActionReq pouze pokud existuje UUID
        // ==========================================
        public static void SetActionReq(int idjob, int actionReq)
        {
            if (idjob <= 0)
                return;

            string cmd =
                $"UPDATE {tabJobListTable} " +
                $"SET ActionReq = {actionReq} " +
                $"WHERE ID = {idjob} " +
                $"AND UUID IS NOT NULL " +
                $"AND UUID <> ''";

            MySQL.RunNonQuery(cmd);
        }

    }
}