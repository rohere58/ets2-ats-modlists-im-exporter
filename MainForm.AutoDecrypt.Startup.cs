// MainForm.AutoDecrypt.Startup.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // Flag, damit der Job nur einmal startet
        private bool _autoDec_Started;

        /// <summary>
        /// Startet NUR EINMAL im Hintergrund das Entschlüsseln ALLER profile.sii
        /// (ETS2 & ATS) mit tools\SII_Decrypt.exe. Macht NICHTS, wenn Datei bereits Text ist.
        /// Ruft nach Abschluss LoadProfiles_Local() auf, damit ggf. hübsche Profilnamen erscheinen.
        /// </summary>
        private void KickOffAutoDecryptAllProfilesInBackground_Once()
        {
            if (_autoDec_Started) return;
            _autoDec_Started = true;

            // UI nicht blockieren
            BeginInvoke(new Action(() =>
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "SII_Decrypt.exe");
                        if (!File.Exists(exe))
                        {
                            TrySafeStatus("Auto-Decrypt: tools\\SII_Decrypt.exe nicht gefunden – übersprungen.");
                            return;
                        }

                        // Profilwurzeln (Einstellungen oder Standard)
                        var (ets2Root, atsRoot) = ResolveProfilesRoots_AutoDec();

                        int total = 0, converted = 0, skipped = 0, failed = 0;

                        foreach (var root in new[] { ets2Root, atsRoot })
                        {
                            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                                continue;

                            foreach (var sii in EnumerateProfileSiiFiles_AutoDec(root))
                            {
                                total++;
                                try
                                {
                                    if (!IsProbablyBinarySii_AutoDec(sii))
                                    {
                                        skipped++;
                                        continue;
                                    }

                                    if (TryAutoDecryptFileInPlace_AutoDec(exe, sii))
                                        converted++;
                                    else
                                        failed++;
                                }
                                catch { failed++; }
                            }
                        }

                        TrySafeStatus($"Auto-Decrypt: {converted} konvertiert, {skipped} schon Text, {failed} Fehler (von {total}).");

                        // Profilnamen neu laden (falls jetzt im Klartext)
                        try { BeginInvoke(new Action(() => { try { LoadProfiles_Local(); } catch { } })); } catch { }
                    }
                    catch (Exception ex)
                    {
                        TrySafeStatus("Auto-Decrypt-Fehler: " + ex.Message);
                    }
                });
            }));
        }

        // ---------- kleine, einzigartige Helfer (keine Kollisionen mit deinem Code) ----------

        private static IEnumerable<string> EnumerateProfileSiiFiles_AutoDec(string root)
        {
            var list = new List<string>();
            try
            {
                list.AddRange(Directory.EnumerateFiles(root, "profile.sii", SearchOption.AllDirectories));
            }
            catch { /* ignore */ }

            try
            {
                // Falls Root auf ...\profiles zeigt, probiere auch ...\steam_profiles
                var parent = Directory.GetParent(root)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    var steamProfiles = Path.Combine(parent, "steam_profiles");
                    if (Directory.Exists(steamProfiles))
                        list.AddRange(Directory.EnumerateFiles(steamProfiles, "profile.sii", SearchOption.AllDirectories));
                }
            }
            catch { /* ignore */ }

            return list;
        }

        private static bool IsProbablyBinarySii_AutoDec(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                int len = (int)Math.Min(4096, fs.Length);
                var buf = new byte[len];
                _ = fs.Read(buf, 0, len);

                // Nullbyte = sehr wahrscheinlich binär
                for (int i = 0; i < len; i++)
                    if (buf[i] == 0) return true;

                var header = Encoding.ASCII.GetString(buf);
                // Wenn "SiiNunit" NICHT im Header: eher binär (verschlüsselt)
                return header.IndexOf("SiiNunit", StringComparison.OrdinalIgnoreCase) < 0;
            }
            catch
            {
                // Im Zweifel als binär behandeln, damit wir es versuchen
                return true;
            }
        }

        private static bool RunAndCapture_AutoDec(string exePath, string args, out string stdout, out string stderr, int timeoutMs = 20000)
        {
            stdout = ""; stderr = "";
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

                stdout = outTask.GetAwaiter().GetResult();
                stderr = errTask.GetAwaiter().GetResult();
                return p.ExitCode == 0 || !string.IsNullOrWhiteSpace(stdout);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryAutoDecryptFileInPlace_AutoDec(string exePath, string siiPath)
        {
            // Variante A: nur Eingabe (Tool schreibt Klartext auf stdout)
            if (RunAndCapture_AutoDec(exePath, $"\"{siiPath}\"", out var so, out _))
            {
                if (!string.IsNullOrWhiteSpace(so) &&
                    so.IndexOf("SiiNunit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        var bak = siiPath + ".bak";
                        if (!File.Exists(bak)) File.Copy(siiPath, bak, overwrite: false);
                        File.WriteAllText(siiPath, so, new UTF8Encoding(false));
                        return true;
                    }
                    catch { /* fallthrough */ }
                }
            }

            // Variante B: in-place input + output gleicher Pfad
            RunAndCapture_AutoDec(exePath, $"\"{siiPath}\" \"{siiPath}\"", out _, out _);
            if (!IsProbablyBinarySii_AutoDec(siiPath)) return true;

            // Variante C: temporäre Ausgabe
            var tmp = Path.ChangeExtension(siiPath, ".txt");
            RunAndCapture_AutoDec(exePath, $"\"{siiPath}\" \"{tmp}\"", out _, out _);
            if (File.Exists(tmp) && !IsProbablyBinarySii_AutoDec(tmp))
            {
                try
                {
                    var bak = siiPath + ".bak";
                    if (!File.Exists(bak)) File.Copy(siiPath, bak, overwrite: false);
                    File.Copy(tmp, siiPath, overwrite: true);
                    File.Delete(tmp);
                    return true;
                }
                catch { /* ignore */ }
            }

            return !IsProbablyBinarySii_AutoDec(siiPath);
        }

        private (string ets2Root, string atsRoot) ResolveProfilesRoots_AutoDec()
        {
            try
            {
                var st = SettingsService.Load();
                string? ets2 = st?.Ets2ProfilesPath;
                string? ats = st?.AtsProfilesPath;

                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrWhiteSpace(ets2))
                    ets2 = Path.Combine(docs, "Euro Truck Simulator 2", "profiles");
                if (string.IsNullOrWhiteSpace(ats))
                    ats = Path.Combine(docs, "American Truck Simulator", "profiles");

                return (ets2!, ats!);
            }
            catch
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return (Path.Combine(docs, "Euro Truck Simulator 2", "profiles"),
                        Path.Combine(docs, "American Truck Simulator", "profiles"));
            }
        }

        private void TrySafeStatus(string msg)
        {
            try { SafeSetStatus(msg); } catch { /* falls SafeSetStatus nicht existiert */ }
        }
    }
}
