// MainForm.SiiDecrypt.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Liest den Text einer .sii-Datei. Falls binär und allowDecrypt=true,
        /// wird versucht, sie mit tools\SII_Decrypt.exe (oder sii_decrypt.exe) zu entschlüsseln.
        /// </summary>
        /// <returns>Der Textinhalt oder null, wenn nicht lesbar/kein Decrypt möglich.</returns>
        private static string? TryReadSiiText(string path, bool allowDecrypt)
        {
            try
            {
                if (!File.Exists(path)) return null;

                if (IsLikelyTextSiiFile(path))
                {
                    return File.ReadAllText(path);
                }

                if (!allowDecrypt) return null;

                // Versuche zu entschlüsseln (verschiedene Tool-Verhaltensweisen abdecken)
                return TryDecryptSiiFile(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Heuristik: prüft, ob die SII-Datei bereits Klartext ist (beginnt i.d.R. mit "SiiNunit"
        /// und enthält keine Nullbytes in den ersten ~4KB).
        /// </summary>
        private static bool IsLikelyTextSiiFile(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var len = (int)Math.Min(4096, fs.Length);
                if (len <= 0) return false;

                var buf = new byte[len];
                fs.Read(buf, 0, len);

                var head = Encoding.ASCII.GetString(buf, 0, Math.Min(16, len));
                if (!head.StartsWith("SiiNunit", StringComparison.Ordinal)) return false;

                for (int i = 0; i < len; i++)
                {
                    if (buf[i] == 0) return false; // Nullbytes deuten auf Binär-Blob
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sucht das Decrypt-Tool im Unterordner ".\tools".
        /// </summary>
        private static string? GetSiiDecryptExePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var toolsDir = Path.Combine(baseDir, "tools");

            string[] candidates =
            {
                Path.Combine(toolsDir, "SII_Decrypt.exe"),
                Path.Combine(toolsDir, "sii_decrypt.exe"),
            };

            foreach (var c in candidates)
                if (File.Exists(c)) return c;

            return null;
        }

        /// <summary>
        /// Versucht, eine binäre SII-Datei zu entschlüsseln und gibt deren Klartext zurück.
        /// Deckt mehrere Tool-Varianten ab:
        /// 1) Ausgabe auf StdOut
        /// 2) Erzeugt .dec neben Input
        /// 3) Unterstützt "input output" (wir schreiben in Tempdatei)
        /// </summary>
        private static string? TryDecryptSiiFile(string inputPath)
        {
            try
            {
                var exe = GetSiiDecryptExePath();
                if (exe == null) return null;

                var inputDir = Path.GetDirectoryName(inputPath) ?? AppDomain.CurrentDomain.BaseDirectory;

                // --- Variante A: Nur Input-Argument, wir lesen StdOut ---
                var a = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"\"{inputPath}\"",
                    WorkingDirectory = inputDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(a))
                {
                    if (p != null)
                    {
                        var stdout = p.StandardOutput.ReadToEnd();
                        var stderr = p.StandardError.ReadToEnd();
                        p.WaitForExit(5000);

                        if (!string.IsNullOrWhiteSpace(stdout) && stdout.Contains("SiiNunit"))
                            return stdout;
                    }
                }

                // --- Variante B: Tool erzeugt .dec neben Input ---
                var decCandidate = inputPath + ".dec";
                if (File.Exists(decCandidate))
                {
                    var txt = File.ReadAllText(decCandidate);
                    if (!string.IsNullOrWhiteSpace(txt) && txt.Contains("SiiNunit"))
                        return txt;
                }

                // --- Variante C: Tool unterstützt "input output" ---
                var tempOut = Path.Combine(Path.GetTempPath(), $"sii_dec_{Guid.NewGuid():N}.sii");
                var b = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"\"{inputPath}\" \"{tempOut}\"",
                    WorkingDirectory = inputDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p2 = Process.Start(b))
                {
                    if (p2 != null)
                    {
                        var _ = p2.StandardOutput.ReadToEnd();
                        var __ = p2.StandardError.ReadToEnd();
                        p2.WaitForExit(5000);
                    }
                }

                if (File.Exists(tempOut))
                {
                    var txt = File.ReadAllText(tempOut);
                    try { File.Delete(tempOut); } catch { /* ignore */ }

                    if (!string.IsNullOrWhiteSpace(txt) && txt.Contains("SiiNunit"))
                        return txt;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
