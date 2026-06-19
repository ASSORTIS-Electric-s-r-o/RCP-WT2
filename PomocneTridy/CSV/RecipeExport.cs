// ===================== NAMESPACES =====================
using RCP_WT1.MySQL;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace RCP_WT1.PomocneTridy.CSV
{
    internal class RecipeExport
    {
        // =========================================================
        // EXPORT VŠECH RECEPTUR DO CSV
        // =========================================================
        public static bool ExportRecipesAllDoCsv(out string cestaKSouboru, out string chyba)
        {
            cestaKSouboru = string.Empty;
            chyba = string.Empty;

            try
            {
                var (recepty, pocet) = tabRECIPES.GetRecipesAll();

                if (recepty == null || pocet == 0)
                {
                    chyba = "Nebyla nalezena žádná data k exportu.";
                    return false;
                }

                string plocha = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string cilovaSlozka = Path.Combine(plocha, "PDF");

                if (!Directory.Exists(cilovaSlozka))
                    Directory.CreateDirectory(cilovaSlozka);

                string datum = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string nazevSouboru = $"Receptury_{datum}.csv";
                cestaKSouboru = Path.Combine(cilovaSlozka, nazevSouboru);

                StringBuilder sb = new StringBuilder();

                sb.AppendLine(string.Join(";",
                    Csv("IDgrp"),
                    Csv("Typ skupiny"),
                    Csv("Název skupiny"),
                    Csv("Obrázek skupiny"),
                    Csv("Skupina je základ"),
                    Csv("Materiál je základ"),
                    Csv("ID základu"),
                    Csv("ID receptury"),
                    Csv("Název receptury"),
                    Csv("Receptura smazána"),
                    Csv("Počet kusů"),
                    Csv("PDF postup"),
                    Csv("ID materiálu"),
                    Csv("Název materiálu"),
                    Csv("Obrázek materiálu"),
                    Csv("Materiál smazán"),
                    Csv("Dávka"),
                    Csv("Tolerance"),
                    Csv("Pořadí řádku"),
                    Csv("Vážit"),
                    Csv("Status"),
                    Csv("Základní dávka"),
                    Csv("Základní tolerance"),
                    Csv("Uživatel")
                ));

                foreach (var item in recepty)
                {
                    sb.AppendLine(string.Join(";",
                        Csv(item.IDgrp),
                        Csv(item.GrpType),
                        Csv(item.GroupName),
                        Csv(item.GroupImage),
                        Csv(item.GroupIsZaklad),
                        Csv(item.MaterialIsZaklad),
                        Csv(item.IDzaklad),
                        Csv(item.IDrcp),
                        Csv(item.RecipeName),
                        Csv(item.RecipeIsDeleted),
                        Csv(item.AmountPcs),
                        Csv(item.PdfProcedurePath),
                        Csv(item.IDmat),
                        Csv(item.MaterialName),
                        Csv(item.MaterialImage),
                        Csv(item.MaterialIsDeleted),
                        Csv(item.Davka),
                        Csv(item.Tolerance),
                        Csv(item.row_no),
                        Csv(item.Vazit),
                        Csv(item.Status),
                        Csv(item.BaseDavka),
                        Csv(item.BaseTolerance),
                        Csv(item.UserName)
                    ));
                }

                File.WriteAllText(cestaKSouboru, sb.ToString(), new UTF8Encoding(true));

                return true;
            }
            catch (Exception ex)
            {
                chyba = ex.Message;
                cestaKSouboru = string.Empty;
                return false;
            }
        }

        // =========================================================
        // POMOCNÁ METODA PRO CSV
        // =========================================================
        private static string Csv(object? hodnota)
        {
            if (hodnota == null)
                return "\"\"";

            string text;
            CultureInfo kultura = new CultureInfo("cs-CZ");

            switch (hodnota)
            {
                case float f:
                    text = f.ToString(kultura);
                    break;

                case double d:
                    text = d.ToString(kultura);
                    break;

                case decimal m:
                    text = m.ToString(kultura);
                    break;

                case int i:
                    text = i.ToString(kultura);
                    break;

                case long l:
                    text = l.ToString(kultura);
                    break;

                case short s:
                    text = s.ToString(kultura);
                    break;

                case byte b:
                    text = b.ToString(kultura);
                    break;

                case bool bol:
                    text = bol ? "1" : "0";
                    break;

                case DateTime dt:
                    text = dt.ToString("dd.MM.yyyy HH:mm:ss", kultura);
                    break;

                default:
                    text = hodnota.ToString() ?? string.Empty;
                    break;
            }

            text = text.Replace("\"", "\"\"");

            return $"\"{text}\"";
        }
    }
}