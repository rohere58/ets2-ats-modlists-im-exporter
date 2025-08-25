// MainForm.ProfilesAndLists.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Helper-Klasse für ComboBox (Anzeigetext + Wert)
        private sealed class ComboItem
        {
            public string Text { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        // ------------------- PROFILES -------------------

        private void LoadProfiles_Local()
        {
            try
            {
                cbProfile.BeginUpdate();
                cbProfile.Items.Clear();

                var st = SettingsService.Load();

                var game = GetCurrentGameForPaths();
                var standard = GetDefaultProfilesPath(game);
                var custom = GetCustomProfilesPathFromSettings(st, game);

                var candidates = new List<string>();

                // Erst Custom (wenn gültig), dann Standard
                if (!string.IsNullOrWhiteSpace(custom) && Directory.Exists(custom))
                    candidates.Add(custom);
                if (Directory.Exists(standard))
                    candidates.Add(standard);

                // Duplikate vermeiden
                candidates = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Alle direkten Unterordner als Profile
                var profileDirs = new List<string>();
                foreach (var basePath in candidates)
                {
                    try
                    {
                        profileDirs.AddRange(Directory.GetDirectories(basePath));
                    }
                    catch { /* ignore */ }
                }

                // Duplikate entfernen
                profileDirs = profileDirs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Profile hübsch aufbereiten
                var items = new List<ComboItem>();
                foreach (var dir in profileDirs)
                {
                    var display = GetPrettyProfileName(dir, allowDecrypt: chkAutoDec.Checked);
                    items.Add(new ComboItem(display, dir));
                }

                // Sortierung nach Anzeigetext
                foreach (var it in items.OrderBy(i => i.Text, StringComparer.CurrentCultureIgnoreCase))
                    cbProfile.Items.Add(it);

                if (cbProfile.Items.Count > 0) cbProfile.SelectedIndex = 0;

                SafeSetStatus(cbProfile.Items.Count > 0
                    ? $"Profile geladen: {cbProfile.Items.Count}"
                    : "Keine Profile gefunden.");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Profile: " + ex.Message);
            }
            finally
            {
                cbProfile.EndUpdate();
            }
        }

        private Game GetCurrentGameForPaths()
        {
            try { return cbGame.SelectedIndex == 1 ? Game.ATS : Game.ETS2; }
            catch { return Game.ETS2; }
        }

        private static string GetDefaultProfilesPath(Game game)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return game == Game.ATS
                ? Path.Combine(docs, "American Truck Simulator", "profiles")
                : Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
        }

        private static string GetCustomProfilesPathFromSettings(AppSettings st, Game game)
        {
            return game == Game.ATS ? st.AtsProfilesPath?.Trim() ?? "" : st.Ets2ProfilesPath?.Trim() ?? "";
        }

        private string? GetSelectedProfilePath()
        {
            if (cbProfile.SelectedItem is ComboItem ci) return ci.Value;
            return cbProfile.SelectedItem as string;
        }

        private void DoOpenSelectedProfileFolder_Local()
        {
            try
            {
                var path = GetSelectedProfilePath();
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    MessageBox.Show(this, "Profilordner nicht gefunden.", "Öffnen",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Öffnen des Profilordners: " + ex.Message);
            }
        }

        // ------------------- MODLISTS (ETS2/ATS Unterordner) -------------------

        private void LoadModlists_Local()
        {
            try
            {
                cbList.BeginUpdate();
                cbList.Items.Clear();

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var listsRoot = Path.Combine(baseDir, "modlists");

                // Unterordner je Spiel wählen
                var sub = (cbGame.SelectedIndex == 1) ? "ATS" : "ETS2";
                var listsDir = Path.Combine(listsRoot, sub);

                // Ordner sicherstellen (damit Nutzer gleich sieht, wo es liegt)
                Directory.CreateDirectory(listsDir);

                // Nur .txt Modlisten laden
                var files = Directory.GetFiles(listsDir, "*.txt", SearchOption.TopDirectoryOnly)
                                     .OrderBy(Path.GetFileName)
                                     .ToList();

                foreach (var f in files)
                {
                    // Optional: schöner Anzeigename ohne Extension
                    var display = Path.GetFileNameWithoutExtension(f);
                    cbList.Items.Add(new ComboItem(display, f));
                }

                if (cbList.Items.Count > 0) cbList.SelectedIndex = 0;

                SafeSetStatus(cbList.Items.Count > 0
                    ? $"Modlisten ({sub}) gefunden: {cbList.Items.Count}"
                    : $"Keine Modlisten im Ordner: {Path.GetFullPath(listsDir)}");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Modlisten: " + ex.Message);
            }
            finally
            {
                cbList.EndUpdate();
            }
        }

        private string? GetSelectedModlistPath()
        {
            // Wir speichern den vollen Pfad im ComboItem.Value – also einfach zurückgeben
            if (cbList.SelectedItem is ComboItem ci) return ci.Value;

            // Falls jemals nur der Dateiname eingetragen sein sollte: auf den akt. Spiel-Unterordner mappen
            var name = cbList.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(name)) return null;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var listsRoot = Path.Combine(baseDir, "modlists");
            var sub = (cbGame.SelectedIndex == 1) ? "ATS" : "ETS2";
            var listsDir = Path.Combine(listsRoot, sub);

            var candidate = Path.Combine(listsDir, name);
            if (File.Exists(candidate)) return candidate;

            // evtl. ohne .txt gewählt
            var withTxt = candidate.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                ? candidate
                : candidate + ".txt";
            return File.Exists(withTxt) ? withTxt : null;
        }

        private void DoLoadSelectedModlistToPreview_Local()
        {
            try
            {
                rtbPreview.Clear();
                var path = GetSelectedModlistPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    SafeSetStatus("Keine Modliste gewählt.");
                    return;
                }

                var text = File.ReadAllText(path);
                rtbPreview.Text = text;
                SafeSetStatus("Modliste in Vorschau geladen: " + Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Modliste: " + ex.Message);
            }
        }

        // ------------------- Helpers: Pretty Profile Name -------------------

        private string GetPrettyProfileName(string profileDir, bool allowDecrypt)
        {
            // 1) Versuche profile_name aus profile.sii
            var siiPath = Path.Combine(profileDir, "profile.sii");
            var name = TryReadProfileNameFromSii(siiPath, allowDecrypt);
            if (!string.IsNullOrWhiteSpace(name))
                return name!;

            // 2) Versuche Ordnername als Hex zu dekodieren
            var folder = Path.GetFileName(profileDir) ?? profileDir;
            var decoded = TryDecodeHexFolder(folder);
            if (!string.IsNullOrWhiteSpace(decoded))
                return decoded!;

            // 3) Fallback: roher Ordnername
            return folder;
        }

        private static string? TryDecodeHexFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return null;
            if (folderName.Length % 2 != 0) return null;

            for (int i = 0; i < folderName.Length; i++)
            {
                char c = folderName[i];
                bool hex = (c >= '0' && c <= '9') ||
                           (c >= 'a' && c <= 'f') ||
                           (c >= 'A' && c <= 'F');
                if (!hex) return null;
            }

            try
            {
                var bytes = new byte[folderName.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(folderName.Substring(i * 2, 2), 16);

                var text = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrWhiteSpace(text) && text.IndexOfAny(new[] { '\0', '\r' }) < 0)
                    return text;
            }
            catch { }
            return null;
        }

        private string? TryReadProfileNameFromSii(string siiPath, bool allowDecrypt)
        {
            try
            {
                var text = TryReadSiiText(siiPath, allowDecrypt); // kommt aus MainForm.SiiDecrypt.cs
                if (string.IsNullOrWhiteSpace(text)) return null;

                // profile_name: "Mein Profil"
                var m = Regex.Match(text, @"profile_name\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return null;
        }
    }
}
