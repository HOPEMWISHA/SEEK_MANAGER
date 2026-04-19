using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.Diagnostics; // Added for Process

namespace SEEK_MANAGER
{
    public class EtatSortieForm : Form
    {
        private readonly HospitalManager hm;
        private readonly bool summaryMode;
        private ComboBox cbPeriod;
        private DateTimePicker dtRef;
        private Button btnApply;
        private Button btnPrint;
        private DataGridView dgv;

        private DataTable currentData;

        public EtatSortieForm(HospitalManager hospitalManager, bool summary = false)
        {
            hm = hospitalManager ?? throw new ArgumentNullException(nameof(hospitalManager));
            summaryMode = summary;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "État de sortie - Filtrer et imprimer";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;

            cbPeriod = new ComboBox { Location = new Point(12, 12), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cbPeriod.Items.AddRange(new object[] { "JOUR", "SEMAINE", "MOIS", "ANNEE" });
            cbPeriod.SelectedIndex = 0;

            dtRef = new DateTimePicker { Location = new Point(184, 12), Width = 160, Format = DateTimePickerFormat.Short };

            btnApply = new Button { Text = "Afficher", Location = new Point(360, 12), Width = 100 };
            btnApply.Click += (s, e) => LoadData();

            btnPrint = new Button { Text = "Imprimer PDF...", Location = new Point(472, 12), Width = 140 };
            btnPrint.Click += (s, e) => PrintCurrentData();

            var btnExport = new Button { Text = "Exporter PDF", Location = new Point(624, 12), Width = 140 };
            btnExport.Click += (s, e) => ExportPdf();

            dgv = new DataGridView { Location = new Point(12, 56), Size = new Size(860, 480), ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            Controls.Add(cbPeriod);
            Controls.Add(dtRef);
            Controls.Add(btnApply);
            Controls.Add(btnPrint);
            Controls.Add(btnExport);
            Controls.Add(dgv);

            // load initial data
            LoadData();
        }

        private void ExportPdf()
        {
            try
            {
                if (currentData == null || currentData.Rows.Count == 0)
                {
                    MessageBox.Show("Aucune donnée à exporter.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // build output folder
                var outDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
                try { System.IO.Directory.CreateDirectory(outDir); } catch { }
                var fileName = $"etat_sortie_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var outPath = System.IO.Path.Combine(outDir, fileName);

                // create PDF using PdfSharpCore
                using (var doc = new PdfDocument())
                {
                    int colCount = currentData.Columns.Count;
                    int left = 40;
                    int topStart = 30;
                    int pageIndex = -1;
                    XGraphics gfx = null;

                    var fontTitle = new XFont("Segoe UI", 18, XFontStyle.Bold);
                    var fontHeader = new XFont("Segoe UI", 10, XFontStyle.Bold);
                    var fontRow = new XFont("Segoe UI", 9, XFontStyle.Regular);

                    int row = 0;
                    while (row < currentData.Rows.Count)
                    {
                        pageIndex++;
                        var page = doc.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                        gfx?.Dispose();
                        gfx = XGraphics.FromPdfPage(page);

                        int top = topStart;
                        gfx.DrawString("SEEK_MANAGER - État de sortie", fontTitle, XBrushes.Black, new XRect(left + 120, top, page.Width.Point - left, 30), XStringFormats.TopLeft);
                        top += 40;

                        int usableWidth = (int)page.Width.Point - left * 2;
                        int colWidth = Math.Max(60, usableWidth / Math.Max(1, colCount));

                        // headers
                        int x = left;
                        for (int c = 0; c < colCount; c++)
                        {
                            gfx.DrawString(currentData.Columns[c].ColumnName, fontHeader, XBrushes.Black, new XRect(x, top, colWidth, 20), XStringFormats.TopLeft);
                            x += colWidth;
                        }
                        top += 22;

                        // rows per page
                        while (row < currentData.Rows.Count)
                        {
                            x = left;
                            for (int c = 0; c < colCount; c++)
                            {
                                var txt = currentData.Rows[row][c]?.ToString() ?? string.Empty;
                                gfx.DrawString(txt, fontRow, XBrushes.Black, new XRect(x, top, colWidth, 16), XStringFormats.TopLeft);
                                x += colWidth;
                            }
                            top += 18;
                            row++;
                            if (top > page.Height.Point - 60)
                                break; // next page
                        }
                    }

                    gfx?.Dispose();

                    using var stream = System.IO.File.Create(outPath);
                    doc.Save(stream);
                }

                MessageBox.Show($"Export PDF créé : {outPath}", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // open folder
                try { Process.Start(new ProcessStartInfo { FileName = outDir, UseShellExecute = true, Verb = "open" }); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur export PDF: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                var period = cbPeriod.SelectedItem?.ToString() ?? "JOUR";
                var dt = dtRef.Value.Date;
                if (summaryMode)
                {
                    currentData = hm.GetEtatSortieSummary(period, dt);
                }
                else
                {
                    currentData = hm.GetEtatSortie(period, dt);
                }
                dgv.DataSource = currentData?.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des états de sortie : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintCurrentData()
        {
            if (currentData == null || currentData.Rows.Count == 0)
            {
                MessageBox.Show("Aucune donnée à imprimer.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var pd = new PrintDocument())
                {
                    pd.DefaultPageSettings.Landscape = true;
                    int rowsPerPage = 40; // simple pagination
                    int currentRow = 0;

                    pd.PrintPage += (sender, e) =>
                    {
                        int left = e.MarginBounds.Left;
                        int top = e.MarginBounds.Top;
                        int width = e.MarginBounds.Width;

                        // draw logo if exists
                        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "medical.png.png");
                        if (File.Exists(logoPath))
                        {
                            try
                            {
                                using var img = Image.FromFile(logoPath);
                                var imgRect = new Rectangle(left, top, 120, 60);
                                e.Graphics.DrawImage(img, imgRect);
                            }
                            catch { }
                        }

                        // draw title
                        using (var titleFont = new Font("Segoe UI", 16, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.Black))
                        {
                            var title = "SEEK_MANAGER - État de sortie";
                            var titleSize = e.Graphics.MeasureString(title, titleFont);
                            e.Graphics.DrawString(title, titleFont, brush, left + 140, top + 8);
                        }

                        top += 80;

                        // headers
                        using (var headerFont = new Font("Segoe UI", 10, FontStyle.Bold))
                        using (var rowFont = new Font("Segoe UI", 9))
                        using (var brush = new SolidBrush(Color.Black))
                        {
                            int colCount = currentData.Columns.Count;
                            int colWidth = Math.Max(80, width / Math.Max(1, colCount));

                            int x = left;
                            for (int c = 0; c < colCount; c++)
                            {
                                var colName = currentData.Columns[c].ColumnName;
                                e.Graphics.DrawString(colName, headerFont, brush, x, top);
                                x += colWidth;
                            }

                            top += 28;

                            // rows
                            int printed = 0;
                            while (currentRow < currentData.Rows.Count && printed < rowsPerPage)
                            {
                                var row = currentData.Rows[currentRow];
                                x = left;
                                for (int c = 0; c < colCount; c++)
                                {
                                    var txt = row[c]?.ToString() ?? string.Empty;
                                    e.Graphics.DrawString(txt, rowFont, brush, x, top);
                                    x += colWidth;
                                }
                                currentRow++; printed++; top += 20;
                            }

                            e.HasMorePages = currentRow < currentData.Rows.Count;
                        }
                    };

                    using (var dlg = new PrintDialog())
                    {
                        dlg.Document = pd;
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            pd.Print();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
