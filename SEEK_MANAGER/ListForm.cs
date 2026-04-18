using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class ListForm : Form
    {
        private ListBox listBox;
        private Button okButton;
        private Button printButton;
        private System.Drawing.Printing.PrintDocument printDoc;
        private int printIndex = 0;

        public ListForm(IEnumerable<string> items, string title = "Liste")
        {
            Text = title;
            Width = 600;
            Height = 400;
            StartPosition = FormStartPosition.CenterParent;

            listBox = new ListBox()
            {
                Dock = DockStyle.Top,
                Height = 320
            };

            okButton = new Button()
            {
                Text = "OK",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            okButton.Click += (s, e) => Close();

            printButton = new Button()
            {
                Text = "Print",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            printButton.Click += PrintButton_Click;

            printDoc = new System.Drawing.Printing.PrintDocument();
            printDoc.PrintPage += PrintDoc_PrintPage;

            Controls.Add(listBox);
            Controls.Add(printButton);
            Controls.Add(okButton);

            if (items != null)
            {
                listBox.Items.AddRange(items.ToArray());
            }
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                printIndex = 0;
                using (var dlg = new PrintDialog())
                {
                    dlg.Document = printDoc;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        printDoc.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur impression: {ex.Message}");
            }
        }

        private void PrintDoc_PrintPage(object? sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            float y = e.MarginBounds.Top;
            var font = new Font("Segoe UI", 10);
            while (printIndex < listBox.Items.Count)
            {
                var s = listBox.Items[printIndex]?.ToString() ?? string.Empty;
                e.Graphics.DrawString(s, font, Brushes.Black, e.MarginBounds.Left, y);
                y += font.GetHeight(e.Graphics) + 4;
                printIndex++;
                if (y > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            e.HasMorePages = false;
        }
    }
}
