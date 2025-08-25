// MainForm.Theme.Grid.cs
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Wendet Hell/Dunkel-Theme auf alle DataGridViews der Form an (auch die Modliste).
        /// </summary>
        private void ApplyGridTheme(bool dark)
        {
            foreach (var grid in this.Controls.OfType<DataGridView>()
                         .Concat(this.Controls.Cast<Control>().SelectMany(GetAllChildGrids)))
            {
                try
                {
                    ApplyThemeToSingleGrid(grid, dark);
                }
                catch { /* egal */ }
            }
        }

        private static void ApplyThemeToSingleGrid(DataGridView grid, bool dark)
        {
            grid.EnableHeadersVisualStyles = false;

            if (dark)
            {
                var bg       = Color.FromArgb(32, 32, 32);
                var bgAlt    = Color.FromArgb(28, 28, 28);
                var fg       = Color.Gainsboro;
                var selBg    = Color.FromArgb(64, 64, 64);
                var selFg    = Color.WhiteSmoke;
                var hdrBg    = Color.FromArgb(45, 45, 48);
                var hdrFg    = Color.Gainsboro;
                var gridLine = Color.FromArgb(70, 70, 70);

                grid.BackgroundColor = bg;
                grid.GridColor = gridLine;

                grid.DefaultCellStyle.BackColor = bg;
                grid.DefaultCellStyle.ForeColor = fg;
                grid.DefaultCellStyle.SelectionBackColor = selBg;
                grid.DefaultCellStyle.SelectionForeColor = selFg;

                grid.AlternatingRowsDefaultCellStyle.BackColor = bgAlt;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = fg;
                grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = selBg;
                grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = selFg;

                grid.ColumnHeadersDefaultCellStyle.BackColor = hdrBg;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = hdrFg;

                // Buttons im Grid (Download / Suchen)
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col is DataGridViewButtonColumn btnCol)
                    {
                        btnCol.DefaultCellStyle.BackColor = Color.FromArgb(58, 58, 60);
                        btnCol.DefaultCellStyle.ForeColor = Color.WhiteSmoke;
                        btnCol.DefaultCellStyle.SelectionBackColor = Color.FromArgb(80, 80, 82);
                        btnCol.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
                        btnCol.FlatStyle = FlatStyle.Flat;
                    }
                }
            }
            else
            {
                var bg       = Color.White;
                var bgAlt    = Color.FromArgb(248, 248, 248);
                var fg       = Color.Black;
                var selBg    = Color.FromArgb(204, 232, 255);
                var selFg    = Color.Black;
                var hdrBg    = SystemColors.ControlLight;
                var hdrFg    = Color.Black;
                var gridLine = Color.FromArgb(220, 220, 220);

                grid.BackgroundColor = bg;
                grid.GridColor = gridLine;

                grid.DefaultCellStyle.BackColor = bg;
                grid.DefaultCellStyle.ForeColor = fg;
                grid.DefaultCellStyle.SelectionBackColor = selBg;
                grid.DefaultCellStyle.SelectionForeColor = selFg;

                grid.AlternatingRowsDefaultCellStyle.BackColor = bgAlt;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = fg;
                grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = selBg;
                grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = selFg;

                grid.ColumnHeadersDefaultCellStyle.BackColor = hdrBg;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = hdrFg;

                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col is DataGridViewButtonColumn btnCol)
                    {
                        btnCol.DefaultCellStyle.BackColor = SystemColors.ControlLight;
                        btnCol.DefaultCellStyle.ForeColor = Color.Black;
                        btnCol.DefaultCellStyle.SelectionBackColor = SystemColors.ControlDark;
                        btnCol.DefaultCellStyle.SelectionForeColor = Color.White;
                        btnCol.FlatStyle = FlatStyle.Standard;
                    }
                }
            }

            grid.Invalidate();
            grid.Refresh();
        }

        private static System.Collections.Generic.IEnumerable<DataGridView> GetAllChildGrids(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is DataGridView dgv) yield return dgv;
                foreach (var sub in GetAllChildGrids(child)) yield return sub;
            }
        }
    }
}
