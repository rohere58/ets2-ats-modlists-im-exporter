// MainForm.ProfileFolderAndRestore.cs
using System;
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
        // Einmalige Verdrahtung für die zwei Buttons
        private void WireFolderAndRestoreButtons()
        {
            try { btnOpen.Click -= BtnOpenProfileFolder_Click; } catch { }
            try { btnRestore.Click -= BtnRestoreBackup_Click; } catch { }

            btnOpen.Click += BtnOpenProfileFolder_Click;
            btnRestore.Click += BtnRestoreBackup_Click;
        }

        // ========== Button: Profilordner öffnen ==========
        private void BtnOpenProfileFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                var dir = ResolveSelectedProfileDir_ForButtons();
                if (!Directory.Exists(dir))
                    throw new DirectoryNotFoundException(dir);

                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });

                SafeSetStatus(GetCurrentLanguageIsEnglish()
                    ? "Opened profile folder."
                    : "Profilordner geöffnet.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    (GetCurrentLanguageIsEnglish() ? "Could not open folder:\n" : "Ordner konnte nicht geöffnet werden:\n") + ex.Message,
                    GetCurrentLanguageIsEnglish() ? "Error" : "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== Button: Backup wiederherstellen ==========
        private void BtnRestoreBackup_Click(object? sender, EventArgs e)
        {
            try
            {
                var siiPath = ResolveSelectedProfileSii_ForButtons();
                var dir = Path.GetDirectoryName(siiPath)!;

                // Kandidaten sammeln (flexibel: alle BAKs, die nach profile.sii aussehen)
                var bakFiles = Directory.GetFiles(dir, "*.bak")
                                        .Where(f => Path.GetFileName(f).StartsWith("profile.sii", StringComparison.OrdinalIgnoreCase))
                                        .Select(f => new FileInfo(f))
                                        .OrderByDescending(f => f.LastWriteTimeUtc)
                                        .ToList();

                if (bakFiles.Count == 0)
                {
                    MessageBox.Show(this,
                        GetCurrentLanguageIsEnglish()
                            ? "No backup files found in the profile folder."
                            : "Keine Backup-Dateien im Profilordner gefunden.",
                        GetCurrentLanguageIsEnglish() ? "Restore backup" : "Backup wiederherstellen",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var ofd = new OpenFileDialog
                {
                    Title = GetCurrentLanguageIsEnglish() ? "Select a backup to restore" : "Backup zum Wiederherstellen auswählen",
                    InitialDirectory = dir,
                    Filter = "Backup (*.bak)|*.bak|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                // Optional: letzten Eintrag vorwählen
                ofd.FileName = bakFiles.First().FullName;

                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                var chosen = ofd.FileName;
                if (!File.Exists(chosen))
                    throw new FileNotFoundException("Backup not found.", chosen);

                // Sicherung der aktuellen profile.sii vor dem Überschreiben
                var safetyName = Path.Combine(dir, $"profile.sii.restored.{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                if (File.Exists(siiPath))
                    File.Copy(siiPath, safetyName, overwrite: false);

                File.Copy(chosen, siiPath, overwrite: true);

                SafeSetStatus(GetCurrentLanguageIsEnglish()
                    ? $"Backup restored: {Path.GetFileName(chosen)}"
                    : $"Backup wiederhergestellt: {Path.GetFileName(chosen)}");

                MessageBox.Show(this,
                    GetCurrentLanguageIsEnglish()
                        ? "Backup restored successfully."
                        : "Backup wurde erfolgreich wiederhergestellt.",
                    GetCurrentLanguageIsEnglish() ? "Restore backup" : "Backup wiederherstellen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    (GetCurrentLanguageIsEnglish() ? "Error while restoring backup:\n" : "Fehler beim Wiederherstellen des Backups:\n") + ex.Message,
                    GetCurrentLanguageIsEnglish() ? "Error" : "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =======================
        //  Lokale Resolver (nur für diese Buttons, keine Kollisionen)
        // =======================
        private string ResolveSelectedProfileSii_ForButtons()
        {
            var dir = ResolveSelectedProfileDir_ForButtons();
            var sii = Path.Combine(dir, "profile.sii");
            if (!File.Exists(sii))
                throw new FileNotFoundException(
                    GetCurrentLanguageIsEnglish()
                        ? "profile.sii not found for the selected profile."
                        : "profile.sii für das ausgewählte Profil nicht gefunden.",
                    sii);
            return sii;
        }

        private string ResolveSelectedProfileDir_ForButtons()
        {
            // 1) Auswahl lesen
            var display = cbProfile?.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(display))
                throw new InvalidOperationException(
                    GetCurrentLanguageIsEnglish() ? "No profile selected." : "Kein Profil ausgewählt.");

            // 2) Root bestimmen (ETS2/ATS + Settings)
            var root = ResolveProfilesRootDir_ForButtons();

            // 3) Fall A: Anzeige enthält einen Pfad in Klammern -> (C:\...\profiles\XXXX)
            var m = Regex.Match(display, @"\((?<p>[A-Za-z]:\\.+?)\)\s*$");
            if (m.Success)
            {
                var p = m.Groups["p"].Value;
                if (Directory.Exists(p)) return p;
            }

            // 4) Fall B: Ordnername = Anzeigetext (direkt)
            var direct = Path.Combine(root, display);
            if (Directory.Exists(direct)) return direct;

            // 5) Fall C: Ordner unter profiles/ bzw. steam_profiles/ mit gleichem Namen (case-insensitive)
            string? hit = Directory.GetDirectories(root)
                .FirstOrDefault(d => string.Equals(Path.GetFileName(d), display, StringComparison.OrdinalIgnoreCase));
            if (hit != null) return hit;

            var steamRoot = Path.Combine(Directory.GetParent(root)?.FullName ?? root, "steam_profiles");
            if (Directory.Exists(steamRoot))
            {
                hit = Directory.GetDirectories(steamRoot)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), display, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }

            // 6) Fall D: Über profile_name in profile.sii auflösen
            foreach (var baseRoot in new[] { root, steamRoot })
            {
                if (!Directory.Exists(baseRoot)) continue;

                foreach (var dir in Directory.GetDirectories(baseRoot))
                {
                    var sii = Path.Combine(dir, "profile.sii");
                    if (!File.Exists(sii)) continue;

                    try
                    {
                        // reicht völlig: vollständiger Text, einfacher Regex
                        var text = File.ReadAllText(sii, Encoding.UTF8);
                        var mm = Regex.Match(text, @"profile_name\s*:\s*""(?<n>[^""]+)""");
                        if (mm.Success)
                        {
                            var profName = mm.Groups["n"].Value;
                            if (string.Equals(profName, display, StringComparison.Ordinal))
                                return dir;
                        }
                    }
                    catch { /* ignorieren und weiter suchen */ }
                }
            }

            // 7) Nichts gefunden -> aussagekräftiger Fehler
            throw new DirectoryNotFoundException($"{root}\\{display}");
        }

        private string ResolveProfilesRootDir_ForButtons()
        {
            // Spiel ermitteln: ETS2 = 0, ATS = 1
            var isAts = (cbGame?.SelectedIndex == 1);

            // Settings beachten (falls gesetzt), sonst Standard in „Dokumente“
            var st = SettingsService.Load();
            if (isAts)
            {
                if (!string.IsNullOrWhiteSpace(st.AtsProfilesPath) && Directory.Exists(st.AtsProfilesPath))
                    return st.AtsProfilesPath;
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(docs, "American Truck Simulator", "profiles");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(st.Ets2ProfilesPath) && Directory.Exists(st.Ets2ProfilesPath))
                    return st.Ets2ProfilesPath;
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
            }
        }
    }
}
