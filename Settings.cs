// Settings.cs
using System;
using System.IO;
using System.Text.Json;

namespace TruckModImporter
{
    public sealed class AppSettings
    {
        public bool DarkMode { get; set; } = false;
        public string Language { get; set; } = "de"; // "de" oder "en"
        public string Ets2ProfilesPath { get; set; } = ""; // optional leer = Standard
        public string AtsProfilesPath { get; set; } = "";  // optional leer = Standard
    }

    public static class SettingsService
    {
        private const string FileName = "settings.json";

        private static string GetSettingsPath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TruckModImporter");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, FileName);
        }

        public static AppSettings Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (!File.Exists(path)) return new AppSettings();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var path = GetSettingsPath();
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { /* ignore */ }
        }
    }
}
