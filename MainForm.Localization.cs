// MainForm.Localization.cs
using System.Collections.Generic;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        public enum AppLanguage { German, English }

        // Schlüssel: UI-Elemente und Tooltips
        private static readonly Dictionary<string, (string de, string en)> T = new()
        {
            ["hdr"] = ("ETS2/ATS Modlist Importer/Exporter", "ETS2/ATS Modlist Importer/Exporter"),

            // Labels
            ["lblGame"]    = ("Spiel:",    "Game:"),
            ["lblProfile"] = ("Profil:",   "Profile:"),
            ["lblList"]    = ("Modliste:", "Mod list:"),

            // Checkboxen
            ["chkRaw"]     = ("Roh übernehmen (1:1 active_mods-Block)", "Import raw (1:1 active_mods block)"),
            ["chkAutoDec"] = ("Auto-Decrypt mit SII_Decrypt.exe (falls Binär)", "Auto-decrypt with SII_Decrypt.exe (if binary)"),

            // Buttons
            ["btnLoad"]    = ("Aktualisieren",                 "Refresh"),
            ["btnApply"]   = ("Modliste übernehmen",   "Apply mod list"),
            ["btnOpen"]    = ("Profilordner öffnen",   "Open profile folder"),
            ["btnExport"]  = ("Modliste exportieren",  "Export mod list"),
            ["btnCheck"]   = ("Text-Check",            "Text check"),
            ["btnDonate"]  = ("Donate (Ko-fi)",        "Donate (Ko-fi)"),
            ["btnRestore"] = ("Backup wiederherstellen…", "Restore backup…"),
            ["btnOptions"] = ("Optionen…",             "Options…"),

            // Combobox-Einträge
            ["cbGameETS2"] = ("Euro Truck Simulator 2 (ETS2)", "Euro Truck Simulator 2 (ETS2)"),
            ["cbGameATS"]  = ("American Truck Simulator (ATS)", "American Truck Simulator (ATS)"),

            // Tooltips
            ["tipGame"]    = ("Spiel auswählen (ETS2/ATS)", "Select game (ETS2/ATS)"),
            ["tipProfile"] = ("Profil auswählen", "Select profile"),
            ["tipList"]    = ("Vorhandene Modliste auswählen", "Select existing mod list"),
            ["tipRaw"]     = ("Kompletten active_mods-Block 1:1 einfügen", "Insert complete active_mods block 1:1"),
            ["tipAutoDec"] = ("Wenn profile.sii binär ist, automatisch entschlüsseln (falls Tool vorhanden)", "If profile.sii is binary, auto-decrypt (if tool present)"),
            ["tipLoad"]    = ("Modliste neu einlesen / Ansicht aktualisieren", "Reload mod list / refresh view"),
            ["tipApply"]   = ("Modliste in das Profil schreiben", "Write mod list into profile"),
            ["tipOpen"]    = ("Ordner des ausgewählten Profils öffnen", "Open selected profile's folder"),
            ["tipExport"]  = ("active_mods aus Profil als .txt exportieren", "Export active_mods from profile as .txt"),
            ["tipCheck"]   = ("Profil prüfen (Text/Binär, Vorschau)", "Check profile (text/binary, preview)"),
            ["tipDonate"]  = ("Ko-fi Seite öffnen", "Open Ko-fi page"),
            ["tipRestore"] = ("Ein Backup wiederherstellen", "Restore a backup"),
            ["tipOptions"] = ("Optionen öffnen", "Open options")
        };

        private void ApplyLanguage(string lang)
        {
            var isEN = (lang?.ToLowerInvariant() == "en");

            // Header (oberstes Docked Label mit Höhe >= 50)
            foreach (var c in Controls)
            {
                if (c is Label l && l.Dock == DockStyle.Top && l.Height >= 50)
                {
                    l.Text = isEN ? T["hdr"].en : T["hdr"].de;
                    break;
                }
            }

            // Labels setzen (wir suchen anhand des alten Textes)
            SetLabelTextLike(_lastLblGame ?? "Spiel:",     isEN ? T["lblGame"].en    : T["lblGame"].de,    ref _lastLblGame);
            SetLabelTextLike(_lastLblProfile ?? "Profil:", isEN ? T["lblProfile"].en : T["lblProfile"].de, ref _lastLblProfile);
            SetLabelTextLike(_lastLblList ?? "Modliste:",  isEN ? T["lblList"].en    : T["lblList"].de,    ref _lastLblList);

            // Checkboxen & Buttons
            chkRaw.Text     = isEN ? T["chkRaw"].en     : T["chkRaw"].de;
            chkAutoDec.Text = isEN ? T["chkAutoDec"].en : T["chkAutoDec"].de;

            btnLoad.Text    = isEN ? T["btnLoad"].en    : T["btnLoad"].de;
            btnApply.Text   = isEN ? T["btnApply"].en   : T["btnApply"].de;
            btnOpen.Text    = isEN ? T["btnOpen"].en    : T["btnOpen"].de;
            btnExport.Text  = isEN ? T["btnExport"].en  : T["btnExport"].de;
            btnCheck.Text   = isEN ? T["btnCheck"].en   : T["btnCheck"].de;
            btnDonate.Text  = isEN ? T["btnDonate"].en  : T["btnDonate"].de;
            btnRestore.Text = isEN ? T["btnRestore"].en : T["btnRestore"].de;
            btnOptions.Text = isEN ? T["btnOptions"].en : T["btnOptions"].de;
            btnOpenModlists.Text = lang == "en" ? "Open Modlist Folder" : "Modlisten-Ordner öffnen";

            // Game-Combo neu befüllen (Auswahl beibehalten)
            var idx = cbGame.SelectedIndex;
            cbGame.Items.Clear();
            cbGame.Items.Add(isEN ? T["cbGameETS2"].en : T["cbGameETS2"].de);
            cbGame.Items.Add(isEN ? T["cbGameATS"].en  : T["cbGameATS"].de);
            if (idx >= 0 && idx < cbGame.Items.Count) cbGame.SelectedIndex = idx;

            // Tooltips zweisprachig
            tips.SetToolTip(cbGame,     isEN ? T["tipGame"].en    : T["tipGame"].de);
            tips.SetToolTip(cbProfile,  isEN ? T["tipProfile"].en : T["tipProfile"].de);
            tips.SetToolTip(cbList,     isEN ? T["tipList"].en    : T["tipList"].de);
            tips.SetToolTip(chkRaw,     isEN ? T["tipRaw"].en     : T["tipRaw"].de);
            tips.SetToolTip(chkAutoDec, isEN ? T["tipAutoDec"].en : T["tipAutoDec"].de);
            tips.SetToolTip(btnLoad,    isEN ? T["tipLoad"].en    : T["tipLoad"].de);
            tips.SetToolTip(btnApply,   isEN ? T["tipApply"].en   : T["tipApply"].de);
            tips.SetToolTip(btnOpen,    isEN ? T["tipOpen"].en    : T["tipOpen"].de);
            tips.SetToolTip(btnExport,  isEN ? T["tipExport"].en  : T["tipExport"].de);
            tips.SetToolTip(btnCheck,   isEN ? T["tipCheck"].en   : T["tipCheck"].de);
            tips.SetToolTip(btnDonate,  isEN ? T["tipDonate"].en  : T["tipDonate"].de);
            tips.SetToolTip(btnRestore, isEN ? T["tipRestore"].en : T["tipRestore"].de);
            tips.SetToolTip(btnOptions, isEN ? T["tipOptions"].en : T["tipOptions"].de);
            tips.SetToolTip(btnOpenModlists, lang == "en" ? "Open existing modlists" : "Vorhandene Modlisten öffnen");

            // >>> Footer-Beschriftung anpassen <<<
            // Methode ist in MainForm.FooterNotes.cs implementiert
            UpdateFooterLanguage(isEN ? "en" : "de");

            try { UpdateBackupProfilesButtonLanguage(); } catch { }
            try { PositionBackupProfilesButton(); } catch { }

            // --- translate dynamic backup button ---
            try
            {
                var isEN2 = (lang?.ToLowerInvariant() == "en");
                var btnBackupProfiles = Controls.Find("btnBackupProfiles", true).FirstOrDefault() as Button;
                if (btnBackupProfiles != null)
                {
                    btnBackupProfiles.Text = isEN2 ? "Backup all profiles" : "Alle Profile sichern";
                    // optional: tooltip
                    if (tips != null)
                        tips.SetToolTip(btnBackupProfiles, isEN2 ? "Back up the whole profiles folder" : "Gesamten Profiles-Ordner sichern");
                }
            }
            catch { /* ignore */ }

            try { EnsureHeaderShareImport_L10n(lang ?? "de"); } catch {}

            if (btnListShare  != null && !btnListShare.IsDisposed)
                btnListShare.Text  = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "Share"  : "Weitergeben";
            if (btnListImport != null && !btnListImport.IsDisposed)
                btnListImport.Text = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "Import" : "Importieren";

            // [Delete-L10n]
            try
            {
                if (btnListDelete != null && !btnListDelete.IsDisposed)
                    btnListDelete.Text = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "Delete" : "Löschen";
            }
            catch { }

            try { EnsureHeaderShareImportButtons(); } catch {}
            try { EnsureHeaderProfileButtons(); } catch {}

            try { EnsureHeaderProfile_L10n(lang); } catch {}

            try { ApplyThemeToDynamicButtons(); } catch {}
            try { EnsureHeaderProfile_Theme(); } catch {}

            // im Bereich der Button-Erstellung für Profile ...
            if (btnProfRename == null || btnProfRename.IsDisposed)
            {
                btnProfRename = new Button
                {
                    Name = "btnProfRename",
                    Text = GetCurrentLanguageIsEnglish() ? "Rename" : "Umbenennen",
                    AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true, Visible = true
                };
                btnProfRename.Click -= BtnProfRename_Click;
                btnProfRename.Click += BtnProfRename_Click;
            }
            if (btnProfDelete == null || btnProfDelete.IsDisposed)
            {
                btnProfDelete = new Button
                {
                    Name = "btnProfDelete",
                    Text = GetCurrentLanguageIsEnglish() ? "Remove" : "Entfernen",
                    AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true, Visible = true
                };
                btnProfDelete.Click -= BtnProfDelete_Click;
                btnProfDelete.Click += BtnProfDelete_Click;
            }
        }

        // Wir merken uns die zuletzt gefundenen Labeltexte, damit SetLabelTextLike sie wiederfindet
        private string? _lastLblGame, _lastLblProfile, _lastLblList;

        private void SetLabelTextLike(string previousText, string newText, ref string? tracker)
        {
            foreach (var c in Controls)
            {
                if (c is Label l && l.Text == previousText)
                {
                    l.Text = newText;
                    tracker = newText;
                    return;
                }
            }
        }
    }
}
