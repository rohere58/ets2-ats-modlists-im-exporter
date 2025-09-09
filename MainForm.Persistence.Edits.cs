using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // === Column names ===
        // ColPackage and ColName are already defined elsewhere in your project:
        //   private const string ColPackage = "Package";
        //   private const string ColName    = "Name";
        // If your Info column is named differently, update this:
        private const string ColInfoName = "ColInfo";

        // DTO for JSON
        private sealed class EditEntry
        {
            public string? Mod  { get; set; }
            public string? Info { get; set; }
        }

        // Cache for the currently selected list
        private Dictionary<string, EditEntry>? _editsCache;

        // one-time UI notice + IO lock
        private bool _persistDiagShown;
        private static readonly object _persistIoLock = new object();

        // 1) Neues Feld auf Klassenebene
        private string? _persistActiveFile;

        // ---------------- Public-ish hooks ----------------

        /// <summary>Wire grid events once (idempotent).</summary>
        private void Persist_EnsureGridWired()
        {
            if (_gridMods == null || _gridMods.IsDisposed) return;

            _gridMods.CellEndEdit -= Persist_Grid_CellEndEdit;
            _gridMods.CellEndEdit += Persist_Grid_CellEndEdit;

            _gridMods.CellValidated -= Persist_Grid_CellValidated_Save;
            _gridMods.CellValidated += Persist_Grid_CellValidated_Save;

            _gridMods.CurrentCellDirtyStateChanged -= Persist_Grid_CurrentCellDirtyStateChanged;
            _gridMods.CurrentCellDirtyStateChanged += Persist_Grid_CurrentCellDirtyStateChanged;

            var __infoCol = Persist_ResolveInfoColName();
            if (!string.IsNullOrEmpty(__infoCol) && _gridMods.Columns.Contains(__infoCol))
            {
                _gridMods.Columns[__infoCol].ReadOnly = false;
            }
            _gridMods.ReadOnly = false;
            _gridMods.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        }

        /// <summary>Flush current cache to disk (call BEFORE switching list/game).</summary>
        private void Persist_FlushCurrentEdits()
        {
            try
            {
                if (_editsCache == null || _editsCache.Count == 0) return;
                if (!string.IsNullOrEmpty(_persistActiveFile))
                    Persist_WriteEditsJson(_persistActiveFile, _editsCache);
            }
            catch { }
        }

        // A) Helper für aktuellen Listen-Namen
        private string Persist_GetCurrentListName()
        {
            // Prefer Text (what the user sees); fallback SelectedItem.ToString()
            var name = (cbList != null ? cbList.Text : null);
            if (string.IsNullOrWhiteSpace(name))
                name = Convert.ToString(cbList?.SelectedItem);

            name = (name ?? "").Trim();
            return string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        }

        // 1) Helper zum Auflösen des Info-Spaltennamens
        private string? Persist_ResolveInfoColName()
        {
            if (_gridMods == null || _gridMods.IsDisposed) return null;

            // Prefer the configured name, else fallback by header text "Info"
            if (_gridMods.Columns.Contains(ColInfoName)) return ColInfoName;

            foreach (DataGridViewColumn c in _gridMods.Columns)
            {
                if (c != null && c.Visible &&
                    string.Equals(c.HeaderText, "Info", StringComparison.OrdinalIgnoreCase))
                    return c.Name;
            }
            return null;
        }

        /// <summary>Load saved Mod/Info edits for the CURRENT list and apply to the grid.</summary>
        private void Persist_LoadEditsForCurrentList()
        {
            _editsCache = null; // reset cache when switching lists
            if (_gridMods == null || _gridMods.IsDisposed) return;
            var __p = Persist_GetEditsPath();
            System.Diagnostics.Debug.WriteLine("[PERSIST LOAD] " + __p + "  exists=" + System.IO.File.Exists(__p));
            if (_gridMods.Columns.Count == 0 || _gridMods.Rows.Count == 0) return;
            if (!_gridMods.Columns.Contains(ColPackage) || !_gridMods.Columns.Contains(ColName)) return;

            string infoCol = Persist_ResolveInfoColName() ?? ColInfoName;
            if (!_gridMods.Columns.Contains(infoCol)) return;

            var path = Persist_GetEditsPath();
            _editsCache = Persist_ReadEditsJson(path);
            _persistActiveFile = path;
            System.Diagnostics.Debug.WriteLine("[PERSIST ACTIVE] now " + _persistActiveFile);
            if (_editsCache == null || _editsCache.Count == 0) return;

            foreach (DataGridViewRow row in _gridMods.Rows)
            {
                if (row.IsNewRow) continue;

                string? pkgRaw = Convert.ToString(row.Cells[ColPackage]?.Value);
                string key = Persist_CanonKey(pkgRaw);
                if (string.IsNullOrEmpty(key)) continue;

                if (!_editsCache.TryGetValue(key, out var entry) || entry == null)
                {
                    var hit = _editsCache.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                    if (hit != null) _editsCache.TryGetValue(hit, out entry);
                    if (entry == null)
                    {
                        var cHit = _editsCache.Keys.FirstOrDefault(k =>
                            string.Equals(Persist_CanonKey(k), key, StringComparison.OrdinalIgnoreCase));
                        if (cHit != null) _editsCache.TryGetValue(cHit, out entry);
                    }
                }

                if (entry != null)
                {
                    if (entry.Mod  != null) row.Cells[ColName].Value  = entry.Mod;
                    if (entry.Info != null) row.Cells[infoCol].Value  = entry.Info;
                }
            }
        }

        // 2) Methode zum Zurücksetzen vor Listen-/Spielwechsel
        private void Persist_BeginListSwitch()
        {
            // prevent any stray writes during switching
            _persistActiveFile = null;
            _editsCache = null;
            System.Diagnostics.Debug.WriteLine("[PERSIST ACTIVE] cleared");
        }

        // ---------------- Grid event handlers ----------------

        private void Persist_Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_gridMods != null && _gridMods.IsCurrentCellDirty)
                _gridMods.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Persist_Grid_CellValidated_Save(object? sender, DataGridViewCellEventArgs e)
        {
            // Reuse CellEndEdit logic to persist
            Persist_Grid_CellEndEdit(sender, e);
        }

        private void Persist_Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_gridMods == null || _gridMods.IsDisposed) return;
                if (e.RowIndex < 0 || e.RowIndex >= _gridMods.Rows.Count) return;

                var row = _gridMods.Rows[e.RowIndex];
                if (row == null || row.IsNewRow) return;

                var col = _gridMods.Columns[e.ColumnIndex];
                if (col == null) return;

                var infoCol = Persist_ResolveInfoColName();
                bool isModCol  = string.Equals(col.Name, ColName, StringComparison.Ordinal);
                bool isInfoCol = (!string.IsNullOrEmpty(infoCol) &&
                                  string.Equals(col.Name, infoCol, StringComparison.Ordinal));
                if (!isModCol && !isInfoCol) return;

                string? pkgRaw = Convert.ToString(row.Cells[ColPackage]?.Value);
                string key = Persist_CanonKey(pkgRaw);
                if (string.IsNullOrEmpty(key)) return;

                _editsCache ??= new Dictionary<string, EditEntry>(StringComparer.OrdinalIgnoreCase);
                if (!_editsCache.TryGetValue(key, out var entry) || entry == null)
                {
                    entry = new EditEntry();
                    _editsCache[key] = entry;
                }

                if (isModCol)
                    entry.Mod = Convert.ToString(row.Cells[ColName]?.Value);

                if (isInfoCol)
                {
                    var val = Convert.ToString(row.Cells[infoCol!]?.Value) ?? "";
                    entry.Info = val;
                    System.Diagnostics.Debug.WriteLine("[PERSIST INFO] row=" + e.RowIndex + " value=\"" + val + "\"");
                }

                if (!string.IsNullOrEmpty(_persistActiveFile))
                    Persist_WriteEditsJson(_persistActiveFile, _editsCache);
            }
            catch { }
        }

        // ---------------- IO helpers ----------------

        private static string Persist_CanonKey(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string s = raw.Trim().Trim('"');
            s = Path.GetFileName(s);
            if (s.EndsWith(".scs", StringComparison.OrdinalIgnoreCase))
                s = s[..^4];
            return s;
        }

        private string Persist_GetEditsPath()
        {
            string listName = (cbList != null && !string.IsNullOrWhiteSpace(cbList.Text))
                ? cbList.Text.Trim()
                : Convert.ToString(cbList?.SelectedItem) ?? "Unnamed";
            string safeList = Persist_SafeFileSegment(listName);
            string dir = Persist_GetEditsDirectory();
            return System.IO.Path.Combine(dir, $"{safeList}.json");
        }

        private string Persist_GetEditsDirectory()
        {
            string gameShort = (cbGame?.SelectedIndex ?? 0) == 0 ? "ETS2" : "ATS";

            string appBase = AppContext.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory ?? "";
            // A: next to exe
            string candA = Path.GetFullPath(Path.Combine(appBase, "modlists", gameShort));
            // B: project root (when running from bin\Debug\...)
            string candB = Path.GetFullPath(Path.Combine(appBase, "..", "..", "..", "modlists", gameShort));

            if (Persist_TryEnsureWritableDir(candA)) return candA;
            if (Persist_TryEnsureWritableDir(candB)) return candB;
            return candA;
        }

        private static bool Persist_TryEnsureWritableDir(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write_test.tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        private static string Persist_SafeFileSegment(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            var invalid = Path.GetInvalidFileNameChars();
            var filtered = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            return string.IsNullOrEmpty(filtered) ? "Unnamed" : filtered;
        }

        private Dictionary<string, EditEntry> Persist_ReadEditsJson(string path)
        {
            try
            {
                if (!File.Exists(path)) return new Dictionary<string, EditEntry>(StringComparer.OrdinalIgnoreCase);
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, EditEntry>>(json,
                    new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                return dict ?? new Dictionary<string, EditEntry>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, EditEntry>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void Persist_WriteEditsJson(string path, Dictionary<string, EditEntry> data)
        {
            try
            {
                var opts = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                };
                var json = JsonSerializer.Serialize(data ?? new Dictionary<string, EditEntry>(), opts);
                lock (_persistIoLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, json);
                }
                // optional one-time notice; comment out if you don't want it
                if (!_persistDiagShown)
                {
                    _persistDiagShown = true;
                    try { MessageBox.Show($"Gespeichert:\n{path}", "Persist", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
                }
            }
            catch { }
        }
    }
}