// MainForm.Hooks.cs
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // --- Branding / Start ---

        private void ApplyBranding()
        {
            // Falls alte Checkboxen irgendwie noch vorhanden sind: verstecken
            try
            {
                if (chkAutoDec != null) chkAutoDec.Visible = false;
                if (chkRaw    != null) chkRaw.Visible     = false;
            }
            catch { /* egal */ }

            var st = SettingsService.Load();

            SetTheme(st.DarkMode);
            ApplyFooterTheme(st.DarkMode);
            ApplyGridTheme(st.DarkMode);        // Grid (Modliste) mitthemen
            ApplyLanguage(st.Language);

            // Buttons sicherstellen
            EnsureAboutButton();
            EnsureDeleteModlistButton();
            UpdateAboutButtonStyle(st.DarkMode);
            UpdateDeleteButtonStyle(st.DarkMode);
            UpdateAboutButtonLanguage(st.Language);
            UpdateDeleteButtonLanguage(st.Language);

            // Platz schaffen, damit die gesamte Buttonleiste sichtbar ist
            EnsureFormWidthForTopButtons();

            // Position des Lösch-Buttons neben der Modlisten-Combo
            PositionDeleteModlistButton();
        }

        private void RestoreWindowBounds() { }

        private void BuildFooterIfNeeded()
        {
            InitializeFooterNotes();
        }

        private void AdjustFooterHeightTo4cm()
        {
            // Höhe ist in BuildFooterPanel() gesetzt; hier wäre Platz für spätere Dynamik
        }

        // --- Optionen-Dialog -> Theme/Language/Paths ---

        private void UpdateThemeFromOptions(bool dark)
        {
            SetTheme(dark);
            ApplyFooterTheme(dark);
            ApplyGridTheme(dark);

            UpdateAboutButtonStyle(dark);
            UpdateDeleteButtonStyle(dark);

            var st = SettingsService.Load();
            st.DarkMode = dark;
            SettingsService.Save(st);

            Invalidate();
            Refresh();
        }

        private void UpdateLanguageFromOptions(string lang)
        {
            ApplyLanguage(lang);
            UpdateFooterLanguage(lang);

            UpdateAboutButtonLanguage(lang);
            UpdateDeleteButtonLanguage(lang);

            var st = SettingsService.Load();
            st.Language = lang;
            SettingsService.Save(st);
        }

        private void UpdatePathsFromOptions(string[] paths)
        {
            if (paths is { Length: >= 2 })
            {
                var st = SettingsService.Load();
                st.Ets2ProfilesPath = paths[0] ?? "";
                st.AtsProfilesPath  = paths[1] ?? "";
                SettingsService.Save(st);
            }
        }

        // --- Nach dem Verdrahten (Shown) ---

        private void OnAfterWireUp()
        {
            InitializeModLinks();

            EnsureFormWidthForTopButtons();
            PositionDeleteModlistButton();

            // Reaktionen auf Größenänderungen
            try { pnlTopButtons.SizeChanged -= PnlTopButtons_SizeChanged_AdjustFormWidth; } catch { }
            pnlTopButtons.SizeChanged += PnlTopButtons_SizeChanged_AdjustFormWidth;

            try { ResizeEnd -= Form_ResizeEnd_AdjustFormWidthAndDeletePos; } catch { }
            ResizeEnd += Form_ResizeEnd_AdjustFormWidthAndDeletePos;

            try { Resize -= Form_Resize_UpdateDeletePos; } catch { }
            Resize += Form_Resize_UpdateDeletePos;
        }

        private void PnlTopButtons_SizeChanged_AdjustFormWidth(object? sender, EventArgs e)
            => EnsureFormWidthForTopButtons();

        private void Form_ResizeEnd_AdjustFormWidthAndDeletePos(object? sender, EventArgs e)
        {
            EnsureFormWidthForTopButtons();
            PositionDeleteModlistButton();
        }

        private void Form_Resize_UpdateDeletePos(object? sender, EventArgs e)
            => PositionDeleteModlistButton();

        // --- Browser-Öffner ---

        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(this, "Ungültige URL.", "Link öffnen",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        // --- Fenster an Buttonleiste anpassen ---

        private void EnsureFormWidthForTopButtons()
        {
            try
            {
                if (pnlTopButtons == null || pnlTopButtons.IsDisposed) return;

                int leftMargin = pnlTopButtons.Left;
                int desiredClientWidth = leftMargin + pnlTopButtons.PreferredSize.Width + 20; // rechter Puffer
                int nonClient = Width - ClientSize.Width;
                int desiredFormWidth = desiredClientWidth + nonClient;

                int minW = Math.Max(MinimumSize.Width, desiredFormWidth);
                int minH = Math.Max(MinimumSize.Height, 300);
                MinimumSize = new Size(minW, minH);

                if (Width < desiredFormWidth)
                    Width = desiredFormWidth;
            }
            catch { /* egal */ }
        }
    }
}
