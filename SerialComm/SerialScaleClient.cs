using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCP_WT1.SerialComm
{
    // ==========================================
    // Klient komunikace s váhou
    // ==========================================
    public sealed class SerialScaleClient : IDisposable
    {
        // ==========================================
        // Veřejné objekty
        // ==========================================
        public WeightDosing Dosing { get; } = new();


        // ==========================================
        // Hodnoty pro zobrazení v UI
        // ==========================================
        public string StatusText { get; private set; } = "Není spojení s váhou";
        public string StatusCode { get; private set; } = "NV";
        public double? WeightDisplay { get; private set; } = null;
        public string? WeightText { get; private set; } = null;
        public string? Units { get; private set; } = null;


        // ==========================================
        // Události
        // ==========================================
        public event EventHandler? Updated;
        public event EventHandler<long>? AlibiCaptured;


        // ==========================================
        // Základní konfigurace váhy
        // ==========================================
        private string _addr = "01";
        private readonly int _scaleIndex = 1;
        private volatile bool _pausePolling = false;


        // ==========================================
        // Typ komunikace
        // ==========================================
        private enum TransportKind
        {
            COM,
            ETH
        }

        private TransportKind _kind;


        // ==========================================
        // COM komunikace
        // ==========================================
        private SerialPort? _com;
        private string _portName = "";
        private int _baud;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private Handshake _handshake;


        // ==========================================
        // TCP komunikace
        // ==========================================
        private string _host = "";
        private int _tcpPort;
        private TcpClient? _tcp;
        private StreamReader? _reader;
        private StreamWriter? _writer;


        // ==========================================
        // Polling a časování
        // ==========================================
        private string _readCmd = "READ";
        private string _newline = "\r\n";
        private bool _asyncRead = false;
        private int _pollMs = 300;
        private int _timeoutMs = 2000;


        // ==========================================
        // Nastavení zobrazení hodnoty
        // ==========================================
        private double _displayDivisor = 1.0;
        private string? _unitsFallback = "kg";


        // ==========================================
        // Parser příchozích dat
        // ==========================================
        private string _incomingFormat = "{status},GS,{weight}{units?}";
        private Dictionary<string, string> _tokenDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _fieldMap = new(StringComparer.OrdinalIgnoreCase);


        // ==========================================
        // Mapování stavů váhy
        // ==========================================
        private Dictionary<string, string> _statusMap = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _statusTextMap = new(StringComparer.OrdinalIgnoreCase);


        // ==========================================
        // Alibi paměť
        // ==========================================
        private bool _alibiEnabled = false;
        private string _alibiStoreCmd = "{addr}PID";
        private string? _alibiSuccessFormat = null;
        private string? _alibiEchoFormat = null;


        // ==========================================
        // Připravené regexy
        // ==========================================
        private Regex? _parserRegex;
        private Regex? _alibiSuccessRegex;
        private Regex? _alibiEchoRegex;


        // ==========================================
        // Worker komunikace
        // ==========================================
        private CancellationTokenSource? _cts;
        private Task? _worker;


        // ==========================================
        // Synchronizace komunikace
        // ==========================================
        private readonly SemaphoreSlim _ioLock = new(1, 1);
        private readonly SemaphoreSlim _cmdLock = new(1, 1);


        // ==========================================
        // Hlídání výpadku spojení
        // =========================================
        private int _timeoutsInRow = 0;
        private const int _nvAfterTimeouts = 3;
        private int _ticksSincePing = 0;
        private const int _heartbeatMs = 3000;


        // ==========================================
        // Čekání na odpověď zařízení
        // ==========================================
        private readonly object _awaitLock = new();
        private TaskCompletionSource<string?>? _awaitTcs;
        private Regex? _awaitRegex;


        // ==========================================
        // Konstruktory
        // =========================================
        public SerialScaleClient(
            string portName = "COM4",
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            _kind = TransportKind.COM;
            _portName = portName;
            _baud = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _handshake = Handshake.None;
        }

        public SerialScaleClient(int scaleIndex)
        {
            _scaleIndex = scaleIndex;
            LoadFromSettings();
        }

        public static SerialScaleClient FromSettings()
        {
            int idx = Settings.Param_ScaleIndex;

            if (idx < 1 || idx > 5)
                idx = 1;

            return new SerialScaleClient(idx);
        }


        // ==========================================
        // Start, stop a dispose
        // ==========================================
        public void Start()
        {
            if (_worker != null)
                return;

            _cts = new CancellationTokenSource();
            _worker = Task.Run(async () => await WorkerAsync(_cts.Token));
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            try
            {
                _worker?.Wait(300);
            }
            catch
            {
            }

            _worker = null;

            _cts?.Dispose();
            _cts = null;

            CloseTransport();
        }

        public void ReloadFromSettings()
        {
            Stop();
            LoadFromSettings();
            Start();
        }

        public void Dispose()
        {
            Stop();
            CloseTransport();
        }


        // ==========================================
        // Alibi funkce
        // ==========================================
        public void SendPid()
        {
            if (!_alibiEnabled)
                return;

            string cmd = BuildCommand(_alibiStoreCmd);

            _ = WriteAsync(
                EnsureNewline(cmd, _newline),
                CancellationToken.None);
        }

        public async Task<long?> CaptureAlibiAsync(
            int timeoutMs = 3000,
            CancellationToken ct = default)
        {
            if (!_alibiEnabled)
                return null;

            TaskCompletionSource<long?> tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, long id)
            {
                AlibiCaptured -= Handler;
                tcs.TrySetResult(id);
            }

            AlibiCaptured += Handler;

            try
            {
                SendPid();

                using CancellationTokenSource timeoutCts =
                    new(timeoutMs);

                using CancellationTokenSource linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        ct,
                        timeoutCts.Token);

                using IDisposable registration =
                    linkedCts.Token.Register(() => tcs.TrySetResult(null));

                return await tcs.Task;
            }
            finally
            {
                try
                {
                    AlibiCaptured -= Handler;
                }
                catch
                {
                }
            }
        }


        // ==========================================
        // Tara váhy
        // ==========================================
        public async Task<bool> SendTareAsync(
            int timeoutMs = 1500,
            CancellationToken ct = default)
        {
            await _cmdLock.WaitAsync(ct);

            try
            {
                _pausePolling = true;

                string cmd = EnsureNewline($"{_addr}ZERO", _newline);

                TaskCompletionSource<string?> tcs =
                    new(TaskCreationOptions.RunContinuationsAsynchronously);

                lock (_awaitLock)
                {
                    _awaitTcs?.TrySetResult(null);
                    _awaitTcs = tcs;

                    _awaitRegex = new Regex(
                        @"^\s*(?:\d{2}\s*)?OK\s*$",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                await WriteAsync(cmd, ct);

                using CancellationTokenSource timeoutCts =
                    new(timeoutMs);

                using CancellationTokenSource linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        ct,
                        timeoutCts.Token);

                using IDisposable registration =
                    linkedCts.Token.Register(() => tcs.TrySetResult(null));

                string? response = await tcs.Task;

                if (!string.IsNullOrWhiteSpace(response) &&
                    Regex.IsMatch(
                        response,
                        @"^\s*(?:\d{2}\s*)?OK\b",
                        RegexOptions.IgnoreCase))
                {
                    return true;
                }

                if (string.Equals(
                        StatusCode,
                        "ST",
                        StringComparison.OrdinalIgnoreCase) &&
                    WeightDisplay.HasValue &&
                    Math.Abs(WeightDisplay.Value) < 0.001)
                {
                    return true;
                }

                return false;
            }
            finally
            {
                _pausePolling = false;

                lock (_awaitLock)
                {
                    _awaitTcs = null;
                    _awaitRegex = null;
                }

                _cmdLock.Release();
            }
        }


        // ==========================================
        // Hlavní worker komunikace
        // ==========================================
        private async Task WorkerAsync(CancellationToken ct)
        {
            _timeoutsInRow = 0;
            _ticksSincePing = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await EnsureOpenAsync(ct);

                        if (!_asyncRead && !_pausePolling)
                        {
                            string cmd = BuildCommand(_readCmd);

                            if (!string.IsNullOrWhiteSpace(cmd))
                            {
                                await WriteAsync(
                                    EnsureNewline(cmd, _newline),
                                    ct);
                            }
                        }

                        string? line = await ReadLineAsync(ct);

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _timeoutsInRow = 0;
                            ProcessLine(line.Trim());
                        }
                        else
                        {
                            if (!_pausePolling)
                            {
                                _timeoutsInRow++;

                                if (_timeoutsInRow >= _nvAfterTimeouts)
                                {
                                    SetStatus("NV");
                                    WeightDisplay = null;
                                    WeightText = null;
                                    Updated?.Invoke(this, EventArgs.Empty);
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        SafeDropConnection();
                    }
                    catch (SocketException)
                    {
                        SafeDropConnection();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "[Scale] Neočekávaná chyba workeru: " + ex);

                        SafeDropConnection();
                    }

                    try
                    {
                        await Task.Delay(_pollMs, ct);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Scale] Worker ukončen.");
            }
        }


        // ==========================================
        // Uvolnění a shození spojení
        // ==========================================
        public static bool TryReleasePort(string portName)
        {
            try
            {
                using SerialPort port = new(portName);

                port.Open();
                port.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SafeDropConnection()
        {
            try
            {
                CloseTransport();
            }
            catch
            {
            }

            SetStatus("NV");

            WeightDisplay = null;
            WeightText = null;

            Updated?.Invoke(this, EventArgs.Empty);

            _timeoutsInRow = 0;
            _ticksSincePing = 0;
        }


        // ==========================================
        // Zpracování příchozích dat
        // ==========================================
        private void ProcessLine(string text)
        {
            //System.Diagnostics.Debug.WriteLine($"[SCALE RAW] {text}");
            TaskCompletionSource<string?>? awaitTcs;
            Regex? awaitRegex;

            lock (_awaitLock)
            {
                awaitTcs = _awaitTcs;
                awaitRegex = _awaitRegex;
            }

            if (awaitTcs != null &&
                awaitRegex != null &&
                awaitRegex.IsMatch(text))
            {
                awaitTcs.TrySetResult(text);
                return;
            }

            if (_alibiEnabled)
            {
                if (_alibiSuccessRegex != null &&
                    _alibiSuccessRegex.Match(text) is { Success: true } alibiSuccessMatch)
                {
                    if (TryParseAlibi(alibiSuccessMatch, out long alibiId))
                    {
                        AlibiCaptured?.Invoke(this, alibiId);
                    }

                    return;
                }

                if (_alibiEchoRegex != null &&
                    _alibiEchoRegex.Match(text) is { Success: true } alibiEchoMatch)
                {
                    ProcessWeightMatch(alibiEchoMatch);

                    if (TryParseAlibi(alibiEchoMatch, out long alibiId))
                    {
                        AlibiCaptured?.Invoke(this, alibiId);
                    }

                    return;
                }
            }

            if (_parserRegex != null &&
                _parserRegex.Match(text) is { Success: true } match)
            {
                ProcessWeightMatch(match);
                return;
            }

            SetStatus("NV");

            WeightDisplay = null;
            WeightText = null;

            Updated?.Invoke(this, EventArgs.Empty);
        }


        // ==========================================
        // Alibi parser
        // ==========================================
        private static bool TryParseAlibi(Match match, out long id)
        {
            id = 0;

            if (!match.Groups.TryGetValue("alibi", out Group? group))
                return false;

            string digits = Regex.Replace(group.Value ?? "", @"\D", "");

            if (digits.Length < 6)
                return false;

            string last6 = digits.Substring(digits.Length - 6, 6);

            return long.TryParse(last6, out id);
        }


        // ==========================================
        // Pomocné čtení regex skupin
        // ==========================================
        private static string GetFirstNonEmpty(Match match, string groupName)
        {
            if (match.Groups.TryGetValue(groupName, out Group? group) &&
                group.Success &&
                !string.IsNullOrWhiteSpace(group.Value))
            {
                return group.Value;
            }

            return "";
        }


        // ==========================================
        // Mapování stavu váhy
        // ==========================================
        private string MapStatus(string incoming)
        {
            if (string.IsNullOrWhiteSpace(incoming))
                return "NV";

            string key = incoming.Trim();

            if (_statusMap.TryGetValue(key, out string? mapped) &&
                !string.IsNullOrWhiteSpace(mapped))
            {
                return mapped.ToUpperInvariant();
            }

            return key.ToUpperInvariant();
        }


        // ==========================================
        // Výchozí texty stavů váhy
        // ==========================================
        private static readonly Dictionary<string, string> DefaultStatusTextMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TL"] = "Chyba",
                ["OL"] = "Mimo max. rozsah",
                ["UL"] = "Mimo min. rozsah",
                ["ST"] = "Stabilní",
                ["US"] = "Nestabilní",
                ["NV"] = "Není spojení s váhou"
            };


        // ==========================================
        // Nastavení stavu váhy
        // ==========================================
        private void SetStatus(string normalizedCode)
        {
            StatusCode = string.IsNullOrWhiteSpace(normalizedCode)
                ? "NV"
                : normalizedCode;

            if (_statusTextMap.TryGetValue(StatusCode, out string? customText) &&
                !string.IsNullOrWhiteSpace(customText))
            {
                StatusText = customText;
                return;
            }

            if (DefaultStatusTextMap.TryGetValue(StatusCode, out string? defaultText))
            {
                StatusText = defaultText;
                return;
            }

            StatusText = string.IsNullOrWhiteSpace(StatusCode)
                ? "Neznámý stav"
                : StatusCode;
        }


        // ==========================================
        // Načtení string hodnoty z nastavení
        // ==========================================
        private static string GetS(string key, string defaultValue)
        {
            try
            {
                return GetSettingValue(key)?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Načtení int hodnoty z nastavení
        // ==========================================
        private static int GetI(string key, int defaultValue)
        {
            try
            {
                object? value = GetSettingValue(key);

                if (value is int intValue)
                    return intValue;

                return int.TryParse(
                    value?.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result)
                    ? result
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Načtení double hodnoty z nastavení
        // ==========================================
        private static double GetDLoose(string key, double defaultValue)
        {
            try
            {
                object? value = GetSettingValue(key);

                if (value is double doubleValue)
                    return doubleValue;

                string text = value?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(text))
                    return defaultValue;

                text = text.Replace(',', '.');

                return double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double result)
                    ? result
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Načtení bool hodnoty z nastavení
        // ==========================================
        private static bool GetB(string key, bool defaultValue)
        {
            try
            {
                object? value = GetSettingValue(key);

                if (value is bool boolValue)
                    return boolValue;

                return bool.TryParse(value?.ToString(), out bool result)
                    ? result
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


        // ==========================================
        // Zpracování hodnoty váhy z regexu
        // ==========================================
        private void ProcessWeightMatch(Match match)
        {
            string statusValue = GetFirstNonEmpty(match, "status");
            string normalizedStatus = MapStatus(statusValue);

            SetStatus(normalizedStatus);

            string weightValue = GetFirstNonEmpty(match, "weight");

            if (!string.IsNullOrWhiteSpace(weightValue))
            {
                WeightText = weightValue.Trim();

                if (double.TryParse(
                    weightValue.Replace(',', '.').Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double rawWeight))
                {
                    WeightDisplay = rawWeight;
                    Dosing.Update(rawWeight);
                }
                else
                {
                    WeightDisplay = null;
                }
            }
            else
            {
                WeightDisplay = null;
                WeightText = null;
            }

            string unitsValue = GetFirstNonEmpty(match, "units");

            Units = !string.IsNullOrWhiteSpace(unitsValue)
                ? unitsValue
                : _unitsFallback;

            Updated?.Invoke(this, EventArgs.Empty);
        }


        // ==========================================
        // Univerzální čtení hodnoty z WinUI Settings
        // ==========================================
        private static object? GetSettingValue(string key)
        {
            return key switch
            {
                "Param_Comm_Type1" => Settings.Param_Comm_Type1,
                "Param_Comm_Type2" => Settings.Param_Comm_Type2,
                "Param_Comm_Type3" => Settings.Param_Comm_Type3,
                "Param_Comm_Type4" => Settings.Param_Comm_Type4,
                "Param_Comm_Type5" => Settings.Param_Comm_Type5,

                "Param_Scale_Address1" => Settings.Param_Scale_Address1,
                "Param_Scale_Address2" => Settings.Param_Scale_Address2,
                "Param_Scale_Address3" => Settings.Param_Scale_Address3,
                "Param_Scale_Address4" => Settings.Param_Scale_Address4,
                "Param_Scale_Address5" => Settings.Param_Scale_Address5,

                "Param_Serial_Port1" => Settings.Param_Serial_Port1,
                "Param_Serial_Port2" => Settings.Param_Serial_Port2,
                "Param_Serial_Port3" => Settings.Param_Serial_Port3,
                "Param_Serial_Port4" => Settings.Param_Serial_Port4,
                "Param_Serial_Port5" => Settings.Param_Serial_Port5,

                "Param_Serial_Baud1" => Settings.Param_Serial_Baud1,
                "Param_Serial_Baud2" => Settings.Param_Serial_Baud2,
                "Param_Serial_Baud3" => Settings.Param_Serial_Baud3,
                "Param_Serial_Baud4" => Settings.Param_Serial_Baud4,
                "Param_Serial_Baud5" => Settings.Param_Serial_Baud5,

                "Param_Serial_Parity1" => Settings.Param_Serial_Parity1,
                "Param_Serial_Parity2" => Settings.Param_Serial_Parity2,
                "Param_Serial_Parity3" => Settings.Param_Serial_Parity3,
                "Param_Serial_Parity4" => Settings.Param_Serial_Parity4,
                "Param_Serial_Parity5" => Settings.Param_Serial_Parity5,

                "Param_Serial_DataBits1" => Settings.Param_Serial_DataBits1,
                "Param_Serial_DataBits2" => Settings.Param_Serial_DataBits2,
                "Param_Serial_DataBits3" => Settings.Param_Serial_DataBits3,
                "Param_Serial_DataBits4" => Settings.Param_Serial_DataBits4,
                "Param_Serial_DataBits5" => Settings.Param_Serial_DataBits5,

                "Param_Serial_StopBits1" => Settings.Param_Serial_StopBits1,
                "Param_Serial_StopBits2" => Settings.Param_Serial_StopBits2,
                "Param_Serial_StopBits3" => Settings.Param_Serial_StopBits3,
                "Param_Serial_StopBits4" => Settings.Param_Serial_StopBits4,
                "Param_Serial_StopBits5" => Settings.Param_Serial_StopBits5,

                "Param_Serial_Handshake1" => Settings.Param_Serial_Handshake1,
                "Param_Serial_Handshake2" => Settings.Param_Serial_Handshake2,
                "Param_Serial_Handshake3" => Settings.Param_Serial_Handshake3,
                "Param_Serial_Handshake4" => Settings.Param_Serial_Handshake4,
                "Param_Serial_Handshake5" => Settings.Param_Serial_Handshake5,

                "Param_Serial_Host1" => Settings.Param_Serial_Host1,
                "Param_Serial_Host2" => Settings.Param_Serial_Host2,
                "Param_Serial_Host3" => Settings.Param_Serial_Host3,
                "Param_Serial_Host4" => Settings.Param_Serial_Host4,
                "Param_Serial_Host5" => Settings.Param_Serial_Host5,

                "Param_Serial_TcpPort1" => Settings.Param_Serial_TcpPort1,
                "Param_Serial_TcpPort2" => Settings.Param_Serial_TcpPort2,
                "Param_Serial_TcpPort3" => Settings.Param_Serial_TcpPort3,
                "Param_Serial_TcpPort4" => Settings.Param_Serial_TcpPort4,
                "Param_Serial_TcpPort5" => Settings.Param_Serial_TcpPort5,

                "Param_Scale_ReadCommand1" => Settings.Param_Scale_ReadCommand1,
                "Param_Scale_ReadCommand2" => Settings.Param_Scale_ReadCommand2,
                "Param_Scale_ReadCommand3" => Settings.Param_Scale_ReadCommand3,
                "Param_Scale_ReadCommand4" => Settings.Param_Scale_ReadCommand4,
                "Param_Scale_ReadCommand5" => Settings.Param_Scale_ReadCommand5,

                "Param_Scale_Newline1" => Settings.Param_Scale_Newline1,
                "Param_Scale_Newline2" => Settings.Param_Scale_Newline2,
                "Param_Scale_Newline3" => Settings.Param_Scale_Newline3,
                "Param_Scale_Newline4" => Settings.Param_Scale_Newline4,
                "Param_Scale_Newline5" => Settings.Param_Scale_Newline5,

                "Param_Scale_Async1" => Settings.Param_Scale_Async1,
                "Param_Scale_Async2" => Settings.Param_Scale_Async2,
                "Param_Scale_Async3" => Settings.Param_Scale_Async3,
                "Param_Scale_Async4" => Settings.Param_Scale_Async4,
                "Param_Scale_Async5" => Settings.Param_Scale_Async5,

                "Param_Scale_PollMs1" => Settings.Param_Scale_PollMs1,
                "Param_Scale_PollMs2" => Settings.Param_Scale_PollMs2,
                "Param_Scale_PollMs3" => Settings.Param_Scale_PollMs3,
                "Param_Scale_PollMs4" => Settings.Param_Scale_PollMs4,
                "Param_Scale_PollMs5" => Settings.Param_Scale_PollMs5,

                "Param_Scale_TimeoutMs1" => Settings.Param_Scale_TimeoutMs1,
                "Param_Scale_TimeoutMs2" => Settings.Param_Scale_TimeoutMs2,
                "Param_Scale_TimeoutMs3" => Settings.Param_Scale_TimeoutMs3,
                "Param_Scale_TimeoutMs4" => Settings.Param_Scale_TimeoutMs4,
                "Param_Scale_TimeoutMs5" => Settings.Param_Scale_TimeoutMs5,

                "Param_Scale_DisplayDivisor1" => Settings.Param_Scale_DisplayDivisor1,
                "Param_Scale_DisplayDivisor2" => Settings.Param_Scale_DisplayDivisor2,
                "Param_Scale_DisplayDivisor3" => Settings.Param_Scale_DisplayDivisor3,
                "Param_Scale_DisplayDivisor4" => Settings.Param_Scale_DisplayDivisor4,
                "Param_Scale_DisplayDivisor5" => Settings.Param_Scale_DisplayDivisor5,

                "Param_Scale_UnitsFallback1" => Settings.Param_Scale_UnitsFallback1,
                "Param_Scale_UnitsFallback2" => Settings.Param_Scale_UnitsFallback2,
                "Param_Scale_UnitsFallback3" => Settings.Param_Scale_UnitsFallback3,
                "Param_Scale_UnitsFallback4" => Settings.Param_Scale_UnitsFallback4,
                "Param_Scale_UnitsFallback5" => Settings.Param_Scale_UnitsFallback5,

                "Param_Scale_Format1" => Settings.Param_Scale_Format1,
                "Param_Scale_Format2" => Settings.Param_Scale_Format2,
                "Param_Scale_Format3" => Settings.Param_Scale_Format3,
                "Param_Scale_Format4" => Settings.Param_Scale_Format4,
                "Param_Scale_Format5" => Settings.Param_Scale_Format5,

                "Param_Scale_TokenDefs1" => Settings.Param_Scale_TokenDefs1,
                "Param_Scale_TokenDefs2" => Settings.Param_Scale_TokenDefs2,
                "Param_Scale_TokenDefs3" => Settings.Param_Scale_TokenDefs3,
                "Param_Scale_TokenDefs4" => Settings.Param_Scale_TokenDefs4,
                "Param_Scale_TokenDefs5" => Settings.Param_Scale_TokenDefs5,

                "Param_Scale_FieldMap1" => Settings.Param_Scale_FieldMap1,
                "Param_Scale_FieldMap2" => Settings.Param_Scale_FieldMap2,
                "Param_Scale_FieldMap3" => Settings.Param_Scale_FieldMap3,
                "Param_Scale_FieldMap4" => Settings.Param_Scale_FieldMap4,
                "Param_Scale_FieldMap5" => Settings.Param_Scale_FieldMap5,

                "Param_Scale_StatusMap1" => Settings.Param_Scale_StatusMap1,
                "Param_Scale_StatusMap2" => Settings.Param_Scale_StatusMap2,
                "Param_Scale_StatusMap3" => Settings.Param_Scale_StatusMap3,
                "Param_Scale_StatusMap4" => Settings.Param_Scale_StatusMap4,
                "Param_Scale_StatusMap5" => Settings.Param_Scale_StatusMap5,

                "Param_Scale_StatusTextMap1" => Settings.Param_Scale_StatusTextMap1,
                "Param_Scale_StatusTextMap2" => Settings.Param_Scale_StatusTextMap2,
                "Param_Scale_StatusTextMap3" => Settings.Param_Scale_StatusTextMap3,
                "Param_Scale_StatusTextMap4" => Settings.Param_Scale_StatusTextMap4,
                "Param_Scale_StatusTextMap5" => Settings.Param_Scale_StatusTextMap5,

                "Param_Scale_EnableAlibi1" => Settings.Param_Scale_EnableAlibi1,
                "Param_Scale_EnableAlibi2" => Settings.Param_Scale_EnableAlibi2,
                "Param_Scale_EnableAlibi3" => Settings.Param_Scale_EnableAlibi3,
                "Param_Scale_EnableAlibi4" => Settings.Param_Scale_EnableAlibi4,
                "Param_Scale_EnableAlibi5" => Settings.Param_Scale_EnableAlibi5,

                "Param_Scale_AlibiStoreCommand1" => Settings.Param_Scale_AlibiStoreCommand1,
                "Param_Scale_AlibiStoreCommand2" => Settings.Param_Scale_AlibiStoreCommand2,
                "Param_Scale_AlibiStoreCommand3" => Settings.Param_Scale_AlibiStoreCommand3,
                "Param_Scale_AlibiStoreCommand4" => Settings.Param_Scale_AlibiStoreCommand4,
                "Param_Scale_AlibiStoreCommand5" => Settings.Param_Scale_AlibiStoreCommand5,

                "Param_Scale_AlibiSuccessFormat1" => Settings.Param_Scale_AlibiSuccessFormat1,
                "Param_Scale_AlibiSuccessFormat2" => Settings.Param_Scale_AlibiSuccessFormat2,
                "Param_Scale_AlibiSuccessFormat3" => Settings.Param_Scale_AlibiSuccessFormat3,
                "Param_Scale_AlibiSuccessFormat4" => Settings.Param_Scale_AlibiSuccessFormat4,
                "Param_Scale_AlibiSuccessFormat5" => Settings.Param_Scale_AlibiSuccessFormat5,

                "Param_Scale_AlibiEchoFormat1" => Settings.Param_Scale_AlibiEchoFormat1,
                "Param_Scale_AlibiEchoFormat2" => Settings.Param_Scale_AlibiEchoFormat2,
                "Param_Scale_AlibiEchoFormat3" => Settings.Param_Scale_AlibiEchoFormat3,
                "Param_Scale_AlibiEchoFormat4" => Settings.Param_Scale_AlibiEchoFormat4,
                "Param_Scale_AlibiEchoFormat5" => Settings.Param_Scale_AlibiEchoFormat5,

                _ => null
            };
        }

        // ==========================================
        // Načtení nastavení
        // =========================================
        private void LoadFromSettings()
        {
            string idx = _scaleIndex.ToString();

            string commType = GetS($"Param_Comm_Type{idx}", "COM").Trim().ToUpperInvariant();

            _addr = GetS($"Param_Scale_Address{idx}", "01");

            if (commType == "ETH")
            {
                _kind = TransportKind.ETH;
                _host = GetS($"Param_Serial_Host{idx}", "127.0.0.1");
                _tcpPort = GetI($"Param_Serial_TcpPort{idx}", 502);
            }
            else
            {
                _kind = TransportKind.COM;
                _portName = GetS($"Param_Serial_Port{idx}", "COM1");
                _baud = GetI($"Param_Serial_Baud{idx}", 9600);

                _parity = Enum.TryParse(GetS($"Param_Serial_Parity{idx}", "None"), true, out Parity parity)
                    ? parity
                    : Parity.None;

                _dataBits = GetI($"Param_Serial_DataBits{idx}", 8);

                _stopBits = Enum.TryParse(GetS($"Param_Serial_StopBits{idx}", "One"), true, out StopBits stopBits)
                    ? stopBits
                    : StopBits.One;

                _handshake = Enum.TryParse(GetS($"Param_Serial_Handshake{idx}", "None"), true, out Handshake handshake)
                    ? handshake
                    : Handshake.None;
            }

            _readCmd = GetS($"Param_Scale_ReadCommand{idx}", "READ");
            _newline = DecodeNewline(GetS($"Param_Scale_Newline{idx}", "\\r\\n"));
            _asyncRead = GetB($"Param_Scale_Async{idx}", false);
            _pollMs = GetI($"Param_Scale_PollMs{idx}", 300);
            _timeoutMs = GetI($"Param_Scale_TimeoutMs{idx}", 2000);
            _displayDivisor = GetDLoose($"Param_Scale_DisplayDivisor{idx}", 1.0);
            _unitsFallback = GetS($"Param_Scale_UnitsFallback{idx}", "kg");

            _incomingFormat = GetS($"Param_Scale_Format{idx}", "{addr?}{status},GS,{weight}{units?}");
            _tokenDefs = ParseKeyValueList(GetS($"Param_Scale_TokenDefs{idx}", DefaultTokenDefs()));
            _fieldMap = ParseKeyValueList(GetS($"Param_Scale_FieldMap{idx}", DefaultFieldMap()));

            _statusMap = ParseKeyValueList(GetS($"Param_Scale_StatusMap{idx}", DefaultStatusMap()));
            _statusTextMap = ParseKeyValueList(GetS($"Param_Scale_StatusTextMap{idx}", DefaultStatusTextOverrides()));

            _alibiEnabled = GetB($"Param_Scale_EnableAlibi{idx}", false);
            _alibiStoreCmd = GetS($"Param_Scale_AlibiStoreCommand{idx}", "{addr}PID");
            _alibiSuccessFormat = GetS($"Param_Scale_AlibiSuccessFormat{idx}", "");
            _alibiEchoFormat = GetS($"Param_Scale_AlibiEchoFormat{idx}", "");

            _parserRegex = CompileFormatToRegex(_incomingFormat, _tokenDefs, _fieldMap);

            _alibiSuccessRegex = string.IsNullOrWhiteSpace(_alibiSuccessFormat)
                ? null
                : CompileFormatToRegex(_alibiSuccessFormat, _tokenDefs, _fieldMap, new[] { "addr", "alibi", "status", "weight", "units" });

            _alibiEchoRegex = string.IsNullOrWhiteSpace(_alibiEchoFormat)
                ? null
                : CompileFormatToRegex(_alibiEchoFormat, _tokenDefs, _fieldMap, new[] { "addr", "alibi", "status", "weight", "units" });
        }


        // ==========================================
        // Převod konce řádku
        // ==========================================
        private static string DecodeNewline(string value)
        {
            return value
                .Replace("\\r\\n", "\r\n")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r");
        }


        // ==========================================
        // Otevření spojení
        // ==========================================
        private async Task EnsureOpenAsync(CancellationToken ct)
        {
            await _ioLock.WaitAsync(ct);

            try
            {
                if (_kind == TransportKind.ETH)
                {
                    if (_tcp is { Connected: true })
                        return;

                    _tcp = new TcpClient();
                    _tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    using CancellationTokenSource timeoutCts = new(_timeoutMs);
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    await _tcp.ConnectAsync(_host, _tcpPort, linkedCts.Token);

                    NetworkStream stream = _tcp.GetStream();

                    stream.ReadTimeout = _timeoutMs;
                    stream.WriteTimeout = _timeoutMs;

                    _reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

                    _writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
                    {
                        NewLine = _newline,
                        AutoFlush = true
                    };
                }
                else
                {
                    if (_com == null)
                    {
                        _com = new SerialPort(_portName, _baud, _parity, _dataBits, _stopBits)
                        {
                            Handshake = _handshake,
                            Encoding = Encoding.ASCII,
                            NewLine = _newline,
                            ReadTimeout = _timeoutMs,
                            WriteTimeout = _timeoutMs
                        };
                    }

                    if (!_com.IsOpen)
                        _com.Open();
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }


        // ==========================================
        // Zápis dat do váhy
        // ==========================================
        private async Task WriteAsync(string text, CancellationToken ct)
        {
            await _ioLock.WaitAsync(ct);

            try
            {
                if (_kind == TransportKind.COM)
                {
                    if (_com == null || !_com.IsOpen)
                        await EnsureOpenAsync(ct);

                    _com!.Write(text);
                }
                else
                {
                    if (_writer == null || _tcp is { Connected: false })
                        await EnsureOpenAsync(ct);

                    if (_writer == null)
                        throw new IOException("TCP writer není inicializován.");

                    await _writer.WriteAsync(text.AsMemory(), ct);
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }


        // ==========================================
        // Čtení dat z váhy
        // ==========================================
        private async Task<string?> ReadLineAsync(CancellationToken ct)
        {
            await _ioLock.WaitAsync(ct);

            try
            {
                if (_kind == TransportKind.COM)
                {
                    if (_com == null || !_com.IsOpen)
                        await EnsureOpenAsync(ct);

                    try
                    {
                        return _com!.ReadLine();
                    }
                    catch (TimeoutException)
                    {
                        return null;
                    }
                }
                else
                {
                    if (_reader == null || _tcp is { Connected: false })
                        await EnsureOpenAsync(ct);

                    if (_reader == null)
                        throw new IOException("TCP reader není inicializován.");

                    Task<string?> readTask = _reader.ReadLineAsync();
                    Task timeoutTask = Task.Delay(_timeoutMs, ct);

                    Task completed = await Task.WhenAny(readTask, timeoutTask);

                    if (completed == readTask)
                        return await readTask;

                    return null;
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }


        // ==========================================
        // Zavření spojení
        // ==========================================
        private void CloseTransport()
        {
            try
            {
                if (_com?.IsOpen == true)
                    _com.Close();
            }
            catch
            {
            }

            try
            {
                _com?.Dispose();
            }
            catch
            {
            }

            _com = null;

            try
            {
                _reader?.Dispose();
            }
            catch
            {
            }

            try
            {
                _writer?.Dispose();
            }
            catch
            {
            }

            try
            {
                _tcp?.Close();
            }
            catch
            {
            }

            _reader = null;
            _writer = null;
            _tcp = null;
        }


        // ==========================================
        // Pomocné funkce příkazů
        // ==========================================
        private string BuildCommand(string template)
        {
            if (string.IsNullOrEmpty(template))
                return "";

            return template.Replace("{addr}", _addr ?? "");
        }

        private string EnsureNewline(string cmd, string newline)
        {
            return cmd.EndsWith(newline, StringComparison.Ordinal)
                ? cmd
                : cmd + newline;
        }


        // ==========================================
        // Parsování konfiguračních textů
        // ==========================================
        private static Dictionary<string, string> ParseKeyValueList(string raw)
        {
            Dictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(raw))
                return dict;

            using StringReader reader = new(raw);

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                string text = line.Trim();

                if (text.Length == 0)
                    continue;

                if (text.StartsWith("#") || text.StartsWith(";"))
                    continue;

                int index = text.IndexOf('=');

                if (index < 0)
                    continue;

                string key = text.Substring(0, index).Trim();
                string value = text.Substring(index + 1).Trim();

                if (key.Length == 0)
                    continue;

                dict[key] = value;
            }

            return dict;
        }


        // ==========================================
        // Výchozí definice parseru
        // ==========================================
        private static string DefaultTokenDefs()
        {
            return
            @"addr   = \d{2}
            status = [A-Za-z]{2}
            weight = \s*-?\d+(?:[,.]\d+)?
            units  = [A-Za-z]{0,3}
            alibi  = \d{5}-?\d{6,}";
        }

        private static string DefaultFieldMap()
        {
            return
            @"addr   = addr
            status = status
            weight = weight
            units  = units
            alibi  = alibi";
        }

        private static string DefaultStatusMap()
        {
            return
            @"OK=ST
            ST=ST
            ATD=US
            US=US
            OL=OL
            UL=UL";
        }

        private static string DefaultStatusTextOverrides()
        {
            return "";
        }


        // ==========================================
        // Překlad formátu zprávy na regex
        // ==========================================

        private static Regex CompileFormatToRegex(
            string format,
            Dictionary<string, string> tokenDefs,
            Dictionary<string, string> fieldMap,
            IEnumerable<string>? onlyAllowedCanonical = null)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Formát příchozí zprávy je prázdný.");

            HashSet<string>? allowed = onlyAllowedCanonical != null
                ? new HashSet<string>(onlyAllowedCanonical, StringComparer.OrdinalIgnoreCase)
                : null;

            StringBuilder regexBuilder = new();

            regexBuilder.Append("^");

            for (int i = 0; i < format.Length;)
            {
                if (i + 3 < format.Length &&
                    format[i] == '{' &&
                    format[i + 1] == '{')
                {
                    int end = format.IndexOf("}}", i + 2, StringComparison.Ordinal);

                    if (end < 0)
                        throw new ArgumentException("Neuzavřený blok {{raw-regex}} ve formátu.");

                    string rawRegex = format.Substring(i + 2, end - (i + 2));

                    regexBuilder.Append(rawRegex);

                    i = end + 2;
                    continue;
                }

                if (format[i] == '{')
                {
                    int end = format.IndexOf('}', i + 1);

                    if (end < 0)
                        throw new ArgumentException("Neuzavřená složená závorka ve formátu.");

                    string tokenRaw = format.Substring(i + 1, end - i - 1).Trim();

                    if (string.IsNullOrWhiteSpace(tokenRaw))
                        throw new ArgumentException("Prázdný token {} ve formátu.");

                    char modifier = '\0';

                    if (tokenRaw.EndsWith("?") ||
                        tokenRaw.EndsWith("+") ||
                        tokenRaw.EndsWith("*"))
                    {
                        modifier = tokenRaw[^1];
                        tokenRaw = tokenRaw.Substring(0, tokenRaw.Length - 1).Trim();
                    }

                    if (!tokenDefs.TryGetValue(tokenRaw, out string? tokenRegex))
                        throw new ArgumentException($"Token {{{tokenRaw}}} nemá definici v Param_Scale_TokenDefs.");

                    string canonical = fieldMap.TryGetValue(tokenRaw, out string? mapped)
                        ? mapped.Trim()
                        : tokenRaw;

                    if (allowed != null && !allowed.Contains(canonical))
                        canonical = "";

                    string group = string.IsNullOrEmpty(canonical)
                        ? $"(?:{tokenRegex})"
                        : $"(?<{canonical}>{tokenRegex})";

                    group = modifier switch
                    {
                        '?' => $"(?:{group})?",
                        '+' => $"(?:{group})+",
                        '*' => $"(?:{group})*",
                        _ => group
                    };

                    regexBuilder.Append(group);

                    i = end + 1;
                    continue;
                }

                int next = format.IndexOf('{', i);

                string literal = next < 0
                    ? format[i..]
                    : format.Substring(i, next - i);

                regexBuilder.Append(Regex.Escape(literal));

                if (next < 0)
                    break;

                i = next;
            }

            regexBuilder.Append(@"\s*$");

            return new Regex(regexBuilder.ToString(), RegexOptions.Compiled);
        }
    }
}