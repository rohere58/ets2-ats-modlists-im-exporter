// MainForm.AutoDecrypt.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Startet das automatische Entschlüsseln aller profile.sii-Dateien
        /// (ETS2 & ATS) im Hintergrund. Nach Abschluss werden Profile neu geladen.
        /// </summary>
        private void KickOffAutoDecryptAllProfilesInBackground()
        {
            // Nicht mehrfach starten
            if (_autoDecryptStarted) return;
            _autoDecryptStarted = true;

            Task.Run(() =>
            {
                try
                {
                    var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "SII_Decrypt.exe");
                    if (!File.Exists(exe))
                    {
                        BeginInvoke(new Action(() =>
                            SafeSetStatus("SII_Decrypt.exe nicht gefunden (tools\\SII_Decrypt.exe). Profile werden im Original gelesen.")));
                        return;
                    }

                    // Wurzeln bestimmen (Einstellungen oder Standard)
                    var (ets2Root, atsRoot) = GetProfilesRoots();

                    int total = 0, converted = 0, skipped = 0, failed = 0;

                    foreach (var root in new[] { ets2Root, atsRoot })
                    {
                        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                            continue;

                        foreach (var siiPath in EnumerateProfileSiiFiles(root))
                        {
                            total++;
                            try
                            {
                                if (IsProbablyTextSii(siiPath))
                                {
                                    skipped++;
                                    continue;
                                }

                                if (TryAutoDecryptFileInPlace(exe, siiPath))
                                    converted++;
                                else
                                    failed++;
                            }
                            catch
                            {
                                failed++;
                            }
                        }
                    }

                    BeginInvoke(new Action(() =>
                    {
                        SafeSetStatus($"Auto-Decrypt: {converted} konvertiert, {skipped} bereits Text, {failed} fehlgeschlagen (von {total}).");
                        // Nach dem Entschlüsseln die Profilnamen neu laden (freundliche Namen möglich)
                        try { LoadProfiles_Local(); } catch { }
                    }));
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() => SafeSetStatus("Auto-Decrypt-Fehler: " + ex.Message)));
                }
            });
        }

        private bool _autoDecryptStarted = false;

        /// <summary> Liefert Standardpfade (oder benutzerdefinierte) zu den Profilordnern. </summary>
        private (string ets2ProfilesRoot, string atsProfilesRoot) GetProfilesRoots()
        {
            try
            {
                var st = SettingsService.Load();
                string? ets2 = st?.Ets2ProfilesPath;
                string? ats  = st?.AtsProfilesPath;

                if (string.IsNullOrWhiteSpace(ets2))
                {
                    var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ets2 = Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
                }
                if (string.IsNullOrWhiteSpace(ats))
                {
                    var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ats = Path.Combine(docs, "American Truck Simulator", "profiles");
                }

                // ab hier garantiert non-null
                return (ets2!, ats!);
            }
            catch
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return (Path.Combine(docs, "Euro Truck Simulator 2", "profiles"),
                        Path.Combine(docs, "American Truck Simulator", "profiles"));
            }
        }

        /// <summary>
        /// Sammelt alle "profile.sii" unterhalb eines Profil-Roots (rekursiv).
        /// KEINE yield-Returns (vermeidet CS1626 in try/catch-Szenarien).
        /// </summary>
        private static IEnumerable<string> EnumerateProfileSiiFiles(string root)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return list;

            try
            {
                // 1) reguläre profiles-Struktur
                list.AddRange(Directory.EnumerateFiles(root, "profile.sii", SearchOption.AllDirectories));
            }
            catch { /* ignore */ }

            try
            {
                // 2) optional: steam_profiles neben profiles (falls Root = ...\profiles)
                var parent = Directory.GetParent(root)?.FullName;
                var steamProfiles = Path.Combine(parent ?? root, "steam_profiles");
                if (Directory.Exists(steamProfiles))
                {
                    list.AddRange(Directory.EnumerateFiles(steamProfiles, "profile.sii", SearchOption.AllDirectories));
                }
            }
            catch { /* ignore */ }

            return list;
        }

        /// <summary>
        /// Sehr einfache Heuristik: Wenn der Anfang größtenteils ASCII ist und
        /// "SiiNunit" enthält, werten wir es als Text. Bei Binärdaten finden sich
        /// i. d. R. viele Nicht-ASCII-Bytes oder Nullbytes.
        /// </summary>
        private static bool IsProbablyTextSii(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var len = (int)Math.Min(4096, fs.Length);
                var buf = new byte[len];
                _ = fs.Read(buf, 0, len);

                var header = Encoding.ASCII.GetString(buf);
                if (header.IndexOf("SiiNunit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    for (int i = 0; i < len; i++)
                    {
                        byte b = buf[i];
                        if (b == 0) return false; // Nullbyte → wahrscheinlich binär
                        if (b < 0x09 || (b > 0x0D && b < 0x20)) return false; // Steuerzeichen
                    }
                    return true;
                }

                return false; // kein "SiiNunit" → eher binär
            }
            catch
            {
                // Im Zweifel binär behandeln, damit wir versuchen zu entschlüsseln
                return false;
            }
        }

        /// <summary>
        /// Versucht mehrere Aufrufvarianten von SII_Decrypt.exe.
        /// Erfolg, wenn am Ende die Datei als Text erkennbar ist.
        /// </summary>
        private static bool TryAutoDecryptFileInPlace(string exePath, string siiPath)
        {
            // Variante A: stdout abfangen und zurückschreiben
            if (RunAndCapture(exePath, $"\"{siiPath}\"", out var stdout, out _))
            {
                if (!string.IsNullOrWhiteSpace(stdout) &&
                    stdout.IndexOf("SiiNunit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        var bak = siiPath + ".bak";
                        if (!File.Exists(bak)) File.Copy(siiPath, bak, overwrite: false);
                        File.WriteAllText(siiPath, stdout, new UTF8Encoding(false));
                        return true;
                    }
                    catch { /* fallthrough */ }
                }
            }

            // Variante B: in-place input+output
            RunAndCapture(exePath, $"\"{siiPath}\" \"{siiPath}\"", out _, out _);
            if (IsProbablyTextSii(siiPath)) return true;

            // Variante C: in temp-Datei ausgeben und dann ersetzen
            var temp = Path.ChangeExtension(siiPath, ".txt");
            RunAndCapture(exePath, $"\"{siiPath}\" \"{temp}\"", out _, out _);
            if (File.Exists(temp) && IsProbablyTextSii(temp))
            {
                try
                {
                    var bak = siiPath + ".bak";
                    if (!File.Exists(bak)) File.Copy(siiPath, bak, overwrite: false);
                    File.Copy(temp, siiPath, overwrite: true);
                    File.Delete(temp);
                    return true;
                }
                catch { /* ignore */ }
            }

            return IsProbablyTextSii(siiPath);
        }

        private static bool RunAndCapture(string exePath, string args, out string stdOut, out string stdErr, int timeoutMs = 20000)
        {
            stdOut = ""; stdErr = "";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
                };

                using var p = Process.Start(psi);
                if (p == null) return false;

                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(true); } catch { }
                    return false;
                }

                stdOut = outTask.GetAwaiter().GetResult();
                stdErr = errTask.GetAwaiter().GetResult();
                return p.ExitCode == 0 || !string.IsNullOrEmpty(stdOut); // manche Tools liefern non-0 trotz brauchbarer Ausgabe
            }
            catch
            {
                return false;
            }
        }
    }
}
