// MainForm.cs — Single Source of Truth für UI + Events
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TruckModImporter
{
    public partial class MainForm : Form
    {
        // ================== App/Version ==================
        private const string AppVersion = "3.1";
        private const string MODLISTS_ROOT = @"C:\Users\Winnie\Desktop\Truck Mod Importer\modlists";

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
        private readonly Button btnOpenModlists = new() { Text = "Modlisten-Ordner öffnen", AutoSize = true };

        // Profile buttons
        private Button? btnProfClone;
        private Button? btnProfRename;
        private Button? btnProfDelete;
        private Control? _profileHeaderContainer;
        private bool _profileInitDone;
        private bool _profileRelayoutWired;

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

        // ================== App-Daten ==================
        private string profilesRoot = ""; // Wurzelverzeichnis für Profile (je nach Spiel)

        // ================== EIN Konstruktor ==================
        public MainForm()
        {
            InitializeComponent();

            // Make sure reverse + renumber also run on the very first auto-load:
            this.Shown += (_, __) => BeginInvoke(new Action(() =>
            {
                try { PreviewOrder_Run(reverse: true, numberFromTopOne: true); } catch {}
            }));

            // Grundfenster
            Text = $"ETS2/ATS Modlist Importer/Exporter v{AppVersion}";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 900);

            // Header
            var lblHeader = new Label
            {
                Text = "ETS2/ATS Modlist Importer/Exporter",
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
            pnlTopButtons.Controls.AddRange(new Control[] { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions, btnOpenModlists });
            Controls.Add(pnlTopButtons);
            btnOpenModlists.Click += (s, e) => OpenSelectedModlistsFolder();
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
            tips.SetToolTip(btnRestore, "Ein Backup wiederherstellen");
            tips.SetToolTip(btnOptions,"Optionen öffnen");
            tips.SetToolTip(btnOpenModlists, "Vorhandene Modlisten öffnen");

            // Events
            cbGame.SelectedIndexChanged += (s, e) =>
            {
                UpdateProfilesRoot();
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

            // Einmal verdrahten:
            WireFolderAndRestoreButtons();

            // Fügen Sie dies in Ihren Initialisierungscode ein (z.B. nach LoadModlists_Local oder in OnAfterWireUp/ApplyBranding):
            try { cbList.SelectionChangeCommitted -= CbList_AutoLoad; } catch {}
            cbList.SelectionChangeCommitted += CbList_AutoLoad;

            // Optional: auch SelectedIndexChanged behandeln, um programmatische Änderungen zu erfassen
            try { cbList.SelectedIndexChanged -= CbList_AutoLoad; } catch {}
            cbList.SelectedIndexChanged += CbList_AutoLoad;

            cbGame.SelectedIndexChanged += OnGameOrModlistChanged;
            cbGame.TextChanged         += OnGameOrModlistChanged;
            cbList.SelectedIndexChanged += OnGameOrModlistChanged;
            cbList.TextChanged         += OnGameOrModlistChanged;

            SafeSetStatus("Bereit.");
        }

        private void InitializeComponent()
        {
            // Diese Methode bleibt leer, da Sie alle Steuerelemente manuell im Konstruktor initialisieren.
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
            UpdateProfilesRoot();
            LoadProfiles_Local();
            LoadModlists_Local();

            TryInvokeNoArg("OnAfterWireUp"); // optionaler Hook (partial)

            // Backup-Button nach anderen Buttons initialisieren und positionieren
            try { EnsureBackupProfilesButton(); } catch { }
            try { PositionBackupProfilesButton(); } catch { }
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

            foreach (var btn in new[] { btnLoad, btnApply, btnOpen, btnExport, btnCheck, btnDonate, btnRestore, btnOptions, btnOpenModlists })
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

        private void UpdateProfilesRoot()
        {
            var st = SettingsService.Load();
            if (cbGame.SelectedIndex == 1 && !string.IsNullOrWhiteSpace(st.AtsProfilesPath))
            {
                profilesRoot = st.AtsProfilesPath;
            }
            else if (cbGame.SelectedIndex == 0 && !string.IsNullOrWhiteSpace(st.Ets2ProfilesPath))
            {
                profilesRoot = st.Ets2ProfilesPath;
            }
            else
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                profilesRoot = cbGame.SelectedIndex == 1
                    ? Path.Combine(docs, "American Truck Simulator", "profiles")
                    : Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
            }
        }

        private void LoadProfiles_Local()
        {
            cbProfile.Items.Clear();
            if (string.IsNullOrWhiteSpace(profilesRoot) || !System.IO.Directory.Exists(profilesRoot))
                return;

            foreach (var dir in Directory.GetDirectories(profilesRoot))
            {
                string siiPath = Path.Combine(dir, "profile.sii");
                string name = Path.GetFileName(dir); // Fallback: Ordnername
                if (File.Exists(siiPath))
                {
                    string[] lines = File.ReadAllLines(siiPath);
                    foreach (var line in lines)
                    {
                        var match = Regex.Match(line, @"profile_name:\s*""([^""]+)""");
                        if (match.Success)
                        {
                            name = match.Groups[1].Value;
                            break;
                        }
                    }
                }
                cbProfile.Items.Add(new ProfileItem { Name = name, Path = dir });
            }
        }

        private void CreateBackupWithRetention(string path, int maxBackups = 10)
        {
            // TODO: Backup-Logik implementieren
        }


        private bool GetCurrentLanguageIsEnglish()
        {
            // TODO: Passe ggf. an deine Spracheinstellungen an
            return true;
        }

        private void CbList_AutoLoad(object? sender, EventArgs e)
        {
            DoLoadSelectedModlistToPreview_Local();
        }

        private void OnGameOrModlistChanged(object? sender, EventArgs e)
        {
            var gameText    = cbGame?.Text?.Trim() ?? string.Empty;
            var modlistText = cbList?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameText) || string.IsNullOrWhiteSpace(modlistText))
                return;

            TruckModImporter.SafeLinkJson.WriteForModlistRoot(MODLISTS_ROOT, gameText, modlistText);
        }

        private Control? ResolveProfileHeaderContainer()
        {
            try { if (cbProfile != null && cbProfile.Parent != null) return cbProfile.Parent; } catch {}
            try { return pnlTopButtons; } catch {}
            return this;
        }

        private void RepositionHeaderProfileButtons()
        {
            _profileHeaderContainer ??= (cbProfile?.Parent ?? pnlTopButtons ?? (Control)this);
            if (_profileHeaderContainer == null || _profileHeaderContainer.IsDisposed) return;

            const int spacing = 8;
            Control? anchor = cbProfile;

            int x = (anchor != null) ? anchor.Right + spacing : 8;
            int baselineY = (anchor != null) ? anchor.Top : 8;

            void place(Button? b)
            {
                if (b == null || b.IsDisposed || !b.Visible) return;
                int y = baselineY + Math.Max(0, ((anchor?.Height ?? b.Height) - b.Height) / 2) - 2;
                b.Location = new System.Drawing.Point(x, y);
                b.BringToFront();
                x = b.Right + spacing;
            }

            place(btnProfClone);
            place(btnProfRename);
            place(btnProfDelete);
        }

        private void Profile_WireHeaderRelayout()
        {
            if (_profileRelayoutWired) return;
            _profileRelayoutWired = true;

            var container = _profileHeaderContainer ?? ResolveProfileHeaderContainer();
            if (container == null || container.IsDisposed) return;

            container.Resize -= HeaderRelayout_ForProfile;
            container.Resize += HeaderRelayout_ForProfile;

            if (cbProfile != null && !cbProfile.IsDisposed)
            {
                cbProfile.LocationChanged -= HeaderRelayout_ForProfile;
                cbProfile.SizeChanged     -= HeaderRelayout_ForProfile;
                cbProfile.LocationChanged += HeaderRelayout_ForProfile;
                cbProfile.SizeChanged     += HeaderRelayout_ForProfile;
            }
            if (btnProfClone != null)  { btnProfClone.SizeChanged  -= HeaderRelayout_ForProfile; btnProfClone.SizeChanged  += HeaderRelayout_ForProfile; }
            if (btnProfRename != null) { btnProfRename.SizeChanged -= HeaderRelayout_ForProfile; btnProfRename.SizeChanged += HeaderRelayout_ForProfile; }
            if (btnProfDelete != null) { btnProfDelete.SizeChanged -= HeaderRelayout_ForProfile; btnProfDelete.SizeChanged += HeaderRelayout_ForProfile; }
        }

        private void HeaderRelayout_ForProfile(object? s, EventArgs e) => RepositionHeaderProfileButtons();

        private void EnsureHeaderProfileButtons()
        {
            if (_profileInitDone)
            {
                _profileHeaderContainer ??= ResolveProfileHeaderContainer();
                RepositionHeaderProfileButtons();
                return;
            }

            _profileHeaderContainer = ResolveProfileHeaderContainer();
            if (_profileHeaderContainer == null || _profileHeaderContainer.IsDisposed) return;

            // create if missing
            if (btnProfClone == null || btnProfClone.IsDisposed)
            {
                btnProfClone = new Button
                {
                    Name = "btnProfClone",
                    Text = GetCurrentLanguageIsEnglish() ? "Clone profile" : "Profil klonen",
                    AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true, Visible = true
                };
                btnProfClone.Click -= BtnProfClone_Click;
                btnProfClone.Click += BtnProfClone_Click;
            }
            if (btnProfRename == null || btnProfRename.IsDisposed)
            {
                btnProfRename = new Button
                {
                    Name = "btnProfRename",
                    Text = GetCurrentLanguageIsEnglish() ? "Rename profile" : "Profil umbenennen",
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
                    Text = GetCurrentLanguageIsEnglish() ? "Delete profile" : "Profil löschen",
                    AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true, Visible = true
                };
                btnProfDelete.Click -= BtnProfDelete_Click;
                btnProfDelete.Click += BtnProfDelete_Click;
            }

            // parent into header
            if (!ReferenceEquals(btnProfClone.Parent, _profileHeaderContainer))
            {
                try { btnProfClone.Parent?.Controls.Remove(btnProfClone); } catch {}
                _profileHeaderContainer.Controls.Add(btnProfClone);
            }
            if (!ReferenceEquals(btnProfRename.Parent, _profileHeaderContainer))
            {
                try { btnProfRename.Parent?.Controls.Remove(btnProfRename); } catch {}
                _profileHeaderContainer.Controls.Add(btnProfRename);
            }
            if (!ReferenceEquals(btnProfDelete.Parent, _profileHeaderContainer))
            {
                try { btnProfDelete.Parent?.Controls.Remove(btnProfDelete); } catch {}
                _profileHeaderContainer.Controls.Add(btnProfDelete);
            }

            // initial layout
            RepositionHeaderProfileButtons();

            // theme + l10n
            try { EnsureHeaderProfile_Theme(); } catch {}
            try { EnsureHeaderProfile_L10n(GetCurrentLanguageIsEnglish() ? "en" : "de"); } catch {}

            // wire future relayout
            try { Profile_WireHeaderRelayout(); } catch {}

            // one more pass after idle
            try { BeginInvoke(new Action(RepositionHeaderProfileButtons)); } catch {}

            _profileInitDone = true;
        }

        private void EnsureHeaderProfile_L10n(string lang)
        {
            bool en = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            if (btnProfClone  != null && !btnProfClone.IsDisposed)
                btnProfClone.Text  = en ? "Clone profile"  : "Profil klonen";
            if (btnProfRename != null && !btnProfRename.IsDisposed)
                btnProfRename.Text = en ? "Rename" : "Umbenennen";
            if (btnProfDelete != null && !btnProfDelete.IsDisposed)
                btnProfDelete.Text = en ? "Remove" : "Entfernen";
}

private void EnsureHeaderProfile_Theme()
{
    try
    {
        if (btnProfClone  != null)  MatchTopButtonLook(btnProfClone);
        if (btnProfRename != null) MatchTopButtonLook(btnProfRename);
        if (btnProfDelete != null) MatchTopButtonLook(btnProfDelete);

        foreach (var b in new[]{ btnProfClone, btnProfRename, btnProfDelete })
        {
            if (b == null) continue;
            b.Padding = new Padding(0, 2, 0, 2);
            b.Invalidate();
        }
    }
    catch { }
}
    }
}
