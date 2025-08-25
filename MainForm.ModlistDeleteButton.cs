// MainForm.ModlistDelete.cs
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private Button? _btnDeleteModlist;

        private void EnsureDeleteModlistButton()
        {
            if (_btnDeleteModlist != null && !_btnDeleteModlist.IsDisposed) return;

            _btnDeleteModlist = new Button
            {
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Text = GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                TabStop = false
            };
            _btnDeleteModlist.FlatAppearance.BorderSize = 0;
            _btnDeleteModlist.Click += (s, e) => DeleteSelectedModlist();

            // Wichtig: in das gleiche Parent-Container wie die ComboBox,
            // damit Z-Order/Clipping stimmt und keine Überdeckung entsteht
            var parent = cbList.Parent ?? this;
            parent.Controls.Add(_btnDeleteModlist);
            _btnDeleteModlist.BringToFront();
            cbList.BringToFront(); // Combo im Zweifelsfall drüber

            // Größe und Stil an einen vorhandenen Top-Button angleichen
            ApplyTopButtonLookAndSizeForDelete(_btnDeleteModlist);

            // Initial positionieren
            PositionDeleteModlistButton();

            // Events für Repositionierung
            try { cbList.SizeChanged -= CbList_SizeOrPosChanged; } catch { }
            try { cbList.LocationChanged -= CbList_SizeOrPosChanged; } catch { }
            cbList.SizeChanged += CbList_SizeOrPosChanged;
            cbList.LocationChanged += CbList_SizeOrPosChanged;

            try { parent.SizeChanged -= Parent_SizeChanged_ReposDelete; } catch { }
            parent.SizeChanged += Parent_SizeChanged_ReposDelete;

            UpdateDeleteButtonStyle(SettingsService.Load().DarkMode);
            UpdateDeleteButtonLanguage(SettingsService.Load().Language);
        }

        private void ApplyTopButtonLookAndSizeForDelete(Button target)
        {
            // Referenz: nimm irgendeinen Button aus der Top-Leiste (gleicher Look)
            var refBtn = pnlTopButtons.Controls.OfType<Button>().FirstOrDefault();
            if (refBtn != null)
            {
                target.Height = cbList.Height;                 // an Combo-Höhe ausrichten
                target.Width  = Math.Max(90, refBtn.Width / 2);// kompakt
                target.Margin = refBtn.Margin;
                target.Padding = refBtn.Padding;
                target.FlatStyle = refBtn.FlatStyle;
                target.FlatAppearance.BorderSize = refBtn.FlatAppearance.BorderSize;
            }
            else
            {
                target.Height = cbList.Height;
                target.Width  = 95;
                target.Margin = new Padding(3);
                target.Padding = new Padding(8, 4, 8, 4);
            }
        }

        private void CbList_SizeOrPosChanged(object? sender, EventArgs e) => PositionDeleteModlistButton();
        private void Parent_SizeChanged_ReposDelete(object? sender, EventArgs e) => PositionDeleteModlistButton();

        private void PositionDeleteModlistButton()
        {
            if (_btnDeleteModlist == null || _btnDeleteModlist.IsDisposed) return;

            var parent = cbList.Parent ?? this;

            // knapp rechts neben der Combo
            _btnDeleteModlist.Height = cbList.Height;
            _btnDeleteModlist.Top = cbList.Top; // exakt gleiche Y-Position
            _btnDeleteModlist.Left = cbList.Right + 8;

            // nicht aus dem Parent herauslaufen lassen
            int rightPadding = 8;
            if (_btnDeleteModlist.Right > parent.ClientSize.Width - rightPadding)
            {
                _btnDeleteModlist.Left = Math.Max(cbList.Right + 4,
                    parent.ClientSize.Width - rightPadding - _btnDeleteModlist.Width);
            }
        }

        private void UpdateDeleteButtonStyle(bool dark)
        {
            if (_btnDeleteModlist == null || _btnDeleteModlist.IsDisposed) return;

            if (dark)
            {
                _btnDeleteModlist.BackColor = Color.FromArgb(58, 58, 60);
                _btnDeleteModlist.ForeColor = Color.WhiteSmoke;
                _btnDeleteModlist.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 82);
                _btnDeleteModlist.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 92);
            }
            else
            {
                _btnDeleteModlist.BackColor = SystemColors.ControlLight;
                _btnDeleteModlist.ForeColor = Color.Black;
                _btnDeleteModlist.FlatAppearance.MouseOverBackColor = SystemColors.ControlDark;
                _btnDeleteModlist.FlatAppearance.MouseDownBackColor = SystemColors.ControlDarkDark;
            }

            tips.SetToolTip(_btnDeleteModlist,
                GetCurrentLanguageIsEnglish()
                    ? "Delete the selected mod list (and its .note file)"
                    : "Ausgewählte Modliste (und .note) löschen");
        }

        private void UpdateDeleteButtonLanguage(string lang)
        {
            if (_btnDeleteModlist == null || _btnDeleteModlist.IsDisposed) return;
            bool en = (lang?.ToLowerInvariant() == "en");
            _btnDeleteModlist.Text = en ? "Delete" : "Löschen";
            tips.SetToolTip(_btnDeleteModlist,
                en ? "Delete the selected mod list (and its .note file)"
                   : "Ausgewählte Modliste (und .note) löschen");
        }

        private void DeleteSelectedModlist()
        {
            try
            {
                var path = GetSelectedModlistPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    SafeSetStatus(GetCurrentLanguageIsEnglish()
                        ? "No mod list selected."
                        : "Keine Modliste ausgewählt.");
                    return;
                }

                var en = GetCurrentLanguageIsEnglish();
                var name = Path.GetFileName(path);
                var confirmText = en
                    ? $"Delete '{name}' and its .note file?"
                    : $"'{name}' und zugehörige .note löschen?";
                var title = en ? "Delete mod list" : "Modliste löschen";

                if (MessageBox.Show(this, confirmText, title,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                try { File.Delete(path); } catch { }
                var notePath = Path.ChangeExtension(path, ".note");
                if (File.Exists(notePath)) { try { File.Delete(notePath); } catch { } }

                int oldIndex = cbList.SelectedIndex;
                LoadModlists_Local();
                if (cbList.Items.Count > 0)
                    cbList.SelectedIndex = Math.Min(Math.Max(0, oldIndex - 1), cbList.Items.Count - 1);

                rtbPreview.Clear();
                RebuildPreviewGridFromRtb();

                SafeSetStatus(en ? "Mod list deleted." : "Modliste gelöscht.");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Delete error: " + ex.Message);
            }
        }
    }
}
