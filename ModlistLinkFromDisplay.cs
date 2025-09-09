using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TruckModImporter
{
    public static class ModlistLinkFromDisplay
    {
        // Öffentlicher Einzeiler: aus Anzeige-Text die .link.json erzeugen
        public static void Write(string modlistsRoot, string gameDisplayText, string modlistDisplayText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modlistsRoot)) return;
                if (string.IsNullOrWhiteSpace(gameDisplayText)) return;
                if (string.IsNullOrWhiteSpace(modlistDisplayText)) return;

                // Spiel normalisieren
                string game = NormalizeGame(gameDisplayText); // "ETS2" oder "ATS"
                string gameDir = Path.Combine(Path.GetFullPath(modlistsRoot), game);
                Directory.CreateDirectory(gameDir);

                // 1) Versuche, eine passende Datei zu finden, deren Basisname == Anzeigename ist
                string baseName = ResolveBaseNameFromFolder(gameDir, modlistDisplayText);

                // 2) links.json-Pfad festlegen
                string linksJsonPath = Path.Combine(gameDir, "links.json");

                // 3) Schreiben (still, ohne Popups)
                SafeLinkJson.WriteForModlist(linksJsonPath, baseName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ModlistLinkFromDisplay] " + ex);
            }
        }

        private static string NormalizeGame(string gameDisplay)
        {
            var g = (gameDisplay ?? "").Trim();
            if (g.Equals("ATS", StringComparison.OrdinalIgnoreCase)
             || g.IndexOf("American Truck", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ATS";
            // Default: ETS2
            return "ETS2";
        }

        private static string ResolveBaseNameFromFolder(string gameDir, string displayName)
        {
            try
            {
                // Alle Dateien im Spiel-Ordner durchsuchen und auf Basisnamen matchen
                var files = Directory.EnumerateFiles(gameDir, "*", SearchOption.TopDirectoryOnly);
                string? match = files.FirstOrDefault(f =>
                    string.Equals(Path.GetFileNameWithoutExtension(f), displayName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(match))
                    return Path.GetFileNameWithoutExtension(match)!;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ModlistLinkFromDisplay:ResolveBaseName] " + ex);
            }

            // Fallback: den Anzeigenamen selbst verwenden
            return displayName.Trim();
        }
    }
}