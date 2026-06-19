using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka skupin
    // ==========================================
    internal static class tabGROUPS
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string tableName = "tabGROUPS";

        public static string viewName = "hmiGROUPS";

        public static string logFile =
            new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";


        // ==========================================
        // Třída jednoho řádku skupiny
        // ==========================================
        public class GroupRow
        {
            public int IDgrp { get; set; }

            public string GrpType { get; set; } = "";

            public string Name { get; set; } = "";

            public string MpImage { get; set; } = "";

            public int IsDeleted { get; set; }

            public int IsZaklad { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public GroupRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public GroupRow(DataRow row)
            {
                IDgrp = MySQL.CInt(row, "IDgrp");
                GrpType = MySQL.CStr(row, "GrpType");
                Name = MySQL.CStr(row, "Name");

                MpImage = row.Table.Columns.Contains("MpImage")
                    ? MySQL.CStr(row, "MpImage")
                    : "";

                IsDeleted = MySQL.CInt(row, "IsDeleted");
                IsZaklad = MySQL.CInt(row, "IsZaklad");
            }
        }


        // ==========================================
        // Načtení všech skupin přímo z tabulky
        // ==========================================
        public static (List<GroupRow>, int) GetGroupsAll_FromTable()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {tableName} " +
                    $"WHERE IFNULL(IsDeleted, 0) = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<GroupRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new GroupRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<GroupRow>(), 0);
            }
        }


        // ==========================================
        // Načtení všech skupin z view
        // ==========================================
        public static (List<GroupRow>, int) GetGroupsAll()
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                List<GroupRow> result = new();

                if (rowcount > 0)
                {
                    foreach (DataRow row in rows.Rows)
                    {
                        result.Add(new GroupRow(row));
                    }
                }

                return (result, rowcount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (new List<GroupRow>(), 0);
            }
        }


        // ==========================================
        // Načtení skupiny podle ID
        // ==========================================
        public static (GroupRow?, bool) GetGroupByID(int idgrp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"SELECT * FROM {viewName} " +
                    $"WHERE IDgrp = {idgrp} " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0)
                    return (new GroupRow(rows.Rows[0]), true);

                return (null, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funkce: {_name} Chyba: {ex.Message}");
                return (null, false);
            }
        }


        // ==========================================
        // Načtení příznaku základ podle ID skupiny
        // ==========================================
        public static int GetIsZakladByGroupID(int idgrp)
        {
            try
            {
                string cmd =
                    $"SELECT IsZaklad FROM {viewName} " +
                    $"WHERE IDgrp = {idgrp} " +
                    $"AND IsDeleted = 0";

                var (rowcount, rows) = MySQL.RunSelectCmd(cmd);

                if (rowcount > 0)
                    return MySQL.CInt(rows.Rows[0], "IsZaklad");

                return 0;
            }
            catch
            {
                return 0;
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
                    $"SELECT IFNULL(MAX(IDgrp), 0) + 1 AS NextID " +
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
        // Vložení nové skupiny
        // ==========================================
        public static int InsertGroup(string name, string grpType = "R", string image = "")
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                name = (name ?? "").Replace("'", "''");
                grpType = (grpType ?? "").Replace("'", "''");
                image = (image ?? "").Replace("\\", "\\\\").Replace("'", "''");

                int newID = GetNextID();

                string cmd =
                    $"INSERT INTO {tableName} " +
                    $"(IDgrp, GrpType, Name, MpImage, IsDeleted, IsZaklad) " +
                    $"VALUES " +
                    $"({newID}, '{grpType}', '{name}', '{image}', 0, 0)";

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
        // Úprava názvu skupiny a příznaku základ
        // ==========================================
        public static bool UpdateGroup(int idgrp, string newName, int isZaklad)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                newName = (newName ?? "").Replace("'", "''");

                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET Name = '{newName}', " +
                    $"IsZaklad = {isZaklad} " +
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
        // Úprava názvu skupiny, příznaku základ a obrázku
        // ==========================================
        public static bool UpdateGroup(int idgrp, string newName, int isZaklad, string mpImage)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                newName = (newName ?? "").Replace("'", "''");
                mpImage = (mpImage ?? "").Replace("\\", "\\\\").Replace("'", "''");

                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET Name = '{newName}', " +
                    $"MpImage = '{mpImage}', " +
                    $"IsZaklad = {isZaklad} " +
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
        // Logické smazání skupiny
        // ==========================================
        public static bool DeleteGroup(int idgrp)
        {
            string _name = MethodBase.GetCurrentMethod()?.Name ?? "";

            try
            {
                string cmd =
                    $"UPDATE {tableName} " +
                    $"SET IsDeleted = 1 " +
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
        // Kontrola existence skupiny
        // ==========================================
        public static bool ExistsGroup(int idgrp)
        {
            try
            {
                string cmd =
                    $"SELECT COUNT(*) AS Pocet " +
                    $"FROM {tableName} " +
                    $"WHERE IDgrp = {idgrp}";

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
        // Vložení nebo aktualizace skupiny
        // ==========================================
        public static bool UpsertGroup(GroupRow row)
        {
            try
            {
                if (row == null)
                    return false;

                string name = (row.Name ?? "").Replace("'", "''");
                string grpType = (row.GrpType ?? "").Replace("'", "''");
                string image = (row.MpImage ?? "").Replace("\\", "\\\\").Replace("'", "''");

                if (ExistsGroup(row.IDgrp))
                {
                    string cmd =
                        $"UPDATE {tableName} " +
                        $"SET GrpType = '{grpType}', " +
                        $"Name = '{name}', " +
                        $"MpImage = '{image}', " +
                        $"IsDeleted = {row.IsDeleted}, " +
                        $"IsZaklad = {row.IsZaklad} " +
                        $"WHERE IDgrp = {row.IDgrp}";

                    return MySQL.RunNonQuery(cmd) > 0;
                }
                else
                {
                    string cmd =
                        $"INSERT INTO {tableName} " +
                        $"(IDgrp, GrpType, Name, MpImage, IsDeleted, IsZaklad) " +
                        $"VALUES " +
                        $"({row.IDgrp}, '{grpType}', '{name}', '{image}', {row.IsDeleted}, {row.IsZaklad})";

                    return MySQL.RunNonQuery(cmd) > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}