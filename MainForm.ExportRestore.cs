// MainForm.ExportRestore.cs
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Exportiert den kompletten active_mods-Block aus der aktuell ausgewählten profile.sii in eine .txt.
        /// </summary>
        private void DoExportModlist()
        {
            try
            {
                // 1) Profilordner ermitteln
                var profileDir = GetSelectedProfilePath();
                if (string.IsNullOrWhiteSpace(profileDir) || !Directory.Exists(profileDir))
                {
                    MessageBox.Show(this, "Bitte zuerst ein Profil auswählen.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2) profile.sii laden (Text erwartet – Autodecrypt sollte vorher gelaufen sein)
                var siiPath = Path.Combine(profileDir, "profile.sii");
                if (!File.Exists(siiPath))
                {
                    MessageBox.Show(this, "profile.sii wurde im ausgewählten Profil nicht gefunden.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var siiText = File.ReadAllText(siiPath, Encoding.UTF8);

                // 3) active_mods-Block extrahieren (zeilenbasiert, robust)
                var block = ExtractActiveModsBlock(siiText);
                if (string.IsNullOrWhiteSpace(block))
                {
                    MessageBox.Show(this, "In der profile.sii wurde kein active_mods-Block gefunden.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 4) Ziel-Datei erfragen (Standard: modlists/<GameTag>/export_YYYYMMDD_HHMM.txt)
                string gameTag = cbGame.SelectedIndex == 1 ? "ATS" : "ETS2";
                var defaultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modlists", gameTag);
                Directory.CreateDirectory(defaultDir);
                var defaultName = $"export_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                using (var sfd = new SaveFileDialog
                {
                    Title = "Modliste exportieren",
                    Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                    InitialDirectory = defaultDir,
                    FileName = defaultName,
                    OverwritePrompt = true
                })
                {
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;

                    File.WriteAllText(sfd.FileName, block, Encoding.UTF8);
                    SafeSetStatus($"Modliste exportiert: {Path.GetFileName(sfd.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Export fehlgeschlagen:\n" + ex.Message, "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Zeilenbasierte, 1:1-Extraktion des active_mods-Blocks.
        /// Nimmt ab der ersten Zeile "active_mods:" alle folgenden Zeilen
        /// "active_mods[<n>]: ..." mit – bis die Serie endet.
        /// </summary>
        private static string? ExtractActiveModsBlock(string text)
        {
            var norm = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = norm.Split('\n');

            var sb = new StringBuilder();
            bool inBlock = false;

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                var trimmedLeft = line.TrimStart();

                if (!inBlock)
                {
                    if (trimmedLeft.StartsWith("active_mods:", StringComparison.Ordinal))
                    {
                        inBlock = true;
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    if (trimmedLeft.StartsWith("active_mods[", StringComparison.Ordinal))
                        sb.AppendLine(line);
                    else
                        break;
                }
            }

            if (!inBlock) return null;
            if (sb.Length > 0 && (sb[^1] == '\n' || sb[^1] == '\r'))
                return sb.ToString().TrimEnd('\r', '\n');

            return sb.ToString();
        }

        /// <summary>
        /// Stellt die profile.sii aus einer .bak wieder her (im ausgewählten Profilordner).
        /// Nutzt rotierende Backups (5 Stück).
        /// </summary>
        private void DoRestoreBackup()
        {
            try
            {
                var profileDir = GetSelectedProfilePath();
                if (string.IsNullOrWhiteSpace(profileDir) || !Directory.Exists(profileDir))
                {
                    MessageBox.Show(this, "Bitte zuerst ein Profil auswählen.", "Backup wiederherstellen",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var siiPath = Path.Combine(profileDir, "profile.sii");

                // sinnvolle .bak vorschlagen
                string? initialBak = null;
                string[] candidates = Array.Empty<string>();
                try
                {
                    candidates = Directory.GetFiles(profileDir, "*.bak", SearchOption.TopDirectoryOnly);
                    var direct = Path.Combine(profileDir, "profile.sii.bak");
                    if (File.Exists(direct)) initialBak = direct;
                    else if (candidates.Length > 0)
                    {
                        Array.Sort(candidates, StringComparer.OrdinalIgnoreCase);
                        initialBak = candidates[^1];
                    }
                }
                catch { /* egal */ }

                using (var ofd = new OpenFileDialog
                {
                    Title = "Backup wählen (profile.sii.bak)",
                    Filter = "Backup (*.bak)|*.bak|Alle Dateien (*.*)|*.*",
                    InitialDirectory = profileDir,
                    FileName = initialBak ?? ""
                })
                {
                    if (ofd.ShowDialog(this) != DialogResult.OK) return;

                    var restoredText = File.ReadAllText(ofd.FileName, Encoding.UTF8);

                    // Rotierendes Backup der aktuellen profile.sii + Schreiben der Wiederherstellung
                    WriteWithBackup(siiPath, restoredText, keep: 5);

                    SafeSetStatus($"Backup wiederhergestellt: {Path.GetFileName(ofd.FileName)} → profile.sii");
                    MessageBox.Show(this, "Backup wurde wiederhergestellt.", "Backup",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Wiederherstellung fehlgeschlagen:\n" + ex.Message, "Backup",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
