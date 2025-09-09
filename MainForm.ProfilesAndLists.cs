// MainForm.ProfilesAndLists.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Helper-Klasse für ComboBox (Anzeigetext + Wert)
        private sealed class ComboItem
        {
            public string Text { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        // ------------------- PROFILES -------------------

        private void LoadProfiles_Local5()
        {
            try
            {
                cbProfile.BeginUpdate();
                cbProfile.Items.Clear();

                var st = SettingsService.Load();

                var game = GetCurrentGameForPaths();
                var standard = GetDefaultProfilesPath(game);
                var custom = GetCustomProfilesPathFromSettings(st, game);

                var candidates = new List<string>();

                // Erst Custom (wenn gültig), dann Standard
                if (!string.IsNullOrWhiteSpace(custom) && Directory.Exists(custom))
                    candidates.Add(custom);
                if (Directory.Exists(standard))
                    candidates.Add(standard);

                // Duplikate vermeiden
                candidates = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Alle direkten Unterordner als Profile
                var profileDirs = new List<string>();
                foreach (var basePath in candidates)
                {
                    try
                    {
                        profileDirs.AddRange(Directory.GetDirectories(basePath));
                    }
                    catch { /* ignore */ }
                }

                // Duplikate entfernen
                profileDirs = profileDirs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Profile hübsch aufbereiten
                var items = new List<ComboItem>();
                foreach (var dir in profileDirs)
                {
                    var display = GetPrettyProfileName(dir, allowDecrypt: chkAutoDec.Checked);
                    items.Add(new ComboItem(display, dir));
                }

                // Sortierung nach Anzeigetext
                foreach (var it in items.OrderBy(i => i.Text, StringComparer.CurrentCultureIgnoreCase))
                    cbProfile.Items.Add(it);

                if (cbProfile.Items.Count > 0) cbProfile.SelectedIndex = 0;

                SafeSetStatus(cbProfile.Items.Count > 0
                    ? $"Profile geladen: {cbProfile.Items.Count}"
                    : "Keine Profile gefunden.");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Profile: " + ex.Message);
            }
            finally
            {
                cbProfile.EndUpdate();
            }
        }

        private Game GetCurrentGameForPaths()
        {
            try { return cbGame.SelectedIndex == 1 ? Game.ATS : Game.ETS2; }
            catch { return Game.ETS2; }
        }

        private static string GetDefaultProfilesPath(Game game)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return game == Game.ATS
                ? Path.Combine(docs, "American Truck Simulator", "profiles")
                : Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
        }

        private static string GetCustomProfilesPathFromSettings(AppSettings st, Game game)
        {
            return game == Game.ATS ? st.AtsProfilesPath?.Trim() ?? "" : st.Ets2ProfilesPath?.Trim() ?? "";
        }

        private string? GetSelectedProfilePath()
        {
            if (cbProfile.SelectedItem is ComboItem ci) return ci.Value;
            return cbProfile.SelectedItem as string;
        }

        // ------------------- MODLISTS (ETS2/ATS Unterordner) -------------------

        private void LoadModlists_Local()
        {
            try
            {
                cbList.BeginUpdate();
                cbList.Items.Clear();

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var listsRoot = Path.Combine(baseDir, "modlists");

                // Unterordner je Spiel wählen
                var sub = (cbGame.SelectedIndex == 1) ? "ATS" : "ETS2";
                var listsDir = Path.Combine(listsRoot, sub);

                // Ordner sicherstellen (damit Nutzer gleich sieht, wo es liegt)
                Directory.CreateDirectory(listsDir);

                // Nur .txt Modlisten laden
                var files = Directory.GetFiles(listsDir, "*.txt", SearchOption.TopDirectoryOnly)
                                     .OrderBy(Path.GetFileName)
                                     .ToList();

                foreach (var f in files)
                {
                    // Optional: schöner Anzeigename ohne Extension
                    var display = Path.GetFileNameWithoutExtension(f);
                    cbList.Items.Add(new ComboItem(display, f));
                }

                if (cbList.Items.Count > 0) cbList.SelectedIndex = 0;

                SafeSetStatus(cbList.Items.Count > 0
                    ? $"Modlisten ({sub}) gefunden: {cbList.Items.Count}"
                    : $"Keine Modlisten im Ordner: {Path.GetFullPath(listsDir)}");
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Modlisten: " + ex.Message);
            }
            finally
            {
                cbList.EndUpdate();
            }
        }

        private string? GetSelectedModlistPath()
        {
            // Wir speichern den vollen Pfad im ComboItem.Value – also einfach zurückgeben
            if (cbList.SelectedItem is ComboItem ci) return ci.Value;

            // Falls jemals nur der Dateiname eingetragen sein sollte: auf den akt. Spiel-Unterordner mappen
            var name = cbList.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(name)) return null;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var listsRoot = Path.Combine(baseDir, "modlists");
            var sub = (cbGame.SelectedIndex == 1) ? "ATS" : "ETS2";
            var listsDir = Path.Combine(listsRoot, sub);

            var candidate = Path.Combine(listsDir, name);
            if (File.Exists(candidate)) return candidate;

            // evtl. ohne .txt gewählt
            var withTxt = candidate.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                ? candidate
                : candidate + ".txt";
            return File.Exists(withTxt) ? withTxt : null;
        }

        private void DoLoadSelectedModlistToPreview_Local()
        {
            try
            {
                rtbPreview.Clear();
                var path = GetSelectedModlistPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    SafeSetStatus("Keine Modliste gewählt.");
                    return;
                }

                var text = File.ReadAllText(path);
                rtbPreview.Text = text;
                SafeSetStatus("Modliste in Vorschau geladen: " + Path.GetFileName(path));
                // Tabelle aus dem Vorschau-Text neu aufbauen (inkl. Info-Spalte/Notizen)
                RebuildPreviewGridFromRtb();
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Modliste: " + ex.Message);
            }
            PreviewOrder_Run(reverse: true, numberFromTopOne: true);
        }

        private void DoLoadSelectedModlistToPreview_Local(string path)
        {
            try
            {
                rtbPreview.Clear();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    SafeSetStatus("Keine Modliste gewählt.");
                    return;
                }

                var text = File.ReadAllText(path);
                rtbPreview.Text = text;
                SafeSetStatus("Modliste in Vorschau geladen: " + Path.GetFileName(path));
                // Tabelle aus dem Vorschau-Text neu aufbauen (inkl. Info-Spalte/Notizen)
                RebuildPreviewGridFromRtb();
            }
            catch (Exception ex)
            {
                SafeSetStatus("Fehler beim Laden der Modliste: " + ex.Message);
            }
            PreviewOrder_Run(reverse: true, numberFromTopOne: true);
        }

        // ------------------- Helpers: Pretty Profile Name -------------------

        private string GetPrettyProfileName(string profileDir, bool allowDecrypt)
        {
            // 1) Versuche profile_name aus profile.sii
            var siiPath = Path.Combine(profileDir, "profile.sii");
            var name = TryReadProfileNameFromSii(siiPath, allowDecrypt);
            if (!string.IsNullOrWhiteSpace(name))
                return name!;

            // 2) Versuche Ordnername als Hex zu dekodieren
            var folder = Path.GetFileName(profileDir) ?? profileDir;
            var decoded = TryDecodeHexFolder(folder);
            if (!string.IsNullOrWhiteSpace(decoded))
                return decoded!;

            // 3) Fallback: roher Ordnername
            return folder;
        }

        private static string? TryDecodeHexFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return null;
            if (folderName.Length % 2 != 0) return null;

            for (int i = 0; i < folderName.Length; i++)
            {
                char c = folderName[i];
                bool hex = (c >= '0' && c <= '9') ||
                           (c >= 'a' && c <= 'f') ||
                           (c >= 'A' && c <= 'F');
                if (!hex) return null;
            }

            try
            {
                var bytes = new byte[folderName.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(folderName.Substring(i * 2, 2), 16);

                var text = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrWhiteSpace(text) && text.IndexOfAny(new[] { '\0', '\r' }) < 0)
                    return text;
            }
            catch { }
            return null;
        }

        private string? TryReadProfileNameFromSii(string siiPath, bool allowDecrypt)
        {
            try
            {
                var text = TryReadSiiText(siiPath, allowDecrypt); // kommt aus MainForm.SiiDecrypt.cs
                if (string.IsNullOrWhiteSpace(text)) return null;

                // profile_name: "Mein Profil"
                var m = Regex.Match(text, @"profile_name\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return null;
        }

        // Fügen Sie dies in die Datei MainForm.ProfilesAndLists.cs ein (oder eine andere MainForm-Partial-Datei):

        // Add near other fields:
        private bool _autoListLoading;

        // Add methods (nur falls nicht vorhanden):
        private void AutoLoadSelectedModlist_Safe()
        {
            if (_autoListLoading) return;
            _autoListLoading = true;
            try
            {
                // Try to call the existing loader. Prefer the parameterless,
                // fall back to path-based if that’s the one we have.
                if (MethodExists(nameof(DoLoadSelectedModlistToPreview_Local)))
                {
                    try { DoLoadSelectedModlistToPreview_Local(); return; } catch { /* try path overload below */ }
                }

                var listPath = GetSelectedModlistPath();
                if (!string.IsNullOrWhiteSpace(listPath) && System.IO.File.Exists(listPath))
                {
                    // If there is an overload with path:
                    try { DoLoadSelectedModlistToPreview_Local(listPath); return; } catch { /* no-op */ }
                }

                // As a fallback just set a status so user sees something
                SafeSetStatus(GetCurrentLanguageIsEnglish()
                    ? "No mod list selected or file missing."
                    : "Keine Modliste ausgewählt oder Datei fehlt.");
            }
            finally { _autoListLoading = false; }

            PreviewOrder_Run(reverse: true, numberFromTopOne: true);
        }

        // tiny reflection helper so we don’t create duplicates elsewhere
        private static bool MethodExists(string name)
        {
            try { return typeof(MainForm).GetMethod(name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) != null; }
            catch { return false; }
        }

        // Beispiel: Im Handler für cbList.SelectedIndexChanged (oder direkt vor dem Laden einer neuen Liste)
        private void cbList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            Persist_BeginListSwitch(); // <<== NEU, ganz oben
            Persist_FlushCurrentEdits(); // <<== wie gehabt
            // ... bestehende Logik ...
            DoLoadSelectedModlistToPreview_Local();
        }

        // 7) Analog in cbGame_SelectedIndexChanged (falls vorhanden):
        private void cbGame_SelectedIndexChanged(object? sender, EventArgs e)
        {
            Persist_BeginListSwitch(); // <<== NEU, ganz oben
            // ... bestehende Logik ...
        }

/// <summary>
/// Called at the end of grid rebuild to wire persistence and load edits.
/// </summary>
private void Persist_AfterGridFilled_Hook()
{
    Persist_EnsureGridWired();
    if (!IsDisposed && _gridMods != null && !_gridMods.IsDisposed)
    {
        BeginInvoke(new Action(() =>
        {
            try { Persist_LoadEditsForCurrentList(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Persist load failed: " + ex.Message);
            }
        }));
    }
}

#if false
private void RebuildPreviewGridFromRtb()
{
    // ... bestehende Logik zum Füllen und Nummerieren des Grids ...

    Persist_EnsureGridWired();
    if (!IsDisposed && _gridMods != null && !_gridMods.IsDisposed)
        BeginInvoke(new Action(() => { Persist_LoadEditsForCurrentList(); }));
}
#endif

        // Helper: Resolves the real profile directory from the current selection in cbProfile
        private bool ProfPaths_TryResolveCurrent(out string resolvedDir)
        {
            resolvedDir = "";
            try
            {
                var root = GetProfilesRootDir();
                var sel  = cbProfile?.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(sel))
                    return false;

                // Strategy 1: SelectedValue carries a path (if data-bound that way)
                if (cbProfile?.SelectedValue is string valPath && System.IO.Directory.Exists(valPath))
                { resolvedDir = valPath; return true; }

                // Strategy 2: direct combine root + selected text (if dropdown holds folder name)
                var try1 = System.IO.Path.Combine(root, sel);
                if (System.IO.Directory.Exists(try1)) { resolvedDir = try1; return true; }

                // Strategy 3: search first-level dirs under root for exact dir name match (case-insensitive)
                foreach (var dir in System.IO.Directory.EnumerateDirectories(root))
                {
                    var name = System.IO.Path.GetFileName(dir);
                    if (string.Equals(name, sel, StringComparison.OrdinalIgnoreCase))
                    { resolvedDir = dir; return true; }
                }

                // Strategy 4: also check sibling "steam_profiles" beside root
                var parent = System.IO.Directory.GetParent(root)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    var steamProfiles = System.IO.Path.Combine(parent, "steam_profiles");
                    if (System.IO.Directory.Exists(steamProfiles))
                    {
                        foreach (var dir in System.IO.Directory.EnumerateDirectories(steamProfiles))
                        {
                            var name = System.IO.Path.GetFileName(dir);
                            if (string.Equals(name, sel, StringComparison.OrdinalIgnoreCase))
                            { resolvedDir = dir; return true; }
                        }

                        // Strategy 5: match by display name inside profile.sii (after auto-decrypt)
                        foreach (var dir in System.IO.Directory.EnumerateDirectories(steamProfiles))
                            if (Prof_ReadDisplayName(dir) is string disp && string.Equals(disp, sel, StringComparison.OrdinalIgnoreCase))
                            { resolvedDir = dir; return true; }
                    }
                }

                // Strategy 6: match by display name under root
                foreach (var dir in System.IO.Directory.EnumerateDirectories(root))
                    if (Prof_ReadDisplayName(dir) is string disp && string.Equals(disp, sel, StringComparison.OrdinalIgnoreCase))
                    { resolvedDir = dir; return true; }

                return false;
            }
            catch { return false; }
        }

        // Extracts display name from ...\profile.sii (already decrypted)
        private static string? Prof_ReadDisplayName(string profileDir)
        {
            try
            {
                var sii = System.IO.Path.Combine(profileDir, "profile.sii");
                if (!System.IO.File.Exists(sii)) return null;
                foreach (var line in System.IO.File.ReadLines(sii))
                {
                    var idx = line.IndexOf("profile_name:", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var s = line.Substring(idx + "profile_name:".Length).Trim();
                        s = s.Trim('"');
                        return s;
                    }
                }
            }
            catch {}
            return null;
        }

        private void BtnProfClone_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ProfPaths_TryResolveCurrent(out var src) || !System.IO.Directory.Exists(src))
                { MessageBox.Show("Profilordner nicht gefunden."); return; }

                var currentDisplay = Prof_ReadDisplayName(src) ?? System.IO.Path.GetFileName(src);
                var suffix = GetCurrentLanguageIsEnglish() ? " - clone" : " - Klon";
                var proposal = currentDisplay + suffix;
                string? newDisplay = ShowProfileNameDialog(GetCurrentLanguageIsEnglish() ? "Clone profile" : "Profil klonen", proposal);
                if (string.IsNullOrWhiteSpace(newDisplay)) return;
                newDisplay = Scs_ValidateDisplayName(newDisplay);

                // Zielordner-Name = hex(UTF8(newDisplay))
                var parentRoot = GetProfileParentRoot(src);
                var newFolderName = Scs_ProfileDisplayToFolder(newDisplay);
                var dst = System.IO.Path.Combine(parentRoot, newFolderName);
                if (System.IO.Directory.Exists(dst)) { MessageBox.Show("Zielprofil (Ordner) existiert bereits."); return; }

                DirCopyRecursive(src, dst);
                // Anzeigenamen in profile.sii setzen
                Scs_WriteProfileDisplayName(dst, newDisplay);

                // Profile neu laden & ggf. vorselektieren
                try { LoadProfiles_Local(); } catch {}
                if (cbProfile != null)
                {
                    foreach (var it in cbProfile.Items)
                    {
                        if (string.Equals(it?.ToString(), newDisplay, StringComparison.OrdinalIgnoreCase))
                        { cbProfile.SelectedItem = it; break; }
                    }
                }
            }
            catch (System.Exception ex) { MessageBox.Show("Klonen fehlgeschlagen:\n" + ex.Message); }
        }

        private void BtnProfRename_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ProfPaths_TryResolveCurrent(out var src) || !System.IO.Directory.Exists(src))
                { MessageBox.Show("Profilordner nicht gefunden."); return; }

                var currentDisplay = Prof_ReadDisplayName(src) ?? System.IO.Path.GetFileName(src);
                string? newDisplay = ShowProfileNameDialog(GetCurrentLanguageIsEnglish() ? "Rename profile" : "Profil umbenennen", currentDisplay);
                if (string.IsNullOrWhiteSpace(newDisplay)) return;
                newDisplay = Scs_ValidateDisplayName(newDisplay);

                var parentRoot = GetProfileParentRoot(src);
                var newFolderName = Scs_ProfileDisplayToFolder(newDisplay);
                var dst = System.IO.Path.Combine(parentRoot, newFolderName);
                if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase) == false &&
                    System.IO.Directory.Exists(dst))
                { MessageBox.Show("Zielprofil (Ordner) existiert bereits."); return; }

                // Falls Quell- und Zielordner gleich (Name ergibt gleichen Hex) → nur profile.sii updaten
                if (!string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                    System.IO.Directory.Move(src, dst);

                Scs_WriteProfileDisplayName(dst, newDisplay);

                try { LoadProfiles_Local(); } catch {}
                if (cbProfile != null)
                {
                    foreach (var it in cbProfile.Items)
                    {
                        if (string.Equals(it?.ToString(), newDisplay, StringComparison.OrdinalIgnoreCase))
                        { cbProfile.SelectedItem = it; break; }
                    }
                }
            }
            catch (System.Exception ex) { MessageBox.Show("Umbenennen fehlgeschlagen:\n" + ex.Message); }
        }

        private void BtnProfDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ProfPaths_TryResolveCurrent(out var src) || !System.IO.Directory.Exists(src))
                { MessageBox.Show("Profilordner nicht gefunden."); return; }

                var currentName = System.IO.Path.GetFileName(src);

                if (MessageBox.Show($"Profil '{currentName}' wirklich löschen?", "Profil löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                System.IO.Directory.Delete(src, true);

                try { LoadProfiles_Local(); } catch {}
            }
            catch (System.Exception ex) { MessageBox.Show("Löschen fehlgeschlagen:\n" + ex.Message); }
        }

// Maximal sinnvolle Länge des Anzeigenamens (konservativ)
private const int SCS_PROFILE_NAME_MAX = 20;

// UTF-8 → hex (lowercase) nach SCS-Schema für Ordnernamen
private static string Scs_ProfileDisplayToFolder(string displayName)
{
    if (displayName == null) displayName = "";
    var bytes = System.Text.Encoding.UTF8.GetBytes(displayName);
    var sb = new System.Text.StringBuilder(bytes.Length * 2);
    foreach (var b in bytes) sb.Append(b.ToString("x2")); // lowercase hex
    return sb.ToString();
}

// profile.sii: profile_name: "..."
private static void Scs_WriteProfileDisplayName(string profileDir, string displayName)
{
    var sii = System.IO.Path.Combine(profileDir, "profile.sii");
    if (!System.IO.File.Exists(sii))
        return; // nichts hart bauen – nur setzen, wenn vorhanden

    var lines = System.IO.File.ReadAllLines(sii);
    bool replaced = false;
    for (int i = 0; i < lines.Length; i++)
    {
        var idx = lines[i].IndexOf("profile_name:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            // ersetze gesamte Zeile (einfach & robust)
            lines[i] = " profile_name: \"" + displayName.Replace("\"", "\\\"") + "\"";
            replaced = true;
            break;
        }
    }

    if (!replaced)
    {
        // einfache Fallback-Strategie: am Anfang einfügen
        // (optional: smarter innerhalb der "profile_unit", aber hier minimal-invasiv)
        var list = lines.ToList();
        list.Insert(0, " profile_name: \"" + displayName.Replace("\"", "\\\"") + "\"");
        lines = list.ToArray();
    }

    System.IO.File.WriteAllLines(sii, lines, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

// Ermittelt Root von src (profiles oder steam_profiles)
private static string GetProfileParentRoot(string srcDir)
{
    var parent = System.IO.Directory.GetParent(srcDir)?.FullName ?? "";
    return parent;
}

// Validiert & kürzt Anzeigename
private static string Scs_ValidateDisplayName(string input)
{
    var name = (input ?? "").Trim();
    if (name.Length > SCS_PROFILE_NAME_MAX)
        name = name.Substring(0, SCS_PROFILE_NAME_MAX);
    return name;
}

// Rekursiv kopieren
private static void DirCopyRecursive(string srcDir, string dstDir)
{
    System.IO.Directory.CreateDirectory(dstDir);
    foreach (var f in System.IO.Directory.GetFiles(srcDir))
        System.IO.File.Copy(f, System.IO.Path.Combine(dstDir, System.IO.Path.GetFileName(f)), overwrite: false);
    foreach (var d in System.IO.Directory.GetDirectories(srcDir))
        DirCopyRecursive(d, System.IO.Path.Combine(dstDir, System.IO.Path.GetFileName(d)));
}
    }
}
