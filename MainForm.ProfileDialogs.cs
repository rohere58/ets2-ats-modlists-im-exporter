using System;
using System.Drawing;
using System.Windows.Forms;

namespace TruckModImporter
{
    public partial class MainForm
    {
        /// <summary>
        /// Zeigt einen Eingabedialog für Profilnamen (max. 20 Zeichen).
        /// Gibt null zurück, wenn der Nutzer abbricht.
        /// </summary>
        private static string? ShowProfileNameDialog(string title, string defaultValue)
        {
            using (var form = new Form())
            using (var textBox = new TextBox())
            using (var buttonOk = new Button())
            using (var buttonCancel = new Button())
            using (var label = new Label())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.Width = 400;
                form.Height = 150;

                label.Text = "Profilname (max. 20 Zeichen):";
                label.SetBounds(10, 10, 380, 20);

                textBox.Text = defaultValue;
                textBox.SetBounds(10, 35, 360, 25);

                buttonOk.Text = "OK";
                buttonOk.DialogResult = DialogResult.OK;
                buttonOk.SetBounds(220, 70, 70, 25);

                buttonCancel.Text = "Abbrechen";
                buttonCancel.DialogResult = DialogResult.Cancel;
                buttonCancel.SetBounds(300, 70, 70, 25);

                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                // Live-Feedback bei mehr als 20 Zeichen
                textBox.TextChanged += (s, e) =>
                {
                    if (textBox.Text.Length > 20)
                    {
                        textBox.BackColor = Color.LightCoral; // Rot, wenn zu lang
                        buttonOk.Enabled = false;
                    }
                    else
                    {
                        textBox.BackColor = Color.White;
                        buttonOk.Enabled = true;
                    }
                };

                if (form.ShowDialog() == DialogResult.OK)
                    return textBox.Text.Trim();

                return null;
            }
        }
    }
}
