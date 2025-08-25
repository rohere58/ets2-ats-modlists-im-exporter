// MainForm.About.cs
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private Button? _btnAbout;

        private void EnsureAboutButton()
        {
            if (_btnAbout != null && !_btnAbout.IsDisposed) return;

            _btnAbout = new Button
            {
                AutoSize = false,                // gleiche Höhe/Breite wie Referenzbutton
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false, // wir stylen selbst
                Text = GetCurrentLanguageIsEnglish() ? "About" : "Über",
                TabStop = false
            };
            _btnAbout.FlatAppearance.BorderSize = 0;
            _btnAbout.Click += (s, e) => ShowAboutDialog();

            // In die TOP-Button-Leiste einhängen
            pnlTopButtons.Controls.Add(_btnAbout);
            // ganz nach rechts
            pnlTopButtons.Controls.SetChildIndex(_btnAbout, pnlTopButtons.Controls.Count - 1);

            // Stil und Größe exakt an einen vorhandenen Button anlehnen (z.B. btnOptions)
            ApplyTopButtonLookAndSize(_btnAbout);

            // Theme & Sprache final anwenden
            UpdateAboutButtonStyle(SettingsService.Load().DarkMode);
            UpdateAboutButtonLanguage(SettingsService.Load().Language);
        }

        /// <summary>
        /// Nimmt den ersten vorhandenen Button in pnlTopButtons als Referenz
        /// und kopiert Größe/Margins/Padding, damit _btnAbout exakt gleich aussieht.
        /// </summary>
        private void ApplyTopButtonLookAndSize(Button target)
        {
            // Referenz suchen (irgendein existierender Top-Button, nicht unser About selbst)
            var refBtn = pnlTopButtons.Controls
                .OfType<Button>()
                .FirstOrDefault(b => b != target);

            if (refBtn != null)
            {
                // Größe übernehmen
                target.Height = refBtn.Height;
                target.MinimumSize = new Size(refBtn.MinimumSize.Width, refBtn.MinimumSize.Height);

                // Breite minimal so groß wie der Referenz-Buttontext – oder 90 px als Fallback
                target.Width = Math.Max(refBtn.Width, 90);

                // Margin & Padding übernehmen
                target.Margin  = refBtn.Margin;
                target.Padding = refBtn.Padding;

                target.FlatStyle = refBtn.FlatStyle;
                target.FlatAppearance.BorderSize = refBtn.FlatAppearance.BorderSize;
            }
            else
            {
                // Fallback – sanfte Defaults
                target.Height = 30;
                target.Width  = 90;
                target.Margin = new Padding(6, 3, 0, 3);
                target.Padding = new Padding(10, 6, 10, 6);
            }
        }

        private void UpdateAboutButtonStyle(bool dark)
        {
            if (_btnAbout == null || _btnAbout.IsDisposed) return;

            if (dark)
            {
                _btnAbout.BackColor = Color.FromArgb(58, 58, 60);
                _btnAbout.ForeColor = Color.WhiteSmoke;
                _btnAbout.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 82);
                _btnAbout.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 92);
            }
            else
            {
                _btnAbout.BackColor = SystemColors.ControlLight;
                _btnAbout.ForeColor = Color.Black;
                _btnAbout.FlatAppearance.MouseOverBackColor = SystemColors.ControlDark;
                _btnAbout.FlatAppearance.MouseDownBackColor = SystemColors.ControlDarkDark;
            }
        }

        private void UpdateAboutButtonLanguage(string lang)
        {
            if (_btnAbout == null || _btnAbout.IsDisposed) return;
            bool en = (lang?.ToLowerInvariant() == "en");
            _btnAbout.Text = en ? "About" : "Über";
            tips.SetToolTip(_btnAbout, en ? "About this app" : "Über dieses Programm");
        }

        private void ShowAboutDialog()
        {
            var ver = "1.5.1";
            var en = GetCurrentLanguageIsEnglish();
            var title = en ? "About" : "Über";
            var body = en
                ? $"Truck Modlist Importer\nVersion {ver}\n\nBy Winne (rore58)\nMade for the DanielDoubleU community."
                : $"Truck Modlist Importer\nVersion {ver}\n\nvon Winne (rore58)\nFür die DanielDoubleU-Community.";

            MessageBox.Show(this, body, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool GetCurrentLanguageIsEnglish()
            => (SettingsService.Load().Language?.ToLowerInvariant() == "en");
    }
}
