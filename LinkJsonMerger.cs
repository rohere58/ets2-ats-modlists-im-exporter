using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TruckModImporter
{
    public static class LinkJsonMerger
    {
        /// <summary>
        /// Fügt **nur fehlende** Einträge aus <ModlistName>.link.json in links.json ein.
        /// </summary>
        public static void MergeLinkJson(string modlistsRoot, string gameFolderName, string modlistName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modlistsRoot)) return;
                if (string.IsNullOrWhiteSpace(gameFolderName)) return;
                if (string.IsNullOrWhiteSpace(modlistName)) return;

                string game = gameFolderName.Equals("ATS", StringComparison.OrdinalIgnoreCase) ? "ATS" : "ETS2";
                string gameDir = Path.Combine(Path.GetFullPath(modlistsRoot), game);
                Directory.CreateDirectory(gameDir);

                string linksJsonPath = Path.Combine(gameDir, "links.json");
                string linkJsonPath  = Path.Combine(gameDir, $"{SanitizeFileName(modlistName)}.link.json");

                if (!File.Exists(linkJsonPath))
                {
                    Debug.WriteLine($"[LinkJsonMerger] .link.json fehlt → Skip: {linkJsonPath}");
                    return;
                }

                var incoming = LoadMap(linkJsonPath);   // aus .link.json
                var target   = LoadMap(linksJsonPath);  // zentrale links.json (leer wenn nicht vorhanden)

                int before = target.Count, added = 0;
                foreach (var kv in incoming)
                {
                    if (!target.ContainsKey(kv.Key))
                    {
                        target[kv.Key] = kv.Value;
                        added++;
                    }
                }

                WritePrettyTransactional(linksJsonPath, target);
                Debug.WriteLine($"[LinkJsonMerger] Merge OK: +{added} (vorher {before} → jetzt {target.Count}) -> {linksJsonPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LinkJsonMerger ERR] " + ex);
            }
        }

        // ---- Helpers (still, robust) ----
        private static Dictionary<string,string> LoadMap(string path)
        {
            var dict = new Dictionary<string,string>(StringComparer.Ordinal);
            try
            {
                if (!File.Exists(path)) return dict;
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return dict;

                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string,string>>(json);
                    if (map != null)
                    {
                        foreach (var kv in map)
                            if (!string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value) && !dict.ContainsKey(kv.Key))
                                dict[kv.Key] = kv.Value;
                    }
                }
                catch { /* not a dict */ }

                if (dict.Count == 0)
                {
                    try
                    {
                        var arr = JsonSerializer.Deserialize<List<Row>>(json);
                        if (arr != null)
                        {
                            foreach (var r in arr)
                                if (!string.IsNullOrWhiteSpace(r?.Package) && !string.IsNullOrWhiteSpace(r?.Url) && !dict.ContainsKey(r.Package!))
                                    dict[r.Package!] = r.Url!;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Debug.WriteLine("[LinkJsonMerger:Load] " + ex); }
            return dict;
        }

        private static void WritePrettyTransactional(string jsonPath, Dictionary<string,string> map)
        {
            try
            {
                string dir = Path.GetDirectoryName(jsonPath)!;
                Directory.CreateDirectory(dir);

                var sorted = map.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(k => k.Key, v => v.Value, StringComparer.Ordinal);

                string tmp = Path.Combine(dir, $"links.json.tmp-{Guid.NewGuid():N}");
                string jsonOut = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tmp, jsonOut, new UTF8Encoding(false));

                if (File.Exists(jsonPath))
                {
                    string bak = Path.Combine(dir, $"links.json.bak-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    File.Replace(tmp, jsonPath, bak, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tmp, jsonPath);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[LinkJsonMerger:Write] " + ex); }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "modlist" : name;
        }

        private sealed class Row { public string? Package { get; set; } public string? Url { get; set; } }
    }
}