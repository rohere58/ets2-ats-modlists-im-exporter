// MainForm.Backups.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Auf true lassen, bis wir sehen, dass Backups entstehen
        private const bool VerboseBackups = true;

        /// <summary>
        /// Schreibt <paramref name="content"/> nach <paramref name="path"/> und legt vorher Backups an:
        /// - Plain:  <name>.bak  (wird überschrieben)
        /// - Timestamped: <name>.yyyyMMdd_HHmmss_fff.bak (rotierend, Standard 5)
        /// </summary>
        private void WriteWithBackup(string path, string content, int keep = 5)
        {
            if (VerboseBackups) SafeSetStatus($"[Backup] WriteWithBackup -> {path}");
            CreateBackupWithRetention(path, keep);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, Encoding.UTF8);
            if (VerboseBackups) SafeSetStatus($"[Backup] geschrieben: {Path.GetFileName(path)}");
        }

        /// <summary>
        /// Legt Backups an. Auch wenn die Datei (noch) nicht existiert, loggen wir das sauber.
        /// </summary>
        private void CreateBackupWithRetention(string path, int keep = 5)
        {
            try
            {
                var dir  = Path.GetDirectoryName(path)!;
                var name = Path.GetFileName(path);

                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(name))
                {
                    if (VerboseBackups) SafeSetStatus("[Backup] Ungültiger Pfad.");
                    return;
                }

                Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    if (VerboseBackups) SafeSetStatus($"[Backup] Quelle existiert nicht (kein Plain-Backup): {name}");
                }
                else
                {
                    // 1) Plain .bak (immer überschreiben)
                    var plainBak = Path.Combine(dir, $"{name}.bak");
                    File.Copy(path, plainBak, overwrite: true);
                    if (VerboseBackups) SafeSetStatus($"[Backup] Plain: {Path.GetFileName(plainBak)} erstellt.");
                }

                // 2) Timestamped .bak immer anlegen, sofern Quelle existiert
                if (File.Exists(path))
                {
                    var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    var tsBak = Path.Combine(dir, $"{name}.{stamp}.bak");
                    if (File.Exists(tsBak))
                    {
                        var uniq = Guid.NewGuid().ToString("N")[..6];
                        tsBak = Path.Combine(dir, $"{name}.{stamp}.{uniq}.bak");
                    }
                    File.Copy(path, tsBak, overwrite: true);
                    if (VerboseBackups) SafeSetStatus($"[Backup] Time:  {Path.GetFileName(tsBak)} erstellt.");

                    // 3) Rotieren: nur timestamped aufräumen
                    var tsFiles = new List<string>(Directory.GetFiles(dir, $"{name}.*.bak"));
                    tsFiles.RemoveAll(p =>
                        string.Equals(p, Path.Combine(dir, $"{name}.bak"), StringComparison.OrdinalIgnoreCase));

                    tsFiles.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    for (int i = keep; i < tsFiles.Count; i++)
                    {
                        try { File.Delete(tsFiles[i]); } catch { /* egal */ }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeSetStatus("[Backup] Fehler: " + ex.Message);
            }
        }
    }
}
