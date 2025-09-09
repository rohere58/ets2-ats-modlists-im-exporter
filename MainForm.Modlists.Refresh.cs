using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace TruckModImporter
{
    public partial class MainForm
    {
        private FileSystemWatcher? _modlistsWatcher;
        private WinFormsTimer? _modlistsDebounce;
        private string? _pendingPreselect;

        private void Modlists_RefreshDropdown(string? preselectName = null, bool preserveIfPossible = true)
        {
            if (cbList == null || cbList.IsDisposed) return;

            string dir = ResolveModlistsDirSafe();
            List<string> names = new();
            try
            {
                if (Directory.Exists(dir))
                {
                    names = Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly)
                             .Select(Path.GetFileNameWithoutExtension)
                             .Where(n => n != null) // <-- Hinzugefügt, um Nullwerte zu filtern
                             .Cast<string>()        // <-- Hinzugefügt, um von string? zu string zu casten
                             .Distinct(StringComparer.CurrentCultureIgnoreCase)
                             .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                             .ToList();
                }
            }
            catch { }

            string? previous = preserveIfPossible ? (cbList.SelectedItem as string ?? cbList.Text) : null;

            cbList.BeginUpdate();
            try
            {
                cbList.Items.Clear();
                foreach (var n in names) cbList.Items.Add(n);

                int idx = -1;
                if (!string.IsNullOrWhiteSpace(preselectName))
                    idx = names.FindIndex(n => string.Equals(n, preselectName, StringComparison.CurrentCultureIgnoreCase));
                if (idx < 0 && !string.IsNullOrWhiteSpace(previous))
                    idx = names.FindIndex(n => string.Equals(n, previous, StringComparison.CurrentCultureIgnoreCase));
                if (idx < 0 && names.Count > 0)
                    idx = 0;

                cbList.SelectedIndex = idx;
            }
            finally { cbList.EndUpdate(); }
        }

        private void Modlists_ScheduleRefresh(string? preselectName = null)
        {
            void ArmDebounce()
            {
                _modlistsDebounce ??= new WinFormsTimer { Interval = 200 };
                _modlistsDebounce.Tick -= _modlistsDebounce_Tick;
                _modlistsDebounce.Tick += _modlistsDebounce_Tick;
                _pendingPreselect = preselectName ?? _pendingPreselect;
                _modlistsDebounce.Stop();
                _modlistsDebounce.Start();
            }

            if (IsHandleCreated)
                BeginInvoke((MethodInvoker)ArmDebounce);
            else
                ArmDebounce();
        }

        private void _modlistsDebounce_Tick(object? sender, EventArgs e)
        {
            _modlistsDebounce?.Stop();
            string? pre = _pendingPreselect;
            _pendingPreselect = null;
            try { Modlists_RefreshDropdown(pre, preserveIfPossible: string.IsNullOrEmpty(pre)); } catch { }
        }

        private void Modlists_Watch_StartOrReset()
        {
            try
            {
                _modlistsWatcher?.Dispose();
                var dir = ResolveModlistsDirSafe();
                _modlistsWatcher = new FileSystemWatcher(dir, "*.txt")
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                _modlistsWatcher.Created += (_, __) => Modlists_ScheduleRefresh();
                _modlistsWatcher.Renamed += (_, __) => Modlists_ScheduleRefresh();
                _modlistsWatcher.Deleted += (_, __) => Modlists_ScheduleRefresh();
                _modlistsWatcher.Changed += (_, __) => Modlists_ScheduleRefresh();
            }
            catch { }
        }

        private void OnGameSelectedIndexChanged(object sender, EventArgs e)
        {
            Modlists_Watch_StartOrReset();
            Modlists_RefreshDropdown();
        }
    }
}