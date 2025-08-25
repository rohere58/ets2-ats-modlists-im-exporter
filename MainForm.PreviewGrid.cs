// MainForm.PreviewGrid.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private DataGridView? _gridMods;
        private ContextMenuStrip? _gridMenu;

        // Spaltennamen
        private const string ColRaw      = "Raw";
        private const string ColPackage  = "Package";
        private const string ColName     = "Name";
        private const string ColLinkVal  = "LinkValue";  // versteckt (enthält die URL)
        private const string ColStatus   = "Status";     // ✓ / –
        private const string ColDownload = "Download";   // Button
        private const string ColTrucky   = "Trucky";
        private const string ColGoogle   = "Google";

        /// <summary>
        /// Baut bei Bedarf die tabellarische Vorschau.
        /// </summary>
        private void EnsurePreviewGridBuilt()
        {
            if (_gridMods != null && !_gridMods.IsDisposed) return;

            _gridMods = new DataGridView
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Location = rtbPreview.Location,
                Size = rtbPreview.Size,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackgroundColor = this.BackColor,
                GridColor = SystemColors.ControlDark
            };

            // Spalten definieren
            var cRaw = new DataGridViewTextBoxColumn
            {
                Name = ColRaw, HeaderText = "Raw", Visible = false
            };
            var cPkg = new DataGridViewTextBoxColumn
            {
                Name = ColPackage, HeaderText = "Package", Width = 240, FillWeight = 24
            };
            var cName = new DataGridViewTextBoxColumn
            {
                Name = ColName, HeaderText = "Mod", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 46
            };
            var cLinkVal = new DataGridViewTextBoxColumn
            {
                Name = ColLinkVal, HeaderText = "LinkValue", Visible = false
            };
            var cStatus = new DataGridViewTextBoxColumn
            {
                Name = ColStatus, HeaderText = " ", Width = 36
            };
            var cDownload = new DataGridViewButtonColumn
            {
                Name = ColDownload, HeaderText = "", Width = 100, UseColumnTextForButtonValue = false // Text pro Zelle
            };
            var cTrucky = new DataGridViewButtonColumn
            {
                Name = ColTrucky, HeaderText = "", Text = "Trucky", UseColumnTextForButtonValue = true, Width = 80
            };
            var cGoogle = new DataGridViewButtonColumn
            {
                Name = ColGoogle, HeaderText = "", Text = "Google", UseColumnTextForButtonValue = true, Width = 80
            };

            _gridMods.Columns.AddRange(cRaw, cPkg, cName, cLinkVal, cStatus, cDownload, cTrucky, cGoogle);

            // Status zentrieren
            _gridMods.Columns[ColStatus].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Events
            _gridMods.CellContentClick += GridMods_CellContentClick;
            _gridMods.CellMouseDown += GridMods_CellMouseDown_SelectOnRightClick;
            _gridMods.CellDoubleClick += GridMods_CellDoubleClick;
            _gridMods.Resize += GridMods_Resize_Reposition;
            this.Resize += (s, e) => GridMods_Resize_Reposition(s, e);

            // Einfügen & rtbPreview ausblenden
            Controls.Add(_gridMods);
            _gridMods.BringToFront();
            rtbPreview.Visible = false;
            _gridMods.Visible = true;

            ApplyGridThemeFromSettings();
            EnsureGridContextMenuBuilt();
        }

        private void GridMods_Resize_Reposition(object? sender, EventArgs e)
        {
            if (_gridMods == null) return;
            _gridMods.Left = rtbPreview.Left;
            _gridMods.Top = rtbPreview.Top;
            _gridMods.Width = rtbPreview.Width;
            _gridMods.Height = rtbPreview.Height;
        }

        private void ApplyGridThemeFromSettings()
        {
            try
            {
                var st = SettingsService.Load();
                bool dark = st.DarkMode;
                if (_gridMods == null) return;

                if (dark)
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
            }
            catch { }
        }

        /// <summary>
        /// Baut die Grid-Zeilen anhand des Textes in rtbPreview neu auf.
        /// Erkennt/füllt Links, setzt Status und Download-Button-Text.
        /// </summary>
        private void RebuildPreviewGridFromRtb()
        {
            EnsurePreviewGridBuilt();
            if (_gridMods == null) return;

            _gridMods.SuspendLayout();
            _gridMods.Rows.Clear();

            var text = rtbPreview.Text ?? string.Empty;
            var lines = text.Replace("\r", "").Split('\n');
            bool changedDb = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Nur Modzeilen mit erstem "..." berücksichtigen
                ExtractModKeys(line, out var modName, out var packageId, out var rawToken);
                if (string.IsNullOrWhiteSpace(rawToken))
                    continue;

                // Link lookup (Fallback-Kette)
                string url;
                if (!TryGetLinkWithFallback(line, modName, packageId, rawToken, out url) || string.IsNullOrWhiteSpace(url))
                {
                    // Steam-ID erraten
                    if (TryGuessSteamUrl(line, out var steamUrl))
                    {
                        var key = GetPreferredKey(modName, packageId, line);
                        _modLinks[key] = steamUrl;
                        url = steamUrl;
                        changedDb = true;
                    }
                    else
                    {
                        url = "";
                    }
                }

                int idx = _gridMods.Rows.Add();
                var row = _gridMods.Rows[idx];
                row.Cells[ColRaw].Value = raw;
                row.Cells[ColPackage].Value = packageId;
                row.Cells[ColName].Value = !string.IsNullOrWhiteSpace(modName) ? modName : packageId;
                row.Cells[ColLinkVal].Value = url;

                UpdateRowUi(idx, url);
            }

            if (changedDb)
                SaveLinksDbForCurrentGame();

            _gridMods.ResumeLayout();
            _gridMods.Refresh();
        }

        private void UpdateRowUi(int rowIndex, string? url)
        {
            if (_gridMods == null || rowIndex < 0 || rowIndex >= _gridMods.Rows.Count) return;
            var row = _gridMods.Rows[rowIndex];
            bool has = !string.IsNullOrWhiteSpace(url);

            row.Cells[ColStatus].Value = has ? "✓" : "–";
            row.Cells[ColDownload].Value = has ? "Download" : "—";
        }

        private void GridMods_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (_gridMods == null || e.RowIndex < 0) return;

            var col = _gridMods.Columns[e.ColumnIndex].Name;

            if (col == ColDownload)
            {
                var url = (_gridMods.Rows[e.RowIndex].Cells[ColLinkVal].Value ?? "").ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    OpenUrl(url!);
                return;
            }

            if (col == ColTrucky || col == ColGoogle)
            {
                var modName = (_gridMods.Rows[e.RowIndex].Cells[ColName].Value ?? "").ToString();
                var pkg = (_gridMods.Rows[e.RowIndex].Cells[ColPackage].Value ?? "").ToString();
                var query = !string.IsNullOrWhiteSpace(modName) ? modName : pkg;
                if (string.IsNullOrWhiteSpace(query)) return;

                string url = col == ColTrucky
                    ? $"https://truckyapp.com/?s={Uri.EscapeDataString(query)}"
                    : $"https://www.google.com/search?q={Uri.EscapeDataString(query + " ETS2 ATS mod")}";

                OpenUrl(url);
            }
        }

        private void GridMods_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (_gridMods == null || e.RowIndex < 0) return;

            var url = (_gridMods.Rows[e.RowIndex].Cells[ColLinkVal].Value ?? "").ToString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                OpenUrl(url!);
            }
            else
            {
                var modName = (_gridMods.Rows[e.RowIndex].Cells[ColName].Value ?? "").ToString();
                if (!string.IsNullOrWhiteSpace(modName))
                    OpenUrl($"https://truckyapp.com/?s={Uri.EscapeDataString(modName)}");
            }
        }

        // --- Kontextmenü ------------------------------------------------------

        private void EnsureGridContextMenuBuilt()
        {
            if (_gridMenu != null) return;

            _gridMenu = new ContextMenuStrip();
            var miOpen   = new ToolStripMenuItem("Im Browser öffnen");
            var miSet    = new ToolStripMenuItem("Link setzen…");
            var miRemove = new ToolStripMenuItem("Link entfernen");
            var miCopy   = new ToolStripMenuItem("Link kopieren");

            miOpen.Click   += (s, e) => CtxOpenSelected();
            miSet.Click    += (s, e) => CtxSetSelected();
            miRemove.Click += (s, e) => CtxRemoveSelected();
            miCopy.Click   += (s, e) => CtxCopySelected();

            _gridMenu.Items.AddRange(new ToolStripItem[] { miOpen, miSet, miRemove, miCopy });

            _gridMenu.Opening += (s, e) =>
            {
                // Ein-/Ausgrauen je nach Link
                var (ok, _, _, _, url) = TryGetSelectedRow();
                if (!ok) { e.Cancel = true; return; }

                miOpen.Enabled   = !string.IsNullOrWhiteSpace(url);
                miRemove.Enabled = !string.IsNullOrWhiteSpace(url);
                miCopy.Enabled   = !string.IsNullOrWhiteSpace(url);
            };

            if (_gridMods != null)
                _gridMods.ContextMenuStrip = _gridMenu;
        }

        private void GridMods_CellMouseDown_SelectOnRightClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (_gridMods == null) return;
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _gridMods.ClearSelection();
                _gridMods.Rows[e.RowIndex].Selected = true;
                _gridMods.CurrentCell = _gridMods.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
            }
        }

        private (bool ok, int rowIndex, string raw, string modName, string url) TryGetSelectedRow()
        {
            if (_gridMods == null || _gridMods.CurrentRow == null) return (false, -1, "", "", "");
            var r = _gridMods.CurrentRow;
            var raw = (r.Cells[ColRaw].Value ?? "").ToString() ?? "";
            var name = (r.Cells[ColName].Value ?? "").ToString() ?? "";
            var url = (r.Cells[ColLinkVal].Value ?? "").ToString() ?? "";
            return (true, r.Index, raw, name, url);
        }

        private void CtxOpenSelected()
        {
            var (ok, _, _, _, url) = TryGetSelectedRow();
            if (ok && !string.IsNullOrWhiteSpace(url)) OpenUrl(url);
        }

        private void CtxCopySelected()
        {
            var (ok, _, _, _, url) = TryGetSelectedRow();
            if (!ok || string.IsNullOrWhiteSpace(url)) return;
            try { Clipboard.SetText(url); SafeSetStatus("Link in Zwischenablage kopiert."); }
            catch { }
        }

        private void CtxRemoveSelected()
        {
            var (ok, rowIndex, raw, name, url) = TryGetSelectedRow();
            if (!ok) return;

            // Schlüssel präferieren (Name > Package > volle Zeile)
            ExtractModKeys(raw, out var modName, out var packageId, out _);
            var key = GetPreferredKey(modName, packageId, raw);

            bool removed = false;
            if (_modLinks.Remove(key)) removed = true;
            else
            {
                // Fallbacks entsorgen, falls unter anderem Schlüssel gespeichert
                removed |= _modLinks.Remove(raw.Trim());
                if (!string.IsNullOrWhiteSpace(modName)) removed |= _modLinks.Remove(modName.Trim());
                if (!string.IsNullOrWhiteSpace(packageId)) removed |= _modLinks.Remove(packageId.Trim());
            }

            if (removed)
            {
                SaveLinksDbForCurrentGame();
                // UI zurücksetzen
                _gridMods!.Rows[rowIndex].Cells[ColLinkVal].Value = "";
                UpdateRowUi(rowIndex, "");
                SafeSetStatus($"Link entfernt für „{name}“.");
            }
        }

        private void CtxSetSelected()
        {
            var (ok, rowIndex, raw, name, _) = TryGetSelectedRow();
            if (!ok) return;

            var url = PromptForUrl($"URL für „{name}“ eingeben:", "");
            if (string.IsNullOrWhiteSpace(url)) return;

            // Validierung
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) ||
                (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show(this, "Bitte eine gültige http/https-URL eingeben.", "Ungültige URL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Schlüssel bestimmen und speichern
            ExtractModKeys(raw, out var modName, out var packageId, out _);
            var key = GetPreferredKey(modName, packageId, raw);
            _modLinks[key] = url;
            SaveLinksDbForCurrentGame();

            // UI aktualisieren
            _gridMods!.Rows[rowIndex].Cells[ColLinkVal].Value = url;
            UpdateRowUi(rowIndex, url);
            SafeSetStatus($"Link gesetzt für „{name}“.");
        }

        private string? PromptForUrl(string title, string defaultValue)
        {
            using var dlg = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(520, 120)
            };
            var txt = new TextBox { Left = 12, Top = 20, Width = 496, Text = defaultValue };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 336, Width = 80, Top = 60 };
            var btnCancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Left = 428, Width = 80, Top = 60 };
            dlg.Controls.AddRange(new Control[] { txt, btnOk, btnCancel });
            dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;

            return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
