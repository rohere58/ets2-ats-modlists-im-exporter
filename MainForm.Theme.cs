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
            var buttons = new[] { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions, btnOpenModlists };
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

            // --- style dynamic backup button exactly like the others ---
            var btnBackupProfiles = Controls.Find("btnBackupProfiles", true).FirstOrDefault() as Button;
            if (btnBackupProfiles != null)
            {
                btnBackupProfiles.FlatStyle = FlatStyle.Flat;
                btnBackupProfiles.BackColor = pal.ButtonBg;
                btnBackupProfiles.ForeColor = pal.BaseFg;
                btnBackupProfiles.FlatAppearance.BorderColor = pal.ButtonBorder;
                btnBackupProfiles.FlatAppearance.MouseOverBackColor = pal.ButtonHover;
                btnBackupProfiles.Height = 32;
                btnBackupProfiles.Margin = new Padding(0, 0, 8, 0);
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

            // b) additionally try to access a Strong field generated in FooterNotes.cs
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

            if (_gridMods != null)
            {
                if (theme == AppTheme.Dark)
                {
                    _gridMods.BackgroundColor = Color.FromArgb(28, 28, 28);
                    _gridMods.DefaultCellStyle.BackColor = Color.FromArgb(28, 28, 28);
                    _gridMods.DefaultCellStyle.ForeColor = Color.WhiteSmoke;
                    _gridMods.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
                    _gridMods.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
                    _gridMods.GridColor = Color.FromArgb(64, 64, 64);
                }
                else
                {
                    _gridMods.BackgroundColor = SystemColors.Window;
                    _gridMods.DefaultCellStyle.BackColor = SystemColors.Window;
                    _gridMods.DefaultCellStyle.ForeColor = SystemColors.WindowText;
                    _gridMods.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                    _gridMods.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
                    _gridMods.GridColor = SystemColors.ControlDark;
                }
                _gridMods.EnableHeadersVisualStyles = false;
                _gridMods.CellPainting += GridMods_CellPainting_ThemeButtons;
            }

            // Am ENDE von ApplyThemeToDynamicButtons()
            if (btnListShare != null)  MatchTopButtonLook(btnListShare);
            if (btnListImport != null) MatchTopButtonLook(btnListImport);
            try { RepositionHeaderShareImport(); } catch { }

            // Am ENDE von ApplyThemeToDynamicButtons() anhängen, falls noch nicht vorhanden:
            try
            {
                if (btnListDelete != null && !btnListDelete.IsDisposed)
                    MatchTopButtonLook(btnListDelete);
            }
            catch { }
            // Keine Positionsänderung hier!

            // ===== [HeaderButtons:UniformHeight] BEGIN =====
            try
            {
                var headerButtons = new[] { btnListShare, btnListImport, btnListDelete }
                    .Where(b => b != null && !b.IsDisposed && b.Visible)
                    .ToArray();

                if (headerButtons.Length > 0)
                {
                    // 1) Harmonize padding for consistent visual height
                    //    (adjust these values if your header style uses other paddings)
                    foreach (var b in headerButtons)
                    {
                        // Only set if different to avoid unnecessary layout churn
                        if (b!.Padding.Top != 4 || b.Padding.Bottom != 4 || b.Padding.Left != 6 || b.Padding.Right != 6)
                            b.Padding = new Padding(6, 4, 6, 4);
                    }

                    // 2) Compute the maximum preferred height after padding normalization
                    //    PreferredSize respects AutoSize and Font; good for equal visual height
                    int targetHeight = headerButtons
                        .Select(b => b!.PreferredSize.Height)
                        .DefaultIfEmpty(0)
                        .Max();

                    // Safety floor
                    if (targetHeight < 24) targetHeight = 24;

                    // 3) Enforce minimum height so AutoSize buttons won't shrink below it
                    foreach (var b in headerButtons)
                    {
                        var min = b!.MinimumSize;
                        if (min.Height != targetHeight)
                            b.MinimumSize = new Size(min.Width, targetHeight);

                        // If AutoSize is OFF for any button, also set Height directly
                        if (b.AutoSize == false && b.Height != targetHeight)
                            b.Height = targetHeight;
                    }

                    // 4) Ask layout to recompute; positions are handled elsewhere
                    foreach (var b in headerButtons) b!.PerformLayout();

                    // 5) Optional: trigger your header re-layout (no-op if not present)
                    try { RepositionHeaderShareImport(); } catch { }
                }
            }
            catch { }
            // ===== [HeaderButtons:UniformHeight] END =====
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

        /// <summary>
        /// Überträgt das Aussehen der Top-Buttons auf einen anderen Button.
        /// </summary>
        private void MatchTopButtonLook(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = _currentTheme == AppTheme.Dark
                ? Color.FromArgb(50, 50, 55)
                : Color.White;
            btn.ForeColor = _currentTheme == AppTheme.Dark
                ? Color.FromArgb(240, 240, 240)
                : Color.FromArgb(34, 34, 34);
            btn.FlatAppearance.BorderColor = _currentTheme == AppTheme.Dark
                ? Color.FromArgb(72, 72, 78)
                : Color.LightGray;
            btn.FlatAppearance.MouseOverBackColor = _currentTheme == AppTheme.Dark
                ? Color.FromArgb(64, 64, 70)
                : Color.FromArgb(229, 229, 229);
            btn.Height = 32;
            btn.Margin = new Padding(0, 0, 8, 0);
        }

    }
}
