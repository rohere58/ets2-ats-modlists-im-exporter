// MainForm.Hooks.cs
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

#if TEST_HOOKS_FILE_COMPILED
#error TEST: MainForm.Hooks.cs is compiled
#endif

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

            // Sicherstellen, dass der Backup-Button und seine Sprache gesetzt werden
            EnsureBackupProfilesButton();
            UpdateBackupProfilesButtonLanguage();

            // Platz schaffen, damit die gesamte Buttonleiste sichtbar ist
            EnsureFormWidthForTopButtons();


            Hooks_BootstrapDebugOnce(); // <-- hier einfügen
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

            // Reaktionen auf Größenänderungen
            try { pnlTopButtons.SizeChanged -= PnlTopButtons_SizeChanged_AdjustFormWidth; } catch { }
            pnlTopButtons.SizeChanged += PnlTopButtons_SizeChanged_AdjustFormWidth;

            try { ResizeEnd -= Form_ResizeEnd_AdjustFormWidthAndDeletePos; } catch { }
            ResizeEnd += Form_ResizeEnd_AdjustFormWidthAndDeletePos;

            // Startet das Auto-Decrypt nur einmal im Hintergrund nach dem WireUp
            BeginInvoke(new Action(KickOffAutoDecryptAllProfilesInBackground_Once));
        }

        private void PnlTopButtons_SizeChanged_AdjustFormWidth(object? sender, EventArgs e)
            => EnsureFormWidthForTopButtons();

        private void Form_ResizeEnd_AdjustFormWidthAndDeletePos(object? sender, EventArgs e)
        {
            EnsureFormWidthForTopButtons();

        }

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

        private void OpenSelectedModlistsFolder()
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modlists");
            string subFolder = cbGame.SelectedIndex == 1 ? "ATS" : "ETS2";
            string fullPath = Path.Combine(basePath, subFolder);

            if (!Directory.Exists(fullPath))
            {
                MessageBox.Show(this, $"Ordner nicht gefunden:\n{fullPath}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Ordner konnte nicht geöffnet werden:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        // --- Profile laden ---

        private void LoadProfiles_Local4()
        {
            string[] profilePaths = { "", "" };

            var st = SettingsService.Load();
            profilePaths[0] = st.Ets2ProfilesPath;
            profilePaths[1] = st.AtsProfilesPath;

            using (var f = new FolderBrowserDialog())
            {
                f.Description = "Wählen Sie den Ordner mit Ihren ETS2-Profilen aus:";
                f.SelectedPath = profilePaths[0];
                f.ShowNewFolderButton = false;

                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    profilePaths[0] = f.SelectedPath;
                    st.Ets2ProfilesPath = profilePaths[0];
                }
            }

            using (var f = new FolderBrowserDialog())
            {
                f.Description = "Wählen Sie den Ordner mit Ihren ATS-Profilen aus:";
                f.SelectedPath = profilePaths[1];
                f.ShowNewFolderButton = false;

                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    profilePaths[1] = f.SelectedPath;
                    st.AtsProfilesPath = profilePaths[1];
                }
            }

            // Nur tatsächlich geänderte Pfade speichern
            if (profilePaths[0] != st.Ets2ProfilesPath || profilePaths[1] != st.AtsProfilesPath)
                SettingsService.Save(st);
        }

        private void SelectProfileFoldersViaDialog()
        {
            // ... (Inhalt bleibt gleich)
        }

        // Beispiel: In der Methode, die den Header (pnlTopButtons, cbList, Delete) aufbaut:
        private void TopButtons_BuildOrUpdate()
        {
            // ... bestehender Code, der pnlTopButtons, cbList, Delete usw. hinzufügt ...

            EnsureHeaderShareImportButtons(); // <— HIER EINFÜGEN, EINMALIG
        }

        // 1) Felder am Anfang der partial class ergänzen:
        private bool __hooksShownWired;
        private bool __debugDumpDone;

        // 2) Bootstrap-Methode:
        private void Hooks_BootstrapDebugOnce()
        {
            if (__hooksShownWired) return;
            __hooksShownWired = true;

            try { this.Shown -= Hooks_OnShown_DebugOnce; } catch { }
            try { this.Shown += Hooks_OnShown_DebugOnce; } catch { }
        }

        // 3) Shown-Handler:
        private void Hooks_OnShown_DebugOnce(object? sender, EventArgs e)
        {
            try { Debug_ControlTreeAndInjectMarker(); } catch { }

            try
            {
                BeginInvoke(new Action(() =>
                {
                    try { Debug_ControlTreeAndInjectMarker(); } catch { }
                }));
            }
            catch { }

            var t = new System.Windows.Forms.Timer { Interval = 700 };
            t.Tick += (s2, e2) =>
            {
                try { Debug_ControlTreeAndInjectMarker(); } catch { }
                t.Stop(); t.Dispose();
            };
            t.Start();

            try { this.Shown -= Hooks_OnShown_DebugOnce; } catch { }
        }

        // 4) Debug-Helfer:
        private void Debug_ControlTreeAndInjectMarker()
        {
            if (__debugDumpDone) { /* still allow marker refresh */ } else { __debugDumpDone = true; }

            try
            {
                var marker = this.Controls.Cast<Control>().FirstOrDefault(c => c.Name == "__DEBUG_MARKER__") as Panel;
                if (marker == null || marker.IsDisposed)
                {
                    marker = new Panel
                    {
                        Name = "__DEBUG_MARKER__",
                        BackColor = System.Drawing.Color.Red,
                        Size = new System.Drawing.Size(120, 40),
                        Location = new System.Drawing.Point(10, 10),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left,
                        Visible = true
                    };
                    var lbl = new Label { AutoSize = true, Text = "DEBUG MARKER", ForeColor = System.Drawing.Color.White, Location = new System.Drawing.Point(6, 10) };
                    marker.Controls.Add(lbl);
                    this.Controls.Add(marker);
                    marker.BringToFront();
                }
                else
                {
                    marker.Visible = true;
                    marker.BringToFront();
                }
            }
            catch { }

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== FORM CONTROLS (top-level) ===");
                foreach (Control c in this.Controls)
                    sb.AppendLine($"{c.GetType().Name}  Name='{c.Name}'  Visible={c.Visible}  Bounds={c.Bounds}");

                Control? cb = null;
                foreach (Control c in this.Controls) { cb = FindByNameDeep(c, "cbList"); if (cb != null) break; }
                if (cb == null && this is Control root) cb = FindByNameDeep(root, "cbList");

                if (cb != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[FOUND] cbList: Type={cb.GetType().Name} Name='{cb.Name}' Visible={cb.Visible} Bounds={cb.Bounds}");
                    var parent = cb.Parent;
                    if (parent != null)
                    {
                        sb.AppendLine($"   Parent: Type={parent.GetType().Name} Name='{parent.Name}' Bounds={parent.Bounds}");
                        sb.AppendLine("   Parent children:");
                        foreach (Control pc in parent.Controls)
                            sb.AppendLine($"     {pc.GetType().Name} Name='{pc.Name}' Text='{pc.Text}' Visible={pc.Visible} Bounds={pc.Bounds}");
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("[WARN] cbList NOT FOUND in control tree.");
                }

                System.Diagnostics.Debug.WriteLine(sb.ToString());
                MessageBox.Show(this, sb.ToString(), "DEBUG: Control tree", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }

        private Control? FindByNameDeep(Control root, string name)
        {
            if (root.Name == name) return root;
            foreach (Control c in root.Controls)
            {
                var hit = FindByNameDeep(c, name);
                if (hit != null) return hit;
            }
            return null;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Ensure header buttons exist as soon as a handle is ready.
            try { EnsureHeaderShareImportButtons(); } catch { }

            // And once more after the first UI idle, to win over late layout.
            try { BeginInvoke(new Action(() => EnsureHeaderShareImportButtons())); } catch { }
        }
    }
}
