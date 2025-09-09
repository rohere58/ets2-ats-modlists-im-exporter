using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private Button? _btnBackupProfiles;

        // === PUBLIC ENTRYPOINTS we will call from other places ===
        public void EnsureBackupProfilesButton()
        {
            Control parent = pnlTopButtons ?? cbList?.Parent ?? this;

            if (_btnBackupProfiles == null || _btnBackupProfiles.IsDisposed)
            {
                _btnBackupProfiles = new Button
                {
                    Name = "btnBackupProfiles",
                    AutoSize = false,
                    Height = (cbList?.Height ?? 28),
                    Width  = 160,
                    FlatStyle = FlatStyle.Flat,
                    TabStop = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Padding = new Padding(8, 2, 8, 2)
                };

                // Klicken
                try { _btnBackupProfiles.Click -= BtnBackupProfiles_Click; } catch { }
                _btnBackupProfiles.Click += BtnBackupProfiles_Click;

                // add to parent
                parent.Controls.Add(_btnBackupProfiles);

                // Sprach-Text setzen
                UpdateBackupProfilesButtonLanguage();

                // if FlowLayoutPanel: push to the end (rightmost)
                if (parent is FlowLayoutPanel flp)
                {
                    flp.Controls.SetChildIndex(_btnBackupProfiles, flp.Controls.Count - 1);
                    _btnBackupProfiles.Margin = new Padding(0, 0, 8, 0);
                    flp.PerformLayout();
                }
                else
                {
                    // keep your existing right-align logic if you have one
                }

                // Optional: Tooltip
                try
                {
                    if (tips != null)
                        tips.SetToolTip(_btnBackupProfiles,
                            (SettingsService.Load()?.Language ?? "de").Trim().ToLowerInvariant().StartsWith("en")
                                ? "Backup the whole 'profiles' folder"
                                : "Den kompletten 'profiles'-Ordner sichern");
                }
                catch { /* ignore */ }

                // Beim Größen-Ändern rechts ausrichten
                try { parent.SizeChanged -= Parent_SizeChanged_RepositionBackup; } catch { }
                parent.SizeChanged += Parent_SizeChanged_RepositionBackup;
            }

            // Look an die bestehenden Buttons angleichen
            RefreshBackupProfilesButtonLook();

            // Theme/language refreshers
            try { UpdateBackupProfilesButtonLanguage(); } catch {}
            try { ApplyThemeToDynamicButtons(); } catch {}
        }

        private void UpdateBackupProfilesButtonLanguage()
        {
            try
            {
                var btn = _btnBackupProfiles ?? Controls.Find("btnBackupProfiles", true).FirstOrDefault() as Button;
                if (btn == null || btn.IsDisposed) return;

                var st = SettingsService.Load();
                bool isEN = (st?.Language ?? "de").Trim().ToLowerInvariant().StartsWith("en");

                btn.Text = isEN ? "Backup all profiles" : "Alle Profile sichern";
                btn.AccessibleName = btn.Text;
                btn.AccessibleDescription = btn.Text;
            }
            catch { /* ignore */ }
        }

        private Button? GetSampleTopButton()
        {
            Control parent = pnlTopButtons ?? _btnBackupProfiles?.Parent ?? this;

            // Priorisierte Liste stabiler Top-Buttons
            string[] names = { "btnOpenModlists", "btnOptions", "btnExport", "btnApply", "btnLoad" };

            foreach (var n in names)
            {
                var b = parent.Controls.OfType<Button>()
                    .FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase));
                if (b != null && b != _btnBackupProfiles) return b;
            }

            // Fallback: irgendein anderer Button
            return parent.Controls.OfType<Button>().FirstOrDefault(b => b != _btnBackupProfiles);
        }

        private int GetPreferredButtonWidth(Button b)
        {
            // Textbreite + horizontales Padding + etwas Reserve
            var sz = TextRenderer.MeasureText(b.Text ?? "", b.Font);
            int pad = b.Padding.Left + b.Padding.Right;
            return Math.Max(sz.Width + pad + 16, 120);
        }

        private void CopyLookFromSampleButton(Button target)
        {
            var sample = GetSampleTopButton();
            if (sample is Button s)
            {
                target.FlatStyle = s.FlatStyle;
                target.BackColor = s.BackColor;
                target.ForeColor = s.ForeColor;
                try
                {
                    target.FlatAppearance.BorderColor = s.FlatAppearance.BorderColor;
                    target.FlatAppearance.MouseOverBackColor = s.FlatAppearance.MouseOverBackColor;
                    target.FlatAppearance.MouseDownBackColor = s.FlatAppearance.MouseDownBackColor;
                }
                catch { /* ignore */ }

                // Höhe identisch zum Sample
                target.Height = s.Height;

                // Breite: mindestens wie Sample, sonst passend zum Text
                int wanted = GetPreferredButtonWidth(target);
                target.Width = Math.Max(s.Width, wanted);
            }
            else
            {
                // Fallback
                target.Height = 32;
                target.Width = Math.Max(target.Width, GetPreferredButtonWidth(target));
            }
        }

        public void RefreshBackupProfilesButtonLook()
        {
            if (_btnBackupProfiles == null || _btnBackupProfiles.IsDisposed) return;
            CopyLookFromSampleButton(_btnBackupProfiles);
            PositionBackupProfilesButton();
        }

        public void PositionBackupProfilesButton()
        {
            if (_btnBackupProfiles == null || _btnBackupProfiles.IsDisposed) return;

            Control? parent = _btnBackupProfiles.Parent;
            if (parent == null)
                parent = pnlTopButtons != null ? pnlTopButtons : this;

            // Sonderfall: FlowLayoutPanel – dort bestimmt die Controls-Reihenfolge das Layout
            if (parent is FlowLayoutPanel flp)
            {
                // ans Ende schieben (ganz rechts)
                flp.Controls.SetChildIndex(_btnBackupProfiles, flp.Controls.Count - 1);
                _btnBackupProfiles.Margin = new Padding(0, 0, 8, 0);
                flp.PerformLayout();
                return;
            }

            // Klassische absolute Positionierung: rechts ausrichten
            var openModlists = parent.Controls
                                     .OfType<Button>()
                                     .FirstOrDefault(b => string.Equals(b.Name, "btnOpenModlists", StringComparison.OrdinalIgnoreCase));

            int top = cbList?.Top ?? 8;
            _btnBackupProfiles.Top = top;

            int rightMargin = 8;

            if (openModlists != null && !openModlists.IsDisposed)
            {
                // links neben "Modlisten-Ordner öffnen"
                _btnBackupProfiles.Left = openModlists.Left - _btnBackupProfiles.Width - 8;
            }
            else
            {
                // ganz rechts
                _btnBackupProfiles.Left = parent.ClientSize.Width - _btnBackupProfiles.Width - rightMargin;
            }

            _btnBackupProfiles.Height = (cbList?.Height ?? _btnBackupProfiles.Height);
            _btnBackupProfiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }

        // === INTERNALS ===

        private void Parent_SizeChanged_RepositionBackup(object? sender, EventArgs e)
            => PositionBackupProfilesButton();

        private void BtnBackupProfiles_Click(object? sender, EventArgs e)
        {
            try
            {
                // profiles-Root je nach Spiel
                string profilesRoot = GetProfilesRootDir(); // vorhandene Helper-Methode benutzen

                if (string.IsNullOrWhiteSpace(profilesRoot) || !Directory.Exists(profilesRoot))
                {
                    MessageBox.Show(this,
                        GetCurrentLanguageIsEnglish()
                        ? "Could not resolve the 'profiles' folder for the selected game."
                        : "Der 'profiles'-Ordner für das ausgewählte Spiel konnte nicht ermittelt werden.",
                        GetCurrentLanguageIsEnglish() ? "Error" : "Fehler",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Ziel-Root: <Spiel-Ordner>\profiles_backups
                string? baseDir = Directory.GetParent(profilesRoot)?.FullName ?? profilesRoot;
                string backupsRoot = Path.Combine(baseDir, "profiles_backups");
                Directory.CreateDirectory(backupsRoot);

                // Zeitstempel
                string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string dest = Path.Combine(backupsRoot, $"profiles_{stamp}");

                // Kopieren
                CopyDirectoryRecursive(profilesRoot, dest);

                // Status/Meldung
                SafeSetStatus(GetCurrentLanguageIsEnglish()
                    ? $"Backup completed: {dest}"
                    : $"Backup erstellt: {dest}");

                var open = MessageBox.Show(this,
                    GetCurrentLanguageIsEnglish()
                    ? "Backup finished.\nOpen the backup folder now?"
                    : "Backup fertig.\nBackup-Ordner jetzt öffnen?",
                    GetCurrentLanguageIsEnglish() ? "Backup" : "Backup",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (open == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = dest, UseShellExecute = true });
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    (GetCurrentLanguageIsEnglish() ? "Backup failed:\n" : "Backup fehlgeschlagen:\n") + ex.Message,
                    GetCurrentLanguageIsEnglish() ? "Error" : "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceDir, dir);
                Directory.CreateDirectory(Path.Combine(destDir, rel));
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var target = Path.Combine(destDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        private void ApplyThemeToDynamicButtons()
        {
            try
            {
                var btn = Controls.Find("btnBackupProfiles", true).FirstOrDefault() as Button;
                if (btn == null) return;

                var st = SettingsService.Load();
                bool dark = st?.DarkMode ?? false;

                // Match the same palette used for your other buttons
                var btnBg     = dark ? Color.FromArgb(45,45,48) : SystemColors.Control;
                var btnFg     = dark ? Color.White              : Color.Black;
                var btnBorder = dark ? Color.FromArgb(70,70,73) : Color.Silver;
                var btnHover  = dark ? Color.FromArgb(63,63,70) : Color.FromArgb(230,230,230);

                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = btnBg;
                btn.ForeColor = btnFg;
                btn.FlatAppearance.BorderColor = btnBorder;
                btn.FlatAppearance.MouseOverBackColor = btnHover;
                btn.FlatAppearance.MouseDownBackColor = btnHover;
                btn.Height = 32;
                btn.Margin = new Padding(0, 0, 8, 0);
            }
            catch { /* ignore */ }
        }
    }
}