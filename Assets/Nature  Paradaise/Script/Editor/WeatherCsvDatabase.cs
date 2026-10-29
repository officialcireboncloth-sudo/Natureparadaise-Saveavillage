using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Independent transactional weather CSV import; unrelated cafe edits cannot block a weather correction.</summary>
public static class WeatherCsvDatabase
{
    public const string Root = ScheduleCafeCsvDatabase.Root;
    public const string ProfilePath = "Assets/Nature  Paradaise/Resources/Profiles/Season Weather Probabilities.asset";
    public static bool SuppressAutoImport;
    public static string LastReport { get; private set; }
    static readonly string[] DateHeaders = { "year", "month", "season", "day", "weather", "note" };
    static readonly string[] Labels = { "Cerah", "Cerah Berawan", "Panas", "Gerimis", "Hujan", "Hujan Lebat", "Badai Hujan dan Angin", "Topan", "Badai Petir", "Hujan Salju", "Badai Salju", "Mendung" };
    static readonly string[] ProbabilityHeaders = new[] { "month", "season", "days" }.Concat(Labels).ToArray();
    static readonly CropSeason[] Seasons = { CropSeason.Spring, CropSeason.Summer, CropSeason.Autumn, CropSeason.Winter };
    static string S(int n) => n.ToString(CultureInfo.InvariantCulture);
    static string Key(string s) => new string((s ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    static int Number(Dictionary<string, string> row, string key, int min, int max)
    {
        if (!row.TryGetValue(key, out string value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < min || n > max)
            throw new InvalidDataException($"{key} harus {min}–{max}.");
        return n;
    }
    static List<Dictionary<string, string>> Read(string name, params string[] required)
    {
        var rows = BalanceCsvDatabase.Csv.Read(Root + "/" + name + ".csv", out var headers, out string error);
        if (error != null) throw new InvalidDataException(name + ": " + error);
        if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length || required.Any(c => !headers.Contains(c, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidDataException(name + ": header hilang / duplikat.");
        return rows;
    }
    public static void CheckSeason(Dictionary<string, string> row, int month)
    {
        if (!row.TryGetValue("season", out string text) || string.IsNullOrWhiteSpace(text)) return;
        string key = Key(text);
        if (key != Key(GameScheduleData.SeasonEnglishName(month - 1)) && key != Key(GameScheduleData.SeasonName(month - 1)) && !(month == 3 && key == "fall"))
            throw new InvalidDataException($"month {month} adalah {GameScheduleData.SeasonEnglishName(month - 1)}, bukan '{text}'.");
    }
    public static bool TryWeather(string text, out WeatherType weather, out bool auto)
    {
        string key = Key(text); auto = key == "" || key == "auto" || key == "otomatis"; weather = WeatherType.Sunny;
        if (auto) return true;
        foreach (WeatherType type in Enum.GetValues(typeof(WeatherType)))
            if (key == Key(type.ToString()) || key == Key(Labels[(int)type])) { weather = type; return true; }
        if (key is "sun" or "sunn") { weather = WeatherType.Sunny; return true; }
        if (key == "drizzl") { weather = WeatherType.Drizzle; return true; }
        if (key == "badai") { weather = WeatherType.WindRainStorm; return true; }
        return false;
    }
    /// <summary>Reads both the legacy absolute-day CSV and the named four-month date format.</summary>
    public static List<WeatherDateRecord> ReadSchedule()
    {
        var rows = Read("WeatherSchedule", "weather"); var result = new List<WeatherDateRecord>(); var seen = new HashSet<int>(); int line = 1;
        foreach (var row in rows)
        {
            line++;
            try
            {
                int absolute;
                if (row.ContainsKey("month"))
                {
                    int year = Number(row, "year", 1, GameScheduleData.AuthoredYears), month = Number(row, "month", 1, 4);
                    CheckSeason(row, month);
                    absolute = GameScheduleData.AbsoluteDay(year, month - 1, Number(row, "day", 1, GameScheduleData.DaysInSeason(month - 1)));
                }
                else absolute = Number(row, "absolute_day", 1, GameScheduleData.AuthoredDays);
                if (!seen.Add(absolute)) throw new InvalidDataException("Tanggal duplikat.");
                if (!TryWeather(row["weather"], out var weather, out bool auto)) throw new InvalidDataException("Cuaca tidak dikenal: '" + row["weather"] + "'. Gunakan Auto, Cerah, Gerimis, Hujan, atau nama cuaca yang valid.");
                result.Add(new WeatherDateRecord { absoluteDay = absolute, useOverride = !auto, weather = weather, note = row.TryGetValue("note", out string note) ? note : "" });
            }
            catch (Exception e) { throw new InvalidDataException($"WeatherSchedule.csv baris {line}: {e.Message}"); }
        }
        if (result.Count != GameScheduleData.AuthoredDays) throw new InvalidDataException($"WeatherSchedule memerlukan {GameScheduleData.AuthoredDays} tanggal (tahun 1–10), dengan Auto untuk cuaca normal.");
        return result.OrderBy(r => r.absoluteDay).ToList();
    }
    public static List<SeasonWeatherDistribution> ReadProbabilities()
    {
        // Existing projects without the new CSV retain their authored resource until an explicit export.
        if (!File.Exists(Root + "/WeatherProbabilities.csv")) return null;
        var rows = Read("WeatherProbabilities", ProbabilityHeaders); var result = new List<SeasonWeatherDistribution>(); var seen = new HashSet<int>();
        foreach (var row in rows)
        {
            int month = Number(row, "month", 1, 4); CheckSeason(row, month);
            if (!seen.Add(month)) throw new InvalidDataException("WeatherProbabilities: month duplikat " + month);
            if (Number(row, "days", 1, 31) != GameScheduleData.DaysInSeason(month - 1)) throw new InvalidDataException("WeatherProbabilities: jumlah hari harus mengikuti 31/28/31/30.");
            var distribution = new SeasonWeatherDistribution { season = Seasons[month - 1] }; int total = 0;
            var values = Enumerable.Range(0, Labels.Length).ToDictionary(i => (WeatherType)i, i => Number(row, Labels[i], 0, 100));
            var profile = AssetDatabase.LoadAssetAtPath<SeasonWeatherProfile>(ProfilePath);
            var oldOrder = profile?.seasons.FirstOrDefault(s => s.season == distribution.season)?.probabilities.Select(p => p.weather) ?? Enumerable.Empty<WeatherType>();
            foreach (var type in oldOrder.Concat(values.Keys).Distinct()) { int percent = values[type]; total += percent; if (percent > 0) distribution.probabilities.Add(new(type, percent)); }
            if (total != 100) throw new InvalidDataException($"WeatherProbabilities: {GameScheduleData.SeasonEnglishName(month - 1)} total {total}%; wajib 100%.");
            result.Add(distribution);
        }
        if (result.Count != 4) throw new InvalidDataException("WeatherProbabilities harus memuat tepat 4 bulan.");
        return result.OrderBy(r => Array.IndexOf(Seasons, r.season)).ToList();
    }
    [MenuItem("Nature Paradise/Data CSV/Weather/Validate Weather CSV")]
    public static void Validate()
    {
        try
        {
            ReadSchedule(); ReadProbabilities();
            if (GameScheduleData.Load() == null || AssetDatabase.LoadAssetAtPath<SeasonWeatherProfile>(ProfilePath) == null)
                throw new InvalidDataException("Asset jadwal / profil probabilitas tidak terbaca; periksa referensi script dan meta sebelum impor.");
            LastReport = "VALID: kalender 4 bulan dan probabilitas cuaca."; Debug.Log("[WEATHER CSV] " + LastReport);
        }
        catch (Exception e) { LastReport = e.Message; Debug.LogWarning("[WEATHER CSV] " + LastReport); }
    }
    [MenuItem("Nature Paradise/Data CSV/Weather/Import Weather CSV")]
    public static void Import() => TryImport();
    internal static bool TryImport(bool reportFailure = true)
    {
        try
        {
            var dates = ReadSchedule(); var rules = ReadProbabilities();
            var schedule = AssetDatabase.LoadAssetAtPath<GameScheduleData>(ScheduleCafeCsvDatabase.SchedulePath);
            var profile = AssetDatabase.LoadAssetAtPath<SeasonWeatherProfile>(ProfilePath);
            if (schedule == null || profile == null) throw new InvalidDataException("Asset Game Schedules / Season Weather Probabilities belum tersedia.");
            var candidate = Object.Instantiate(schedule); candidate.weather = dates;
            var nextProfile = Object.Instantiate(profile); if (rules != null) nextProfile.seasons = rules;
            try
            {
                // Instantiate adds '(Clone)' to the name; names are not data changes.
                candidate.name = schedule.name; nextProfile.name = profile.name;
                bool changed = EditorJsonUtility.ToJson(candidate) != EditorJsonUtility.ToJson(schedule) || EditorJsonUtility.ToJson(nextProfile) != EditorJsonUtility.ToJson(profile);
                if (changed)
                {
                    string backup = "BalanceBackups/Weather/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"); Directory.CreateDirectory(backup);
                    foreach (string path in new[] { ScheduleCafeCsvDatabase.SchedulePath, ProfilePath }) File.Copy(path, backup + "/" + Path.GetFileName(path));
                    if (!Application.isPlaying) Undo.RecordObjects(new Object[] { schedule, profile }, "Import Weather CSV");
                    EditorUtility.CopySerialized(candidate, schedule); EditorUtility.CopySerialized(nextProfile, profile);
                    EditorUtility.SetDirty(schedule); EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
                }
                WeatherSystem.Instance?.RefreshImportedWeatherRules();
                LastReport = $"{dates.Count} tanggal, 4 bulan; {dates.Count(r => r.useOverride)} tanggal khusus; Auto mengikuti probabilitas musim.";
                if (WeatherSystem.Instance != null) LastReport += " Sumber aktif: " + WeatherSystem.Instance.ForecastSource;
                Debug.Log("[WEATHER CSV] " + LastReport);
                return true;
            }
            finally { Object.DestroyImmediate(candidate); Object.DestroyImmediate(nextProfile); }
        }
        catch (Exception e)
        {
            LastReport = "Import dibatalkan; aturan sebelumnya tetap dipakai. " + e.Message;
            if (reportFailure) Debug.LogWarning("[WEATHER CSV] " + LastReport);
            return false;
        }
    }
    [MenuItem("Nature Paradise/Data CSV/Weather/Export Weather CSV")]
    public static void Export()
    {
        var schedule = GameScheduleData.Load(); var profile = AssetDatabase.LoadAssetAtPath<SeasonWeatherProfile>(ProfilePath);
        if (schedule == null || profile == null) throw new InvalidOperationException("Asset jadwal cuaca belum tersedia.");
        Directory.CreateDirectory(Root);
        BalanceCsvDatabase.Csv.Write(Root + "/WeatherSchedule.csv", DateHeaders, schedule.weather.OrderBy(r => r.absoluteDay).Select(r =>
        { GameScheduleData.Date(r.absoluteDay, out int year, out int season, out int day); return new[] { S(year), S(season + 1), GameScheduleData.SeasonEnglishName(season), S(day), r.useOverride ? Labels[(int)r.weather] : "Auto", r.note ?? "" }; }));
        BalanceCsvDatabase.Csv.Write(Root + "/WeatherProbabilities.csv", ProbabilityHeaders, Enumerable.Range(0, 4).Select(s =>
        { var rule = profile.seasons.Single(r => r.season == Seasons[s]); return new[] { S(s + 1), GameScheduleData.SeasonEnglishName(s), S(GameScheduleData.DaysInSeason(s)) }.Concat(Enumerable.Range(0, Labels.Length).Select(w => S(rule.probabilities.Where(p => (int)p.weather == w).Sum(p => p.percent)))).ToArray(); }));
        AssetDatabase.Refresh();
    }
}

/// <summary>Imports saved weather edits on editor refresh, with validation before touching authored assets.</summary>
[InitializeOnLoad]
sealed class WeatherCsvAutoImport : AssetPostprocessor
{
    const string PendingKey = "NatureParadise.WeatherCsv.Pending";
    static bool pending; static double requestedAt; static int attempts;
    static WeatherCsvAutoImport()
    {
        EditorApplication.playModeStateChanged += BeforePlay;
        // A script reload can happen between the CSV refresh and the debounced import.
        if (SessionState.GetBool(PendingKey, false)) Queue();
    }
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        if (WeatherCsvDatabase.SuppressAutoImport || !imported.Concat(moved).Any(IsWeatherCsv)) return;
        Queue();
    }
    static void Queue()
    {
        SessionState.SetBool(PendingKey, true); attempts = 0;
        requestedAt = EditorApplication.timeSinceStartup;
        if (pending) return; pending = true; EditorApplication.update += Flush;
    }
    static void Complete()
    {
        pending = false; SessionState.SetBool(PendingKey, false); EditorApplication.update -= Flush;
    }
    static void BeforePlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode || WeatherCsvDatabase.SuppressAutoImport) return;
        // Read disk before scene/forecast startup, even if Auto Refresh is disabled
        // or Play was pressed before the debounce elapsed. Never export over CSV edits.
        if (WeatherCsvDatabase.TryImport()) Complete();
        else Queue();
    }
    static bool IsWeatherCsv(string p) => p == WeatherCsvDatabase.Root + "/WeatherSchedule.csv" || p == WeatherCsvDatabase.Root + "/WeatherProbabilities.csv";
    static void Flush()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup - requestedAt < .75) return;
        if (WeatherCsvDatabase.SuppressAutoImport) { Complete(); return; }
        if (WeatherCsvDatabase.TryImport(false)) { Complete(); return; }
        // WPS/Excel may briefly lock or replace a file during Save. Keep the last
        // valid assets and retry without blocking the editor or flooding Console.
        if (++attempts < 6) { requestedAt = EditorApplication.timeSinceStartup; return; }
        Complete(); Debug.LogWarning("[WEATHER CSV] " + WeatherCsvDatabase.LastReport);
    }
}
