// ===================== NAMESPACES =====================
using RCP_WT1.MySQL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RCP_WT1.PomocneTridy.CSV
{
    internal class RecipeImport
    {
        // =========================================================
        // INTERNÍ STRUKTURA IMPORTOVANÉHO ŘÁDKU CSV
        // =========================================================
        private class ImportRow
        {
            public int IDgrp { get; set; }
            public string GrpType { get; set; } = "";
            public string GroupName { get; set; } = "";
            public string GroupImage { get; set; } = "";
            public int GroupIsZaklad { get; set; }

            public int MaterialIsZaklad { get; set; }
            public int IDzaklad { get; set; }

            public int IDrcp { get; set; }
            public string RecipeName { get; set; } = "";
            public int RecipeIsDeleted { get; set; }
            public float AmountPcs { get; set; }
            public string PdfProcedurePath { get; set; } = "";

            public int IDmat { get; set; }
            public string MaterialName { get; set; } = "";
            public string MaterialImage { get; set; } = "";
            public int MaterialIsDeleted { get; set; }

            public float Davka { get; set; }
            public float Tolerance { get; set; }
            public int RowNo { get; set; }
            public int Vazit { get; set; }

            public int Status { get; set; }
            public float BaseDavka { get; set; }
            public float BaseTolerance { get; set; }
            public string UserName { get; set; } = "";
        }

        // =========================================================
        // IMPORT VŠECH RECEPTUR Z CSV DO DB
        // =========================================================
        public static bool ImportRecipesAllZCsv(
            string cestaKSouboru,
            out string chyba,
            Action<int, string>? progress = null)
        {
            chyba = string.Empty;

            try
            {
                progress?.Invoke(0, "Kontroluji CSV soubor...");

                if (string.IsNullOrWhiteSpace(cestaKSouboru) || !File.Exists(cestaKSouboru))
                {
                    chyba = "CSV soubor nebyl nalezen.";
                    return false;
                }

                progress?.Invoke(2, "Načítám CSV soubor...");

                string[] radky = File.ReadAllLines(cestaKSouboru, Encoding.UTF8);

                if (radky.Length <= 1)
                {
                    chyba = "CSV soubor neobsahuje žádná data.";
                    return false;
                }

                progress?.Invoke(5, "Kontroluji hlavičku CSV souboru...");

                string[] ocekavanaHlavicka =
                {
                    "IDgrp",
                    "Typ skupiny",
                    "Název skupiny",
                    "Obrázek skupiny",
                    "Skupina je základ",
                    "Materiál je základ",
                    "ID základu",
                    "ID receptury",
                    "Název receptury",
                    "Receptura smazána",
                    "Počet kusů",
                    "PDF postup",
                    "ID materiálu",
                    "Název materiálu",
                    "Obrázek materiálu",
                    "Materiál smazán",
                    "Dávka",
                    "Tolerance",
                    "Pořadí řádku",
                    "Vážit",
                    "Status",
                    "Základní dávka",
                    "Základní tolerance",
                    "Uživatel"
                };

                List<string> hlavicka = RozdelCsvRadek(radky[0])
                    .Select(x => x.Trim().TrimStart('\uFEFF'))
                    .ToList();

                if (hlavicka.Count != ocekavanaHlavicka.Length)
                {
                    chyba = $"CSV má nesprávný počet sloupců. Očekáváno: {ocekavanaHlavicka.Length}, nalezeno: {hlavicka.Count}.";
                    return false;
                }

                for (int i = 0; i < ocekavanaHlavicka.Length; i++)
                {
                    if (!string.Equals(hlavicka[i], ocekavanaHlavicka[i], StringComparison.OrdinalIgnoreCase))
                    {
                        chyba = $"CSV má nesprávnou hlavičku ve sloupci {i + 1}. Očekáváno: '{ocekavanaHlavicka[i]}', nalezeno: '{hlavicka[i]}'.";
                        return false;
                    }
                }

                progress?.Invoke(8, "Převádím CSV data do paměti...");

                List<ImportRow> importRows = new();

                for (int i = 1; i < radky.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(radky[i]))
                        continue;

                    List<string> s = RozdelCsvRadek(radky[i]);

                    if (s.Count != ocekavanaHlavicka.Length)
                    {
                        chyba = $"Řádek {i + 1} nemá správný počet sloupců. Očekáváno: {ocekavanaHlavicka.Length}, nalezeno: {s.Count}.";
                        return false;
                    }

                    ImportRow row = new ImportRow
                    {
                        IDgrp = CInt(s[0]),
                        GrpType = s[1],
                        GroupName = s[2],
                        GroupImage = s[3],
                        GroupIsZaklad = CInt(s[4]),

                        MaterialIsZaklad = CInt(s[5]),
                        IDzaklad = CInt(s[6]),

                        IDrcp = CInt(s[7]),
                        RecipeName = s[8],
                        RecipeIsDeleted = CInt(s[9]),
                        AmountPcs = CFloat(s[10]),
                        PdfProcedurePath = s[11],

                        IDmat = CInt(s[12]),
                        MaterialName = s[13],
                        MaterialImage = s[14],
                        MaterialIsDeleted = CInt(s[15]),

                        Davka = CFloat(s[16]),
                        Tolerance = CFloat(s[17]),
                        RowNo = CInt(s[18]),
                        Vazit = CInt(s[19]),

                        Status = CInt(s[20]),
                        BaseDavka = CFloat(s[21]),
                        BaseTolerance = CFloat(s[22]),
                        UserName = s[23]
                    };

                    if (row.IDgrp <= 0)
                    {
                        chyba = $"Řádek {i + 1}: Neplatné ID skupiny.";
                        return false;
                    }

                    if (row.IDrcp <= 0)
                    {
                        chyba = $"Řádek {i + 1}: Neplatné ID receptury.";
                        return false;
                    }

                    importRows.Add(row);
                }

                if (importRows.Count == 0)
                {
                    chyba = "CSV soubor neobsahuje žádné importovatelné řádky.";
                    return false;
                }

                var skupiny = importRows
                    .GroupBy(x => x.IDgrp)
                    .Select(x => x.First())
                    .ToList();

                for (int i = 0; i < skupiny.Count; i++)
                {
                    ImportRow item = skupiny[i];
                    int percent = VypocitejProcenta(i, skupiny.Count, 10, 25);

                    progress?.Invoke(percent, $"Ukládám skupinu {i + 1} z {skupiny.Count}: {item.GroupName}");

                    tabGROUPS.GroupRow groupRow = new tabGROUPS.GroupRow
                    {
                        IDgrp = item.IDgrp,
                        GrpType = item.GrpType,
                        Name = item.GroupName,
                        MpImage = item.GroupImage,
                        IsDeleted = 0,
                        IsZaklad = item.GroupIsZaklad
                    };

                    if (!tabGROUPS.UpsertGroup(groupRow))
                    {
                        chyba = $"Nepodařilo se uložit skupinu ID {item.IDgrp}.";
                        return false;
                    }
                }

                var materialy = importRows
                    .Where(x => x.IDmat > 0)
                    .GroupBy(x => x.IDmat)
                    .Select(x => x.First())
                    .ToList();

                for (int i = 0; i < materialy.Count; i++)
                {
                    ImportRow item = materialy[i];
                    int percent = VypocitejProcenta(i, materialy.Count, 25, 45);

                    progress?.Invoke(percent, $"Ukládám materiál {i + 1} z {materialy.Count}: {item.MaterialName}");

                    tabMATERIAL.MaterialRow materialRow = new tabMATERIAL.MaterialRow
                    {
                        IDmat = item.IDmat,
                        Cislo = "",
                        Name = item.MaterialName,
                        MpImage = item.MaterialImage,
                        IsDeleted = item.MaterialIsDeleted
                    };

                    if (!tabMATERIAL.UpsertMaterial(materialRow))
                    {
                        chyba = $"Nepodařilo se uložit materiál ID {item.IDmat}.";
                        return false;
                    }
                }

                var receptury = importRows
                    .GroupBy(x => x.IDrcp)
                    .Select(x => x.First())
                    .ToList();

                for (int i = 0; i < receptury.Count; i++)
                {
                    ImportRow item = receptury[i];
                    int percent = VypocitejProcenta(i, receptury.Count, 45, 65);

                    progress?.Invoke(percent, $"Ukládám recepturu {i + 1} z {receptury.Count}: {item.RecipeName}");

                    tabRECIPES.RecipeRow recipeRow = new tabRECIPES.RecipeRow
                    {
                        IDrcp = item.IDrcp,
                        IDgrp = item.IDgrp,
                        Cislo = "",
                        Name = item.RecipeName,
                        RecipeName = item.RecipeName,
                        AmountPcs = item.AmountPcs,
                        IsDeleted = item.RecipeIsDeleted,
                        IsZaklad = item.MaterialIsZaklad,
                        PdfProcedurePath = item.PdfProcedurePath
                    };

                    if (!tabRECIPES.UpsertRecipe(recipeRow))
                    {
                        chyba = $"Nepodařilo se uložit recepturu ID {item.IDrcp}.";
                        return false;
                    }
                }

                var recepturyDetail = importRows
                    .GroupBy(x => x.IDrcp)
                    .ToList();

                for (int i = 0; i < recepturyDetail.Count; i++)
                {
                    var receptura = recepturyDetail[i];
                    int idrcp = receptura.Key;
                    int percent = VypocitejProcenta(i, recepturyDetail.Count, 65, 99);

                    string nazevReceptury = receptura.FirstOrDefault()?.RecipeName ?? "";
                    progress?.Invoke(percent, $"Synchronizuji řádky receptury {i + 1} z {recepturyDetail.Count}: {nazevReceptury}");

                    List<tabRECIPES_MAT.RecipeMaterialRow> detailRows = receptura
                        .OrderBy(x => x.RowNo)
                        .Select(x => new tabRECIPES_MAT.RecipeMaterialRow
                        {
                            IDrcp = x.IDrcp,
                            IDmat = x.IDmat,
                            IDzaklad = x.IDzaklad,
                            Davka = x.Davka,
                            Jednotky = "",
                            Tolerance = x.Tolerance,
                            row_no = x.RowNo,
                            Vazit = x.Vazit,
                            IsDeleted = 0
                        })
                        .ToList();

                    if (!tabRECIPES_MAT.SyncRecipeMaterialsImport(idrcp, detailRows))
                    {
                        chyba = $"Nepodařilo se synchronizovat řádky receptury ID {idrcp}.";
                        return false;
                    }
                }

                progress?.Invoke(100, $"Import dokončen. Importováno řádků CSV: {importRows.Count}.");

                return true;
            }
            catch (Exception ex)
            {
                chyba = ex.Message;
                progress?.Invoke(100, $"Chyba importu: {ex.Message}");
                return false;
            }
        }

        // =========================================================
        // ROZDĚLENÍ CSV ŘÁDKU
        // =========================================================
        private static List<string> RozdelCsvRadek(string radek)
        {
            List<string> hodnoty = new();
            StringBuilder aktualni = new();

            bool uvnitrUvozovek = false;

            for (int i = 0; i < radek.Length; i++)
            {
                char znak = radek[i];

                if (znak == '"')
                {
                    if (uvnitrUvozovek && i + 1 < radek.Length && radek[i + 1] == '"')
                    {
                        aktualni.Append('"');
                        i++;
                    }
                    else
                    {
                        uvnitrUvozovek = !uvnitrUvozovek;
                    }
                }
                else if (znak == ';' && !uvnitrUvozovek)
                {
                    hodnoty.Add(aktualni.ToString());
                    aktualni.Clear();
                }
                else
                {
                    aktualni.Append(znak);
                }
            }

            hodnoty.Add(aktualni.ToString());

            return hodnoty;
        }

        // =========================================================
        // BEZPEČNÝ PŘEVOD NA INT
        // =========================================================
        private static int CInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Trim();

            if (int.TryParse(value, NumberStyles.Any, new CultureInfo("cs-CZ"), out int result))
                return result;

            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        // =========================================================
        // BEZPEČNÝ PŘEVOD NA FLOAT
        // =========================================================
        private static float CFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Trim();

            if (float.TryParse(value, NumberStyles.Any, new CultureInfo("cs-CZ"), out float result))
                return result;

            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        // =========================================================
        // VÝPOČET PROCENT PRO JEDNOTLIVÉ FÁZE IMPORTU
        // =========================================================
        private static int VypocitejProcenta(int index, int total, int startPercent, int endPercent)
        {
            if (total <= 0)
                return endPercent;

            double pomer = (double)(index + 1) / total;
            int rozsah = endPercent - startPercent;

            return startPercent + (int)Math.Round(pomer * rozsah);
        }
    }
}