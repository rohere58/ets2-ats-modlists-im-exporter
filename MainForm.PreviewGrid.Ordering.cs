// MainForm.PreviewGrid.Ordering.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Versucht das Vorschau-Grid zu finden (Feld "gridPreview" oder erstes DataGridView).
        /// </summary>
        private DataGridView? GetPreviewGrid_ForOrder()
        {
            // 1) Versuche das Feld "gridPreview"
            try
            {
                var f = typeof(MainForm).GetField("gridPreview",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f?.GetValue(this) is DataGridView g && !g.IsDisposed)
                    return g;
            }
            catch { /* ignore */ }

            // 2) Fallback: irgend ein Grid in der Form
            try { return this.Controls.OfType<DataGridView>().FirstOrDefault(); }
            catch { return null; }
        }

        /// <summary>
        /// Reihenfolge und Nummerierung setzen.
        /// reverse=true -> Zeilen werden umgedreht.
        /// numberFromTopOne=true -> erste Spalte: 1 .. N von oben nach unten.
        /// </summary>
        public void PreviewOrder_Run(bool reverse = true, bool numberFromTopOne = true)
        {
            var grid = GetPreviewGrid_ForOrder();
            if (grid == null) return;

            grid.SuspendLayout();
            try
            {
                if (reverse) ReverseRowsInPlace(grid);
                if (numberFromTopOne) RenumberFirstColumn_AscendingFromTop(grid);
            }
            finally
            {
                grid.ResumeLayout();
                grid.Invalidate();
            }
        }

        private static void ReverseRowsInPlace(DataGridView grid)
        {
            // Werte puffern (ohne NewRow)
            var rows = new List<object[]>();
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow) continue;
                var values = new object[grid.Columns.Count];
                for (int c = 0; c < grid.Columns.Count; c++)
                    values[c] = r.Cells[c].Value;
                rows.Add(values);
            }

            // Grid neu befüllen, aber in umgekehrter Reihenfolge
            bool allowAdd = grid.AllowUserToAddRows;
            grid.AllowUserToAddRows = false;
            try
            {
                if (grid.DataSource != null)
                    grid.DataSource = null;

                grid.Rows.Clear();
                for (int i = rows.Count - 1; i >= 0; i--)
                    grid.Rows.Add(rows[i]);
            }
            finally
            {
                grid.AllowUserToAddRows = allowAdd;
            }
        }

        private static void RenumberFirstColumn_AscendingFromTop(DataGridView grid)
        {
            // Erste Spalte (Index 0) mit 1..N von oben befüllen
            int n = 0;
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow) continue;
                try
                {
                    if (grid.Columns.Count > 0)
                        r.Cells[0].Value = ++n;
                }
                catch { /* egal */ }
            }
        }
    }
}