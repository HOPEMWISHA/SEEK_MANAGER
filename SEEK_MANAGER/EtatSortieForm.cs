using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using Guna.UI2.WinForms;
using System.Diagnostics; // Added for Process
using System.ComponentModel;

namespace SEEK_MANAGER
{
    public class EtatSortieForm : Form
    {
        private readonly HospitalManager hm;
        private readonly bool summaryMode;
        private readonly DataTable? initialData;
        private readonly string? customTitle;
        private Guna2ComboBox cbPeriod;
        private Guna2DateTimePicker dtRef;
        private Guna2Button btnApply;
        private Guna2Button btnPrint;
        private Guna2Button btnExport;
        private Guna2TextBox txtSearch;
        private Guna2DataGridView dgv;

        private DataTable currentData;
        // Optional loader to fetch full table from DB for the calling interface (used when clicking "Afficher")
        // The loader receives the selected period and reference date and must return a DataTable filtered accordingly.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<string, DateTime, DataTable?>? FallbackLoader { get; set; }

        public EtatSortieForm(HospitalManager hospitalManager, bool summary = false)
        {
            hm = hospitalManager ?? throw new ArgumentNullException(nameof(hospitalManager));
            summaryMode = summary;
            initialData = null;
            customTitle = null;
            InitializeComponents();
        }

        // Overload: display an existing DataTable (from the calling interface) with a custom title
        public EtatSortieForm(HospitalManager hospitalManager, DataTable table, string title)
        {
            hm = hospitalManager ?? throw new ArgumentNullException(nameof(hospitalManager));
            summaryMode = false;
            initialData = table?.Copy();
            customTitle = title ?? "État de sortie";
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "État de sortie - Filtrer et imprimer";
            if (!string.IsNullOrEmpty(customTitle)) Text = customTitle;
            Size = new Size(1120, 600);
            StartPosition = FormStartPosition.CenterParent;

            // Toolbar using Guna controls placed in a top FlowLayoutPanel to avoid overlap
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(12),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            cbPeriod = new Guna2ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FillColor = Color.White,
                ForeColor = Color.FromArgb(33, 37, 41),
                ItemHeight = 30,
                Margin = new Padding(4)
            };
            cbPeriod.Items.AddRange(new object[] { "JOUR", "SEMAINE", "MOIS", "ANNEE" });
            cbPeriod.SelectedIndex = 0;

            dtRef = new Guna2DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, FillColor = Color.White, Margin = new Padding(4) };

            btnApply = new Guna2Button { Text = "Afficher", Width = 100, FillColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, Margin = new Padding(8,4,4,4) };
            btnApply.Click += (s, e) => LoadData();

            btnPrint = new Guna2Button { Text = "Imprimer PDF...", Width = 100, FillColor = Color.FromArgb(155, 89, 182), ForeColor = Color.White, Margin = new Padding(4) };
            btnPrint.Click += (s, e) => PrintCurrentData();

            btnExport = new Guna2Button { Text = "Exporter PDF", Width = 100, FillColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, Margin = new Padding(4) };
            btnExport.Click += (s, e) => ExportPdf();

            txtSearch = new Guna2TextBox { PlaceholderText = "Rechercher...", Width = 300, IconLeftSize = new Size(16, 16), Margin = new Padding(12,4,4,4) };
            txtSearch.TextChanged += (s, e) =>
            {
                try
                {
                    if (currentData == null) return;
                    var dv = currentData.DefaultView;
                    var q = txtSearch.Text.Trim().Replace("'", "''");
                    if (string.IsNullOrWhiteSpace(q)) dv.RowFilter = string.Empty;
                    else
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        foreach (DataColumn c in currentData.Columns)
                        {
                            if (c.DataType == typeof(string) || c.DataType == typeof(object))
                                parts.Add($"CONVERT([{c.ColumnName}], 'System.String') LIKE '%{q}%'");
                        }
                        dv.RowFilter = parts.Count == 0 ? string.Empty : string.Join(" OR ", parts);
                    }
                }
                catch { }
            };

            toolbar.Controls.Add(cbPeriod);
            toolbar.Controls.Add(dtRef);
            toolbar.Controls.Add(btnApply);
            toolbar.Controls.Add(btnPrint);
            toolbar.Controls.Add(btnExport);
            toolbar.Controls.Add(txtSearch);

            dgv = new Guna2DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            dgv.RowTemplate.Height = 30;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.GridColor = Color.FromArgb(230, 230, 230);
            dgv.BorderStyle = BorderStyle.None;

            Controls.Add(dgv);
            Controls.Add(toolbar);

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

        private void LoadData(bool forceDb = false)
        {
            try
            {
                var period = cbPeriod.SelectedItem?.ToString() ?? "JOUR";
                var dt = dtRef.Value.Date;
                // If forceDb is requested and a fallback loader is provided, prefer it
                if (forceDb && FallbackLoader != null)
                {
                    try { currentData = FallbackLoader(period, dt)?.Copy(); }
                    catch { currentData = null; }
                }
                else if (initialData != null)
                {
                    // Filter the provided initialData according to selected period/date so behavior matches HOSPITALISATION
                    try
                    {
                        var src = initialData.Copy();
                        var dest = src.Clone();

                        // find a date column to use for filtering (common names)
                        string dateCol = null;
                        foreach (var cand in new[] { "date_sortie", "date_consultation", "date_enregistrement", "date_entree", "paid_at" })
                        {
                            if (src.Columns.Contains(cand)) { dateCol = cand; break; }
                        }

                        if (dateCol == null)
                        {
                            // no suitable date column — fall back to returning the provided table
                            currentData = initialData.Copy();
                        }
                        else
                        {
                            foreach (DataRow r in src.Rows)
                            {
                                if (r.Table.Columns.Contains(dateCol) && r[dateCol] != DBNull.Value)
                                {
                                    if (DateTime.TryParse(r[dateCol].ToString(), out var ds))
                                    {
                                        bool add = false;
                                        switch ((period ?? "JOUR").ToUpperInvariant())
                                        {
                                            case "JOUR":
                                                add = ds.Date == dt.Date;
                                                break;
                                            case "SEMAINE":
                                                // week starting Monday
                                                var diff = (int)dt.DayOfWeek - (int)DayOfWeek.Monday;
                                                if (diff < 0) diff += 7;
                                                var weekStart = dt.AddDays(-diff).Date;
                                                var weekEnd = weekStart.AddDays(7).Date;
                                                add = ds.Date >= weekStart && ds.Date < weekEnd;
                                                break;
                                            case "MOIS":
                                                add = ds.Year == dt.Year && ds.Month == dt.Month;
                                                break;
                                            case "ANNEE":
                                                add = ds.Year == dt.Year;
                                                break;
                                            default:
                                                add = ds.Date == dt.Date;
                                                break;
                                        }

                                        if (add) dest.ImportRow(r);
                                    }
                                }
                            }
                            currentData = dest;
                        }
                    }
                    catch
                    {
                        currentData = initialData.Copy();
                    }
                }
                else if (summaryMode)
                {
                    currentData = hm.GetEtatSortieSummary(period, dt); // Summary mode
                }
                else
                {
                    currentData = hm.GetEtatSortie(period, dt); // Regular mode
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
