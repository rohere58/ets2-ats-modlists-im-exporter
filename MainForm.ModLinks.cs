// MainForm.ModLinks.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Link-Datenbank (pro Spiel)
        private readonly Dictionary<string, string> _modLinks =
            new(StringComparer.OrdinalIgnoreCase);

        // Steam-ID: 9–12-stellig
        private static readonly Regex SteamIdRegex = new(@"\b(\d{9,12})\b", RegexOptions.Compiled);
        private static readonly Regex FirstQuotedRegex = new("\"([^\"]+)\"", RegexOptions.Compiled);

        private void InitializeModLinks()
        {
            try
            {
                // Spielwechsel -> DB neu laden & Grid neu aufbauen
                try { cbGame.SelectedIndexChanged -= CbGame_SelectedIndexChanged_LoadLinks; } catch { }
                cbGame.SelectedIndexChanged += CbGame_SelectedIndexChanged_LoadLinks;

                // Nach "Modliste laden" -> Anzeige formatieren + Grid neu aufbauen
                try { btnLoad.Click -= BtnLoad_Regrid; } catch { }
                btnLoad.Click += BtnLoad_Regrid;

                // Erstmalig laden & Grid bereitstellen
                LoadLinksDbForCurrentGame();
                rtbPreview.DetectUrls = true;

                EnsurePreviewGridBuilt();
                RebuildPreviewGridFromRtb();

                // Anzeige ggf. als umgekehrte, nummerierte Liste darstellen (UI-only)
                ApplyLoadOrderDisplay();

                // Trucky überall entfernen (Buttons + DataGridView-Spalten)
                RemoveTruckyUI(this);

                // Index-Spalte sicherstellen & füllen
                AddOrUpdateIndexColumn();
            }
            catch (Exception ex)
            {
                SafeSetStatus("Links-Init-Fehler: " + ex.Message);
            }
        }

        private void CbGame_SelectedIndexChanged_LoadLinks(object? sender, EventArgs e)
        {
            LoadLinksDbForCurrentGame();

            // Anzeige ggf. neu formatieren (falls eine Liste schon sichtbar ist)
            ApplyLoadOrderDisplay();

            RebuildPreviewGridFromRtb();
            RemoveTruckyUI(this);
            AddOrUpdateIndexColumn();
        }

        private void BtnLoad_Regrid(object? sender, EventArgs e)
        {
            // *** Nur Anzeige ändern: reverse + nummerieren ***
            ApplyLoadOrderDisplay();

            RebuildPreviewGridFromRtb();
            RemoveTruckyUI(this);
            AddOrUpdateIndexColumn();
        }

        // === Modzeile analysieren & Lookup ===
        /// <summary>
        /// Extrahiert aus einer Zeile:
        /// - modName: Anzeigename (rechts vom '|')
        /// - packageId: Paketname (links vom '|')
        /// - rawToken: kompletter gequoteter Teil
        /// </summary>
        private static void ExtractModKeys(string line, out string modName, out string packageId, out string rawToken)
        {
            modName = ""; packageId = ""; rawToken = "";
            var m = FirstQuotedRegex.Match(line);
            if (!m.Success) return;

            rawToken = m.Groups[1].Value;
            var parts = rawToken.Split('|');
            if (parts.Length >= 2)
            {
                packageId = parts[0].Trim();
                modName   = parts[^1].Trim();
            }
            else
            {
                packageId = rawToken.Trim();
                modName   = "";
            }
        }

        private static string GetPreferredKey(string modName, string packageId, string fallbackFullLine)
        {
            if (!string.IsNullOrWhiteSpace(modName))   return modName.Trim();
            if (!string.IsNullOrWhiteSpace(packageId)) return packageId.Trim();
            return fallbackFullLine.Trim();
        }

        private bool TryGetLinkWithFallback(string fullLine, string modName, string packageId, string rawToken, out string url)
        {
            if (_modLinks.TryGetValue(fullLine.Trim(), out url) && !string.IsNullOrWhiteSpace(url)) return true;
            if (!string.IsNullOrWhiteSpace(modName) &&
                _modLinks.TryGetValue(modName.Trim(), out url) && !string.IsNullOrWhiteSpace(url)) return true;
            if (!string.IsNullOrWhiteSpace(packageId) &&
                _modLinks.TryGetValue(packageId.Trim(), out url) && !string.IsNullOrWhiteSpace(url)) return true;
            if (!string.IsNullOrWhiteSpace(rawToken) &&
                _modLinks.TryGetValue(rawToken.Trim(), out url) && !string.IsNullOrWhiteSpace(url)) return true;

            url = ""; return false;
        }

        private bool TryGuessSteamUrl(string line, out string url)
        {
            url = "";
            var m = SteamIdRegex.Match(line);
            if (m.Success && m.Groups.Count > 1)
            {
                var id = m.Groups[1].Value;
                url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={id}";
                return true;
            }
            return false;
        }

        // === Links-DB laden/speichern ===
        private void LoadLinksDbForCurrentGame()
        {
            _modLinks.Clear();

            // Standard: links.json, Fallback: link.json
            var pathPreferred = GetLinksJsonPathForCurrentGame(preferredPlural: true);
            var pathFallback  = GetLinksJsonPathForCurrentGame(preferredPlural: false);
            var pathToLoad = File.Exists(pathPreferred) ? pathPreferred : pathFallback;

            try
            {
                if (File.Exists(pathToLoad))
                {
                    var json = File.ReadAllText(pathToLoad);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);

                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key) &&
                                !string.IsNullOrWhiteSpace(kv.Value))
                            {
                                _modLinks[kv.Key] = kv.Value!;
                            }
                        }
                    }

                    SafeSetStatus($"Links geladen: {Path.GetFileName(pathToLoad)} ({_modLinks.Count})");
                }
                else
                {
                    SafeSetStatus("Keine links.json gefunden – wird bei Bedarf angelegt.");
                }
            }
            catch (Exception ex)
            {
                SafeSetStatus("Konnte links.json nicht laden: " + ex.Message);
            }
        }

        private void SaveLinksDbForCurrentGame()
        {
            var path = GetLinksJsonPathForCurrentGame(preferredPlural: true);
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_modLinks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                SafeSetStatus($"Links gespeichert: {Path.GetFileName(path)} ({_modLinks.Count})");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Konnte links.json nicht speichern: " + ex.Message);
            }
        }

        private string GetLinksJsonPathForCurrentGame(bool preferredPlural)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var listsRoot = Path.Combine(baseDir, "modlists");
            var tag = GetCurrentGameTag(); // "ETS2" oder "ATS"
            var folder = Path.Combine(listsRoot, tag);
            var file = preferredPlural ? "links.json" : "link.json";
            return Path.Combine(folder, file);
        }

        private string GetCurrentGameTag()
        {
            // 0 = ETS2, 1 = ATS
            return cbGame.SelectedIndex == 1 ? "ATS" : "ETS2";
        }

        // ======================================================================
        // *** Nur Anzeige anpassen: Reverse + Nummerierung (ohne Dateien zu ändern)
        // ======================================================================

        /// <summary>
        /// Formatiert die aktuelle Vorschau (rtbPreview) NUR FÜR DIE ANZEIGE:
        /// - Zeilen umkehren (letzte Mod zuoberst)
        /// - sauber nummerieren: 1), 2), 3) ...
        /// Die eigentlichen Modlisten-Dateien bleiben unberührt.
        /// </summary>
        private void ApplyLoadOrderDisplay()
        {
            try
            {
                var original = rtbPreview.Lines;
                if (original == null || original.Length == 0) return;

                // 1) Vorhandene Nummerierung am Zeilenanfang entfernen (z.B. "12) " oder "12. ")
                var cleaned = new List<string>(original.Length);
                foreach (var line in original)
                {
                    var s = line ?? string.Empty;

                    // Linksseitige Nummern entfernen
                    var ltrim = s.TrimStart();
                    int i = 0;
                    while (i < ltrim.Length && char.IsDigit(ltrim[i])) i++;
                    if (i > 0 && i < ltrim.Length && (ltrim[i] == ')' || ltrim[i] == '.'))
                    {
                        int j = i + 1;
                        if (j < ltrim.Length && char.IsWhiteSpace(ltrim[j]))
                        {
                            int startIdx = s.Length - ltrim.Length;
                            s = s.Substring(startIdx + j + 1);
                        }
                    }

                    cleaned.Add(s);
                }

                // 2) Nicht-leere Zeilen sammeln (Mod-Zeilen)
                var lines = new List<string>();
                foreach (var s in cleaned)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        lines.Add(s);
                }
                if (lines.Count == 0) return;

                // 3) Umkehren (die zuletzt geladene Mod steht oben)
                lines.Reverse();

                // 4) Neu nummerieren (1..N) — Nummerierung nur visuell im Text
                var display = new string[lines.Count];
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    int num = idx + 1;
                    display[idx] = $"{num}) {lines[idx]}";
                }

                // 5) In der Vorschau anzeigen (UI only)
                rtbPreview.Lines = display;
                SafeSetStatus($"Anzeige: {lines.Count} Mods – umgekehrt & nummeriert.");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Anzeigeformat-Fehler: " + ex.Message);
            }
        }

        // ======================================================================
        // Suche: NUR Google – Trucky-Button wurde entfernt/umgeleitet
        // ======================================================================

        /// <summary>
        /// Öffnet eine Google-Suche, auf Wunsch auf truckymods.io eingeschränkt.
        /// </summary>
        private static string BuildGoogleSiteSearchUrl(string modDisplayName)
        {
            string q = Uri.EscapeDataString($"site:truckymods.io {modDisplayName}");
            return $"https://www.google.com/search?q={q}";
        }

        /// <summary>
        /// Kompatibilität: Falls irgendwo noch ein "Trucky"-Handler aufgerufen wird,
        /// leiten wir ab jetzt IMMER auf die funktionierende Google-Suche um.
        /// </summary>
        public void OpenTruckyModsSearch(string modTextOrLine)
        {
            var term = CleanSearchTerm(modTextOrLine);
            if (string.IsNullOrWhiteSpace(term))
            {
                // Fallback: aktuelle Zeile aus Vorschau
                var line = GetCurrentPreviewLine() ?? "";
                term = CleanSearchTerm(line);
            }

            if (string.IsNullOrWhiteSpace(term))
            {
                SafeSetStatus("Suche: Kein Modname gefunden.");
                OpenUrl("https://truckymods.io/"); // neutrale Seite, falls gar kein Begriff
                return;
            }

            OpenUrl(BuildGoogleSiteSearchUrl(term));
            SafeSetStatus($"Suche (Google/truckymods.io): {term}");
        }

        public void OpenTruckySearch(string modDisplayNameOrLine)
            => OpenTruckyModsSearch(modDisplayNameOrLine);

        /// <summary>
        /// Macht aus einer kompletten active_mods-Zeile oder einem beliebigen Text
        /// einen sauberen Suchbegriff (bevorzugt Anzeigename rechts vom '|').
        /// </summary>
        private static string CleanSearchTerm(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            ExtractModKeys(input, out var modName, out var packageId, out var _);
            if (!string.IsNullOrWhiteSpace(modName))
                return modName.Trim();
            if (!string.IsNullOrWhiteSpace(packageId))
                return packageId.Trim();

            var s = input.Trim();

            // a) active_mods[...] Präfix wegschneiden
            var idxColon = s.IndexOf(':');
            if (idxColon >= 0 && s.StartsWith("active_mods", StringComparison.OrdinalIgnoreCase))
                s = s[(idxColon + 1)..].Trim();

            // b) Tokens in Anführungszeichen herauslösen
            var q1 = s.IndexOf('"');
            var q2 = s.LastIndexOf('"');
            if (q1 >= 0 && q2 > q1)
                s = s.Substring(q1 + 1, q2 - q1 - 1);

            // c) Wenn '|' vorhanden, rechten Teil (Anzeigename) nehmen
            var pipe = s.LastIndexOf('|');
            if (pipe >= 0 && pipe < s.Length - 1)
                s = s[(pipe + 1)..];

            return s.Trim();
        }

        /// <summary>
        /// Liest die aktuelle Zeile aus der Vorschau (rtbPreview) anhand der Selektion/Einfügemarke.
        /// </summary>
        private string? GetCurrentPreviewLine()
        {
            try
            {
                var text = rtbPreview.Text;
                if (string.IsNullOrEmpty(text)) return null;

                int pos = rtbPreview.SelectionStart;
                if (pos < 0 || pos > text.Length) pos = 0;

                int lineStart = text.LastIndexOf('\n', Math.Max(0, pos - 1));
                lineStart = (lineStart == -1) ? 0 : lineStart + 1;

                int lineEnd = text.IndexOf('\n', pos);
                if (lineEnd == -1) lineEnd = text.Length;

                var line = text.Substring(lineStart, lineEnd - lineStart);
                return line.TrimEnd('\r', '\n');
            }
            catch
            {
                return null;
            }
        }

        // ======================================================================
        // UI-Aufräumen: Trucky-UI wirklich weg (Buttons + DataGridView-Spalten)
        // ======================================================================

        /// <summary>
        /// Entfernt alle sichtbaren "Trucky"-Bedienelemente:
        /// - Buttons (Name/Text/Tag enthält "trucky")
        /// - DataGridView-Spalten (HeaderText/Name enthält "trucky")
        /// </summary>
        private void RemoveTruckyUI(Control root)
        {
            try
            {
                // 1) Buttons ausblenden/deaktivieren
                HideTruckyButtonsRecursive(root);

                // 2) DGV-Spalten, die nach Trucky aussehen, entfernen
                RemoveTruckyColumnsRecursive(root);
            }
            catch
            {
                // stillschweigend – UI bleibt benutzbar
            }
        }

        private void HideTruckyButtonsRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button b)
                {
                    var name = (b.Name ?? "").ToLowerInvariant();
                    var text = (b.Text ?? "").ToLowerInvariant();
                    var tag  = (b.Tag?.ToString() ?? "").ToLowerInvariant();

                    if (name.Contains("trucky") || text.Contains("trucky") || tag.Contains("trucky"))
                    {
                        b.Visible = false;
                        b.Enabled = false;
                    }
                }

                if (c.HasChildren)
                    HideTruckyButtonsRecursive(c);
            }
        }

        private void RemoveTruckyColumnsRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is DataGridView dgv)
                {
                    try
                    {
                        for (int i = dgv.Columns.Count - 1; i >= 0; i--)
                        {
                            var col = dgv.Columns[i];
                            var name  = (col.Name ?? "").ToLowerInvariant();
                            var header= (col.HeaderText ?? "").ToLowerInvariant();

                            if (name.Contains("trucky") || header.Contains("trucky"))
                            {
                                dgv.Columns.RemoveAt(i);
                            }
                        }
                    }
                    catch { /* egal */ }
                }

                if (c.HasChildren)
                    RemoveTruckyColumnsRecursive(c);
            }
        }

        // ======================================================================
        // Nummern-Spalte im Mods-Grid (UI-only)
        // ======================================================================

        /// <summary>
        /// Sucht das Mods-Grid (erstes DataGridView im Formular), legt bei Bedarf eine
        /// Index-Spalte an (Name=colIndex, Header="#") und nummeriert alle Zeilen 1..N.
        /// </summary>
        private void AddOrUpdateIndexColumn()
        {
            try
            {
                var dgv = FindFirstDataGridView(this);
                if (dgv == null) return;

                // Spalte vorhanden?
                DataGridViewColumn? colIndex = null;
                foreach (DataGridViewColumn c in dgv.Columns)
                {
                    if (string.Equals(c.Name, "colIndex", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.HeaderText, "#", StringComparison.OrdinalIgnoreCase))
                    {
                        colIndex = c; break;
                    }
                }

                // Wenn nicht vorhanden -> anlegen und an Position 0 einsortieren
                if (colIndex == null)
                {
                    var newCol = new DataGridViewTextBoxColumn
                    {
                        Name = "colIndex",
                        HeaderText = "#",
                        ReadOnly = true,
                        Width = 48,
                        Frozen = true,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                    };
                    dgv.Columns.Insert(0, newCol);
                    colIndex = newCol;
                }
                else
                {
                    // Sicherstellen, dass sie an Position 0 steht
                    if (colIndex.DisplayIndex != 0)
                        colIndex.DisplayIndex = 0;
                }

                // Alle Zeilen durchzählen (sichtbare Reihenfolge)
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    var row = dgv.Rows[i];
                    if (!row.IsNewRow)
                    {
                        row.Cells["colIndex"].Value = (i + 1).ToString();
                    }
                }
            }
            catch
            {
                // UI bleibt nutzbar, Nummernspalte optional
            }
        }

        /// <summary>
        /// Findet das erste DataGridView im Control-Baum (wir gehen davon aus, dass
        /// du nur eins für die Modliste hast).
        /// </summary>
        private static DataGridView? FindFirstDataGridView(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is DataGridView dgv) return dgv;
                if (c.HasChildren)
                {
                    var found = FindFirstDataGridView(c);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }
}
