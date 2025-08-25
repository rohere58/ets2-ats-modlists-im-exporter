// MainForm.Theme.cs
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Öffentliche API fürs Optionen-Menü
        public enum AppTheme { Light, Dark }

        private AppTheme _currentTheme = AppTheme.Light;

        /// <summary> Aus Optionen aufrufen: true = Dark, false = Light. </summary>
        private void SetTheme(bool dark) => ApplyTheme(dark ? AppTheme.Dark : AppTheme.Light);

        /// <summary> Intern: Thema anwenden (Form, Buttons, Preview, Status, Footer). </summary>
        private void ApplyTheme(AppTheme theme)
        {
            _currentTheme = theme;

            // Farbpalette
            var pal = theme == AppTheme.Dark
                ? new ThemePalette(
                    formBg: Color.FromArgb(32, 32, 35),
                    baseFg: Color.FromArgb(240, 240, 240),
                    subtleFg: Color.FromArgb(180, 180, 185),
                    panelBg: Color.FromArgb(42, 42, 46),
                    buttonBg: Color.FromArgb(50, 50, 55),
                    buttonHover: Color.FromArgb(64, 64, 70),
                    buttonBorder: Color.FromArgb(72, 72, 78),
                    editorBg: Color.FromArgb(28, 28, 30),
                    editorFg: Color.FromArgb(230, 230, 230))
                : new ThemePalette(
                    formBg: Color.FromArgb(243, 243, 243),
                    baseFg: Color.FromArgb(34, 34, 34),
                    subtleFg: Color.DimGray,
                    panelBg: Color.FromArgb(238, 238, 238),
                    buttonBg: Color.White,
                    buttonHover: Color.FromArgb(229, 229, 229),
                    buttonBorder: Color.LightGray,
                    editorBg: Color.WhiteSmoke,
                    editorFg: Color.Black);

            // Form
            BackColor = pal.FormBg;
            ForeColor = pal.BaseFg;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Alle Labels dezent
            foreach (var lab in Controls.OfType<Label>())
                lab.ForeColor = pal.BaseFg;

            // Combos & Checkboxes
            foreach (var combo in new[] { cbGame, cbProfile, cbList })
            {
                combo.Font = new Font("Segoe UI", 9F);
                combo.ForeColor = pal.BaseFg;
                combo.BackColor = theme == AppTheme.Dark ? pal.PanelBg : Color.White;
            }
            foreach (var chk in new[] { chkRaw, chkAutoDec })
            {
                chk.ForeColor = pal.BaseFg;
                chk.BackColor = Color.Transparent;
            }

            // Buttons
            var buttons = new[] { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions };
            foreach (var btn in buttons)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = pal.ButtonBg;
                btn.ForeColor = pal.BaseFg;
                btn.FlatAppearance.BorderColor = pal.ButtonBorder;
                btn.FlatAppearance.MouseOverBackColor = pal.ButtonHover;
                btn.Height = 32;
                btn.Margin = new Padding(0, 0, 8, 0);
            }

            // Preview (Editor)
            rtbPreview.Font = new Font("Consolas", 10F);
            rtbPreview.BackColor = pal.EditorBg;
            rtbPreview.ForeColor = pal.EditorFg;

            // Statusbar
            statusStrip.BackColor = pal.PanelBg;
            foreach (ToolStripItem it in statusStrip.Items)
            {
                it.ForeColor = it == lblVersion ? pal.SubtleFg : pal.BaseFg;
            }

            // Footer (falls vorhanden)
            try
            {
                // a) offizielles privates Feld aus MainForm.cs
                var fld = GetType().GetField("_footerPanel", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fld?.GetValue(this) is Control footerCtl)
                    footerCtl.BackColor = pal.PanelBg;
            }
            catch { /* ignore */ }

            // b) zusätzlich versuchen wir, ein in FooterNotes.cs erzeugtes Strong-Feld zu erreichen
            try
            {
                var fldStrong = GetType().GetField("_footerPanelStrong", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fldStrong?.GetValue(this) is Control footerStrong)
                    footerStrong.BackColor = pal.PanelBg;
            }
            catch { /* ignore */ }

            // Tooltips (WinForms ToolTip erbt nicht automatisch)
            try
            {
                tips.UseAnimation = true;
                tips.UseFading = true;
                // Farben von ToolTip kann man nur begrenzt steuern; wir lassen Standard.
            }
            catch { /* ignore */ }

            // Reflow sicherheitshalber
            try { UpdatePreviewBounds(); } catch { /* ignore */ }
        }

        // Datenträger für Farben
        private readonly struct ThemePalette
        {
            public ThemePalette(Color formBg, Color baseFg, Color subtleFg, Color panelBg, Color buttonBg, Color buttonHover, Color buttonBorder, Color editorBg, Color editorFg)
            {
                FormBg = formBg;
                BaseFg = baseFg;
                SubtleFg = subtleFg;
                PanelBg = panelBg;
                ButtonBg = buttonBg;
                ButtonHover = buttonHover;
                ButtonBorder = buttonBorder;
                EditorBg = editorBg;
                EditorFg = editorFg;
            }
            public Color FormBg { get; }
            public Color BaseFg { get; }
            public Color SubtleFg { get; }
            public Color PanelBg { get; }
            public Color ButtonBg { get; }
            public Color ButtonHover { get; }
            public Color ButtonBorder { get; }
            public Color EditorBg { get; }
            public Color EditorFg { get; }
        }
    }
}
