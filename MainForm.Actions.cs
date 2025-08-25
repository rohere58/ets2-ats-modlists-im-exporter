// MainForm.Actions.cs  — v2 (lockerere Erkennung der active_mods-Einträge)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // ============================================================
        // Button: "Modliste übernehmen" – reines 1:1 Copy&Paste
        // ============================================================
        private void DoApplyModlist()
        {
            try
            {
                var modlistPath = GetSelectedModlistPath();
                if (string.IsNullOrWhiteSpace(modlistPath) || !File.Exists(modlistPath))
                {
                    MessageBox.Show(this, "Bitte eine Modliste auswählen.", "Modliste übernehmen",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var profileDir = GetSelectedProfilePath();
                if (string.IsNullOrWhiteSpace(profileDir) || !Directory.Exists(profileDir))
                {
                    MessageBox.Show(this, "Kein Profil ausgewählt.", "Modliste übernehmen",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var profileSii = Path.Combine(profileDir, "profile.sii");
                if (!File.Exists(profileSii))
                {
                    MessageBox.Show(this, "Im ausgewählten Profil wurde keine profile.sii gefunden.",
                        "Modliste übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 1) Modlistentext lesen (LF-normalisiert)
                var modlistRaw = AM_NormalizeLf(File.ReadAllText(modlistPath));

                // 2) Neuen active_mods-Block exakt gewinnen (bevorzugt: flaches Format; sonst: Block mit {} 1:1)
                if (!AM_TryExtractActiveModsRawBlock(modlistRaw, out var newBlockRaw, out int count))
                {
                    MessageBox.Show(this,
                        "Die Modliste enthält keinen erkennbaren active_mods-Bereich.",
                        "Modliste übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3) profile.sii laden
                var profileLf = AM_NormalizeLf(File.ReadAllText(profileSii));

                // 4) Alle vorhandenen active_mods-Bereiche entfernen und neuen an Stelle des ersten einfügen
                var finalLf = AM_ReplaceAllActiveMods(profileLf, newBlockRaw, out bool hadAny, out int insertAtLine);

                // 5) Sicher schreiben (CRLF wiederherstellen + .bak)
                AM_WriteFileWithBackup(profileSii, finalLf.Replace("\n", Environment.NewLine));

                SafeSetStatus((hadAny ? "active_mods ersetzt" : "active_mods angefügt")
                               + $" – {count} Einträge – Zeile {insertAtLine + 1}");
                MessageBox.Show(this, $"Fertig.\nÜbernommen: {count} Mods", "Modliste übernehmen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Fehler beim Übernehmen:\n\n" + ex.Message,
                    "Modliste übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // EXTRAKTION DES NEUEN BLOCKS
        // – Flaches Format: Header + alle Zeilen, die mit 'active_mods[Zahl]:' beginnen
        // – Blockformat: { … } 1:1 übernehmen
        // ============================================================

        private static readonly Regex ReHeaderMaybeBrace =
            new(@"^\s*active_mods\s*:\s*\d+(\s*\{)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // LOCKER: nur Prefix prüfen – danach beliebig (damit Sonderfälle nicht abbrechen)
        private static readonly Regex ReEntryPrefix =
            new(@"^\s*active_mods\s*\[\s*\d+\s*\]\s*:\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static bool AM_TryExtractActiveModsRawBlock(string modlistLf, out string newBlock, out int count)
        {
            newBlock = string.Empty;
            count = 0;

            var lines = modlistLf.Split('\n');
            int n = lines.Length;

            // 1) Kopfzeile finden
            int headerIdx = -1;
            for (int i = 0; i < n; i++)
            {
                if (ReHeaderMaybeBrace.IsMatch(lines[i]))
                {
                    headerIdx = i;
                    break;
                }
            }
            if (headerIdx < 0) return false;

            // 2) Prüfen, ob Blockformat ({...}) verwendet wird
            bool hasBrace = lines[headerIdx].Contains("{");
            int openIdx = -1;
            if (hasBrace) openIdx = headerIdx;
            else
            {
                for (int i = headerIdx + 1; i < n; i++)
                {
                    if (lines[i].Contains("{")) { hasBrace = true; openIdx = i; break; }
                    // Sobald eine Nicht-Entry-Zeile (außer leer) kommt, abbrechen -> flach.
                    var t = lines[i].TrimEnd('\r');
                    if (t.Length == 0) continue;
                    if (!ReEntryPrefix.IsMatch(t)) break;
                }
            }

            if (hasBrace)
            {
                // 3A) Block 1:1 ausschneiden (bis passende '}' – Klammerbalance)
                int start = headerIdx;
                int brace = 0;
                int end = -1;

                for (int i = headerIdx; i < n; i++)
                {
                    foreach (var ch in lines[i])
                    {
                        if (ch == '{') brace++;
                        else if (ch == '}') brace--;
                    }

                    if (brace == 0 && i >= openIdx)
                    {
                        end = i;
                        break;
                    }
                }
                if (end < 0) end = n - 1; // failsafe

                var slice = lines[start..(end + 1)];
                newBlock = string.Join("\n", slice).TrimEnd('\n', '\r');

                // Einträge zählen (locker über Prefix)
                count = slice.Count(s => ReEntryPrefix.IsMatch(s));
                return true;
            }
            else
            {
                // 3B) Flaches Format: Header + alle folgenden active_mods[i]-Zeilen (kontigu)
                var buf = new List<string>();
                buf.Add(lines[headerIdx].TrimEnd('\r'));

                int i = headerIdx + 1;
                for (; i < n; i++)
                {
                    var raw = lines[i];
                    var t = raw.TrimEnd('\r');

                    if (t.Length == 0) continue;            // echte Leerzeilen überspringen
                    if (!ReEntryPrefix.IsMatch(t)) break;    // Ende des Blocks

                    // Originalzeile (ohne CR) übernehmen – 1:1
                    buf.Add(t);
                }

                count = Math.Max(0, buf.Count - 1);
                newBlock = string.Join("\n", buf);
                return true;
            }
        }

        // ============================================================
        // ERSETZEN IM PROFILE.SII
        // – Entfernt ALLE vorhandenen active_mods-Bereiche (flach oder {…})
        // – Fügt neuen an Stelle des ersten gefundenen ein (oder ans Ende, wenn keiner existiert)
        // – Fügt KEINE zusätzlichen Leerzeilen/Klammern ein
        // ============================================================

        private static string AM_ReplaceAllActiveMods(string profileLf, string newBlockRaw, out bool hadAny, out int insertedAtLine)
        {
            var lines = profileLf.Split('\n').ToList();
            var ranges = AM_FindAllActiveModsRanges(lines);

            hadAny = ranges.Count > 0;
            insertedAtLine = hadAny ? ranges.Min(r => r.start) : lines.Count;

            var keep = new List<string>(lines.Count + 32);
            var skipMask = new bool[lines.Count];

            foreach (var r in ranges)
                for (int i = r.start; i <= r.end && i < skipMask.Length; i++)
                    skipMask[i] = true;

            // Vorbereich
            for (int i = 0; i < Math.Min(insertedAtLine, lines.Count); i++)
                if (!skipMask[i]) keep.Add(lines[i]);

            // Neuen Block 1:1 einfügen (ohne extra Leerzeile)
            var newLines = AM_NormalizeLf(newBlockRaw).Split('\n');
            keep.AddRange(newLines);

            // Nachbereich
            for (int i = insertedAtLine; i < lines.Count; i++)
                if (!skipMask[i]) keep.Add(lines[i]);

            return string.Join("\n", keep);
        }

        private static List<(int start, int end)> AM_FindAllActiveModsRanges(List<string> lines)
        {
            var ranges = new List<(int, int)>();
            int n = lines.Count;

            int i = 0;
            while (i < n)
            {
                if (!ReHeaderMaybeBrace.IsMatch(lines[i])) { i++; continue; }

                int start = i;

                // Prüfen, ob Blockstil oder flach
                bool hasBrace = lines[i].Contains("{");
                int openLine = hasBrace ? i : -1;

                if (!hasBrace)
                {
                    // Suche '{' in Folgelinien, stoppe, wenn vor '{' eine Nicht-Entry-Zeile kommt
                    for (int k = i + 1; k < n; k++)
                    {
                        var s = lines[k];
                        if (s.Contains("{")) { hasBrace = true; openLine = k; break; }
                        if (s.Trim().Length == 0) continue;
                        if (!ReEntryPrefix.IsMatch(s)) break; // flaches Format
                    }
                }

                if (hasBrace)
                {
                    // Klammerbereich balancieren
                    int brace = 0;
                    int end = i;
                    for (int k = i; k < n; k++)
                    {
                        foreach (var ch in lines[k])
                        {
                            if (ch == '{') brace++;
                            else if (ch == '}') brace--;
                        }
                        end = k;
                        if (brace == 0 && k >= openLine) break;
                    }
                    ranges.Add((start, end));
                    i = end + 1;
                }
                else
                {
                    // Flaches Format: header + alle folgenden Entry-Zeilen (kontigu),
                    int end = i;
                    int k = i + 1;
                    while (k < n)
                    {
                        var t = lines[k].TrimEnd('\r');
                        if (t.Length == 0) { k++; continue; }      // echte Leerzeilen überspringen
                        if (!ReEntryPrefix.IsMatch(t)) break;
                        end = k;
                        k++;
                    }
                    ranges.Add((start, end));
                    i = end + 1;
                }
            }

            return ranges;
        }

        // ============================================================
        // Utils (eindeutig benannt, um Kollisionen zu vermeiden)
        // ============================================================
        private static string AM_NormalizeLf(string s)
            => s.Replace("\r\n", "\n").Replace("\r", "\n");

        private static void AM_WriteFileWithBackup(string path, string content)
        {
            try { File.Copy(path, path + ".bak", overwrite: true); } catch { /* best effort */ }
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}
