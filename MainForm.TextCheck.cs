// MainForm.TextCheck.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Wird vom "Text-Check"-Button via TryInvokeNoArg("DoCheckProfileFormat") aufgerufen.
        /// Prüft die profile.sii des gewählten Profils: Text/Binär, Größe, optional Vorschau.
        /// </summary>
        private void DoCheckProfileFormat()
        {
            try
            {
                // Hole den aktuell ausgewählten Profilnamen
                string? selectedProfile = cbProfile.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selectedProfile))
                {
                    MessageBox.Show(this, "Bitte ein Profil auswählen.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Baue den Pfad zur profile.sii
                string? profilePath = TryGetSelectedProfileFolderPath_CheckOnly();
                if (string.IsNullOrWhiteSpace(profilePath))
                {
                    MessageBox.Show(this, "Profilordner konnte nicht ermittelt werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string siiPath = Path.Combine(profilePath, "profile.sii");

                // Jetzt prüfen, ob die Datei existiert und lesbar ist
                if (!File.Exists(siiPath))
                {
                    MessageBox.Show(this, $"Datei nicht gefunden:\n{siiPath}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Datei lesen (kleinen Chunk reicht zur Erkennung)
                byte[] head;
                long size;
                using (var fs = new FileStream(siiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    size = fs.Length;
                    int take = (int)Math.Min(8192, size);
                    head = new byte[take];
                    fs.Read(head, 0, take);
                }

                bool isBinary = LooksLikeBinarySii(head);
                var sb = new StringBuilder();
                sb.AppendLine($"Profilordner : {profilePath}");
                sb.AppendLine($"Datei        : profile.sii");
                sb.AppendLine($"Größe        : {size:N0} Bytes");
                sb.AppendLine($"Format       : {(isBinary ? "BINÄR" : "TEXT")}");

                // Wenn TEXT: kleine Vorschau im Dialog anzeigen
                if (!isBinary)
                {
                    string preview = SafeReadAllTextHead(siiPath, 200); // ~200 Zeilen
                    ShowPreviewDialog("Text-Check – Vorschau (profile.sii)", sb.ToString(), preview);
                    SafeSetStatus("Text-Check: profile.sii ist Text.");
                    return;
                }

                // BINÄR: ggf. Auto-Decrypt versuchen
                bool autoDec = chkAutoDec.Checked;
                var toolsExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "SII_Decrypt.exe");
                bool canDec = File.Exists(toolsExe);

                if (autoDec && canDec)
                {
                    // Versuch: SII_Decrypt.exe ausführen und Text abgreifen
                    if (TryRunSiiDecryptGetText(toolsExe, siiPath, out string decryptedText, out string decError))
                    {
                        sb.AppendLine("Auto-Decrypt : OK (tools\\SII_Decrypt.exe)");
                        ShowPreviewDialog("Text-Check – Vorschau (entschlüsselt)", sb.ToString(), TakeFirstLines(decryptedText, 200));
                        SafeSetStatus("Text-Check: profile.sii war binär – Vorschau (entschlüsselt) angezeigt.");
                        return;
                    }
                    else
                    {
                        sb.AppendLine("Auto-Decrypt : FEHLGESCHLAGEN");
                        if (!string.IsNullOrWhiteSpace(decError))
                            sb.AppendLine("Fehler       : " + decError);
                    }
                }
                else
                {
                    sb.AppendLine("Auto-Decrypt : " + (autoDec ? "Tool nicht gefunden (tools\\SII_Decrypt.exe)" : "deaktiviert"));
                }

                // Zusammenfassung anzeigen (ohne Vorschau)
                MessageBox.Show(this, sb.ToString(), "Text-Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SafeSetStatus("Text-Check: profile.sii ist binär.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Text-Check – Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeSetStatus("Text-Check-Fehler: " + ex.Message);
            }
        }

        // ===== Helpers =========================================================

        /// <summary>
        /// Versucht aus der Combobox das Profilverzeichnis zu ermitteln.
        /// Greift NICHT in andere Teile ein – nur read-only Heuristik.
        /// </summary>
        private string? TryGetSelectedProfileFolderPath_CheckOnly()
        {
            var item = cbProfile.SelectedItem;
            if (item == null) return null;

            // 1) Direkter String, der evtl. ein Pfad ist
            if (item is string s)
            {
                if (Directory.Exists(s)) return s;
                // Fallback: Wenn String wie "name [C:\...]" formatiert wäre
                int i = s.IndexOf('['), j = s.LastIndexOf(']');
                if (i >= 0 && j > i)
                {
                    var inside = s.Substring(i + 1, j - i - 1).Trim();
                    if (Directory.Exists(inside)) return inside;
                }
            }

            // 2) Reflection: nach Property/Field mit Pfad suchen
            string? TryFromMember(object obj)
            {
                var t = obj.GetType();

                // Properties
                var propNames = new[] { "Path", "Folder", "FolderPath", "FullPath", "Dir" };
                foreach (var pn in propNames)
                {
                    var p = t.GetProperty(pn);
                    if (p?.PropertyType == typeof(string))
                    {
                        var v = p.GetValue(obj) as string;
                        if (!string.IsNullOrWhiteSpace(v) && Directory.Exists(v)) return v;
                    }
                }

                // Fields
                var fieldNames = new[] { "Path", "Folder", "FolderPath", "FullPath", "Dir" };
                foreach (var fn in fieldNames)
                {
                    var f = t.GetField(fn);
                    if (f?.FieldType == typeof(string))
                    {
                        var v = f.GetValue(obj) as string;
                        if (!string.IsNullOrWhiteSpace(v) && Directory.Exists(v)) return v;
                    }
                }

                // ToString als Pfad?
                var ts = obj.ToString();
                if (!string.IsNullOrWhiteSpace(ts) && Directory.Exists(ts)) return ts;

                return null;
            }

            var fromObj = TryFromMember(item);
            if (!string.IsNullOrWhiteSpace(fromObj)) return fromObj;

            // 3) Als letzter, sehr konservativer Fallback: Standardpfade durchsuchen und
            // schauen, ob im Unterordner eine profile.sii liegt – ohne Namens-Matching.
            try
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var tag = GetCurrentGameTag(); // "ETS2"/"ATS"
                var root = tag == "ETS2"
                    ? Path.Combine(docs, "Euro Truck Simulator 2", "profiles")
                    : Path.Combine(docs, "American Truck Simulator", "profiles");

                if (Directory.Exists(root))
                {
                    var firstHit = Directory.EnumerateDirectories(root)
                                            .Select(d => Path.Combine(d, "profile.sii"))
                                            .FirstOrDefault(File.Exists);
                    if (!string.IsNullOrWhiteSpace(firstHit))
                        return Path.GetDirectoryName(firstHit);
                }
            }
            catch { }

            return null;
        }

        private static bool LooksLikeBinarySii(byte[] head)
        {
            // Heuristik: Nullbytes oder sehr niedriger Anteil druckbarer Zeichen
            bool hasNull = head.Any(b => b == 0);
            var text = Encoding.UTF8.GetString(head);
            bool hasMarker = text.Contains("SiiNunit", StringComparison.OrdinalIgnoreCase);
            if (hasMarker) return false;           // klar Text
            if (hasNull) return true;              // klar Binär

            // fallback: „druckbare“ Zeichenquote
            int printable = text.Count(ch => ch == '\r' || ch == '\n' || (ch >= 32 && ch < 127));
            double ratio = (double)printable / Math.Max(1, text.Length);
            return ratio < 0.8;
        }

        private static string SafeReadAllTextHead(string path, int maxLines)
        {
            try
            {
                var all = File.ReadAllLines(path, Encoding.UTF8);
                if (all.Length <= maxLines) return string.Join("\r\n", all);
                return string.Join("\r\n", all.Take(maxLines)) + "\r\n…";
            }
            catch
            {
                // Fallback Latin1
                try
                {
                    var all = File.ReadAllLines(path, Encoding.GetEncoding(1252));
                    if (all.Length <= maxLines) return string.Join("\r\n", all);
                    return string.Join("\r\n", all.Take(maxLines)) + "\r\n…";
                }
                catch
                {
                    return "(Vorschau nicht lesbar.)";
                }
            }
        }

        private static string TakeFirstLines(string text, int maxLines)
        {
            var lines = text.Replace("\r", "").Split('\n');
            if (lines.Length <= maxLines) return string.Join("\r\n", lines);
            return string.Join("\r\n", lines.Take(maxLines)) + "\r\n…";
        }

        private static void ShowPreviewDialog(string title, string info, string previewText)
        {
            using var dlg = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                MaximizeBox = true,
                ClientSize = new Size(900, 600)
            };

            var lbl = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 90,
                Text = info,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var txt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Text = previewText
            };

            dlg.Controls.Add(txt);
            dlg.Controls.Add(lbl);
            dlg.ShowDialog();
        }

        private static bool TryRunSiiDecryptGetText(string exePath, string inputSiiPath, out string text, out string error)
        {
            text = ""; error = "";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{inputSiiPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory
                };

                using var p = Process.Start(psi);
                if (p == null) { error = "Prozessstart fehlgeschlagen."; return false; }

                var output = p.StandardOutput.ReadToEnd();
                var err = p.StandardError.ReadToEnd();
                p.WaitForExit(7000);

                if (!string.IsNullOrWhiteSpace(output) && output.Contains("SiiNunit"))
                {
                    text = output;
                    return true;
                }

                error = string.IsNullOrWhiteSpace(err) ? "Keine Textausgabe von SII_Decrypt." : err.Trim();
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
