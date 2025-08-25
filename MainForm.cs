﻿// MainForm.cs — Single Source of Truth für UI + Events
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm : Form
    {
        // ================== App/Version ==================
        private const string AppVersion = "1.5.2";

        // ================== UI-Felder (einmalig hier!) ==================
        private readonly ComboBox cbGame    = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cbProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cbList    = new() { DropDownStyle = ComboBoxStyle.DropDownList };

        private readonly CheckBox chkRaw     = new() { Text = "Roh übernehmen (1:1 active_mods-Block)" };
        private readonly CheckBox chkAutoDec = new() { Text = "Auto-Decrypt mit SII_Decrypt.exe (falls Binär)" };

        private readonly Button btnLoad    = new() { Text = "Modliste laden", AutoSize = true };
        private readonly Button btnApply   = new() { Text = "Modliste übernehmen", AutoSize = true };
        private readonly Button btnOpen    = new() { Text = "Profilordner öffnen", AutoSize = true };
        private readonly Button btnExport  = new() { Text = "Modliste exportieren", AutoSize = true };
        private readonly Button btnCheck   = new() { Text = "Text-Check", AutoSize = true };
        private readonly Button btnDonate  = new() { Text = "Donate (Ko-fi)", AutoSize = true };
        private readonly Button btnRestore = new() { Text = "Backup wiederherstellen…", AutoSize = true };
        private readonly Button btnOptions = new() { Text = "Optionen…", AutoSize = true };

        private readonly FlowLayoutPanel pnlTopButtons = new()
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 2, 0, 2)
        };

        private readonly RichTextBox rtbPreview = new()
        {
            Multiline = true,
            DetectUrls = false,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = RichTextBoxScrollBars.ForcedVertical
        };

        private readonly StatusStrip statusStrip = new();
        private readonly ToolStripStatusLabel lblVersion = new();
        private readonly ToolStripStatusLabel lblStatus  = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        private readonly ToolTip tips = new() { AutomaticDelay = 250, AutoPopDelay = 8000, ReshowDelay = 200, ShowAlways = true };

        // Layout-Helfer
        private int previewTopY;

        // ================== EIN Konstruktor ==================
        public MainForm()
        {
            // Grundfenster
            Text = $"Truck Modlist Importer (ETS2 & ATS) v{AppVersion}";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 900);

            // Header
            var lblHeader = new Label
            {
                Text = "ETS2/ATS Modlist Importer",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 52
            };
            Controls.Add(lblHeader);

            // Obere Eingabefelder (Spiel / Profil / Liste)
            int y = 64;

            var lblGame = new Label { Location = new Point(20, y), Size = new Size(90, 24), Text = "Spiel:" };
            cbGame.Location = new Point(120, y - 2); cbGame.Size = new Size(320, 28);
            cbGame.Items.AddRange(new object[]
            {
                "Euro Truck Simulator 2 (ETS2)",
                "American Truck Simulator (ATS)"
            });
            Controls.Add(lblGame); Controls.Add(cbGame);
            y += 40;

            var lblProfile = new Label { Location = new Point(20, y), Size = new Size(90, 24), Text = "Profil:" };
            cbProfile.Location = new Point(120, y - 2); cbProfile.Size = new Size(860, 28);
            Controls.Add(lblProfile); Controls.Add(cbProfile);
            y += 40;

            var lblList = new Label { Location = new Point(20, y), Size = new Size(90, 24), Text = "Modliste:" };
            cbList.Location = new Point(120, y - 2); cbList.Size = new Size(860, 28);
            Controls.Add(lblList); Controls.Add(cbList);
            y += 36;

            // Optionen-Checkboxen
            chkRaw.Location     = new Point(20, y);      chkRaw.Size     = new Size(860, 24); chkRaw.Checked = true;
            chkAutoDec.Location = new Point(20, y + 26); chkAutoDec.Size = new Size(500, 24);
            Controls.Add(chkRaw); Controls.Add(chkAutoDec);
            y += 56;

            // Button-Reihe
            pnlTopButtons.Location = new Point(20, y);
            pnlTopButtons.Controls.AddRange(new Control[]
            { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions });
            Controls.Add(pnlTopButtons);
            y += pnlTopButtons.Height + 10;

            // Preview
            previewTopY = y;
            rtbPreview.Location = new Point(20, previewTopY);
            rtbPreview.Width = ClientSize.Width - 40;
            Controls.Add(rtbPreview);

            // Statusleiste
            lblVersion.Text = "Version: " + AppVersion;
            statusStrip.Items.Add(lblVersion);
            statusStrip.Items.Add(new ToolStripStatusLabel { Text = " | " });
            statusStrip.Items.Add(lblStatus);
            Controls.Add(statusStrip);

            // Tooltips
            tips.SetToolTip(cbGame,    "Spiel auswählen (ETS2/ATS)");
            tips.SetToolTip(cbProfile, "Profil auswählen");
            tips.SetToolTip(cbList,    "Vorhandene Modliste auswählen");
            tips.SetToolTip(chkRaw,    "Kompletten active_mods-Block 1:1 einfügen");
            tips.SetToolTip(chkAutoDec,"Wenn profile.sii binär ist, automatisch entschlüsseln (falls Tool vorhanden)");
            tips.SetToolTip(btnLoad,   "Ausgewählte Modliste in die Vorschau laden");
            tips.SetToolTip(btnApply,  "Modliste in das Profil schreiben");
            tips.SetToolTip(btnOpen,   "Ordner des ausgewählten Profils öffnen");
            tips.SetToolTip(btnExport, "active_mods aus Profil als .txt exportieren");
            tips.SetToolTip(btnCheck,  "Profil prüfen (Text/Binär, Vorschau)");
            tips.SetToolTip(btnDonate, "Ko-fi Seite öffnen");
            tips.SetToolTip(btnRestore,"Ein Backup wiederherstellen");
            tips.SetToolTip(btnOptions,"Optionen öffnen");

            // Events
            cbGame.SelectedIndexChanged += (s, e) =>
            {
                // Game-Wechsel → lokale Loader neu ausführen
                LoadProfiles_Local();
                LoadModlists_Local();

                SafeSetStatus($"Spiel gewechselt: {GetCurrentGameName()}");
            };

            btnLoad.Click += (s, e) => DoLoadSelectedModlistToPreview_Local();
            btnApply.Click += (s, e) => { TryInvokeNoArg("OnBeforeApply"); TryInvokeNoArg("DoApplyModlist"); };

            btnOpen.Click += (s, e) => DoOpenSelectedProfileFolder_Local();

            btnExport.Click  += (s, e) => TryInvokeNoArg("DoExportModlist");
            btnCheck.Click   += (s, e) => TryInvokeNoArg("DoCheckProfileFormat");
            btnRestore.Click += (s, e) => TryInvokeNoArg("DoRestoreBackup");
            btnDonate.Click  += (s, e) => TryInvokeWithArg("OpenUrl", "https://ko-fi.com/rore58");

            // Optionen: sauberer Fallback
            btnOptions.Click += (s, e) =>
            {
                if (!TryInvokeNoArg("ShowOptionsDialog"))
                    TryInvokeNoArg("ShowOptionsDialogSafe");
            };

            chkAutoDec.CheckedChanged += (s, e) =>
                TryInvokeWithArg("UpdateAutoDecryptSettingFromUI", chkAutoDec.Checked);

            Resize += (s, e) => UpdatePreviewBounds();
            Shown  += MainForm_Shown_Safe;

            // Grunddesign
            ApplyModernTheme();

            SafeSetStatus("Bereit.");
        }

        // ================== Shown/Initial-Load ==================
        private void MainForm_Shown_Safe(object? sender, EventArgs e)
        {
            TryInvokeNoArg("ApplyBranding");
            TryInvokeNoArg("RestoreWindowBounds");
            TryInvokeNoArg("BuildFooterIfNeeded");
            TryInvokeNoArg("AdjustFooterHeightTo4cm");

            UpdatePreviewBounds();

            if (cbGame.Items.Count > 0 && cbGame.SelectedIndex < 0)
                cbGame.SelectedIndex = 0;

            // NUR unsere lokalen Loader verwenden:
            LoadProfiles_Local();
            LoadModlists_Local();

            TryInvokeNoArg("OnAfterWireUp"); // optionaler Hook (partial)
        }

        // ================== Layout ==================
        private void UpdatePreviewBounds()
        {
            rtbPreview.Left = 20;
            rtbPreview.Top = previewTopY;
            rtbPreview.Width = ClientSize.Width - 40;

            int footerH = 0;
            try
            {
                var field = GetType().GetField("_footerPanel",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field?.GetValue(this) is Control footerCtl)
                    footerH = footerCtl.Height;
            }
            catch { /* ignore */ }

            int reservedBottom = statusStrip.Height + footerH + 16;
            int newHeight = ClientSize.Height - reservedBottom - previewTopY - 16;
            rtbPreview.Height = Math.Max(180, newHeight);
        }

        // ================== Design/Theme (nur Grundoptik; echtes Thema in Theme.cs) ==================
        private void ApplyModernTheme()
        {
            BackColor = Color.FromArgb(243, 243, 243);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            foreach (Control ctl in Controls)
                if (ctl is Label lab) lab.ForeColor = Color.FromArgb(34, 34, 34);

            cbGame.Font = cbProfile.Font = cbList.Font = new Font("Segoe UI", 9F);
            chkRaw.Font = chkAutoDec.Font = new Font("Segoe UI", 9F);

            foreach (var btn in new[] { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions })
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Color.White;
                btn.ForeColor = Color.Black;
                btn.FlatAppearance.BorderColor = Color.LightGray;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 229, 229);
                btn.Height = 32;
                btn.Margin = new Padding(0, 0, 8, 0);
            }

            rtbPreview.Font = new Font("Consolas", 10F);
            rtbPreview.BackColor = Color.WhiteSmoke;
            rtbPreview.ForeColor = Color.Black;

            statusStrip.BackColor = Color.FromArgb(238, 238, 238);
            lblVersion.ForeColor = Color.DimGray;
            lblStatus.ForeColor = Color.Black;

            try
            {
                var fld = GetType().GetField("_footerPanel", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fld?.GetValue(this) is Control footerCtl)
                    footerCtl.BackColor = Color.FromArgb(238, 238, 238);
            }
            catch { }
        }

        // ================== Helfer ==================
        private string GetCurrentGameName()
        {
            return cbGame.SelectedIndex == 1
                ? "American Truck Simulator (ATS)"
                : "Euro Truck Simulator 2 (ETS2)";
        }

        private void SafeSetStatus(string text)
        {
            try { lblStatus.Text = text ?? string.Empty; } catch { /* ignore */ }
        }

        private bool TryInvokeNoArg(string methodName)
        {
            try
            {
                var m = GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (m != null && m.GetParameters().Length == 0)
                {
                    m.Invoke(this, null);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private bool TryInvokeWithArg<T>(string methodName, T arg)
        {
            try
            {
                var m = GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null,
                    types: new Type[] { typeof(T) },
                    modifiers: null);

                if (m != null)
                {
                    m.Invoke(this, new object[] { arg! });
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
