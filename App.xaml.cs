using Microsoft.UI.Xaml;
using RCP_WT1.MySQL;
using RCP_WT1.SerialComm;
using RCP_WT1.Vizualizace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RCP_WT1
{
    public partial class App : Application
    {
        private Window? _window;
        private Mutex? _singleInstanceMutex;
        public bool DatabaseConnected { get; private set; } = false;

        private const string MutexName = @"Global\RCP_WT1_JedinaInstance";

        public static Window? MainWindow { get; private set; }

        public SerialScaleClient? Scale { get; private set; }

        public event Action<SerialScaleClient?>? ScaleChanged;

        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "START_OK.TXT"),
                DateTime.Now.ToString());

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                Exit();
                return;
            }

            InitWindow? initWindow = null;

            try
            {
                initWindow = new InitWindow();
                _window = initWindow;
                initWindow.Activate();

                await SpustitInicializaciAsync(initWindow);

                MainWindow mainWindow = new MainWindow(!DatabaseConnected);

                _window = mainWindow;
                MainWindow = mainWindow;

                mainWindow.Activate();

                initWindow.Close();
                initWindow = null;

            }
            catch (Exception ex)
            {
                LogException("CHYBA PŘI STARTU APLIKACE", ex);

                try
                {
                    initWindow?.Close();
                }
                catch
                {
                    // Ignorováno.
                }

                Exit();
            }
        }

        private async Task SpustitInicializaciAsync(InitWindow initWindow)
        {
            void Status(string text)
            {
                initWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    initWindow.SetStatus(text);
                });
            }

            void Detail(string text)
            {
                initWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    initWindow.SetDetail(text);
                });
            }

            Status("Spouštím aplikaci...");
            Detail("Připravuji základní prostředí aplikace.");
            await Task.Delay(1000);

            Status("Načítám nastavení aplikace...");
            Detail("Kontroluji uložené parametry a konfiguraci.");
            await Task.Delay(1000);

            Status("Nastavení načteno.");
            Detail("Konfigurace aplikace byla úspěšně připravena.");
            await Task.Delay(1000);

            Status("Inicializuji váhu...");
            Detail("Kontroluji povolené váhy a čekám na první data.");

            try
            {
                Scale = await InitEnabledScalesAsync(Status);

                if (Scale == null)
                {
                    Status("Žádná povolená váha není dostupná.");
                    Detail("Aplikace bude spuštěna bez aktivní váhy.");
                    await Task.Delay(1000);
                }
                else
                {
                    Status("Váha připravena.");
                    Detail("Komunikace s váhou byla úspěšně navázána.");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Status("Váha: CHYBA – inicializace selhala.");
                Detail("Aplikace bude spuštěna bez aktivní váhy.");

                LogException("CHYBA PŘI INICIALIZACI VÁHY", ex);

                Scale = null;
                ScaleChanged?.Invoke(null);

                await Task.Delay(1000);
            }

            Status("Připojuji k databázi...");
            Detail("Ověřuji dostupnost databázového serveru.");

            DatabaseConnected = false;

            try
            {
                DatabaseConnected = MySQL.MySQL.CheckConnection();
            }
            catch (Exception ex)
            {
                LogException("CHYBA PŘI PŘIPOJENÍ K DB", ex);
            }

            if (DatabaseConnected)
            {
                Status("Databáze: připojeno.");
                Detail("Databázové připojení je aktivní.");
            }
            else
            {
                Status("Databáze: CHYBA – nelze se připojit.");
                Detail("Aplikace pokračuje, ale databáze není dostupná.");
            }

            await Task.Delay(1000);

            Status("Spouštím monitoring databáze...");
            Detail("Připravuji sledování stavu databázového spojení.");

            try
            {
                MySQL.MySQL.StartMonitoringConnection();
                MySQL.MySQL.ConnectionStatusChanged += MySql_ConnectionStatusChanged;
            }
            catch (Exception ex)
            {
                LogException("CHYBA PŘI SPUŠTĚNÍ MONITORINGU DB", ex);
            }

            await Task.Delay(1000);

            Status("Spouštím hlavní aplikaci...");
            Detail("Dokončuji inicializaci systému.");
            await Task.Delay(1000);
        }

        private async Task<SerialScaleClient?> InitEnabledScalesAsync(Action<string> status)
        {
            List<int> povoleneVahy = new();

            for (int i = 1; i <= 5; i++)
            {
                bool povolena = i switch
                {
                    1 => Settings.Param_ScaleEnabled1,
                    2 => Settings.Param_ScaleEnabled2,
                    3 => Settings.Param_ScaleEnabled3,
                    4 => Settings.Param_ScaleEnabled4,
                    5 => Settings.Param_ScaleEnabled5,
                    _ => false
                };

                if (povolena)
                    povoleneVahy.Add(i);
            }

            if (povoleneVahy.Count == 0)
                return null;

            int preferovanaVaha = Settings.Param_ScaleIndex;

            if (povoleneVahy.Contains(preferovanaVaha))
            {
                povoleneVahy.Remove(preferovanaVaha);
                povoleneVahy.Insert(0, preferovanaVaha);
            }

            foreach (int indexVahy in povoleneVahy)
            {
                status($"Testuji váhu {indexVahy}...");

                try
                {
                    Scale?.Stop();
                    Scale?.Dispose();
                    Scale = null;

                    SerialScaleClient scale = new SerialScaleClient(indexVahy);

                    Scale = scale;
                    ScaleChanged?.Invoke(Scale);

                    scale.Start();

                    TaskCompletionSource<bool> prvniData =
                        new(TaskCreationOptions.RunContinuationsAsynchronously);

                    void Scale_Updated(object? sender, EventArgs e)
                    {
                        prvniData.TrySetResult(true);
                    }

                    scale.Updated += Scale_Updated;

                    await Task.WhenAny(prvniData.Task, Task.Delay(4000));

                    scale.Updated -= Scale_Updated;

                    if (scale.WeightDisplay != null)
                    {
                        status($"Váha {indexVahy}: OK");
                        Settings.Param_ScaleIndex = indexVahy;
                        return scale;
                    }

                    status($"Váha {indexVahy}: neodeslala data.");

                    scale.Stop();
                    scale.Dispose();

                    Scale = null;
                    ScaleChanged?.Invoke(null);

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    LogException($"CHYBA PŘI TESTU VÁHY {indexVahy}", ex);

                    Scale = null;
                    ScaleChanged?.Invoke(null);

                    await Task.Delay(1000);
                }
            }

            return null;
        }

        public void SwitchScale(int scaleIndex)
        {
            if (scaleIndex < 1 || scaleIndex > 5)
                scaleIndex = 1;

            Settings.Param_ScaleIndex = scaleIndex;

            try
            {
                Scale?.Stop();
                Scale?.Dispose();
                Scale = null;

                Thread.Sleep(150);
            }
            catch
            {
                // Ignorováno.
            }

            try
            {
                Scale = new SerialScaleClient(scaleIndex);
                Scale.Start();
            }
            catch (Exception ex)
            {
                LogException("CHYBA PŘI PŘEPNUTÍ VÁHY", ex);
                Scale = null;
            }

            ScaleChanged?.Invoke(Scale);
        }

        private void MySql_ConnectionStatusChanged()
        {
            try
            {
                if (MySQL.MySQL.IsOnline)
                    return;

                MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    Debug.WriteLine("Databáze je offline.");
                });
            }
            catch (Exception ex)
            {
                LogException("CHYBA PŘI REAKCI NA STAV DB", ex);
            }
        }

        private void LogException(string title, Exception ex)
        {
            string message =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"{title}\n" +
                $"{ex.Message}\n\n" +
                $"{ex.StackTrace}\n";

            try
            {
                File.AppendAllText("chyby.log", message);
            }
            catch
            {
                // Ignorováno.
            }

            Debug.WriteLine(message);
        }

        private void CurrentDomain_UnhandledException(
            object sender,
            System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogException("NEZACHYCENÁ CHYBA V APLIKACI", ex);
        }

        private void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            LogException("CHYBA V ASYNC ÚLOZE", e.Exception);
            e.SetObserved();
        }
    }
}