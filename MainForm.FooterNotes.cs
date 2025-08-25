// MainForm.FooterNotes.cs
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // ===== Footer-Felder (einheitliche Namen für alle Partial-Dateien) =====
        private TableLayoutPanel? _footerPanel;      // wird von UpdatePreviewBounds via Reflection gefunden
        private PictureBox? _footerLogo;
        private Label? _footerRightLabel;
        private Label? _footerCaptionLabel;          // NEU: Überschrift „Info zur Modliste“/„Modlist description“
        private TextBox? _txtFooterNotes;

        // Notes
        private string? _currentNotePath;
        private readonly System.Windows.Forms.Timer _noteSaveTimer = new System.Windows.Forms.Timer { Interval = 800 };

        /// <summary>
        /// Footer sicher bauen, Events verbinden, Notes/Logo/Theme initialisieren.
        /// Wird aus BuildFooterIfNeeded() (Hooks) aufgerufen.
        /// </summary>
        private void InitializeFooterNotes()
        {
            try
            {
                EnsureFooterBuilt();

                // Events (doppelte Anmeldungen vermeiden)
                try { cbList.SelectedIndexChanged -= CbList_SelectedIndexChanged_LoadNote; } catch { }
                cbList.SelectedIndexChanged += CbList_SelectedIndexChanged_LoadNote;

                try { cbGame.SelectedIndexChanged -= CbGame_SelectedIndexChanged_UpdateLogo; } catch { }
                cbGame.SelectedIndexChanged += CbGame_SelectedIndexChanged_UpdateLogo;

                if (_txtFooterNotes != null)
                {
                    try { _txtFooterNotes.TextChanged -= TxtFooterNotes_TextChanged; } catch { }
                    _txtFooterNotes.TextChanged += TxtFooterNotes_TextChanged;
                }

                try { _noteSaveTimer.Tick -= NoteSaveTimer_Tick; } catch { }
                _noteSaveTimer.Tick += NoteSaveTimer_Tick;

                try { this.FormClosing -= MainForm_FormClosing_SaveNote; } catch { }
                this.FormClosing += MainForm_FormClosing_SaveNote;

                // Initial laden
                LoadOrCreateNoteForCurrentModlist();
                UpdateFooterLogo();

                // Theme (falls SettingsService vorhanden)
                try { ApplyFooterThemeFromSettings(); } catch { }

                // Vorschau neu layouten
                try { UpdatePreviewBounds(); } catch { }
            }
            catch (Exception ex)
            {
                SafeSetStatus("Footer-Init-Fehler: " + ex.Message);
            }
        }

        // ===== Footer-Erstellung =====
        private void EnsureFooterBuilt()
        {
            if (_footerPanel != null && !_footerPanel.IsDisposed) return;
            BuildFooterPanel();
        }

        /// <summary>
        /// Footer: 3 Spalten (20% | 60% | 20%), feste Höhe ~4 cm.
        /// Mittlere Zelle enthält Mini-Layout: oben Caption-Label, darunter Textbox (füllt).
        /// </summary>
        private void BuildFooterPanel()
        {
            _footerPanel = new TableLayoutPanel
            {
                Name = "_footerPanel",
                Dock = DockStyle.Bottom,
                Height = CmToPixels(4.0),
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };

            _footerPanel.ColumnStyles.Clear();
            _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Logo
            _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Caption + Textbox
            _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Rechts-Label

            _footerPanel.RowStyles.Clear();
            _footerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Links: Logo
            _footerLogo = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                Margin = new Padding(4)
            };
            _footerPanel.Controls.Add(_footerLogo, 0, 0);

            // Mitte: Caption + Textbox in eigenem Layout
            var mid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(8)
            };
            mid.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Caption
            mid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Textbox

            _footerCaptionLabel = new Label
            {
                Text = "Info zur Modliste:",
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };
            mid.Controls.Add(_footerCaptionLabel, 0, 0);

            _txtFooterNotes = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            mid.Controls.Add(_txtFooterNotes, 0, 1);

            _footerPanel.Controls.Add(mid, 1, 0);

            // Rechts: Info-Label
            _footerRightLabel = new Label
            {
                Text = "Autor: Winnie (rore58)\r\nHergestellt für die DanielDoubleU Community",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Margin = new Padding(4)
            };
            _footerPanel.Controls.Add(_footerRightLabel, 2, 0);

            Controls.Add(_footerPanel);
            _footerPanel.BringToFront();
        }

        // ===== Events =====
        private void MainForm_FormClosing_SaveNote(object? sender, FormClosingEventArgs e)
        {
            TrySaveCurrentNote();
        }

        private void CbList_SelectedIndexChanged_LoadNote(object? sender, EventArgs e)
        {
            TrySaveCurrentNote();
            LoadOrCreateNoteForCurrentModlist();
        }

        private void CbGame_SelectedIndexChanged_UpdateLogo(object? sender, EventArgs e)
        {
            UpdateFooterLogo();
        }

        private void TxtFooterNotes_TextChanged(object? sender, EventArgs e)
        {
            _noteSaveTimer.Stop();
            _noteSaveTimer.Start(); // debounce
        }

        private void NoteSaveTimer_Tick(object? sender, EventArgs e)
        {
            _noteSaveTimer.Stop();
            TrySaveCurrentNote();
        }

        // ===== Notes-Logik =====
        private void LoadOrCreateNoteForCurrentModlist()
        {
            try
            {
                if (_txtFooterNotes == null) return;

                _currentNotePath = null;
                _txtFooterNotes.Text = "";

                var modlistPath = GetSelectedModlistPath(); // aus MainForm.ProfilesAndLists.cs
                if (string.IsNullOrWhiteSpace(modlistPath) || !File.Exists(modlistPath))
                {
                    SafeSetStatus("Keine Modliste gewählt – keine Notes geladen.");
                    return;
                }

                var notePath = Path.ChangeExtension(modlistPath, ".note");
                Directory.CreateDirectory(Path.GetDirectoryName(notePath)!);

                if (!File.Exists(notePath))
                    File.WriteAllText(notePath, "");

                _txtFooterNotes.Text = File.ReadAllText(notePath);
                _currentNotePath = notePath;

                SafeSetStatus($"Notes geladen: {Path.GetFileName(notePath)}");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Notes: " + ex.Message);
            }
        }

        private void TrySaveCurrentNote()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentNotePath) || _txtFooterNotes == null)
                    return;

                var text = _txtFooterNotes.Text ?? string.Empty;
                text = TrimTrailingBlankLines(text);

                Directory.CreateDirectory(Path.GetDirectoryName(_currentNotePath)!);
                File.WriteAllText(_currentNotePath, text);

                SafeSetStatus($"Notes gespeichert: {Path.GetFileName(_currentNotePath)}");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Speichern der Notes: " + ex.Message);
            }
        }

        private static string TrimTrailingBlankLines(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", "");
            var lines = s.Split('\n');
            int end = lines.Length;
            while (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1])) end--;
            return string.Join("\n", lines, 0, end);
        }

        // ===== Logo & Caption =====
        /// <summary>
        /// Wechselt das Logo je nach Spiel (ETS2/ATS). Sucht in /assets nach üblichen Dateinamen.
        /// </summary>
        private void UpdateFooterLogo()
        {
            try
            {
                if (_footerLogo == null) return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string assets = Path.Combine(baseDir, "assets");
                string? candidate = null;

                bool isATS = (cbGame.SelectedIndex == 1);
                string[] namesATS = { "ats.png", "ATS.png", "logo_ats.png" };
                string[] namesETS = { "ets2.png", "ETS2.png", "logo_ets2.png" };

                foreach (var n in isATS ? namesATS : namesETS)
                {
                    string p = Path.Combine(assets, n);
                    if (File.Exists(p)) { candidate = p; break; }
                }

                if (candidate != null)
                {
                    try
                    {
                        using var temp = Image.FromFile(candidate);
                        _footerLogo.Image?.Dispose();
                        _footerLogo.Image = new Bitmap(temp);
                    }
                    catch { _footerLogo.Image = null; }
                }
                else
                {
                    _footerLogo.Image = null;
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Beschriftung oberhalb des Textfeldes in DE/EN umschalten.
        /// </summary>
        private void UpdateFooterLanguage(string lang)
        {
            if (_footerCaptionLabel == null) return;
            _footerCaptionLabel.Text = (lang?.ToLowerInvariant() == "en")
                ? "Modlist description:"
                : "Info zur Modliste:";
        }

        // ===== Utils =====
        private int CmToPixels(double cm)
        {
            try
            {
                double dpi = this.DeviceDpi > 0 ? this.DeviceDpi : 96.0;
                return (int)Math.Round(cm * (dpi / 2.54));
            }
            catch
            {
                return (int)Math.Round(cm * (96.0 / 2.54));
            }
        }
    }
}
