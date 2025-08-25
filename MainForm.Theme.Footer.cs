// MainForm.Theme.Footer.cs
using System.Drawing;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Wendet die Theme-Farben auf den Footer an (Dark/Light).
        /// </summary>
        private void ApplyFooterTheme(bool dark)
        {
            if (_footerPanel == null || _footerPanel.IsDisposed) return;

            // Farben
            Color panelBack, panelFore, txtBack, txtFore, rightFore, logoBack, captionFore;
            if (dark)
            {
                panelBack = Color.FromArgb(30, 30, 30);
                panelFore = Color.Gainsboro;
                captionFore = Color.Gainsboro;
                txtBack   = Color.FromArgb(28, 28, 28);
                txtFore   = Color.WhiteSmoke;
                rightFore = Color.Gainsboro;
                logoBack  = Color.FromArgb(30, 30, 30);
            }
            else
            {
                panelBack = System.Drawing.SystemColors.Control;
                panelFore = System.Drawing.SystemColors.ControlText;
                captionFore = System.Drawing.SystemColors.ControlText;
                txtBack   = Color.White;
                txtFore   = Color.Black;
                rightFore = System.Drawing.SystemColors.ControlText;
                logoBack  = System.Drawing.SystemColors.Control;
            }

            // Panel
            _footerPanel.BackColor = panelBack;
            _footerPanel.ForeColor = panelFore;

            // Caption
            if (_footerCaptionLabel != null && !_footerCaptionLabel.IsDisposed)
            {
                _footerCaptionLabel.ForeColor = captionFore;
                _footerCaptionLabel.BackColor = panelBack;
            }

            // Textbox (Notes)
            if (_txtFooterNotes != null && !_txtFooterNotes.IsDisposed)
            {
                _txtFooterNotes.BackColor = txtBack;
                _txtFooterNotes.ForeColor = txtFore;
                _txtFooterNotes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            }

            // Rechts-Label
            if (_footerRightLabel != null && !_footerRightLabel.IsDisposed)
            {
                _footerRightLabel.ForeColor = rightFore;
                _footerRightLabel.BackColor = panelBack;
            }

            // Logo-Hintergrund
            if (_footerLogo != null && !_footerLogo.IsDisposed)
            {
                _footerLogo.BackColor = logoBack;
            }
        }

        /// <summary>
        /// Holt das aktuelle Setting und wendet es auf den Footer an.
        /// </summary>
        private void ApplyFooterThemeFromSettings()
        {
            try
            {
                var st = SettingsService.Load();
                ApplyFooterTheme(st.DarkMode);
            }
            catch
            {
                // ignorieren, falls SettingsService noch nicht existiert
            }
        }
    }
}
