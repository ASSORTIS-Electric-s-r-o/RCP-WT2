using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace RCP_WT1
{
    // Nastavení aplikace uložené v souboru settings.json vedle spuštěné aplikace.
    // Názvy všech public property zůstávají stejné jako dříve, takže zbytek programu není potřeba upravovat.
    internal static class Settings
    {
        private static readonly object _lock = new();

        private static readonly string _filePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RCP-WT1",
                "settings.json");

        private static readonly Dictionary<string, object?> _values = NactiSoubor();

        private static Dictionary<string, object?> NactiSoubor()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(_filePath)!);

            try
            {
                if (!File.Exists(_filePath))
                    return new Dictionary<string, object?>();

                string json = File.ReadAllText(_filePath);

                return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                       ?? new Dictionary<string, object?>();
            }
            catch
            {
                return new Dictionary<string, object?>();
            }
        }

        private static void UlozSoubor()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(_filePath)!);

            try
            {
                string json = JsonSerializer.Serialize(
                    _values,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Nastavení nesmí shodit aplikaci.
            }
        }

        private static string GetString(string key, string defaultValue)
        {
            lock (_lock)
            {
                if (_values.TryGetValue(key, out object? value) && value != null)
                    return value.ToString() ?? defaultValue;

                return defaultValue;
            }
        }

        private static int GetInt(string key, int defaultValue)
        {
            lock (_lock)
            {
                if (!_values.TryGetValue(key, out object? value) || value == null)
                    return defaultValue;

                if (value is JsonElement json)
                {
                    if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out int jsonInt))
                        return jsonInt;

                    if (json.ValueKind == JsonValueKind.String &&
                        int.TryParse(json.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int jsonStringInt))
                        return jsonStringInt;
                }

                if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                    return result;

                return defaultValue;
            }
        }

        private static double GetDouble(string key, double defaultValue)
        {
            lock (_lock)
            {
                if (!_values.TryGetValue(key, out object? value) || value == null)
                    return defaultValue;

                if (value is JsonElement json)
                {
                    if (json.ValueKind == JsonValueKind.Number && json.TryGetDouble(out double jsonDouble))
                        return jsonDouble;

                    if (json.ValueKind == JsonValueKind.String)
                    {
                        string? text = json.GetString();

                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double invariantValue))
                            return invariantValue;

                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double currentValue))
                            return currentValue;
                    }
                }

                string textValue = value.ToString() ?? string.Empty;

                if (double.TryParse(textValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    return result;

                if (double.TryParse(textValue, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                    return result;

                return defaultValue;
            }
        }

        private static bool GetBool(string key, bool defaultValue)
        {
            lock (_lock)
            {
                if (!_values.TryGetValue(key, out object? value) || value == null)
                    return defaultValue;

                if (value is JsonElement json)
                {
                    if (json.ValueKind == JsonValueKind.True)
                        return true;

                    if (json.ValueKind == JsonValueKind.False)
                        return false;

                    if (json.ValueKind == JsonValueKind.String && bool.TryParse(json.GetString(), out bool jsonBool))
                        return jsonBool;
                }

                if (bool.TryParse(value.ToString(), out bool result))
                    return result;

                return defaultValue;
            }
        }

        private static void SetValue(string key, object? value)
        {
            lock (_lock)
            {
                _values[key] = value;
                UlozSoubor();
            }
        }

        // ==========================================
        // Nastavení MySQL
        // ==========================================
        public static string Param_SQL_IP { get => GetString(nameof(Param_SQL_IP), "82.142.97.63"); set => SetValue(nameof(Param_SQL_IP), value); }
        public static string Param_SQL_DB { get => GetString(nameof(Param_SQL_DB), "RCP"); set => SetValue(nameof(Param_SQL_DB), value); }
        public static string Param_SQL_USER { get => GetString(nameof(Param_SQL_USER), "buradmin"); set => SetValue(nameof(Param_SQL_USER), value); }
        public static string Param_SQL_PASSWORD { get => GetString(nameof(Param_SQL_PASSWORD), ".buradmin"); set => SetValue(nameof(Param_SQL_PASSWORD), value); }

        // ==========================================
        // Nastavení systému
        // ==========================================
        public static int LastSelectedStatusID { get => GetInt(nameof(LastSelectedStatusID), 0); set => SetValue(nameof(LastSelectedStatusID), value); }
        public static int Param_PC_ID { get => GetInt(nameof(Param_PC_ID), 0); set => SetValue(nameof(Param_PC_ID), value); }
        public static bool Param_LoginRequired { get => GetBool(nameof(Param_LoginRequired), false); set => SetValue(nameof(Param_LoginRequired), value); }
        public static bool Param_ZakladActive { get => GetBool(nameof(Param_ZakladActive), true); set => SetValue(nameof(Param_ZakladActive), value); }
        public static bool Param_ActPageOpen { get => GetBool(nameof(Param_ActPageOpen), false); set => SetValue(nameof(Param_ActPageOpen), value); }
        public static int Param_ScaleIndex { get => GetInt(nameof(Param_ScaleIndex), 0); set => SetValue(nameof(Param_ScaleIndex), value); }
        public static bool Param_AutoStartWindows { get => GetBool(nameof(Param_AutoStartWindows), false); set => SetValue(nameof(Param_AutoStartWindows), value); }

        // ==========================================
        // Provozní parametry
        // ==========================================
        public static bool Param_AutoRecipeStart { get => GetBool(nameof(Param_AutoRecipeStart), false); set => SetValue(nameof(Param_AutoRecipeStart), value); }
        public static bool Param_AutoTare { get => GetBool(nameof(Param_AutoTare), false); set => SetValue(nameof(Param_AutoTare), value); }
        public static bool Param_VypocetVarky { get => GetBool(nameof(Param_VypocetVarky), false); set => SetValue(nameof(Param_VypocetVarky), value); }
        public static int Param_VypocetVarkyMode { get => GetInt(nameof(Param_VypocetVarkyMode), 0); set => SetValue(nameof(Param_VypocetVarkyMode), value); }
        public static double Param_MaxBatchSize { get => GetDouble(nameof(Param_MaxBatchSize), 0.0); set => SetValue(nameof(Param_MaxBatchSize), value); }
        public static int Param_Units { get => GetInt(nameof(Param_Units), 0); set => SetValue(nameof(Param_Units), value); }
        public static bool Param_ZobrazSkupiny { get => GetBool(nameof(Param_ZobrazSkupiny), false); set => SetValue(nameof(Param_ZobrazSkupiny), value); }
        public static bool Param_PovolitPreskoceni { get => GetBool(nameof(Param_PovolitPreskoceni), false); set => SetValue(nameof(Param_PovolitPreskoceni), value); }

        // ==========================================
        // Váhy - povolení
        // ==========================================
        public static bool Param_ScaleEnabled1 { get => GetBool(nameof(Param_ScaleEnabled1), false); set => SetValue(nameof(Param_ScaleEnabled1), value); }
        public static bool Param_ScaleEnabled2 { get => GetBool(nameof(Param_ScaleEnabled2), false); set => SetValue(nameof(Param_ScaleEnabled2), value); }
        public static bool Param_ScaleEnabled3 { get => GetBool(nameof(Param_ScaleEnabled3), false); set => SetValue(nameof(Param_ScaleEnabled3), value); }
        public static bool Param_ScaleEnabled4 { get => GetBool(nameof(Param_ScaleEnabled4), false); set => SetValue(nameof(Param_ScaleEnabled4), value); }
        public static bool Param_ScaleEnabled5 { get => GetBool(nameof(Param_ScaleEnabled5), false); set => SetValue(nameof(Param_ScaleEnabled5), value); }

        // ==========================================
        // Váhy - názvy
        // ==========================================
        public static string Param_Scale_Name1 { get => GetString(nameof(Param_Scale_Name1), "Váha 1"); set => SetValue(nameof(Param_Scale_Name1), value); }
        public static string Param_Scale_Name2 { get => GetString(nameof(Param_Scale_Name2), "Váha 2"); set => SetValue(nameof(Param_Scale_Name2), value); }
        public static string Param_Scale_Name3 { get => GetString(nameof(Param_Scale_Name3), "Váha 3"); set => SetValue(nameof(Param_Scale_Name3), value); }
        public static string Param_Scale_Name4 { get => GetString(nameof(Param_Scale_Name4), "Váha 4"); set => SetValue(nameof(Param_Scale_Name4), value); }
        public static string Param_Scale_Name5 { get => GetString(nameof(Param_Scale_Name5), "Váha 5"); set => SetValue(nameof(Param_Scale_Name5), value); }

        // ==========================================
        // Váhy - typ komunikace
        // ==========================================
        public static string Param_Comm_Type1 { get => GetString(nameof(Param_Comm_Type1), "COM"); set => SetValue(nameof(Param_Comm_Type1), value); }
        public static string Param_Comm_Type2 { get => GetString(nameof(Param_Comm_Type2), "COM"); set => SetValue(nameof(Param_Comm_Type2), value); }
        public static string Param_Comm_Type3 { get => GetString(nameof(Param_Comm_Type3), "COM"); set => SetValue(nameof(Param_Comm_Type3), value); }
        public static string Param_Comm_Type4 { get => GetString(nameof(Param_Comm_Type4), "COM"); set => SetValue(nameof(Param_Comm_Type4), value); }
        public static string Param_Comm_Type5 { get => GetString(nameof(Param_Comm_Type5), "COM"); set => SetValue(nameof(Param_Comm_Type5), value); }

        // ==========================================
        // Váhy - sériová komunikace
        // ==========================================
        public static string Param_Serial_Port1 { get => GetString(nameof(Param_Serial_Port1), "COM3"); set => SetValue(nameof(Param_Serial_Port1), value); }
        public static string Param_Serial_Port2 { get => GetString(nameof(Param_Serial_Port2), "COM4"); set => SetValue(nameof(Param_Serial_Port2), value); }
        public static string Param_Serial_Port3 { get => GetString(nameof(Param_Serial_Port3), "COM5"); set => SetValue(nameof(Param_Serial_Port3), value); }
        public static string Param_Serial_Port4 { get => GetString(nameof(Param_Serial_Port4), "COM6"); set => SetValue(nameof(Param_Serial_Port4), value); }
        public static string Param_Serial_Port5 { get => GetString(nameof(Param_Serial_Port5), "COM7"); set => SetValue(nameof(Param_Serial_Port5), value); }

        public static int Param_Serial_Baud1 { get => GetInt(nameof(Param_Serial_Baud1), 9600); set => SetValue(nameof(Param_Serial_Baud1), value); }
        public static int Param_Serial_Baud2 { get => GetInt(nameof(Param_Serial_Baud2), 9600); set => SetValue(nameof(Param_Serial_Baud2), value); }
        public static int Param_Serial_Baud3 { get => GetInt(nameof(Param_Serial_Baud3), 9600); set => SetValue(nameof(Param_Serial_Baud3), value); }
        public static int Param_Serial_Baud4 { get => GetInt(nameof(Param_Serial_Baud4), 9600); set => SetValue(nameof(Param_Serial_Baud4), value); }
        public static int Param_Serial_Baud5 { get => GetInt(nameof(Param_Serial_Baud5), 9600); set => SetValue(nameof(Param_Serial_Baud5), value); }

        public static string Param_Serial_Parity1 { get => GetString(nameof(Param_Serial_Parity1), "None"); set => SetValue(nameof(Param_Serial_Parity1), value); }
        public static string Param_Serial_Parity2 { get => GetString(nameof(Param_Serial_Parity2), "None"); set => SetValue(nameof(Param_Serial_Parity2), value); }
        public static string Param_Serial_Parity3 { get => GetString(nameof(Param_Serial_Parity3), "None"); set => SetValue(nameof(Param_Serial_Parity3), value); }
        public static string Param_Serial_Parity4 { get => GetString(nameof(Param_Serial_Parity4), "None"); set => SetValue(nameof(Param_Serial_Parity4), value); }
        public static string Param_Serial_Parity5 { get => GetString(nameof(Param_Serial_Parity5), "None"); set => SetValue(nameof(Param_Serial_Parity5), value); }

        public static int Param_Serial_DataBits1 { get => GetInt(nameof(Param_Serial_DataBits1), 8); set => SetValue(nameof(Param_Serial_DataBits1), value); }
        public static int Param_Serial_DataBits2 { get => GetInt(nameof(Param_Serial_DataBits2), 8); set => SetValue(nameof(Param_Serial_DataBits2), value); }
        public static int Param_Serial_DataBits3 { get => GetInt(nameof(Param_Serial_DataBits3), 8); set => SetValue(nameof(Param_Serial_DataBits3), value); }
        public static int Param_Serial_DataBits4 { get => GetInt(nameof(Param_Serial_DataBits4), 8); set => SetValue(nameof(Param_Serial_DataBits4), value); }
        public static int Param_Serial_DataBits5 { get => GetInt(nameof(Param_Serial_DataBits5), 8); set => SetValue(nameof(Param_Serial_DataBits5), value); }

        public static string Param_Serial_StopBits1 { get => GetString(nameof(Param_Serial_StopBits1), "One"); set => SetValue(nameof(Param_Serial_StopBits1), value); }
        public static string Param_Serial_StopBits2 { get => GetString(nameof(Param_Serial_StopBits2), "One"); set => SetValue(nameof(Param_Serial_StopBits2), value); }
        public static string Param_Serial_StopBits3 { get => GetString(nameof(Param_Serial_StopBits3), "One"); set => SetValue(nameof(Param_Serial_StopBits3), value); }
        public static string Param_Serial_StopBits4 { get => GetString(nameof(Param_Serial_StopBits4), "One"); set => SetValue(nameof(Param_Serial_StopBits4), value); }
        public static string Param_Serial_StopBits5 { get => GetString(nameof(Param_Serial_StopBits5), "One"); set => SetValue(nameof(Param_Serial_StopBits5), value); }

        public static string Param_Serial_Handshake1 { get => GetString(nameof(Param_Serial_Handshake1), "None"); set => SetValue(nameof(Param_Serial_Handshake1), value); }
        public static string Param_Serial_Handshake2 { get => GetString(nameof(Param_Serial_Handshake2), "None"); set => SetValue(nameof(Param_Serial_Handshake2), value); }
        public static string Param_Serial_Handshake3 { get => GetString(nameof(Param_Serial_Handshake3), "None"); set => SetValue(nameof(Param_Serial_Handshake3), value); }
        public static string Param_Serial_Handshake4 { get => GetString(nameof(Param_Serial_Handshake4), "None"); set => SetValue(nameof(Param_Serial_Handshake4), value); }
        public static string Param_Serial_Handshake5 { get => GetString(nameof(Param_Serial_Handshake5), "None"); set => SetValue(nameof(Param_Serial_Handshake5), value); }

        public static string Param_Serial_Host1 { get => GetString(nameof(Param_Serial_Host1), ""); set => SetValue(nameof(Param_Serial_Host1), value); }
        public static string Param_Serial_Host2 { get => GetString(nameof(Param_Serial_Host2), ""); set => SetValue(nameof(Param_Serial_Host2), value); }
        public static string Param_Serial_Host3 { get => GetString(nameof(Param_Serial_Host3), ""); set => SetValue(nameof(Param_Serial_Host3), value); }
        public static string Param_Serial_Host4 { get => GetString(nameof(Param_Serial_Host4), ""); set => SetValue(nameof(Param_Serial_Host4), value); }
        public static string Param_Serial_Host5 { get => GetString(nameof(Param_Serial_Host5), ""); set => SetValue(nameof(Param_Serial_Host5), value); }

        public static int Param_Serial_TcpPort1 { get => GetInt(nameof(Param_Serial_TcpPort1), 0); set => SetValue(nameof(Param_Serial_TcpPort1), value); }
        public static int Param_Serial_TcpPort2 { get => GetInt(nameof(Param_Serial_TcpPort2), 0); set => SetValue(nameof(Param_Serial_TcpPort2), value); }
        public static int Param_Serial_TcpPort3 { get => GetInt(nameof(Param_Serial_TcpPort3), 0); set => SetValue(nameof(Param_Serial_TcpPort3), value); }
        public static int Param_Serial_TcpPort4 { get => GetInt(nameof(Param_Serial_TcpPort4), 0); set => SetValue(nameof(Param_Serial_TcpPort4), value); }
        public static int Param_Serial_TcpPort5 { get => GetInt(nameof(Param_Serial_TcpPort5), 0); set => SetValue(nameof(Param_Serial_TcpPort5), value); }

        // ==========================================
        // Váhy - čtení
        // ==========================================
        public static string Param_Scale_Address1 { get => GetString(nameof(Param_Scale_Address1), "01"); set => SetValue(nameof(Param_Scale_Address1), value); }
        public static string Param_Scale_Address2 { get => GetString(nameof(Param_Scale_Address2), "02"); set => SetValue(nameof(Param_Scale_Address2), value); }
        public static string Param_Scale_Address3 { get => GetString(nameof(Param_Scale_Address3), "03"); set => SetValue(nameof(Param_Scale_Address3), value); }
        public static string Param_Scale_Address4 { get => GetString(nameof(Param_Scale_Address4), "04"); set => SetValue(nameof(Param_Scale_Address4), value); }
        public static string Param_Scale_Address5 { get => GetString(nameof(Param_Scale_Address5), "05"); set => SetValue(nameof(Param_Scale_Address5), value); }

        public static string Param_Scale_ReadCommand1 { get => GetString(nameof(Param_Scale_ReadCommand1), "{addr}READ"); set => SetValue(nameof(Param_Scale_ReadCommand1), value); }
        public static string Param_Scale_ReadCommand2 { get => GetString(nameof(Param_Scale_ReadCommand2), "{addr}READ"); set => SetValue(nameof(Param_Scale_ReadCommand2), value); }
        public static string Param_Scale_ReadCommand3 { get => GetString(nameof(Param_Scale_ReadCommand3), "{addr}READ"); set => SetValue(nameof(Param_Scale_ReadCommand3), value); }
        public static string Param_Scale_ReadCommand4 { get => GetString(nameof(Param_Scale_ReadCommand4), "{addr}READ"); set => SetValue(nameof(Param_Scale_ReadCommand4), value); }
        public static string Param_Scale_ReadCommand5 { get => GetString(nameof(Param_Scale_ReadCommand5), "{addr}READ"); set => SetValue(nameof(Param_Scale_ReadCommand5), value); }

        public static string Param_Scale_Newline1 { get => GetString(nameof(Param_Scale_Newline1), "\\r\\n"); set => SetValue(nameof(Param_Scale_Newline1), value); }
        public static string Param_Scale_Newline2 { get => GetString(nameof(Param_Scale_Newline2), "\\r\\n"); set => SetValue(nameof(Param_Scale_Newline2), value); }
        public static string Param_Scale_Newline3 { get => GetString(nameof(Param_Scale_Newline3), "\\r\\n"); set => SetValue(nameof(Param_Scale_Newline3), value); }
        public static string Param_Scale_Newline4 { get => GetString(nameof(Param_Scale_Newline4), "\\r\\n"); set => SetValue(nameof(Param_Scale_Newline4), value); }
        public static string Param_Scale_Newline5 { get => GetString(nameof(Param_Scale_Newline5), "\\r\\n"); set => SetValue(nameof(Param_Scale_Newline5), value); }

        public static bool Param_Scale_Async1 { get => GetBool(nameof(Param_Scale_Async1), false); set => SetValue(nameof(Param_Scale_Async1), value); }
        public static bool Param_Scale_Async2 { get => GetBool(nameof(Param_Scale_Async2), false); set => SetValue(nameof(Param_Scale_Async2), value); }
        public static bool Param_Scale_Async3 { get => GetBool(nameof(Param_Scale_Async3), false); set => SetValue(nameof(Param_Scale_Async3), value); }
        public static bool Param_Scale_Async4 { get => GetBool(nameof(Param_Scale_Async4), false); set => SetValue(nameof(Param_Scale_Async4), value); }
        public static bool Param_Scale_Async5 { get => GetBool(nameof(Param_Scale_Async5), false); set => SetValue(nameof(Param_Scale_Async5), value); }

        public static int Param_Scale_PollMs1 { get => GetInt(nameof(Param_Scale_PollMs1), 300); set => SetValue(nameof(Param_Scale_PollMs1), value); }
        public static int Param_Scale_PollMs2 { get => GetInt(nameof(Param_Scale_PollMs2), 300); set => SetValue(nameof(Param_Scale_PollMs2), value); }
        public static int Param_Scale_PollMs3 { get => GetInt(nameof(Param_Scale_PollMs3), 300); set => SetValue(nameof(Param_Scale_PollMs3), value); }
        public static int Param_Scale_PollMs4 { get => GetInt(nameof(Param_Scale_PollMs4), 300); set => SetValue(nameof(Param_Scale_PollMs4), value); }
        public static int Param_Scale_PollMs5 { get => GetInt(nameof(Param_Scale_PollMs5), 300); set => SetValue(nameof(Param_Scale_PollMs5), value); }

        public static int Param_Scale_TimeoutMs1 { get => GetInt(nameof(Param_Scale_TimeoutMs1), 2000); set => SetValue(nameof(Param_Scale_TimeoutMs1), value); }
        public static int Param_Scale_TimeoutMs2 { get => GetInt(nameof(Param_Scale_TimeoutMs2), 2000); set => SetValue(nameof(Param_Scale_TimeoutMs2), value); }
        public static int Param_Scale_TimeoutMs3 { get => GetInt(nameof(Param_Scale_TimeoutMs3), 2000); set => SetValue(nameof(Param_Scale_TimeoutMs3), value); }
        public static int Param_Scale_TimeoutMs4 { get => GetInt(nameof(Param_Scale_TimeoutMs4), 2000); set => SetValue(nameof(Param_Scale_TimeoutMs4), value); }
        public static int Param_Scale_TimeoutMs5 { get => GetInt(nameof(Param_Scale_TimeoutMs5), 2000); set => SetValue(nameof(Param_Scale_TimeoutMs5), value); }

        // ==========================================
        // Váhy - parsování
        // ==========================================
        public static double Param_Scale_DisplayDivisor1 { get => GetDouble(nameof(Param_Scale_DisplayDivisor1), 1.0); set => SetValue(nameof(Param_Scale_DisplayDivisor1), value); }
        public static double Param_Scale_DisplayDivisor2 { get => GetDouble(nameof(Param_Scale_DisplayDivisor2), 1.0); set => SetValue(nameof(Param_Scale_DisplayDivisor2), value); }
        public static double Param_Scale_DisplayDivisor3 { get => GetDouble(nameof(Param_Scale_DisplayDivisor3), 1.0); set => SetValue(nameof(Param_Scale_DisplayDivisor3), value); }
        public static double Param_Scale_DisplayDivisor4 { get => GetDouble(nameof(Param_Scale_DisplayDivisor4), 1.0); set => SetValue(nameof(Param_Scale_DisplayDivisor4), value); }
        public static double Param_Scale_DisplayDivisor5 { get => GetDouble(nameof(Param_Scale_DisplayDivisor5), 1.0); set => SetValue(nameof(Param_Scale_DisplayDivisor5), value); }

        public static string Param_Scale_UnitsFallback1 { get => GetString(nameof(Param_Scale_UnitsFallback1), "Kg"); set => SetValue(nameof(Param_Scale_UnitsFallback1), value); }
        public static string Param_Scale_UnitsFallback2 { get => GetString(nameof(Param_Scale_UnitsFallback2), "Kg"); set => SetValue(nameof(Param_Scale_UnitsFallback2), value); }
        public static string Param_Scale_UnitsFallback3 { get => GetString(nameof(Param_Scale_UnitsFallback3), "Kg"); set => SetValue(nameof(Param_Scale_UnitsFallback3), value); }
        public static string Param_Scale_UnitsFallback4 { get => GetString(nameof(Param_Scale_UnitsFallback4), "Kg"); set => SetValue(nameof(Param_Scale_UnitsFallback4), value); }
        public static string Param_Scale_UnitsFallback5 { get => GetString(nameof(Param_Scale_UnitsFallback5), "Kg"); set => SetValue(nameof(Param_Scale_UnitsFallback5), value); }

        public static string Param_Scale_TokenDefs1 { get => GetString(nameof(Param_Scale_TokenDefs1), VychoziTokenDefs); set => SetValue(nameof(Param_Scale_TokenDefs1), value); }
        public static string Param_Scale_TokenDefs2 { get => GetString(nameof(Param_Scale_TokenDefs2), VychoziTokenDefs); set => SetValue(nameof(Param_Scale_TokenDefs2), value); }
        public static string Param_Scale_TokenDefs3 { get => GetString(nameof(Param_Scale_TokenDefs3), VychoziTokenDefs); set => SetValue(nameof(Param_Scale_TokenDefs3), value); }
        public static string Param_Scale_TokenDefs4 { get => GetString(nameof(Param_Scale_TokenDefs4), VychoziTokenDefs); set => SetValue(nameof(Param_Scale_TokenDefs4), value); }
        public static string Param_Scale_TokenDefs5 { get => GetString(nameof(Param_Scale_TokenDefs5), VychoziTokenDefs); set => SetValue(nameof(Param_Scale_TokenDefs5), value); }

        public static string Param_Scale_FieldMap1 { get => GetString(nameof(Param_Scale_FieldMap1), VychoziFieldMap); set => SetValue(nameof(Param_Scale_FieldMap1), value); }
        public static string Param_Scale_FieldMap2 { get => GetString(nameof(Param_Scale_FieldMap2), VychoziFieldMap); set => SetValue(nameof(Param_Scale_FieldMap2), value); }
        public static string Param_Scale_FieldMap3 { get => GetString(nameof(Param_Scale_FieldMap3), VychoziFieldMap); set => SetValue(nameof(Param_Scale_FieldMap3), value); }
        public static string Param_Scale_FieldMap4 { get => GetString(nameof(Param_Scale_FieldMap4), VychoziFieldMap); set => SetValue(nameof(Param_Scale_FieldMap4), value); }
        public static string Param_Scale_FieldMap5 { get => GetString(nameof(Param_Scale_FieldMap5), VychoziFieldMap); set => SetValue(nameof(Param_Scale_FieldMap5), value); }

        public static string Param_Scale_Format1 { get => GetString(nameof(Param_Scale_Format1), "{addr?}{status},GS,{weight},{units?}"); set => SetValue(nameof(Param_Scale_Format1), value); }
        public static string Param_Scale_Format2 { get => GetString(nameof(Param_Scale_Format2), "{addr?}{status},GS,{weight},{units?}"); set => SetValue(nameof(Param_Scale_Format2), value); }
        public static string Param_Scale_Format3 { get => GetString(nameof(Param_Scale_Format3), "{addr?}{status},GS,{weight},{units?}"); set => SetValue(nameof(Param_Scale_Format3), value); }
        public static string Param_Scale_Format4 { get => GetString(nameof(Param_Scale_Format4), "{addr?}{status},GS,{weight},{units?}"); set => SetValue(nameof(Param_Scale_Format4), value); }
        public static string Param_Scale_Format5 { get => GetString(nameof(Param_Scale_Format5), "{addr?}{status},GS,{weight},{units?}"); set => SetValue(nameof(Param_Scale_Format5), value); }

        // ==========================================
        // Váhy - alibi
        // ==========================================
        public static bool Param_Scale_EnableAlibi1 { get => GetBool(nameof(Param_Scale_EnableAlibi1), false); set => SetValue(nameof(Param_Scale_EnableAlibi1), value); }
        public static bool Param_Scale_EnableAlibi2 { get => GetBool(nameof(Param_Scale_EnableAlibi2), false); set => SetValue(nameof(Param_Scale_EnableAlibi2), value); }
        public static bool Param_Scale_EnableAlibi3 { get => GetBool(nameof(Param_Scale_EnableAlibi3), false); set => SetValue(nameof(Param_Scale_EnableAlibi3), value); }
        public static bool Param_Scale_EnableAlibi4 { get => GetBool(nameof(Param_Scale_EnableAlibi4), false); set => SetValue(nameof(Param_Scale_EnableAlibi4), value); }
        public static bool Param_Scale_EnableAlibi5 { get => GetBool(nameof(Param_Scale_EnableAlibi5), false); set => SetValue(nameof(Param_Scale_EnableAlibi5), value); }

        public static string Param_Scale_AlibiStoreCommand1 { get => GetString(nameof(Param_Scale_AlibiStoreCommand1), "{addr}PID"); set => SetValue(nameof(Param_Scale_AlibiStoreCommand1), value); }
        public static string Param_Scale_AlibiStoreCommand2 { get => GetString(nameof(Param_Scale_AlibiStoreCommand2), "{addr}PID"); set => SetValue(nameof(Param_Scale_AlibiStoreCommand2), value); }
        public static string Param_Scale_AlibiStoreCommand3 { get => GetString(nameof(Param_Scale_AlibiStoreCommand3), "{addr}PID"); set => SetValue(nameof(Param_Scale_AlibiStoreCommand3), value); }
        public static string Param_Scale_AlibiStoreCommand4 { get => GetString(nameof(Param_Scale_AlibiStoreCommand4), "{addr}PID"); set => SetValue(nameof(Param_Scale_AlibiStoreCommand4), value); }
        public static string Param_Scale_AlibiStoreCommand5 { get => GetString(nameof(Param_Scale_AlibiStoreCommand5), "{addr}PID"); set => SetValue(nameof(Param_Scale_AlibiStoreCommand5), value); }

        public static string Param_Scale_AlibiSuccessFormat1 { get => GetString(nameof(Param_Scale_AlibiSuccessFormat1), "{addr?}ALRD{alibi}"); set => SetValue(nameof(Param_Scale_AlibiSuccessFormat1), value); }
        public static string Param_Scale_AlibiSuccessFormat2 { get => GetString(nameof(Param_Scale_AlibiSuccessFormat2), "{addr?}ALRD{alibi}"); set => SetValue(nameof(Param_Scale_AlibiSuccessFormat2), value); }
        public static string Param_Scale_AlibiSuccessFormat3 { get => GetString(nameof(Param_Scale_AlibiSuccessFormat3), "{addr?}ALRD{alibi}"); set => SetValue(nameof(Param_Scale_AlibiSuccessFormat3), value); }
        public static string Param_Scale_AlibiSuccessFormat4 { get => GetString(nameof(Param_Scale_AlibiSuccessFormat4), "{addr?}ALRD{alibi}"); set => SetValue(nameof(Param_Scale_AlibiSuccessFormat4), value); }
        public static string Param_Scale_AlibiSuccessFormat5 { get => GetString(nameof(Param_Scale_AlibiSuccessFormat5), "{addr?}ALRD{alibi}"); set => SetValue(nameof(Param_Scale_AlibiSuccessFormat5), value); }

        public static string Param_Scale_AlibiEchoFormat1 { get => GetString(nameof(Param_Scale_AlibiEchoFormat1), "{addr?}PID{status},{{[^,]*}},{weight}{units?},{{[^,]*}},{alibi}"); set => SetValue(nameof(Param_Scale_AlibiEchoFormat1), value); }
        public static string Param_Scale_AlibiEchoFormat2 { get => GetString(nameof(Param_Scale_AlibiEchoFormat2), "{addr?}PID{status},{{[^,]*}},{weight}{units?},{{[^,]*}},{alibi}"); set => SetValue(nameof(Param_Scale_AlibiEchoFormat2), value); }
        public static string Param_Scale_AlibiEchoFormat3 { get => GetString(nameof(Param_Scale_AlibiEchoFormat3), "{addr?}PID{status},{{[^,]*}},{weight}{units?},{{[^,]*}},{alibi}"); set => SetValue(nameof(Param_Scale_AlibiEchoFormat3), value); }
        public static string Param_Scale_AlibiEchoFormat4 { get => GetString(nameof(Param_Scale_AlibiEchoFormat4), "{addr?}PID{status},{{[^,]*}},{weight}{units?},{{[^,]*}},{alibi}"); set => SetValue(nameof(Param_Scale_AlibiEchoFormat4), value); }
        public static string Param_Scale_AlibiEchoFormat5 { get => GetString(nameof(Param_Scale_AlibiEchoFormat5), "{addr?}PID{status},{{[^,]*}},{weight}{units?},{{[^,]*}},{alibi}"); set => SetValue(nameof(Param_Scale_AlibiEchoFormat5), value); }

        // ==========================================
        // Váhy - statusy
        // ==========================================
        public static string Param_Scale_StatusMap1 { get => GetString(nameof(Param_Scale_StatusMap1), VychoziStatusMap); set => SetValue(nameof(Param_Scale_StatusMap1), value); }
        public static string Param_Scale_StatusMap2 { get => GetString(nameof(Param_Scale_StatusMap2), VychoziStatusMap); set => SetValue(nameof(Param_Scale_StatusMap2), value); }
        public static string Param_Scale_StatusMap3 { get => GetString(nameof(Param_Scale_StatusMap3), VychoziStatusMap); set => SetValue(nameof(Param_Scale_StatusMap3), value); }
        public static string Param_Scale_StatusMap4 { get => GetString(nameof(Param_Scale_StatusMap4), VychoziStatusMap); set => SetValue(nameof(Param_Scale_StatusMap4), value); }
        public static string Param_Scale_StatusMap5 { get => GetString(nameof(Param_Scale_StatusMap5), VychoziStatusMap); set => SetValue(nameof(Param_Scale_StatusMap5), value); }

        public static string Param_Scale_StatusTextMap1 { get => GetString(nameof(Param_Scale_StatusTextMap1), VychoziStatusTextMap); set => SetValue(nameof(Param_Scale_StatusTextMap1), value); }
        public static string Param_Scale_StatusTextMap2 { get => GetString(nameof(Param_Scale_StatusTextMap2), VychoziStatusTextMap); set => SetValue(nameof(Param_Scale_StatusTextMap2), value); }
        public static string Param_Scale_StatusTextMap3 { get => GetString(nameof(Param_Scale_StatusTextMap3), VychoziStatusTextMap); set => SetValue(nameof(Param_Scale_StatusTextMap3), value); }
        public static string Param_Scale_StatusTextMap4 { get => GetString(nameof(Param_Scale_StatusTextMap4), VychoziStatusTextMap); set => SetValue(nameof(Param_Scale_StatusTextMap4), value); }
        public static string Param_Scale_StatusTextMap5 { get => GetString(nameof(Param_Scale_StatusTextMap5), VychoziStatusTextMap); set => SetValue(nameof(Param_Scale_StatusTextMap5), value); }

        // ==========================================
        // Sdílené výchozí texty pro parsování váhy
        // ==========================================
        private const string VychoziTokenDefs = @"addr   = \d{2}
            status = [A-Za-z]{2}
            weight = \s*-?\d+(?:[,.]\d+)?
            units  = [A-Za-z]{0,3}
            alibi  = \d{5}-?\d{6,}";

        private const string VychoziFieldMap = @"addr   = addr
            status = status
            weight = weight
            units  = units
            alibi  = alibi";

        private const string VychoziStatusMap = @"TL=TL
            OL=OL
            UL=UL
            ST=ST
            US=US";

        private const string VychoziStatusTextMap = @"TL=Chyba váhy
            OL=Condition of overload
            UL=Condition of underload
            ST=Váha stabilní
            US=Váha nestabilní";
    }
}
