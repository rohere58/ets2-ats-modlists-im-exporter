// MainForm.ProfileFolder.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Öffnet den Ordner des aktuell ausgewählten Profils.
        /// Funktioniert sowohl mit Klartext-Ordnern als auch hex/Steam-Ordnern.
        /// Liest bei Bedarf profile_name aus profile.sii (Auto-Decrypt lokal enthalten).
        /// </summary>
        private void DoOpenSelectedProfileFolder_Local()
        {
            try
            {
                // 1) Direkt aus der ComboBox versuchen (falls dort ein Pfad als Value hinterlegt ist)
                var dir = TryGetSelectedProfileDirFromCombo();
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    OpenFolder_Local(dir!);
                    return;
                }

                // 2) Über profile_name in profile.sii auflösen (inkl. Auto-Decrypt)
                var root = ResolveProfilesRootDir_Fix();
                if (!Directory.Exists(root))
                    throw new DirectoryNotFoundException(
                        (IsEnglishUi_Local() ? "Profile root not found: " : "Profile-Wurzel nicht gefunden: ") + root);

                var display = GetSelectedProfileDisplayText();
                var found = FindProfileFolderByDisplayName_Local(root, display);
                if (!string.IsNullOrWhiteSpace(found) && Directory.Exists(found))
                {
                    OpenFolder_Local(found!);
                    return;
                }

                // 3) Letzter Fallback: <root>\<display>
                var direct = Path.Combine(root, SanitizePathSegment_Local(display));
                if (Directory.Exists(direct))
                {
                    OpenFolder_Local(direct);
                    return;
                }

                MessageBox.Show(this,
                    IsEnglishUi_Local()
                        ? "Could not resolve the selected profile folder."
                        : "Konnte den ausgewählten Profilordner nicht auflösen.",
                    IsEnglishUi_Local() ? "Info" : "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    (IsEnglishUi_Local() ? "Could not open folder:\n" : "Ordner konnte nicht geöffnet werden:\n") + ex.Message,
                    IsEnglishUi_Local() ? "Error" : "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========================
        //   Helfer (robust/neutral)
        // ========================

        /// <summary>Ermittelt das Profile-Root je nach Spiel und Settings. Eigener Name, daher keine Konflikte.</summary>
        private string ResolveProfilesRootDir_Fix()
        {
            var st = SettingsService.Load();
            bool isAts = (cbGame?.SelectedIndex == 1);

            // 1) Benutzerpfad
            if (isAts)
            {
                if (!string.IsNullOrWhiteSpace(st.AtsProfilesPath) && Directory.Exists(st.AtsProfilesPath))
                    return st.AtsProfilesPath;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(st.Ets2ProfilesPath) && Directory.Exists(st.Ets2ProfilesPath))
                    return st.Ets2ProfilesPath;
            }

            // 2) Standardpfade
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var gameFolder = isAts ? "American Truck Simulator" : "Euro Truck Simulator 2";

            // Bevorzugt "profiles", ansonsten "steam_profiles"
            var p1 = Path.Combine(docs, gameFolder, "profiles");
            if (Directory.Exists(p1)) return p1;

            var p2 = Path.Combine(docs, gameFolder, "steam_profiles");
            return p2; // darf notfalls nicht existieren; Aufrufer behandelt das
        }

        /// <summary>Holt die sichtbare Anzeige aus der ComboBox (z. B. „Mein Profil“).</summary>
        private string GetSelectedProfileDisplayText()
        {
            var obj = cbProfile?.SelectedItem;
            return obj?.ToString() ?? "";
        }

        /// <summary>Versucht, aus der ComboBox direkt den Ordnerpfad zu bekommen (wenn ComboItem.Value=Pfad gesetzt wurde).</summary>
        private string? TryGetSelectedProfileDirFromCombo()
        {
            var item = cbProfile?.SelectedItem;
            if (item == null) return null;

            // Greife per Reflection zu, ohne einen konkreten ComboItem-Typ zu erzwingen:
            var prop = item.GetType().GetProperty("Value");
            if (prop != null && prop.PropertyType == typeof(string))
            {
                var val = prop.GetValue(item) as string;
                return string.IsNullOrWhiteSpace(val) ? null : val;
            }
            return null;
        }

        /// <summary>
        /// Durchsucht alle Profilordner und vergleicht den profile_name in profile.sii mit dem Displaynamen.
        /// Entschlüsselt bei Bedarf automatisch (lokale _Local-Methoden).
        /// </summary>
        private string? FindProfileFolderByDisplayName_Local(string profilesRoot, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return null;

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(profilesRoot))
                {
                    var sii = Path.Combine(dir, "profile.sii");
                    if (!File.Exists(sii)) continue;

                    try
                    {
                        if (IsLikelyBinaryFile_Local(sii))
                            EnsureSiiDecryptedInPlace_Local(sii);

                        var text = File.ReadAllText(sii, Encoding.UTF8);
                        var name = ExtractProfileNameFromSiiText_Local(text);
                        if (!string.IsNullOrWhiteSpace(name) &&
                            string.Equals(name!.Trim(), displayName.Trim(), StringComparison.Ordinal))
                        {
                            return dir;
                        }
                    }
                    catch
                    {
                        // Ignorieren und nächsten Ordner probieren
                    }
                }
            }
            catch { /* ignorieren */ }

            return null;
        }

        /// <summary>Extrahiert profile_name aus SII-Text (ohne Regex, robust gegen Sonderfälle).</summary>
        private static string? ExtractProfileNameFromSiiText_Local(string text)
        {
            var idx = text.IndexOf("profile_name", StringComparison.Ordinal);
            if (idx < 0) return null;

            // erstes Anführungszeichen nach "profile_name"
            var q1 = text.IndexOf('"', idx);
            if (q1 < 0 || q1 + 1 >= text.Length) return null;
            var q2 = text.IndexOf('"', q1 + 1);
            if (q2 < 0 || q2 <= q1) return null;

            return text.Substring(q1 + 1, q2 - (q1 + 1));
        }

        /// <summary>Öffnet einen Ordner im Explorer.</summary>
        private static void OpenFolder_Local(string dir)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch { /* Anzeige übernimmt Aufrufer */ }
        }

        /// <summary>Kleiner Helfer: entfernt unzulässige Pfadzeichen aus einem Segment.</summary>
        private static string SanitizePathSegment_Local(string s)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(s.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "profile" : cleaned;
        }

        // ========================
        //   Lokale, eindeutige Helpers
        // ========================

        private bool IsEnglishUi_Local()
        {
            try
            {
                var st = SettingsService.Load();
                var lang = (st.Language ?? "de").ToLowerInvariant();
                return lang.StartsWith("en");
            }
            catch { return false; }
        }

        /// <summary>Sehr einfache Binär-Heuristik wie gewohnt: Nullbytes oder kein "SiiNunit".</summary>
        private static bool IsLikelyBinaryFile_Local(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var buf = new byte[Math.Min(4096, (int)fs.Length)];
                _ = fs.Read(buf, 0, buf.Length);
                if (buf.Any(b => b == 0)) return true;

                var head = Encoding.UTF8.GetString(buf);
                return !head.Contains("SiiNunit");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Ruft tools\SII_Decrypt.exe auf (AppBase\tools\SII_Decrypt.exe), wartet ~15s.</summary>
        private void EnsureSiiDecryptedInPlace_Local(string siiPath)
        {
            if (!IsLikelyBinaryFile_Local(siiPath)) return;

            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "SII_Decrypt.exe");
            if (!File.Exists(exe))
                throw new FileNotFoundException(
                    IsEnglishUi_Local() ? "SII_Decrypt.exe not found in tools\\." : "SII_Decrypt.exe nicht in tools\\ gefunden.",
                    exe);

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"\"{siiPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exe)!
            };

            using var p = Process.Start(psi);
            if (p == null)
                throw new InvalidOperationException(
                    IsEnglishUi_Local() ? "Failed to start SII_Decrypt.exe." : "SII_Decrypt.exe konnte nicht gestartet werden.");
            p.WaitForExit(15000);

            // Nachlaufprüfung
            if (IsLikelyBinaryFile_Local(siiPath))
                throw new InvalidOperationException(
                    IsEnglishUi_Local() ? "profile.sii still looks binary after decrypt." : "profile.sii wirkt nach dem Entschlüsseln weiterhin binär.");
        }
    }
}
