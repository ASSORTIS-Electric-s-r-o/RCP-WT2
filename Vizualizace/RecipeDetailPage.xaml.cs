using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RCP_WT1.MySQL;
using RCP_WT1.PomocneTridy;
using RCP_WT1.Vizualizace.DialogovaOkna;
using RCP_WT1.Vizualizace.Klavesnice;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Foundation;

namespace RCP_WT1.Vizualizace
{
    public sealed partial class RecipeDetailPage : Page
    {
        // ==========================================
        // Režim otevření detailu receptury
        // ==========================================

        public enum EditMode
        {
            Planned,
            History,
            Recipes,
            MergeRecipes
        }

        // ==========================================
        // Aktuální stav stránky
        // ==========================================

        private EditMode _currentMode = EditMode.Recipes;

        private readonly List<tabRECIPES.RecipeDetailRow> _rows = new();

        // Slovník uchovává běžící animace posuvných textů v levém panelu.
        // Při každé změně hodnoty se stará animace bezpečně zastaví a vytvoří se nová.
        private readonly Dictionary<TextBlock, Storyboard> _posuvneTextyAnimace = new();

        // Pevná šířka hodnot v levém panelu. Stejná hodnota je použita v XAML
        // u ořezových Gridů jednotlivých hodnot.
        private const double SirkaTextuLevehoPanelu = 135;

        private List<MergeRecipeInputRow> _mergeReceptury = new();

        private bool _zpetNaProduction = false;
        private bool _history = false;
        private bool _dialogVelikostiUzOtevren = false;
        private bool _nacitamHistorickeSarze = false;

        private int _idJob = 0;
        private int _idRcp = 0;
        private int _idGrp = 0;
        private int _idMrg = 0;
        private int _status = 99;
        private int _batchIndex = 1;
        private int _zaklad = 0;

        private float _pozadovaneMnozstviReceptu = 0;
        private float _velikostVarky = 0;
        private float _puvodniVelikostVarky = 0;
        private float _defaultVelikostReceptu = 0;
        private int _pocetVarek = 1;
        private float _celkovaVelikost = 0;

        // ==========================================
        // Jednotky
        // ==========================================
        private bool JeRezimKg =>
            _zaklad == 1 || Settings.Param_Units == 0;

        private string JednotkaText =>
            JeRezimKg ? "kg" : "ks";

        // ==========================================
        // Konstruktor stránky
        // ==========================================
        public RecipeDetailPage()
        {
            InitializeComponent();

            Loaded += RecipeDetailPage_Loaded;
        }

        // ==========================================
        // Převzetí navigačních parametrů
        // ==========================================
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            RecipeDetailArgs args = e.Parameter as RecipeDetailArgs
                ?? new RecipeDetailArgs
                {
                    Mode = EditMode.Recipes,
                    Id = e.Parameter is int id ? id : 0,
                    BatchIndex = 1
                };

            _currentMode = args.Mode;
            _batchIndex = args.BatchIndex <= 0 ? 1 : args.BatchIndex;
            _mergeReceptury = args.MergeRecipes ?? new List<MergeRecipeInputRow>();

            // Detail otevřený ze zakázek se vrací zpět na JobPage.
            // Nově vytvářené sloučení z RecipePage se vrací zpět na RecipePage.
            _zpetNaProduction =
                _currentMode == EditMode.Planned ||
                _currentMode == EditMode.History ||
                (_currentMode == EditMode.MergeRecipes && _mergeReceptury.Count == 0);

            LoadData(args.Mode, args.Id, _batchIndex);
            AktualizujUI();
        }

        // ==========================================
        // Loaded stránky
        // ==========================================
        private void RecipeDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            DotykovaKlavesniceService.Pripoj(
                this,
                App.MainWindow,
                _ => VirtualKeyboard.KeyboardMode.Str);

            AktualizujOperatora();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (_dialogVelikostiUzOtevren)
                    return;

                if (_currentMode == EditMode.History)
                    return;

                if (_currentMode == EditMode.MergeRecipes)
                {
                    if (!Settings.Param_VypocetVarky)
                        return;

                    _dialogVelikostiUzOtevren = true;

                    OtevriKrokVelikostDize();
                    return;
                }

                if (_currentMode == EditMode.Recipes)
                {
                    _dialogVelikostiUzOtevren = true;

                    OtevriKrokyPrepocetVarky();
                }
            });
        }

        // ==========================================
        // Formátování hodnot
        // ==========================================
        private string FormatMnozstvi(float value)
        {
            return JeRezimKg
                ? $"{value:0.###} {JednotkaText}"
                : $"{(int)Math.Ceiling(value)} {JednotkaText}";
        }

        private string FormatKg(float value)
        {
            return $"{value:0.###} kg";
        }

        // ==========================================
        // Načtení dat podle režimu
        // ==========================================
        private void LoadData(EditMode mode, int id, int batchIndex)
        {
            _rows.Clear();

            switch (mode)
            {
                case EditMode.Planned:
                    LoadPlanned(id);
                    break;

                case EditMode.History:
                    LoadHistoryOrMergeHistory(id, batchIndex);
                    break;

                case EditMode.Recipes:
                    LoadRecipeTemplate(id);
                    break;

                case EditMode.MergeRecipes:
                    if (_mergeReceptury.Count > 0)
                        LoadMergeRecipes();
                    else
                        LoadMergeProduction(id);
                    break;
            }
        }

        // ==========================================
        // Načtení detailu běžné receptury z RecipePage
        // ==========================================
        private void LoadRecipeTemplate(int idRcp)
        {
            var (recipes, _) = tabRECIPES.GetRecipesByID(idRcp);
            tabRECIPES.RecipeDetailRow? rcp = recipes.FirstOrDefault();

            if (rcp == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Receptura", "Receptura nebyla nalezena.");
                return;
            }

            _history = false;
            _idRcp = rcp.IDrcp;
            _idGrp = rcp.IDgrp;
            _idJob = 0;
            _idMrg = 0;
            _status = 99;
            _zaklad = rcp.GroupIsZaklad;

            _velikostVarky = rcp.AmountPcs;
            _puvodniVelikostVarky = _velikostVarky;
            _defaultVelikostReceptu = rcp.AmountPcs;
            _pozadovaneMnozstviReceptu = _velikostVarky;
            _pocetVarek = 1;
            _celkovaVelikost = _velikostVarky;

            TxtRecipeName.Text = string.IsNullOrWhiteSpace(rcp.RecipeCislo)
                ? rcp.RecipeName
                : $"{rcp.RecipeCislo} - {rcp.RecipeName}";

            TxtJobNo.Text = "-----";
            TxtBatchNo.Text = DateTime.Now.AddDays(1).ToString("yyMMdd");

            foreach (tabRECIPES.RecipeDetailRow r in recipes)
            {
                bool jeZaklad = r.IDzaklad > 0;

                _rows.Add(new tabRECIPES.RecipeDetailRow
                {
                    IDmat = jeZaklad ? r.IDzaklad : r.IDmat,
                    IDzaklad = r.IDzaklad,
                    MaterialCislo = r.MaterialCislo,
                    MaterialName = string.IsNullOrWhiteSpace(r.RecipeMatCislo)
                        ? r.MaterialName
                        : $"{r.RecipeMatCislo} - {r.MaterialName}",
                    BaseDavka = r.Davka,
                    BaseTolerance = r.Tolerance,
                    Davka = r.Davka,
                    Tolerance = r.Tolerance,
                    Status = jeZaklad ? 20 : 0,
                    MaterialIsZaklad = jeZaklad ? 1 : 0
                });
            }
        }

        // ==========================================
        // Načtení plánované zakázky
        // ==========================================
        private void LoadPlanned(int jobId)
        {
            var (jobRows, _) = tabJOB_LIST.GetJobByID(jobId);
            var job = jobRows.FirstOrDefault();

            if (job == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Zakázka", "Zakázka nebyla nalezena.");
                return;
            }

            if (job.IDmrg > 0)
            {
                LoadMergeProduction(jobId);
                return;
            }

            var (recipeRows, _) = tabRECIPES.GetRecipesByID(job.IDrcp);
            var recipe = recipeRows.FirstOrDefault();

            if (recipe == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Receptura", "Receptura zakázky nebyla nalezena.");
                return;
            }

            _history = false;
            _idJob = job.IDjob;
            _idRcp = recipe.IDrcp;
            _idGrp = recipe.IDgrp;
            _idMrg = 0;
            _status = job.Status;
            _zaklad = job.RecipeIsZaklad;

            _velikostVarky = job.ReqAmountPcs;
            _puvodniVelikostVarky = recipe.AmountPcs;
            _defaultVelikostReceptu = recipe.AmountPcs;
            _pocetVarek = job.ReqNumberBatch <= 0 ? 1 : job.ReqNumberBatch;
            _pozadovaneMnozstviReceptu = _velikostVarky * _pocetVarek;
            _celkovaVelikost = _velikostVarky * _pocetVarek;

            TxtRecipeName.Text = string.IsNullOrWhiteSpace(recipe.RecipeCislo)
                ? recipe.RecipeName
                : $"{recipe.RecipeCislo} - {recipe.RecipeName}";

            TxtJobNo.Text = job.JobNo;
            TxtBatchNo.Text = job.BatchNo;

            float scale = _puvodniVelikostVarky > 0
                ? _velikostVarky / _puvodniVelikostVarky
                : 1f;

            foreach (tabRECIPES.RecipeDetailRow r in recipeRows)
            {
                bool jeZaklad = r.IDzaklad > 0;

                _rows.Add(new tabRECIPES.RecipeDetailRow
                {
                    IDmat = jeZaklad ? r.IDzaklad : r.IDmat,
                    IDzaklad = r.IDzaklad,
                    MaterialCislo = r.MaterialCislo,
                    MaterialName = string.IsNullOrWhiteSpace(r.RecipeMatCislo)
                        ? r.MaterialName
                        : $"{r.RecipeMatCislo} - {r.MaterialName}",
                    BaseDavka = r.Davka,
                    BaseTolerance = r.Tolerance,
                    Davka = (float)Math.Round(r.Davka * scale, 3),
                    Tolerance = r.Tolerance,
                    Status = jeZaklad ? 20 : 0,
                    MaterialIsZaklad = jeZaklad ? 1 : 0
                });
            }
        }

        // ==========================================
        // Načtení historie nebo merge historie podle zakázky
        // ==========================================
        private void LoadHistoryOrMergeHistory(int jobId, int batchIndex)
        {
            var (jobRows, _) = tabJOB_LIST.GetJobByID(jobId);
            var job = jobRows.FirstOrDefault();

            if (job == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Zakázka", "Zakázka nebyla nalezena.");
                return;
            }

            if (job.IDmrg > 0)
            {
                LoadMergeHistory(jobId, batchIndex);
                return;
            }

            LoadHistory(jobId, batchIndex);
        }

        // ==========================================
        // Načtení historie běžné zakázky
        // ==========================================
        private void LoadHistory(int jobId, int batchIndex)
        {
            var (prodRows, _) = tabPRODUCTION.GetProductionViewByID_IDX(jobId, batchIndex);
            var first = prodRows.FirstOrDefault();

            if (first == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Historie", "Historie zakázky nebyla nalezena.");
                return;
            }

            var (jobRows, _) = tabJOB_LIST.GetJobByID(jobId);
            var job = jobRows.FirstOrDefault();

            if (job == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Zakázka", "Zakázka nebyla nalezena.");
                return;
            }

            _history = true;
            _idJob = jobId;
            _idRcp = job.IDrcp;
            _idGrp = job.IDgrp;
            _idMrg = 0;
            _status = job.Status;
            _zaklad = job.RecipeIsZaklad;

            _velikostVarky = first.JobAmountPcs;
            _puvodniVelikostVarky = _velikostVarky;
            _defaultVelikostReceptu = _velikostVarky;
            _pocetVarek = batchIndex;
            _celkovaVelikost = _velikostVarky;
            _pozadovaneMnozstviReceptu = _velikostVarky * _pocetVarek;

            TxtRecipeName.Text = string.IsNullOrWhiteSpace(first.RecipeCislo)
                ? first.RecipeName
                : $"{first.RecipeCislo} - {first.RecipeName}";

            TxtJobNo.Text = first.JobNo;
            TxtBatchNo.Text = first.BatchNo;
            NaplnComboHistorickychSarzi();

            foreach (var r in prodRows)
            {
                _rows.Add(new tabRECIPES.RecipeDetailRow
                {
                    IDmat = r.IDzaklad > 0 ? r.IDzaklad : r.IDmat,
                    IDzaklad = r.IDzaklad,
                    MaterialCislo = r.MaterialCislo,
                    MaterialName = r.MaterialName,
                    MaterialIsZaklad = r.IDzaklad > 0 ? 1 : 0,
                    Davka = r.HmotnostNavazena,
                    BaseDavka = r.HmotnostPozadovana,
                    Tolerance = r.Tolerance,
                    BaseTolerance = r.Tolerance,
                    Status = r.IDzaklad > 0 ? 20 : 0,
                    UserName = r.UserName
                });
            }
        }

        // ==========================================
        // Vytvoření nové sloučené receptury z vybraných receptů
        // ==========================================
        private void LoadMergeRecipes()
        {
            _history = false;
            _idJob = 0;
            _idRcp = 0;
            _idGrp = 0;
            _idMrg = 0;
            _status = 99;
            _zaklad = Settings.Param_Units == 0 ? 1 : 0;

            TxtRecipeName.Text = "Sloučená receptura";

            TxtJobNo.Text = string.Join(
                "|",
                _mergeReceptury
                    .Select(x =>
                    {
                        var (rows, _) = tabRECIPES.GetRecipesByID(x.IDrcp);
                        var r = rows.FirstOrDefault();

                        if (r == null)
                            return "";

                        return string.IsNullOrWhiteSpace(r.RecipeCislo)
                            ? r.RecipeName
                            : r.RecipeCislo;
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            TxtBatchNo.Text = DateTime.Now.AddDays(1).ToString("yyMMdd");

            List<tabRECIPES.RecipeDetailRow> vypocteneRadky = new();

            foreach (MergeRecipeInputRow mergeRow in _mergeReceptury)
            {
                var (recipeRows, _) = tabRECIPES.GetRecipesByID(mergeRow.IDrcp);
                tabRECIPES.RecipeDetailRow? recipeHeader = recipeRows.FirstOrDefault();

                if (recipeHeader == null)
                    continue;

                float puvodniVelikostReceptu = recipeHeader.AmountPcs;

                if (puvodniVelikostReceptu <= 0)
                    continue;

                float zadaneMnozstvi = (float)mergeRow.VelikostValue;
                float koeficient = zadaneMnozstvi / puvodniVelikostReceptu;

                foreach (tabRECIPES.RecipeDetailRow r in recipeRows)
                {
                    bool jeZaklad = r.IDzaklad > 0;
                    int idMaterialu = jeZaklad ? r.IDzaklad : r.IDmat;

                    vypocteneRadky.Add(new tabRECIPES.RecipeDetailRow
                    {
                        IDmat = idMaterialu,
                        IDzaklad = r.IDzaklad,
                        MaterialCislo = r.MaterialCislo,
                        MaterialName = string.IsNullOrWhiteSpace(r.RecipeMatCislo)
                            ? r.MaterialName
                            : $"{r.RecipeMatCislo} - {r.MaterialName}",
                        BaseDavka = r.Davka,
                        BaseTolerance = r.Tolerance,
                        Davka = (float)Math.Round(r.Davka * koeficient, 3),
                        Tolerance = r.Tolerance,
                        Status = jeZaklad ? 20 : 0,
                        MaterialIsZaklad = jeZaklad ? 1 : 0
                    });
                }
            }

            List<tabRECIPES.RecipeDetailRow> slouceneRadky = vypocteneRadky
                .GroupBy(x => new
                {
                    x.IDmat,
                    x.MaterialCislo,
                    x.MaterialName,
                    x.MaterialIsZaklad
                })
                .Select(g => new tabRECIPES.RecipeDetailRow
                {
                    IDmat = g.Key.IDmat,
                    MaterialCislo = g.Key.MaterialCislo,
                    MaterialName = g.Key.MaterialName,
                    MaterialIsZaklad = g.Key.MaterialIsZaklad,
                    BaseDavka = (float)Math.Round(g.Sum(x => x.Davka), 3),
                    Davka = (float)Math.Round(g.Sum(x => x.Davka), 3),
                    BaseTolerance = g.Max(x => x.Tolerance),
                    Tolerance = g.Max(x => x.Tolerance),
                    Status = g.Key.MaterialIsZaklad > 0 ? 20 : 0
                })
                .OrderBy(x => x.MaterialName)
                .ToList();

            _rows.AddRange(slouceneRadky);

            _pozadovaneMnozstviReceptu =
                (float)_mergeReceptury.Sum(x => x.VelikostValue);

            _velikostVarky = _pozadovaneMnozstviReceptu;
            _puvodniVelikostVarky = _pozadovaneMnozstviReceptu;
            _defaultVelikostReceptu = _pozadovaneMnozstviReceptu;
            _pocetVarek = 1;
            _celkovaVelikost = _pozadovaneMnozstviReceptu;
        }

        // ==========================================
        // Načtení naplánované nebo aktivní sloučené zakázky
        // ==========================================
        private void LoadMergeProduction(int jobId)
        {
            var (jobRows, _) = tabJOB_LIST.GetJobByID(jobId);
            var job = jobRows.FirstOrDefault();

            if (job == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Zakázka", "Sloučená zakázka nebyla nalezena.");
                return;
            }

            var (prodRows, _) = tabPRODUCTION.GetProductionViewByID_IDX(jobId, 1);

            _history = false;
            _idJob = job.IDjob;
            _idRcp = 0;
            _idGrp = 0;
            _idMrg = job.IDmrg;
            _status = job.Status;
            _zaklad = Settings.Param_Units == 0 ? 1 : 0;

            _velikostVarky = job.ReqAmountPcs;
            _puvodniVelikostVarky = job.ReqAmountPcs;
            _defaultVelikostReceptu = job.ReqAmountPcs;
            _pozadovaneMnozstviReceptu = job.ReqAmountPcs;
            _pocetVarek = job.ReqNumberBatch <= 0 ? 1 : job.ReqNumberBatch;
            _celkovaVelikost = _velikostVarky * _pocetVarek;

            TxtRecipeName.Text = "Sloučená receptura";
            TxtJobNo.Text = job.JobNo;
            TxtBatchNo.Text = job.BatchNo;

            foreach (var r in prodRows)
            {
                bool jeZaklad = r.IDzaklad > 0;

                _rows.Add(new tabRECIPES.RecipeDetailRow
                {
                    IDmat = jeZaklad ? r.IDzaklad : r.IDmat,
                    IDzaklad = r.IDzaklad,
                    MaterialCislo = r.MaterialCislo,
                    MaterialName = string.IsNullOrWhiteSpace(r.RecipeMatCislo)
                        ? r.MaterialName
                        : $"{r.RecipeMatCislo} - {r.MaterialName}",
                    MaterialIsZaklad = jeZaklad ? 1 : 0,
                    Davka = r.HmotnostPozadovana,
                    BaseDavka = r.HmotnostPozadovana,
                    Tolerance = r.Tolerance,
                    BaseTolerance = r.Tolerance,
                    Status = jeZaklad ? 20 : 0,
                    UserName = r.UserName
                });
            }
        }

        // ==========================================
        // Načtení historie sloučené zakázky
        // ==========================================
        private void LoadMergeHistory(int jobId, int batchIndex)
        {
            var (prodRows, _) = tabPRODUCTION.GetProductionViewByID_IDX(jobId, batchIndex);
            var first = prodRows.FirstOrDefault();

            if (first == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Historie", "Historie sloučené zakázky nebyla nalezena.");
                return;
            }

            var (jobRows, _) = tabJOB_LIST.GetJobByID(jobId);
            var job = jobRows.FirstOrDefault();

            if (job == null)
            {
                ZobrazInfo(InfoBarSeverity.Error, "Zakázka", "Zakázka nebyla nalezena.");
                return;
            }

            _history = true;
            _idJob = jobId;
            _idRcp = 0;
            _idGrp = 0;
            _idMrg = job.IDmrg;
            _status = job.Status;
            _zaklad = Settings.Param_Units == 0 ? 1 : 0;

            _velikostVarky = first.JobAmountPcs;
            _puvodniVelikostVarky = _velikostVarky;
            _defaultVelikostReceptu = _velikostVarky;
            _pocetVarek = batchIndex;
            _celkovaVelikost = _velikostVarky;
            _pozadovaneMnozstviReceptu = _velikostVarky * _pocetVarek;

            TxtRecipeName.Text = "Sloučená receptura";
            TxtJobNo.Text = first.JobNo;
            TxtBatchNo.Text = first.BatchNo;
            NaplnComboHistorickychSarzi();
            foreach (var r in prodRows)
            {
                bool jeZaklad = r.IDzaklad > 0;

                _rows.Add(new tabRECIPES.RecipeDetailRow
                {
                    IDmat = jeZaklad ? r.IDzaklad : r.IDmat,
                    IDzaklad = r.IDzaklad,
                    MaterialCislo = r.MaterialCislo,
                    MaterialName = string.IsNullOrWhiteSpace(r.RecipeMatCislo)
                        ? r.MaterialName
                        : $"{r.RecipeMatCislo} - {r.MaterialName}",
                    MaterialIsZaklad = jeZaklad ? 1 : 0,
                    Davka = r.HmotnostNavazena,
                    BaseDavka = r.HmotnostPozadovana,
                    Tolerance = r.Tolerance,
                    BaseTolerance = r.Tolerance,
                    Status = jeZaklad ? 20 : 0,
                    UserName = r.UserName
                });
            }
        }

        // ==========================================
        // Aktualizace UI po změně dat
        // ==========================================
        private void AktualizujUI()
        {
            TxtAmountCaption.Text = $"Množství receptu ({JednotkaText})";
            TxtAmount.Text = FormatMnozstvi(_velikostVarky);
            TxtBatchCount.Text = _pocetVarek.ToString();

            TxtTotalPcs.Text = FormatMnozstvi(_celkovaVelikost);

            float totalKg =
                _rows.Sum(x => x.Davka) * (_history ? 1 : _pocetVarek);

            TxtTotalKg.Text = FormatKg(totalKg);

            PanelTotalPcs.Visibility = JeRezimKg
                ? Visibility.Collapsed
                : Visibility.Visible;

            HeaderDavka.Text = _history ? "Naváženo" : "Požadováno";
            HeaderBaseDavka.Visibility = _history ? Visibility.Visible : Visibility.Collapsed;

            BtnStartRecipe.Visibility = _history ? Visibility.Collapsed : Visibility.Visible;
            BtnPlanRecipe.Visibility = _history ? Visibility.Collapsed : Visibility.Visible;

            HeaderUser.Visibility = _history ? Visibility.Visible : Visibility.Collapsed;

            BtnAmount.IsEnabled = !_history;
            BtnBatchCount.IsEnabled = !_history;
            BtnJobNo.IsEnabled = !_history;
            BtnBatchNo.IsEnabled = true;

            TxtBatchNo.Visibility = _history
                ? Visibility.Collapsed
                : Visibility.Visible;

            CmbHistoryBatch.Visibility = _history
                ? Visibility.Visible
                : Visibility.Collapsed;

            MaterialsTable.ItemsSource = _rows
                .Select(x => new RecipeDetailDisplayRow
                {
                    Source = x,
                    MaterialCislo = x.MaterialCislo ?? "",
                    MaterialName = x.MaterialName ?? "",
                    DavkaText = FormatKg(x.Davka),
                    BaseDavkaText = FormatKg(x.BaseDavka),
                    ToleranceText = FormatKg(x.Tolerance),
                    BaseDavkaVisibility = _history ? Visibility.Visible : Visibility.Collapsed,
                    UserName = x.UserName ?? "",
                    UserVisibility = _history ? Visibility.Visible : Visibility.Collapsed
                })
                .ToList();



            if (_history)
            {
                ZobrazInfoStatus(
                    "Historie",
                    "Zobrazen je přehled historické dávky.",
                    InfoBarSeverity.Informational);
            }
            else
            {
                ZobrazInfoStatus(
                    "Detail receptury",
                    "Zkontrolujte recepturu před spuštěním nebo plánováním.",
                    InfoBarSeverity.Informational);
            }

            ObnovPosuvneTextyLevehoPanelu();
        }

        // ==========================================
        // Přepočet surovin podle aktuální velikosti várky
        // ==========================================
        private void UpdateScaledMaterials()
        {
            if (_puvodniVelikostVarky <= 0)
                return;

            float scale = _velikostVarky / _puvodniVelikostVarky;

            foreach (tabRECIPES.RecipeDetailRow row in _rows)
            {
                row.Davka = (float)Math.Round(row.BaseDavka * scale, 3);
            }

            AktualizujUI();
        }

        // ==========================================
        // Automatické kroky přepočtu várky
        // ==========================================
        private void OtevriKrokyPrepocetVarky()
        {
            OtevriEditaciCisla(
                $"Zadej požadované množství ({JednotkaText})",
                _pozadovaneMnozstviReceptu,
                zadaneMnozstvi =>
                {
                    if (zadaneMnozstvi <= 0)
                        return;

                    if (!Settings.Param_VypocetVarky)
                    {
                        _pozadovaneMnozstviReceptu = zadaneMnozstvi;

                        _velikostVarky = JeRezimKg
                            ? (float)Math.Round(zadaneMnozstvi, 3)
                            : (float)Math.Ceiling(zadaneMnozstvi);

                        _pocetVarek = 1;
                        _celkovaVelikost = _velikostVarky;

                        UpdateScaledMaterials();
                        return;
                    }

                    OtevriEditaciCisla(
                        $"Zadej velikost díže ({JednotkaText})",
                        (float)Settings.Param_MaxBatchSize,
                        velikostDize =>
                        {
                            if (velikostDize <= 0)
                                return;

                            ProvedPrepocetVarky(zadaneMnozstvi, velikostDize);
                        });
                });
        }

        // ==========================================
        // Přepočet pouze přes velikost díže
        // ==========================================
        private void OtevriKrokVelikostDize()
        {
            OtevriEditaciCisla(
                $"Zadej velikost díže ({JednotkaText})",
                (float)Settings.Param_MaxBatchSize,
                velikostDize =>
                {
                    if (velikostDize <= 0)
                        return;

                    ProvedPrepocetVarky(_pozadovaneMnozstviReceptu, velikostDize);
                });
        }

        // ==========================================
        // Výpočet rozdělení na várky
        // ==========================================
        private void ProvedPrepocetVarky(float pozadovaneMnozstvi, float velikostDize)
        {
            if (_rows.Count == 0)
                return;

            if (_velikostVarky <= 0)
                return;

            float hmotnostJedneAktualniVarkyKg = _rows.Sum(x => x.Davka);

            if (hmotnostJedneAktualniVarkyKg <= 0)
                return;

            float kgNaJednotkuReceptu =
                hmotnostJedneAktualniVarkyKg / _velikostVarky;

            if (kgNaJednotkuReceptu <= 0)
                return;

            int vypocetMode = Settings.Param_VypocetVarkyMode;

            float maxReceptuNaJednuVarku =
                vypocetMode == 0
                    ? velikostDize
                    : velikostDize / kgNaJednotkuReceptu;

            if (maxReceptuNaJednuVarku <= 0)
                return;

            int novyPocetVarek =
                (int)Math.Ceiling(pozadovaneMnozstvi / maxReceptuNaJednuVarku);

            if (novyPocetVarek < 1)
                novyPocetVarek = 1;

            float novaVelikostJedneVarky = JeRezimKg
                ? (float)Math.Round(pozadovaneMnozstvi / novyPocetVarek, 3)
                : (float)Math.Ceiling(pozadovaneMnozstvi / novyPocetVarek);

            float noveCelkoveMnozstvi =
                novaVelikostJedneVarky * novyPocetVarek;

            float novaJednaVarkaKg =
                (float)Math.Round(novaVelikostJedneVarky * kgNaJednotkuReceptu, 3);

            float noveCelkemKg =
                (float)Math.Round(novaJednaVarkaKg * novyPocetVarek, 3);

            _pozadovaneMnozstviReceptu = pozadovaneMnozstvi;
            _velikostVarky = novaVelikostJedneVarky;
            _pocetVarek = novyPocetVarek;
            _celkovaVelikost = noveCelkoveMnozstvi;

            Settings.Param_MaxBatchSize = (double)velikostDize;

            UpdateScaledMaterials();
            AktualizujUI();

            ZobrazInfoStatus(
                "Přepočet várky",
                $"Zadané množství receptu: {FormatMnozstvi(pozadovaneMnozstvi)} | " +
                $"Velikost díže: {(vypocetMode == 0 ? FormatMnozstvi(velikostDize) : FormatKg(velikostDize))} | " +
                $"Počet várek: {novyPocetVarek} | " +
                $"Množství receptu na 1 várku: {FormatMnozstvi(novaVelikostJedneVarky)} | " +
                $"Skutečná hmotnost 1 várky: {FormatKg(novaJednaVarkaKg)} | " +
                $"Celková suma surovin: {FormatKg(noveCelkemKg)}",
                InfoBarSeverity.Success);
        }

        // ==========================================
        // Editace množství receptu
        // ==========================================
        private void BtnAmount_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_history)
                return;

            OtevriEditaciCisla(
                $"Zadej množství receptu ({JednotkaText})",
                _velikostVarky,
                value =>
                {
                    if (value <= 0)
                        return;

                    _pozadovaneMnozstviReceptu = value;
                    _velikostVarky = JeRezimKg
                        ? (float)Math.Round(value, 3)
                        : (float)Math.Ceiling(value);

                    _pocetVarek = 1;
                    _celkovaVelikost = _velikostVarky;

                    UpdateScaledMaterials();
                });
        }

        // ==========================================
        // Editace počtu várek
        // ==========================================
        private void BtnBatchCount_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_history)
                return;

            OtevriEditaciCisla(
                "Zadej počet várek",
                _pocetVarek,
                value =>
                {
                    int newCount = (int)Math.Round(value);

                    if (newCount < 1)
                        newCount = 1;

                    _pocetVarek = newCount;
                    _celkovaVelikost = _velikostVarky * _pocetVarek;

                    AktualizujUI();
                });
        }

        // ==========================================
        // Editace názvu zakázky
        // ==========================================
        private void BtnJobNo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_history)
                return;

            OtevriEditaciTextu(
                "Zadej název zakázky",
                TxtJobNo.Text ?? "",
                value =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    TxtJobNo.Text = value.Trim();
                    ObnovPosuvneTextyLevehoPanelu();
                });
        }

        // ==========================================
        // Editace názvu šarže
        // ==========================================
        private void BtnBatchNo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_history)
            {
                NaplnComboHistorickychSarzi();
                CmbHistoryBatch.IsDropDownOpen = true;
                return;
            }

            OtevriEditaciTextu(
                "Zadej název šarže",
                TxtBatchNo.Text ?? "",
                value =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    TxtBatchNo.Text = value.Trim();
                    ObnovPosuvneTextyLevehoPanelu();
                });
        }
        private void NaplnComboHistorickychSarzi()
        {
            if (!_history || _idJob <= 0)
                return;

            _nacitamHistorickeSarze = true;

            List<string> list = tabPRODUCTION.GetBatchNoList(_idJob)
                ?? new List<string>();

            List<HistoryBatchComboRow> rows = list
                .Select(x => new HistoryBatchComboRow
                {
                    Text = x,
                    BatchIndex = ZiskejIndexSarzeZTextu(x)
                })
                .ToList();

            CmbHistoryBatch.ItemsSource = rows;
            CmbHistoryBatch.SelectedValue = _batchIndex;

            _nacitamHistorickeSarze = false;
        }
        private void CmbHistoryBatch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_nacitamHistorickeSarze)
                return;

            if (!_history)
                return;

            if (CmbHistoryBatch.SelectedItem is not HistoryBatchComboRow row)
                return;

            if (row.BatchIndex <= 0)
                return;

            if (row.BatchIndex == _batchIndex)
                return;

            Frame?.Navigate(
                typeof(RecipeDetailPage),
                new RecipeDetailArgs
                {
                    Mode = EditMode.History,
                    Id = _idJob,
                    BatchIndex = row.BatchIndex
                });
        }
        private int ZiskejIndexSarzeZTextu(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 1;

            string[] parts = text.Split('-');

            if (parts.Length > 1 &&
                int.TryParse(parts[^1], out int index) &&
                index > 0)
            {
                return index;
            }

            return 1;
        }

        // ==========================================
        // Spuštění receptury
        // ==========================================
        private void StartRecipe_Click(object sender, RoutedEventArgs e)
        {
            bool jeSloucenaReceptura = _currentMode == EditMode.MergeRecipes;

            string jobNo = TxtJobNo.Text?.Trim() ?? "JOB";
            string batchNo = TxtBatchNo.Text?.Trim() ?? "BATCH";
            float amount = _velikostVarky;
            int batchCount = _pocetVarek;

            int newIDjob = _idJob;
            int statusToSet = 1;

            bool exists = newIDjob > 0 && tabJOB_LIST.JobExists(newIDjob);

            int idmrg = 0;

            if (jeSloucenaReceptura)
            {
                if (exists)
                {
                    var (jobRows, _) = tabJOB_LIST.GetJobByID(newIDjob);
                    idmrg = jobRows.FirstOrDefault()?.IDmrg ?? 0;

                    if (idmrg <= 0)
                        idmrg = tabJOB_LIST.GetNextIDmrg();
                }
                else
                {
                    idmrg = tabJOB_LIST.GetNextIDmrg();
                }
            }

            var (_, activeCount) = tabJOB_LIST.GetJobListViewByStatus(1);
            bool otherActive = activeCount > 0;

            if (otherActive && (!exists || _status != 1))
            {
                string msg = exists
                    ? "Jiná zakázka je právě spuštěna.\nChcete uložit změny této naplánované zakázky?"
                    : "Jiná zakázka je právě spuštěna.\nChcete tuto zakázku uložit jako naplánovanou?";

                ConfirmWindow confirm = new ConfirmWindow(
                    "Jiná aktivní zakázka",
                    msg);

                confirm.Closed += (_, _) =>
                {
                    if (!confirm.Potvrzeno)
                        return;

                    int confirmedStatusToSet = 0;
                    int confirmedIDjob = newIDjob;

                    if (!exists)
                    {
                        confirmedIDjob = tabJOB_LIST.InsertNewJob(new tabJOB_LIST.tabJobListInsert
                        {
                            IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                            IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                            IDmrg = jeSloucenaReceptura ? idmrg : 0,
                            JobNo = jobNo,
                            BatchNo = batchNo,
                            AmountPcs = amount,
                            PlannedBatch = batchCount,
                            Status = confirmedStatusToSet,
                            StationIdx = 0,
                            ImportSource = 0,
                            DT = DateTime.Now,
                            ZakladVypocten = 0
                        });

                        _idJob = confirmedIDjob;
                    }
                    else
                    {
                        tabJOB_LIST.UpdateJob(new tabJOB_LIST.viewJobList
                        {
                            IDjob = confirmedIDjob,
                            IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                            IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                            IDmrg = jeSloucenaReceptura ? idmrg : 0,
                            JobNo = jobNo,
                            BatchNo = batchNo,
                            ReqAmountPcs = amount,
                            ReqNumberBatch = batchCount,
                            Status = confirmedStatusToSet,
                            ZakladVypocten = 0
                        });
                    }

                    Frame?.Navigate(typeof(JobPage));
                };

                ModalWindowService.Otevri(confirm);
                return;
            }

            ConfirmWindow startConfirm = new ConfirmWindow(
                "Spuštění receptury",
                $"Opravdu chcete spustit recepturu?\n\n" +
                $"Receptura: {TxtRecipeName.Text}\n" +
                $"Zakázka: {jobNo}\n" +
                $"Šarže: {batchNo}\n" +
                $"Množství: {FormatMnozstvi(amount)}\n" +
                $"Počet várek: {batchCount}");

            startConfirm.Closed += (_, _) =>
            {
                if (!startConfirm.Potvrzeno)
                    return;

                int confirmedIDjob = newIDjob;

                if (!exists)
                {
                    confirmedIDjob = tabJOB_LIST.InsertNewJob(new tabJOB_LIST.tabJobListInsert
                    {
                        IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                        IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                        IDmrg = jeSloucenaReceptura ? idmrg : 0,
                        JobNo = jobNo,
                        BatchNo = batchNo,
                        AmountPcs = amount,
                        PlannedBatch = batchCount,
                        Status = statusToSet,
                        StationIdx = 0,
                        ImportSource = 0,
                        DT = DateTime.Now,
                        ZakladVypocten = 0
                    });

                    _idJob = confirmedIDjob;
                    if (statusToSet == 1)
                    {
                        tabJOB_LIST.SetActionReq(confirmedIDjob, 1);
                    }
                }
                else
                {
                    tabJOB_LIST.UpdateJob(new tabJOB_LIST.viewJobList
                    {
                        IDjob = confirmedIDjob,
                        IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                        IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                        IDmrg = jeSloucenaReceptura ? idmrg : 0,
                        JobNo = jobNo,
                        BatchNo = batchNo,
                        ReqAmountPcs = amount,
                        ReqNumberBatch = batchCount,
                        Status = statusToSet,
                        ZakladVypocten = 0
                    });

                    if (statusToSet == 1)
                    {
                        tabJOB_LIST.SetActionReq(confirmedIDjob, 1);
                    }
                }

                if (!tabPRODUCTION.HasProductionRows(confirmedIDjob) &&
                    (statusToSet == 1 || jeSloucenaReceptura))
                {
                    VytvorProductionRadky(
                        confirmedIDjob,
                        idmrg,
                        batchNo,
                        jeSloucenaReceptura);
                }

                if (statusToSet == 1 && Settings.Param_ActPageOpen)
                    tabSIGNAL.SetIDjob(confirmedIDjob);

                Frame?.Navigate(
                   typeof(RecipeActualPage),
                     new RecipeActualArgs
                     {
                         IDjob = confirmedIDjob,
                         BatchIndex = 1
                     });
            };

            ModalWindowService.Otevri(startConfirm);
        }

        // ==========================================
        // Naplánování receptury
        // ==========================================
        private void PlanRecipe_Click(object sender, RoutedEventArgs e)
        {
            bool jeSloucenaReceptura = _currentMode == EditMode.MergeRecipes;

            string jobNo = TxtJobNo.Text?.Trim() ?? "JOB";
            string batchNo = TxtBatchNo.Text?.Trim() ?? "BATCH";
            float amount = _velikostVarky;
            int batchCount = _pocetVarek;

            int newIDjob = _idJob;
            bool exists = newIDjob > 0 && tabJOB_LIST.JobExists(newIDjob);

            int statusToSet = 0;

            int idmrg = 0;

            if (jeSloucenaReceptura)
            {
                if (exists)
                {
                    var (jobRows, _) = tabJOB_LIST.GetJobByID(newIDjob);
                    idmrg = jobRows.FirstOrDefault()?.IDmrg ?? 0;
                }
                else
                {
                    idmrg = tabJOB_LIST.GetNextIDmrg();
                }

                if (idmrg <= 0)
                    idmrg = tabJOB_LIST.GetNextIDmrg();
            }

            ConfirmWindow confirm = new ConfirmWindow(
                "Naplánování receptury",
                $"Opravdu chcete naplánovat recepturu?\n\n" +
                $"Receptura: {TxtRecipeName.Text}");

            confirm.Closed += (_, _) =>
            {
                if (!confirm.Potvrzeno)
                    return;

                int confirmedIDjob = newIDjob;

                if (!exists)
                {
                    confirmedIDjob = tabJOB_LIST.InsertNewJob(new tabJOB_LIST.tabJobListInsert
                    {
                        IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                        IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                        IDmrg = jeSloucenaReceptura ? idmrg : 0,
                        JobNo = jobNo,
                        BatchNo = batchNo,
                        AmountPcs = amount,
                        PlannedBatch = batchCount,
                        Status = statusToSet,
                        StationIdx = 0,
                        ImportSource = 0,
                        DT = DateTime.Now,
                        ZakladVypocten = 0
                    });

                    _idJob = confirmedIDjob;
                }
                else
                {
                    tabJOB_LIST.UpdateJob(new tabJOB_LIST.viewJobList
                    {
                        IDjob = confirmedIDjob,
                        IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                        IDgrp = jeSloucenaReceptura ? 0 : _idGrp,
                        IDmrg = jeSloucenaReceptura ? idmrg : 0,
                        JobNo = jobNo,
                        BatchNo = batchNo,
                        ReqAmountPcs = amount,
                        ReqNumberBatch = batchCount,
                        Status = statusToSet,
                        ZakladVypocten = 0
                    });
                }

                if (jeSloucenaReceptura &&
                    !tabPRODUCTION.HasProductionRows(confirmedIDjob))
                {
                    VytvorProductionRadky(
                        confirmedIDjob,
                        idmrg,
                        batchNo,
                        true);
                }

                Frame?.Navigate(typeof(JobPage));
            };

            ModalWindowService.Otevri(confirm);
        }


        // ==========================================
        // Vytvoření řádků do tabPRODUCTION
        // ==========================================
        private void VytvorProductionRadky(int idjob, int idmrg, string batchNo, bool jeSloucenaReceptura)
        {
            string newBatchNo = $"{batchNo}-1";

            bool isZakladJob = false;

            if (!jeSloucenaReceptura)
            {
                var (row, ok) = tabRECIPES.GetRecipeRowByIDrcp(_idRcp);

                if (ok && row != null)
                    isZakladJob = row.IsZaklad > 0;
            }

            foreach (tabRECIPES.RecipeDetailRow item in _rows)
            {
                int idmat;
                int idzaklad;

                if (isZakladJob)
                {
                    idmat = item.IDmat;
                    idzaklad = 0;
                }
                else
                {
                    idmat = item.MaterialIsZaklad > 0 ? 0 : item.IDmat;
                    idzaklad = item.MaterialIsZaklad > 0 ? item.IDmat : 0;
                }

                tabPRODUCTION.InsertProductionFromRecipe(new tabPRODUCTION.tabProduction
                {
                    IDjob = idjob,
                    IDrcp = jeSloucenaReceptura ? 0 : _idRcp,
                    IDmrg = jeSloucenaReceptura ? idmrg : 0,
                    IDmat = idmat,
                    IDzaklad = idzaklad,
                    BatchNo = newBatchNo,
                    HmotnostNavazena = 0,
                    HmotnostPozadovana = (float)Math.Round(item.Davka, 3),
                    Tolerance = (float)Math.Round(item.Tolerance, 3),
                    Status = 0
                });
            }
        }

        // ==========================================
        // Obnovení stránky
        // ==========================================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ObnovStranku();
        }

        private void BtnRefresh_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ObnovStranku();
        }

        private void ObnovStranku()
        {
            Frame?.Navigate(
                typeof(RecipeDetailPage),
                new RecipeDetailArgs
                {
                    Mode = _currentMode,
                    Id = _currentMode == EditMode.Recipes ? _idRcp : _idJob,
                    BatchIndex = _batchIndex,
                    MergeRecipes = _mergeReceptury
                });
        }

        // ==========================================
        // Návrat
        // ==========================================
        private void BtnBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentMode == EditMode.Planned || _currentMode == EditMode.History)
            {
                Frame?.Navigate(typeof(JobPage));
                return;
            }

            if (_currentMode == EditMode.MergeRecipes)
            {
                Frame?.Navigate(_zpetNaProduction ? typeof(JobPage) : typeof(RecipePage));
                return;
            }

            Frame?.Navigate(typeof(RecipePage));
        }

        // ==========================================
        // Přihlášení / odhlášení
        // ==========================================
        private void UserPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (UserSession.IsLoggedIn)
            {
                UserSession.Logout();
                AktualizujOperatora();
                return;
            }

            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Closed += (_, _) =>
            {
                AktualizujOperatora();
            };

            ModalWindowService.Otevri(loginWindow);
        }

        // ==========================================
        // Aktualizace operátora
        // ==========================================
        private void AktualizujOperatora()
        {
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                TxtLoginCaption.Text = "Odhlásit";
                TxtLoginUser.Text = UserSession.CurrentUser.Username;
                TxtLoginUser.Visibility = Visibility.Visible;
            }
            else
            {
                TxtLoginCaption.Text = "Přihlášení";
                TxtLoginUser.Text = "";
                TxtLoginUser.Visibility = Visibility.Collapsed;
            }
        }

        // ==========================================
        // Otevření textové klávesnice
        // ==========================================
        private void OtevriEditaciTextu(string titulek, string hodnota, Action<string> poPotvrzeni)
        {
            VirtualKeyboard keyboard = new VirtualKeyboard(
                VirtualKeyboard.KeyboardMode.Str,
                hodnota ?? "",
                titulek);

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                poPotvrzeni.Invoke(keyboard.Vysledek ?? "");
            };

            ModalWindowService.Otevri(keyboard);
        }

        // ==========================================
        // Otevření číselné klávesnice
        // ==========================================
        private void OtevriEditaciCisla(string titulek, float hodnota, Action<float> poPotvrzeni)
        {
            VirtualKeyboard.KeyboardMode mode =
                JeRezimKg
                    ? VirtualKeyboard.KeyboardMode.Float
                    : VirtualKeyboard.KeyboardMode.Int;

            VirtualKeyboard keyboard = new VirtualKeyboard(
                mode,
                hodnota.ToString("0.###", CultureInfo.InvariantCulture),
                titulek);

            keyboard.Closed += (_, _) =>
            {
                if (!keyboard.Potvrzeno)
                    return;

                string text = (keyboard.Vysledek ?? "")
                    .Replace(",", ".");

                if (!float.TryParse(
                        text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out float value))
                {
                    ZobrazInfo(InfoBarSeverity.Warning, titulek, "Zadaná hodnota není platné číslo.");
                    return;
                }

                poPotvrzeni.Invoke(value);
            };

            ModalWindowService.Otevri(keyboard);
        }

        // ==========================================
        // Posuvné texty v levém panelu
        // ==========================================
        private void ObnovPosuvneTextyLevehoPanelu()
        {
            // Měření TextBlocku je spolehlivé až po překreslení layoutu.
            // Proto se spuštění animací odloží do fronty UI vlákna.
            DispatcherQueue.TryEnqueue(() =>
            {
                SpustPosunTextuPokudJeDlouhy(
                    TxtRecipeName,
                    TxtRecipeNameTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtJobNo,
                    TxtJobNoTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtBatchNo,
                    TxtBatchNoTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtAmount,
                    TxtAmountTransform,
                    SirkaTextuLevehoPanelu);

                SpustPosunTextuPokudJeDlouhy(
                    TxtBatchCount,
                    TxtBatchCountTransform,
                    SirkaTextuLevehoPanelu);
            });
        }

        private void SpustPosunTextuPokudJeDlouhy(
            TextBlock textBlock,
            TranslateTransform transform,
            double dostupnaSirka)
        {
            // Při každém obnovení textu se nejdříve zastaví původní animace.
            // Zabrání se tím vrstvení více animací nad stejným TextBlockem.
            if (_posuvneTextyAnimace.TryGetValue(textBlock, out Storyboard? puvodniAnimace))
            {
                puvodniAnimace.Stop();
                _posuvneTextyAnimace.Remove(textBlock);
            }

            transform.X = 0;

            if (string.IsNullOrWhiteSpace(textBlock.Text))
                return;

            // Text se změří bez omezení šířky, aby šlo zjistit,
            // zda se do pevné šířky levého panelu opravdu nevejde.
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double skutecnaSirkaTextu = textBlock.DesiredSize.Width;

            if (skutecnaSirkaTextu <= dostupnaSirka)
                return;

            // Malá rezerva na konci zajistí, že se při posunu zobrazí celý text
            // a poslední znak nebude přilepený přímo na hranu výřezu.
            double cilovyPosun = dostupnaSirka - skutecnaSirkaTextu - 18;

            Storyboard animace = new Storyboard
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            DoubleAnimation posunTextu = new DoubleAnimation
            {
                From = 0,
                To = cilovyPosun,
                BeginTime = TimeSpan.FromSeconds(1),
                Duration = new Duration(TimeSpan.FromSeconds(4))
            };

            Storyboard.SetTarget(posunTextu, transform);
            Storyboard.SetTargetProperty(posunTextu, "X");

            animace.Children.Add(posunTextu);
            _posuvneTextyAnimace[textBlock] = animace;

            animace.Begin();
        }

        // ==========================================
        // Krátké zobrazení horního vysouvacího informačního panelu
        // ==========================================
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _infoStatusTimer;

        private void ZobrazInfo(InfoBarSeverity severity, string title, string message)
        {
            ZobrazInfoStatus(title, message, severity);
        }

        private void ZobrazInfoStatus(string title, string message, InfoBarSeverity severity)
        {
            // Naplnění textů horního toast panelu.
            InfoToastTitle.Text = title;
            InfoToastMessage.Text = message;

            // Nastavení barevné SVG ikony podle typu hlášení.
            InfoToastIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                new Uri(VratInfoIkonu(severity)));

            // Nastavení pozadí a rámečku podle typu hlášení a aktuálního režimu aplikace.
            NastavInfoToastStyl(severity);

            // Krátké skrytí před opětovným zobrazením zajistí,
            // že se animace vysunutí spustí i při rychlém opakovaném hlášení.
            InfoToast.Visibility = Visibility.Collapsed;
            InfoToast.HorizontalAlignment = HorizontalAlignment.Center;
            InfoToast.Visibility = Visibility.Visible;

            _infoStatusTimer?.Stop();

            _infoStatusTimer = DispatcherQueue.CreateTimer();
            _infoStatusTimer.Interval = TimeSpan.FromSeconds(4);

            _infoStatusTimer.Tick += (_, _) =>
            {
                _infoStatusTimer?.Stop();
                InfoToast.Visibility = Visibility.Collapsed;
            };

            _infoStatusTimer.Start();
        }

        private static string VratInfoIkonu(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => "ms-appx:///Assets/MenuIcons/ic_fluent_send_28_color.svg",
                InfoBarSeverity.Warning => "ms-appx:///Assets/MenuIcons/ic_fluent_warning_28_color.svg",
                InfoBarSeverity.Error => "ms-appx:///Assets/MenuIcons/ic_fluent_dismiss_circle_28_color.svg",
                _ => "ms-appx:///Assets/MenuIcons/ic_fluent_alert_28_color.svg"
            };
        }

        private void NastavInfoToastStyl(InfoBarSeverity severity)
        {
            bool darkTheme = ActualTheme == ElementTheme.Dark;

            (byte a, byte r, byte g, byte b) border = severity switch
            {
                InfoBarSeverity.Success => darkTheme ? ((byte)255, (byte)100, (byte)221, (byte)23) : ((byte)255, (byte)16, (byte)124, (byte)16),
                InfoBarSeverity.Warning => darkTheme ? ((byte)255, (byte)255, (byte)193, (byte)7) : ((byte)255, (byte)169, (byte)109, (byte)0),
                InfoBarSeverity.Error => darkTheme ? ((byte)255, (byte)255, (byte)82, (byte)82) : ((byte)255, (byte)176, (byte)0, (byte)32),
                _ => darkTheme ? ((byte)255, (byte)79, (byte)195, (byte)247) : ((byte)255, (byte)0, (byte)95, (byte)184)
            };

            (byte a, byte r, byte g, byte b) background = severity switch
            {
                InfoBarSeverity.Success => darkTheme ? ((byte)240, (byte)24, (byte)44, (byte)24) : ((byte)255, (byte)233, (byte)246, (byte)233),
                InfoBarSeverity.Warning => darkTheme ? ((byte)240, (byte)56, (byte)45, (byte)14) : ((byte)255, (byte)255, (byte)244, (byte)214),
                InfoBarSeverity.Error => darkTheme ? ((byte)240, (byte)58, (byte)22, (byte)30) : ((byte)255, (byte)253, (byte)236, (byte)238),
                _ => darkTheme ? ((byte)240, (byte)16, (byte)36, (byte)56) : ((byte)255, (byte)236, (byte)244, (byte)252)
            };

            InfoToast.BorderBrush = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(border.a, border.r, border.g, border.b));

            InfoToast.Background = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(background.a, background.r, background.g, background.b));
        }
    }

    // ==========================================
    // Navigační parametry detailu receptury
    // ==========================================
    public sealed class RecipeDetailArgs
    {
        public RecipeDetailPage.EditMode Mode { get; set; } =
            RecipeDetailPage.EditMode.Recipes;

        public int Id { get; set; }

        public int BatchIndex { get; set; } = 1;

        public List<MergeRecipeInputRow>? MergeRecipes { get; set; }
    }



    // ==========================================
    // Navigační parametry aktivní receptury
    // =========================================
    public sealed class RecipeActualArgs
    {
        public int IDjob { get; set; }

        public int BatchIndex { get; set; } = 1;
    }

    // ==========================================
    // Zobrazovací řádek tabulky detailu receptury
    // ==========================================
    internal sealed class RecipeDetailDisplayRow
    {
        public tabRECIPES.RecipeDetailRow Source { get; set; } = new();

        public string MaterialCislo { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public string DavkaText { get; set; } = "";

        public string BaseDavkaText { get; set; } = "";

        public string ToleranceText { get; set; } = "";

        public string UserName { get; set; } = "";

        public Visibility BaseDavkaVisibility { get; set; } = Visibility.Collapsed;

        public Visibility UserVisibility { get; set; } = Visibility.Collapsed;
    }

    // ==========================================
    // Batch pro výběr historických šarží
    // ==========================================
    internal sealed class HistoryBatchComboRow
    {
        public string Text { get; set; } = "";

        public int BatchIndex { get; set; } = 1;
    }
}