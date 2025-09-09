using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // --- Path helpers (safe, no duplicates elsewhere) ---
        private string ResolveGameFolderShort()
        {
            // cbGame: 0 = ETS2, 1 = ATS
            int idx = (cbGame?.SelectedIndex ?? 0);
            return (idx == 1) ? "ATS" : "ETS2";
        }

        private string ResolveModlistsDir()
        {
            // App base dir + "modlists\<Game>"
            string baseDir = AppContext.BaseDirectory;
            string game = ResolveGameFolderShort();
            string dir = Path.Combine(baseDir, "modlists", game);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string SafeFileSegment(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(name.Where(ch => !invalid.Contains(ch)).ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "Unnamed" : safe;
        }

        private string CurrentModlistDisplayName()
        {
            if (cbList != null && !string.IsNullOrWhiteSpace(cbList.Text))
                return cbList.Text.Trim();
            var sel = Convert.ToString(cbList?.SelectedItem);
            return string.IsNullOrWhiteSpace(sel) ? "Unnamed" : sel!;
        }

        private (string fTxt, string fJson, string fNote) BuildListFileTriplet(string safeName)
        {
            string dir = ResolveModlistsDir();
            string fTxt  = Path.Combine(dir, $"{safeName}.txt");
            string fJson = Path.Combine(dir, $"{safeName}.json");
            string fNote = Path.Combine(dir, $"{safeName}.note");
            return (fTxt, fJson, fNote);
        }

        // === EXPORT ===
        private void ModlistShare_ExportZipForCurrentList()
        {
            try
            {
                string listName = CurrentModlistDisplayName();
                string safe = SafeFileSegment(listName);
                var (fTxt, fJson, fNote) = BuildListFileTriplet(safe);

                EnsureLinkJsonForCurrentList(safe);
                string fLink = BuildLinkJsonPath(safe);
                var files = new[] { fTxt, fJson, fNote, fLink }.Where(File.Exists).ToList();
                if (files.Count == 0)
                {
                    MessageBox.Show(this,
                        GetCurrentLanguageIsEnglish()
                            ? "No files found to export for this list."
                            : "Keine Dateien zum Exportieren gefunden.",
                        GetCurrentLanguageIsEnglish() ? "Share" : "Weitergeben",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Title = GetCurrentLanguageIsEnglish() ? "Export Modlist ZIP" : "Modliste als ZIP exportieren",
                    FileName = $"{safe}.zip",
                    Filter = "ZIP (*.zip)|*.zip",
                    OverwritePrompt = true
                };

                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                // (Re)create zip
                if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                using (var zip = ZipFile.Open(sfd.FileName, ZipArchiveMode.Create))
                {
                    foreach (var f in files)
                    {
                        // Store just the base name
                        zip.CreateEntryFromFile(f, Path.GetFileName(f), CompressionLevel.Optimal);
                    }
                }

                MessageBox.Show(this,
                    GetCurrentLanguageIsEnglish() ? "ZIP exported." : "ZIP exportiert.",
                    GetCurrentLanguageIsEnglish() ? "Share" : "Weitergeben",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    GetCurrentLanguageIsEnglish() ? "Share (export)" : "Weitergeben (Export)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // === IMPORT ===
        private void ModlistShare_ImportZip()
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Title = GetCurrentLanguageIsEnglish() ? "Import Modlist ZIP" : "Modliste aus ZIP importieren",
                    Filter = "ZIP (*.zip)|*.zip",
                    Multiselect = false
                };
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                string targetDir = ResolveModlistsDir();
                Directory.CreateDirectory(targetDir);

                // Extract to temp and copy only the files we care about
                string tempDir = Path.Combine(Path.GetTempPath(), "tmi_zipimport_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                try
                {
                    ZipFile.ExtractToDirectory(ofd.FileName, tempDir, overwriteFiles: true);

                    // Find any *.txt / *.json / *.note in root of the zip (and subfolders)
                    var all = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories)
                        .Where(p =>
                            p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                            p.EndsWith(".note", StringComparison.OrdinalIgnoreCase) ||
                            p.EndsWith(".link.json", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();

                    if (all.Count == 0)
                    {
                        MessageBox.Show(this,
                            GetCurrentLanguageIsEnglish()
                                ? "No modlist files found in ZIP."
                                : "Keine Modlisten-Dateien in der ZIP gefunden.",
                            GetCurrentLanguageIsEnglish() ? "Import" : "Importieren",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Ask for target name (default from first .txt if exists, else from zip name)
                    string defaultBase = Path.GetFileNameWithoutExtension(ofd.FileName);
                    var firstTxt = all.FirstOrDefault(p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                    if (firstTxt != null)
                        defaultBase = Path.GetFileNameWithoutExtension(firstTxt);

                    string baseName = defaultBase;
                    // Optional: prompt user to rename target (commented to stay minimal)
                    // using (var prompt = new InputBoxForm("Name", "Zielname:", defaultBase)) { ... }

                    string safe = SafeFileSegment(baseName);
                    var dstTxt  = Path.Combine(targetDir, $"{safe}.txt");
                    var dstJson = Path.Combine(targetDir, $"{safe}.json");
                    var dstNote = Path.Combine(targetDir, $"{safe}.note");
                    string dstLink = Path.Combine(targetDir, $"{safe}.link.json");

                    // Copy the matching three (if present)
                    void tryCopy(string ext, string dst, bool exact = false)
                    {
                        var src = all.FirstOrDefault(p =>
                            p.EndsWith(ext, StringComparison.OrdinalIgnoreCase) &&
                            (!exact || string.Equals(Path.GetFileNameWithoutExtension(p), Path.GetFileNameWithoutExtension(dst), StringComparison.OrdinalIgnoreCase))
                        );
                        if (src != null)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                            File.Copy(src, dst, overwrite: true);
                        }
                    }
                    tryCopy(".txt",       dstTxt);
                    tryCopy(".json",      dstJson);
                    tryCopy(".note",      dstNote);
                    tryCopy(".link.json", dstLink, exact: true);

                    // Falls keine passende .link.json im ZIP war, lokal erzeugen
                    if (!File.Exists(dstLink))
                    {
                        EnsureLinkJsonForCurrentList(safe);
                    }

                    // Add-Only Merge
                    if (File.Exists(dstLink))
                    {
                        MergeAddOnlyIntoLinksJson(dstLink);
                    }
                    ForceRefreshCurrentListView();

                    MessageBox.Show(this,
                        GetCurrentLanguageIsEnglish()
                            ? "Modlist imported."
                            : "Modliste importiert.",
                        GetCurrentLanguageIsEnglish() ? "Import" : "Importieren",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // --- refresh modlist dropdown after import ---
                    string? importedName = null;
                    try
                    {
                        // Try to detect the list name from the ZIP (first *.txt entry)
                        if (!string.IsNullOrWhiteSpace(ofd.FileName) && File.Exists(ofd.FileName))
                        {
                            using (var z = ZipFile.OpenRead(ofd.FileName))
                            {
                                var txtEntry = z.Entries
                                    .FirstOrDefault(e => e.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                                if (txtEntry != null)
                                    importedName = Path.GetFileNameWithoutExtension(txtEntry.Name);
                            }
                        }
                    }
                    catch { /* ignore zip read issues */ }

                    // Prefer debounce helper if available, else immediate refresh
                    try
                    {
                        // If Modlists_ScheduleRefresh exists in this partial class, call it
                        this.GetType().GetMethod("Modlists_ScheduleRefresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            ?.Invoke(this, new object?[] { importedName });
                    }
                    catch
                    {
                        // Hard fallback: immediate refresh (no debounce), preselect imported
                        try { Modlists_RefreshDropdown(importedName, preserveIfPossible: false); } catch { }
                    }
                }
                finally
                {
                    try { Directory.Delete(tempDir, recursive: true); } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    GetCurrentLanguageIsEnglish() ? "Import" : "Importieren",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // === HILFSMETHODEN (unter deine Helpers einfügen) ===
        private string BuildLinkJsonPath(string safeName)
        {
            string dir = ResolveModlistsDir();
            return Path.Combine(dir, $"{safeName}.link.json");
        }

        private void EnsureLinkJsonForCurrentList(string safeName)
        {
            string dir = ResolveModlistsDir();
            string linksJsonPath = Path.Combine(dir, "links.json");
            string linkJsonPath  = Path.Combine(dir, $"{safeName}.link.json");

            var map = LoadMapLenient(linksJsonPath);
            WritePrettyJson(linkJsonPath, map);
        }

        private void MergeAddOnlyIntoLinksJson(string linkJsonPath)
        {
            string dir = ResolveModlistsDir();
            string linksJsonPath = Path.Combine(dir, "links.json");

            var incoming = LoadMapLenient(linkJsonPath);
            var target   = LoadMapLenient(linksJsonPath);

            int added = 0;
            foreach (var kv in incoming)
            {
                if (!target.ContainsKey(kv.Key))
                {
                    target[kv.Key] = kv.Value;
                    added++;
                }
            }
            WritePrettyJsonTransactional(linksJsonPath, target);
            System.Diagnostics.Debug.WriteLine($"[Import Merge] +{added} → {linksJsonPath}");
        }

        private static Dictionary<string,string> LoadMapLenient(string path)
        {
            var dict = new Dictionary<string,string>(StringComparer.Ordinal);
            if (!File.Exists(path)) return dict;
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return dict;

            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string,string>>(json);
                if (map != null)
                {
                    foreach (var kv in map)
                        if (!string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value) && !dict.ContainsKey(kv.Key))
                            dict[kv.Key] = kv.Value;
                    return dict;
                }
            } catch { }

            try
            {
                var arr = JsonSerializer.Deserialize<List<_Row>>(json);
                if (arr != null)
                {
                    foreach (var r in arr)
                        if (!string.IsNullOrWhiteSpace(r?.Package) && !string.IsNullOrWhiteSpace(r?.Url) && !dict.ContainsKey(r.Package!))
                            dict[r.Package!] = r.Url!;
                }
            } catch { }
            return dict;
        }

        private static void WritePrettyJson(string path, Dictionary<string,string> map)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var sorted = map.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(k => k.Key, v => v.Value, StringComparer.Ordinal);
            string jsonOut = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, jsonOut, new UTF8Encoding(false));
        }

        private static void WritePrettyJsonTransactional(string jsonPath, Dictionary<string,string> map)
        {
            string dir = Path.GetDirectoryName(jsonPath)!;
            Directory.CreateDirectory(dir);

            var sorted = map.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(k => k.Key, v => v.Value, StringComparer.Ordinal);

            string tmp = Path.Combine(dir, $"links.json.tmp-{Guid.NewGuid():N}");
            string jsonOut = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, jsonOut, new UTF8Encoding(false));

            if (File.Exists(jsonPath))
            {
                string bak = Path.Combine(dir, $"links.json.bak-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Replace(tmp, jsonPath, bak, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tmp, jsonPath);
            }
        }

        private Dictionary<string,string> LoadLinksMapForCurrentGame()
        {
            string dir = ResolveModlistsDir(); // ...\modlists\<Game>
            string linksJson = Path.Combine(dir, "links.json");
            return LoadMapLenient(linksJson); // nutzt deinen bestehenden lenient-Loader
        }

        // Refresh für typische UI-Controls:
        // - DataGridView "dgvLinks" mit Spalten Package/Url
        // - ListView "lvLinks" (Details: 2 Spalten) oder einfach Items
        // - ListBox "lbLinks" (Zeilen: "Package\tUrl")
        private void RefreshLinksUi()
        {
            var map = LoadLinksMapForCurrentGame();

            // DataGridView?
            if (this.Controls.Find("dgvLinks", true).FirstOrDefault() is DataGridView dgv)
            {
                var data = new BindingList<LinkRow>(map.Select(kv => new LinkRow { Package = kv.Key, Url = kv.Value }).ToList());
                dgv.AutoGenerateColumns = true;
                dgv.DataSource = data;
                if (dgv.Columns.Contains("Package")) dgv.Columns["Package"].HeaderText = "Package";
                if (dgv.Columns.Contains("Url"))     dgv.Columns["Url"].HeaderText     = "URL";
            }

            // ListView?
            if (this.Controls.Find("lvLinks", true).FirstOrDefault() is ListView lv)
            {
                lv.BeginUpdate();
                try
                {
                    if (lv.View != View.Details) lv.View = View.Details;
                    if (lv.Columns.Count < 2)
                    {
                        lv.Columns.Clear();
                        lv.Columns.Add("Package", 240);
                        lv.Columns.Add("URL", 480);
                    }
                    lv.Items.Clear();
                    foreach (var kv in map)
                    {
                        var item = new ListViewItem(kv.Key);
                        item.SubItems.Add(kv.Value);
                        lv.Items.Add(item);
                    }
                    lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
                finally { lv.EndUpdate(); }
            }

            // ListBox?
            if (this.Controls.Find("lbLinks", true).FirstOrDefault() is ListBox lb)
            {
                lb.BeginUpdate();
                try
                {
                    lb.Items.Clear();
                    foreach (var kv in map)
                        lb.Items.Add($"{kv.Key}\t{kv.Value}");
                }
                finally { lb.EndUpdate(); }
            }
        }

        private sealed class LinkRow
        {
            public string Package { get; set; } = "";
            public string Url { get; set; } = "";
        }

        private sealed class _Row { public string? Package { get; set; } public string? Url { get; set; } }

        private void ForceRefreshCurrentListView()
        {
            try
            {
                // Spielwechsel-Event sanft anstoßen (optional – nur wenn nötig)
                if (cbGame != null && cbGame.Items.Count > 0)
                {
                    int g = cbGame.SelectedIndex;
                    if (g >= 0 && cbGame.Items.Count > 1)
                    {
                        cbGame.SelectedIndex = (g == 0) ? 1 : 0;
                        Application.DoEvents();
                        cbGame.SelectedIndex = g;
                        Application.DoEvents();
                    }
                }

                // Modlistenwechsel-Event gezielt auslösen
                if (cbList == null) return;

                int idx = cbList.SelectedIndex;
                string oldText = cbList.Text;

                if (idx >= 0 && cbList.Items.Count > 1)
                {
                    int temp = (idx == 0) ? 1 : 0;
                    cbList.SelectedIndex = temp;     // feuert SelectedIndexChanged
                    Application.DoEvents();
                    cbList.SelectedIndex = idx;      // zurück auf ursprüngliche Auswahl
                    Application.DoEvents();
                }
                else
                {
                    // Falls nur 1 Eintrag oder -1: Text kurz ändern → feuert TextChanged
                    cbList.Text = oldText + " ";
                    Application.DoEvents();
                    cbList.Text = oldText;
                    Application.DoEvents();
                }
            }
            catch
            {
                // still: keine Popups
            }
        }
    }
}