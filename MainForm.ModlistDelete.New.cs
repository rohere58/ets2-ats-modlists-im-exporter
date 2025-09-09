using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private Button? btnListDelete;

        // 1) Create/wire the Delete button; parent = same as cbList
        private void EnsureHeaderDeleteButton()
        {
            if (btnListDelete != null && !btnListDelete.IsDisposed) { btnListDelete.Visible = true; return; }

            var header = cbList?.Parent ?? pnlTopButtons ?? (Control)this;

            btnListDelete = new Button
            {
                Name = "btnListDelete",
                Text = GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                TabStop = false,
                Visible = true
            };

            btnListDelete.Click -= BtnListDelete_Click;
            btnListDelete.Click += BtnListDelete_Click;

            header.Controls.Add(btnListDelete);
            btnListDelete.BringToFront();

            try { MatchTopButtonLook(btnListDelete); } catch { }

            // D) Mini-Debug (einmalig, später entfernen)
            btnListDelete.Text = (btnListDelete.Text ?? "Löschen") + " [DEL]";
        }

        private string ResolveModlistsDirSafe()
        {
            try { return ResolveModlistsDir(); } catch {}
            var game = (cbGame?.SelectedIndex ?? 0) == 1 ? "ATS" : "ETS2";
            var baseDirDot = System.IO.Path.Combine(AppContext.BaseDirectory, ".modlists", game);
            var baseDirNoDot = System.IO.Path.Combine(AppContext.BaseDirectory, "modlists", game);
            if (Directory.Exists(baseDirDot)) return baseDirDot;
            if (Directory.Exists(baseDirNoDot)) return baseDirNoDot;
            return baseDirDot; // default
        }

        // 2) Click handler: delete selected modlist triplet (txt/json/note)
        private void BtnListDelete_Click(object? sender, EventArgs e)
        {
            var listName = cbList?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(listName))
            {
                MessageBox.Show(this,
                    GetCurrentLanguageIsEnglish() ? "Please select a modlist." : "Bitte eine Modliste auswählen.",
                    GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var baseDir = ResolveModlistsDirSafe();
            string P(string ext) => Path.Combine(baseDir, listName + ext);

            // NEU: Sanitize-Fallback (falls die Dateien mit bereinigtem Namen existieren)
            string Sanitize(string name)
            {
                var invalid = Path.GetInvalidFileNameChars();
                var safe = new string(name.Where(ch => !invalid.Contains(ch)).ToArray());
                return string.IsNullOrWhiteSpace(safe) ? "Unnamed" : safe;
            }

            var safeName = Sanitize(listName);
            string PSafe(string ext) => Path.Combine(baseDir, safeName + ext);

            var txt      = P(".txt");
            var json     = P(".json");
            var note     = P(".note");
            var linkJson = P(".link.json");              // ← NEU

            // Fallback-Pfade prüfen, wenn die „unsanitized“ nicht existieren
            if (!File.Exists(txt))      txt      = PSafe(".txt");
            if (!File.Exists(json))     json     = PSafe(".json");
            if (!File.Exists(note))     note     = PSafe(".note");
            if (!File.Exists(linkJson)) linkJson = PSafe(".link.json");   // ← NEU

            // „Nichts gefunden?“-Check erweitert um .link.json
            if (!File.Exists(txt) && !File.Exists(json) && !File.Exists(note) && !File.Exists(linkJson))
            {
                MessageBox.Show(this,
                    GetCurrentLanguageIsEnglish() ? "No files found for this modlist." : "Für diese Modliste wurden keine Dateien gefunden.",
                    GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Bestätigungs-Text erweitert um .link.json
            var q = GetCurrentLanguageIsEnglish()
                ? $"Delete modlist \"{listName}\" and its related files (.txt/.json/.note/.link.json)?"
                : $"Modliste „{listName}“ und zugehörige Dateien löschen (.txt/.json/.note/.link.json)?";
            if (MessageBox.Show(this, q,
                    GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            void TryDelete(string p) { try { if (File.Exists(p)) File.Delete(p); } catch { } }

            // Dateien löschen (inkl. .link.json)
            TryDelete(txt);
            TryDelete(json);
            TryDelete(note);
            TryDelete(linkJson);      // ← NEU

            // Refresh cbList bleibt unverändert…
            // refresh cbList from baseDir
            try
            {
                var files = Directory.Exists(baseDir)
                    ? Directory.EnumerateFiles(baseDir, "*.txt", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetFileNameWithoutExtension)
                        .Where(n => !string.IsNullOrEmpty(n))                  // ⬅️ Null/leer ausschließen
                        .Select(n => n!)                                      // ⬅️ Compiler sagen: hier sicher non-null
                        .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                        .ToList()
                    : new System.Collections.Generic.List<string>();

                if (cbList != null)
                {
                    cbList.BeginUpdate();
                    try
                    {
                        cbList.Items.Clear();
                        foreach (var n in files) cbList.Items.Add(n);
                        cbList.SelectedIndex = files.Count > 0 ? 0 : -1;
                    }
                    finally { cbList.EndUpdate(); }
                }
            }
            catch { }

            MessageBox.Show(this,
                GetCurrentLanguageIsEnglish() ? "Modlist deleted." : "Modliste gelöscht.",
                GetCurrentLanguageIsEnglish() ? "Delete" : "Löschen",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}