// MainForm.LegacyCleanup.cs
using System;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Öffentlicher Hook-Name, der in deinen Hooks verwendet wird.
        ///</summary>
        private void RemoveLegacyUi()
        {
            CleanupLegacyCheckboxes();
        }

        /// <summary>
        /// Versteckt/neutralisiert alte Checkboxen ("Roh übernehmen", "Auto-Decrypt"), falls sie irgendwo noch auftauchen.
        /// </summary>
        private void CleanupLegacyCheckboxes()
        {
            // Versuch 1: per Name
            HideAndDisable("chkRaw");
            HideAndDisable("chkAutoDec");

            // Versuch 2: per Textinhalt (DE/EN)
            HideByTextStartsWith("Roh übernehmen");
            HideByTextStartsWith("Auto-Decrypt");
            HideByTextStartsWith("Import raw");
            HideByTextStartsWith("Auto-decrypt");
        }

        private void HideAndDisable(string name)
        {
            try
            {
                var found = Controls.Find(name, true);
                foreach (var c in found)
                {
                    if (c is Control ctl)
                    {
                        ctl.Visible = false;
                        ctl.Enabled = false;
                        ctl.Width = 0;
                        ctl.Height = 0;
                        ctl.TabStop = false;
                        try { tips.SetToolTip(ctl, null); } catch { }
                    }
                }
            }
            catch { /* ignorieren */ }
        }

        private void HideByTextStartsWith(string startsWith)
        {
            try
            {
                foreach (Control ctl in Controls)
                    HideByTextStartsWithRecursive(ctl, startsWith);
            }
            catch { /* ignorieren */ }
        }

        private void HideByTextStartsWithRecursive(Control parent, string startsWith)
        {
            if (parent is CheckBox cb && cb.Text.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
            {
                cb.Visible = false;
                cb.Enabled = false;
                cb.Width = 0;
                cb.Height = 0;
                cb.TabStop = false;
                try { tips.SetToolTip(cb, null); } catch { }
            }

            foreach (Control child in parent.Controls)
                HideByTextStartsWithRecursive(child, startsWith);
        }
    }
}
