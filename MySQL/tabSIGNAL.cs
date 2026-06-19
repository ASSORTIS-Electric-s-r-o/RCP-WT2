using System;
using System.Data;

namespace RCP_WT1.MySQL
{
    // ==========================================
    // Tabulka signálů mezi PC
    // ==========================================
    internal static class tabSIGNAL
    {
        // ==========================================
        // Konstanty
        // ==========================================
        public static string TableName = "tabSIGNAL";


        // ==========================================
        // Třída jednoho řádku signálu
        // ==========================================
        public class SignalRow
        {
            public int ID { get; set; }

            public int IDjob { get; set; }

            public int IDprod { get; set; }

            public int IDpc { get; set; }


            // ==========================================
            // Prázdný konstruktor
            // ==========================================
            public SignalRow()
            {
            }


            // ==========================================
            // Konstruktor z DataRow
            // ==========================================
            public SignalRow(DataRow row)
            {
                ID = MySQL.CInt(row, "ID");

                IDjob = MySQL.CInt(row, "IDjob");

                IDprod = MySQL.CInt(row, "IDprod");

                IDpc = MySQL.CInt(row, "IDpc");
            }
        }


        // ==========================================
        // Načtení signálu z cizího PC
        // ==========================================
        public static SignalRow? GetSignal()
        {
            int myPcId = Settings.Param_PC_ID;

            string cmd =
                $"SELECT * FROM {TableName} " +
                $"WHERE ID = 1 " +
                $"AND IDpc <> {myPcId}";

            var (rows, table) = MySQL.RunSelectCmd(cmd);

            if (rows > 0 && table.Rows.Count > 0)
            {
                return new SignalRow(table.Rows[0]);
            }

            return null;
        }


        // ==========================================
        // Nastavení ID zakázky
        // ==========================================
        public static void SetIDjob(int idjob)
        {
            int myPcId = Settings.Param_PC_ID;

            string cmd =
                $"UPDATE {TableName} " +
                $"SET IDjob = {idjob}, " +
                $"IDpc = {myPcId} " +
                $"WHERE ID = 1";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Nastavení ID produkce
        // ==========================================
        public static void SetIDprod(int idprod)
        {
            int myPcId = Settings.Param_PC_ID;

            string cmd =
                $"UPDATE {TableName} " +
                $"SET IDprod = {idprod}, " +
                $"IDpc = {myPcId} " +
                $"WHERE ID = 1";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Vynulování ID zakázky
        // ==========================================
        public static void ResetIDjob()
        {
            string cmd =
                $"UPDATE {TableName} " +
                $"SET IDjob = 0, " +
                $"IDpc = 0 " +
                $"WHERE ID = 1";

            MySQL.RunNonQuery(cmd);
        }


        // ==========================================
        // Vynulování ID produkce
        // ==========================================
        public static void ResetIDprod()
        {
            string cmd =
                $"UPDATE {TableName} " +
                $"SET IDprod = 0, " +
                $"IDpc = 0 " +
                $"WHERE ID = 1";

            MySQL.RunNonQuery(cmd);
        }
    }
}