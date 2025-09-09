// OptionsForm.cs
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TruckModImporter
{
    public class OptionsForm : Form
    {
        // Sektionen
        private readonly Label lblAppearance = new() { Text = "Darstellung / Appearance", Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true };
        private readonly CheckBox chkDark = new() { Text = "Dark Mode aktivieren / Enable Dark Mode", AutoSize = true };

        private readonly Label lblLanguage = new() { Text = "Sprache / Language:", AutoSize = true };
        private readonly ComboBox cbLanguage = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };

        private readonly Label lblPaths = new() { Text = "Profile-Pfade / Profile Paths", Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true };
        private readonly Label lblEts2 = new() { Text = "ETS2 Profile:", AutoSize = true };
        private readonly TextBox txtEts2 = new() { Width = 360 };
        private readonly Button btnPickEts2 = new() { Text = "Durchsuchen…", Width = 110, Height = 28 };

        private readonly Label lblAts = new() { Text = "ATS Profile:", AutoSize = true };
        private readonly TextBox txtAts = new() { Width = 360 };
        private readonly Button btnPickAts = new() { Text = "Durchsuchen…", Width = 110, Height = 28 };

        private readonly Button btnOk = new() { Text = "OK", DialogResult = DialogResult.OK, Width = 100, Height = 32 };
        private readonly Button btnCancel = new() { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Width = 100, Height = 32 };

        // Properties zum Befüllen/Abholen
        public bool DarkModeChecked { get => chkDark.Checked; set => chkDark.Checked = value; }
        public string SelectedLanguage
        {
            get => (cbLanguage.SelectedItem as string) switch { "English" => "en", "Deutsch" => "de", _ => "de" };
            set
            {
                if (value?.ToLowerInvariant() == "en") cbLanguage.SelectedItem = "English";
                else cbLanguage.SelectedItem = "Deutsch";
            }
        }
        public string Ets2ProfilesPath { get => txtEts2.Text; set => txtEts2.Text = value ?? ""; }
        public string AtsProfilesPath { get => txtAts.Text; set => txtAts.Text = value ?? ""; }

        public OptionsForm()
        {
            Text = "Optionen / Options";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 320);
            StartPosition = FormStartPosition.CenterParent;

            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(243, 243, 243);

            // Language Combo
            cbLanguage.Items.AddRange(new object[] { "Deutsch", "English" });

            // Layout
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(16),
                AutoSize = false
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < root.RowCount; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Appearance Section
            root.Controls.Add(lblAppearance, 0, 0); root.SetColumnSpan(lblAppearance, 3);
            root.Controls.Add(chkDark, 0, 1); root.SetColumnSpan(chkDark, 3);

            root.Controls.Add(lblLanguage, 0, 2);
            root.Controls.Add(cbLanguage, 1, 2); root.SetColumnSpan(cbLanguage, 2);

            // Paths Section
            root.Controls.Add(lblPaths, 0, 3); root.SetColumnSpan(lblPaths, 3);

            root.Controls.Add(lblEts2, 0, 4);
            root.Controls.Add(txtEts2, 1, 4);
            root.Controls.Add(btnPickEts2, 2, 4);

            root.Controls.Add(lblAts, 0, 5);
            root.Controls.Add(txtAts, 1, 5);
            root.Controls.Add(btnPickAts, 2, 5);

            // Buttons bottom
            var pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill
            };
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Padding = new Padding(0, 12, 0, 0);
            root.Controls.Add(pnlButtons, 0, 6); root.SetColumnSpan(pnlButtons, 3);

            Controls.Add(root);

            // Events
            btnPickEts2.Click += (_, __) => PickFolderInto(txtEts2);
            btnPickAts.Click += (_, __) => PickFolderInto(txtAts);
            btnOk.Click += (_, __) =>
            {
                // Simple Validierung: Pfade leer ODER existierend
                if (!string.IsNullOrWhiteSpace(txtEts2.Text) && !Directory.Exists(txtEts2.Text))
                {
                    MessageBox.Show(this, "Der ETS2-Pfad existiert nicht.", "Optionen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None; // Dialog offen lassen
                }
                else if (!string.IsNullOrWhiteSpace(txtAts.Text) && !Directory.Exists(txtAts.Text))
                {
                    MessageBox.Show(this, "Der ATS-Pfad existiert nicht.", "Optionen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }

        private void PickFolderInto(TextBox txt)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Wähle den Profil-Ordner",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            if (Directory.Exists(txt.Text)) fbd.SelectedPath = txt.Text;
            if (fbd.ShowDialog(this) == DialogResult.OK)
                txt.Text = fbd.SelectedPath;
        }
    }
}
