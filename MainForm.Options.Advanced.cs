// MainForm.Options.Advanced.cs
using System;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Zentral aufgerufen vom Options-Button
        private void ShowOptionsDialog()
        {
            try
            {
                var settings = SettingsService.Load();

                using var dlg = new OptionsForm
                {
                    StartPosition = FormStartPosition.CenterParent,
                    DarkModeChecked = settings.DarkMode,
                    Ets2ProfilesPath = settings.Ets2ProfilesPath,
                    AtsProfilesPath  = settings.AtsProfilesPath
                };
                // Sprache setzen
                dlg.SelectedLanguage = settings.Language;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Übernahme
                    settings.DarkMode       = dlg.DarkModeChecked;
                    settings.Language       = dlg.SelectedLanguage;
                    settings.Ets2ProfilesPath = dlg.Ets2ProfilesPath?.Trim() ?? "";
                    settings.AtsProfilesPath  = dlg.AtsProfilesPath?.Trim() ?? "";

                    // Speichern
                    SettingsService.Save(settings);

                    // Sofort anwenden
                    TryInvokeWithArg("UpdateThemeFromOptions", settings.DarkMode);
                    TryInvokeWithArg("UpdateLanguageFromOptions", settings.Language);

                    // Pfade weiterreichen (für deine Loader später verwendbar)
                    TryInvokeWithArg("UpdatePathsFromOptions", new string[] { settings.Ets2ProfilesPath, settings.AtsProfilesPath });

                    SafeSetStatus("Optionen gespeichert.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Öffnen der Optionen:\n" + ex.Message,
                    "Optionen", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowOptionsDialogSafe() => ShowOptionsDialog();
    }
}
