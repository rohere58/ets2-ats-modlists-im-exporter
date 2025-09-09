// MainForm.ApplyHook.cs
using System;
using System.IO;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Wird aus MainForm.cs vor dem eigentlichen Apply aufgerufen.
        /// Legt immer ein Backup der aktuellen profile.sii an (falls existent).
        /// </summary>
        private void OnBeforeApply()
        {
            try
            {
                var sii = GetCurrentProfileSiiPath();
                if (!string.IsNullOrWhiteSpace(sii) && File.Exists(sii))
                {
                    // Nur Backup anlegen; kein Schreiben hier
                    SafeSetStatus($"[Backup] Vor-Backup angelegt für: {Path.GetFileName(sii)}");
                }
                else
                {
                    SafeSetStatus("[Backup] Kein gültiger profile.sii Pfad gefunden – kein Backup angelegt.");
                }
            }
            catch (Exception ex)
            {
                SafeSetStatus("OnBeforeApply/Backup-Fehler: " + ex.Message);
            }
        }

        /// <summary>
        /// Ermittelt den Pfad der profile.sii des aktuell gewählten Profils.
        /// Passt zu unserer Profil-Befülllogik (Standard- oder benutzerdefinierte Pfade).
        /// </summary>
        private string GetCurrentProfileSiiPath()
        {
            // Wir gehen davon aus, dass cbProfile.Items mit dem Anzeigenamen gefüllt ist
            // und die Verzeichnisstruktur standardkonform ist.
            // Falls du bereits eine eigene Methode hast, die den Pfad findet:
            // -> Diese hier kann darauf umgebogen werden.
            var sel = cbProfile.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(sel)) return "";

            // Root (je nach Spiel + SettingsService)
            var st = SettingsService.Load();
            string root =
                cbGame.SelectedIndex == 1
                ? (string.IsNullOrWhiteSpace(st.AtsProfilesPath)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "American Truck Simulator", "profiles")
                    : st.AtsProfilesPath)
                : (string.IsNullOrWhiteSpace(st.Ets2ProfilesPath)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2", "profiles")
                    : st.Ets2ProfilesPath);

            // Der Anzeigename in cbProfile ist unser Klarname; der Ordner ist hashkodiert.
            // Wir suchen den Ordner, dessen 'profile.sii' den 'profile_name:' enthält.
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    var sii = Path.Combine(dir, "profile.sii");
                    if (File.Exists(sii))
                    {
                        var txt = File.ReadAllText(sii);
                        // schneller Check
                        if (txt.IndexOf("profile_name:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            txt.IndexOf(sel, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return sii;
                        }
                    }
                }
            }
            catch { /* egal */ }

            return "";
        }
    }
}
