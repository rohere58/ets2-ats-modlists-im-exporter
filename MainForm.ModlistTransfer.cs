using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace TruckModImporter
{
    public partial class MainForm
    {
        // ===== fields (create only if absent) =====
        private Button? btnListShare;
        private Button? btnListImport;
        private Control? _headerContainer;
        private bool _shareImportInitDone;

        // ===== small helper: try find header container =====
        private Control? ResolveHeaderContainer()
        {
            try { if (cbList != null && cbList.Parent != null) return cbList.Parent; } catch { }
            try { return pnlTopButtons; } catch { }
            return this;
        }

        // ===== small helper: find Delete button (anchor) =====
        private Button? FindDeleteInHeader()
        {
            var container = _headerContainer ?? ResolveHeaderContainer();
            if (container == null) return null;

            return container.Controls
                .OfType<Button>()
                .FirstOrDefault(b =>
                    string.Equals(b.Name, "btnDelete", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(b.Text, "Löschen", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(b.Text, "Delete",  StringComparison.OrdinalIgnoreCase));
        }

        // --- SAFE HEADER LAYOUT FOR SHARE/IMPORT ---
        private void RepositionHeaderShareImport()
        {
            _headerContainer ??= (cbList?.Parent ?? pnlTopButtons ?? (Control)this);
            if (_headerContainer == null || _headerContainer.IsDisposed) return;

            const int spacing = 8;
            Control? anchor = cbList;

            int x = (anchor != null) ? anchor.Right + spacing : 8;
            int baselineY = (anchor != null) ? anchor.Top : 8;

            void place(Button? b)
            {
                if (b == null || b.IsDisposed || !b.Visible) return;
                int y = baselineY + Math.Max(0, ((anchor?.Height ?? b.Height) - b.Height) / 2) - 2;
                b.Location = new System.Drawing.Point(x, y);
                b.BringToFront();
                x = b.Right + spacing;
            }

            place(btnListShare);
            place(btnListImport);
            place(btnListDelete); // <== sicherstellen, dass dies am Ende steht
        }

        // ===== wire relayout once =====
        private bool _headerRelayoutWired;
        private void WireHeaderRelayout()
        {
            if (_headerRelayoutWired) return;
            _headerRelayoutWired = true;

            var container = _headerContainer ?? ResolveHeaderContainer();
            if (container == null || container.IsDisposed) return;

            container.Resize -= HeaderRelayout_ForShareImport;
            container.Resize += HeaderRelayout_ForShareImport;

            if (cbList != null && !cbList.IsDisposed)
            {
                cbList.LocationChanged -= HeaderRelayout_ForShareImport;
                cbList.SizeChanged     -= HeaderRelayout_ForShareImport;
                cbList.LocationChanged += HeaderRelayout_ForShareImport;
                cbList.SizeChanged     += HeaderRelayout_ForShareImport;
            }
            if (btnListShare != null)
            {
                btnListShare.SizeChanged -= HeaderRelayout_ForShareImport;
                btnListShare.SizeChanged += HeaderRelayout_ForShareImport;
            }
            if (btnListImport != null)
            {
                btnListImport.SizeChanged -= HeaderRelayout_ForShareImport;
                btnListImport.SizeChanged += HeaderRelayout_ForShareImport;
            }
        }

        private void HeaderRelayout_ForShareImport(object? sender, EventArgs e) => RepositionHeaderShareImport();

        // ===== public entry (call once; guarded) =====
        private void EnsureHeaderShareImportButtons()
        {
            // second and later calls only reposition
            if (_shareImportInitDone)
            {
                _headerContainer ??= ResolveHeaderContainer();
                RepositionHeaderShareImport();
                return;
            }

            _headerContainer = ResolveHeaderContainer();
            if (_headerContainer == null || _headerContainer.IsDisposed) return;

            // create if missing
            if (btnListShare == null || btnListShare.IsDisposed)
            {
                btnListShare = new Button
                {
                    Name = "btnListShare",
                    Text = GetCurrentLanguageIsEnglish() ? "Share" : "Weitergeben",
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true,
                    Visible = true
                };
                btnListShare.Click -= BtnListShare_Click;
                btnListShare.Click += BtnListShare_Click;
            }
            if (btnListImport == null || btnListImport.IsDisposed)
            {
                btnListImport = new Button
                {
                    Name = "btnListImport",
                    Text = GetCurrentLanguageIsEnglish() ? "Import" : "Importieren",
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true,
                    Visible = true
                };
                btnListImport.Click -= BtnListImport_Click;
                btnListImport.Click += BtnListImport_Click;
            }

            // parent into header
            if (!ReferenceEquals(btnListShare.Parent, _headerContainer))
            {
                try { btnListShare.Parent?.Controls.Remove(btnListShare); } catch { }
                _headerContainer.Controls.Add(btnListShare);
            }
            if (!ReferenceEquals(btnListImport.Parent, _headerContainer))
            {
                try { btnListImport.Parent?.Controls.Remove(btnListImport); } catch { }
                _headerContainer.Controls.Add(btnListImport);
            }

            // initial layout
            RepositionHeaderShareImport();

            // ensure delete button
            try { EnsureHeaderDeleteButton(); } catch { }

            // wire future relayout
            try { WireHeaderRelayout(); } catch { }

            // one more pass after idle
            try { BeginInvoke(new Action(RepositionHeaderShareImport)); } catch { }

            _shareImportInitDone = true;
        }

        private void BtnListShare_Click(object? sender, EventArgs e)
        {
            ModlistShare_ExportZipForCurrentList();
        }

        private void BtnListImport_Click(object? sender, EventArgs e)
        {
            ModlistShare_ImportZip();
        }

        private void EnsureHeaderShareImport_L10n(string lang)
        {
            bool en = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            if (btnListShare  != null && !btnListShare.IsDisposed)
                btnListShare.Text  = en ? "Share"  : "Weitergeben";
            if (btnListImport != null && !btnListImport.IsDisposed)
                btnListImport.Text = en ? "Import" : "Importieren";
        }

        private void EnsureHeaderShareImport_Theme()
        {
            // make sure buttons exist before styling
            try { EnsureHeaderShareImportButtons(); } catch { }

            if (btnListShare  != null && !btnListShare.IsDisposed)
            {
                try { MatchTopButtonLook(btnListShare); } catch { }
                btnListShare.Padding = new Padding(6, 2, 6, 2);
                btnListShare.Invalidate();
            }
            if (btnListImport != null && !btnListImport.IsDisposed)
            {
                try { MatchTopButtonLook(btnListImport); } catch { }
                btnListImport.Padding = new Padding(6, 2, 6, 2);
                btnListImport.Invalidate();
            }
        }

        // Optional: Direkt nach dem Hinzufügen der Buttons in EnsureHeaderShareImportButtons():
        // try { EnsureHeaderShareImport_Theme(); } catch {}
    }
}