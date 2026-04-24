using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace SEEK_MANAGER
{
    public class PaiementControl : UserControl
    {
        private readonly HospitalManager hm = new HospitalManager();
        private Panel main;
        private Guna2DataGridView dgv;
        private Guna2TextBox txtSearch;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private Button btnPrint;

        public PaiementControl()
        {
            Dock = DockStyle.Fill;
            main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            dgv = new Guna2DataGridView { Dock = DockStyle.Bottom, Height = 400 };
            btnAdd = new Button { Text = "Ajouter", Width = 120, Height = 40 };
            btnEdit = new Button { Text = "Modifier", Width = 120, Height = 40 };
            btnDelete = new Button { Text = "Supprimer", Width = 120, Height = 40 };
            btnRefresh = new Button { Text = "Actualiser", Width = 120, Height = 40 };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft };
            btnPanel.Controls.Add(btnRefresh);
            // print button - generates a PDF invoice for the selected payment
            btnPrint = new Button { Text = "Imprimer Facture", Width = 140, Height = 40 };
            btnPanel.Controls.Add(btnPrint);
            btnPanel.Controls.Add(btnDelete);
            btnPanel.Controls.Add(btnEdit);
            btnPanel.Controls.Add(btnAdd);

            // search box to filter the payments displayed in the grid
            txtSearch = new Guna2TextBox { Dock = DockStyle.Top, Height = 45, PlaceholderText = "Rechercher (référence, client, méthode, montant)..." };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            main.Controls.Add(txtSearch);
            main.Controls.Add(dgv);
            main.Controls.Add(btnPanel);
            Controls.Add(main);

            Load += PaiementControl_Load;

            btnRefresh.Click += (s, e) => LoadData();
            btnPrint.Click += PrintInvoice_Click;
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
        }

        private void PrintInvoiceByClient_Click(object? sender, EventArgs e)
        {
            int? patientId = null;
            string patientName = string.Empty;

            // try to get patient from selected row
            if (dgv.CurrentRow != null && dgv.CurrentRow.DataBoundItem is DataRowView drv)
            {
                var r = drv.Row;
                if (r.Table.Columns.Contains("patient_id") && r["patient_id"] != DBNull.Value)
                    patientId = Convert.ToInt32(r["patient_id"]);
                if (r.Table.Columns.Contains("patient_nom"))
                    patientName = r["patient_nom"].ToString();
            }

            // if no patient found from selection, ask user to pick one
            if (!patientId.HasValue)
            {
                try
                {
                    var dt = hm.GetPatientsTable();
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Aucun patient disponible.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    using var pick = new Form();
                    pick.Text = "Sélectionner un patient";
                    pick.FormBorderStyle = FormBorderStyle.FixedDialog;
                    pick.StartPosition = FormStartPosition.CenterParent;
                    pick.Width = 420; pick.Height = 140; pick.MaximizeBox = false; pick.MinimizeBox = false;

                    var cb = new ComboBox { Left = 12, Top = 12, Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
                    cb.DisplayMember = "Text"; cb.ValueMember = "Id";
                    cb.Items.Add(new { Id = (int?)null, Text = "-- Aucun --" });
                    foreach (DataRow r in dt.Rows)
                    {
                        var id = Convert.ToInt32(r["id_patient"]);
                        var txt = r["nom"].ToString() + " " + r["prenom"].ToString();
                        cb.Items.Add(new { Id = (int?)id, Text = txt });
                    }
                    if (cb.Items.Count > 0) cb.SelectedIndex = 0;

                    var ok = new Button { Text = "OK", Left = 220, Top = 50, Width = 80 };
                    var cancel = new Button { Text = "Annuler", Left = 312, Top = 50, Width = 80 };
                    ok.Click += (s, ev) => pick.DialogResult = DialogResult.OK;
                    cancel.Click += (s, ev) => pick.DialogResult = DialogResult.Cancel;

                    pick.Controls.Add(cb); pick.Controls.Add(ok); pick.Controls.Add(cancel);

                    if (pick.ShowDialog(this) != DialogResult.OK) return;

                    if (cb.SelectedItem != null)
                    {
                        // selectedItem is an anonymous type -> use reflection to get Id/Text
                        var sel = cb.SelectedItem;
                        var idProp = sel.GetType().GetProperty("Id");
                        var textProp = sel.GetType().GetProperty("Text");
                        if (idProp != null && idProp.GetValue(sel) is int idv)
                        {
                            patientId = idv;
                            if (textProp != null) patientName = textProp.GetValue(sel)?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur récupération patients: " + ex.Message);
                    return;
                }
            }

            if (!patientId.HasValue)
            {
                MessageBox.Show("Aucun patient sélectionné.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var payments = hm.GetPaymentsByPatientId(patientId.Value);
                if (payments.Rows.Count == 0)
                {
                    MessageBox.Show("Aucune facture trouvée pour ce client.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog { Filter = "Fichiers PDF|*.pdf", FileName = $"facture_client_{patientName}.pdf" };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write);
                var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                // logo
                try
                {
                    string logoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "medical.png.png");
                    if (!File.Exists(logoFile))
                    {
                        var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                        if (Directory.Exists(assetsDir))
                        {
                            var first = Directory.GetFiles(assetsDir);
                            if (first.Length > 0) logoFile = first[0];
                        }
                    }
                    if (File.Exists(logoFile))
                    {
                        using var img = XImage.FromFile(logoFile);
                        gfx.DrawImage(img, 40, 40, 100, 100);
                    }
                }
                catch { }

                var y = 40;
                var xStart = 160;
                var titleFont = new XFont("Segoe UI", 16, XFontStyle.Bold);
                var headerFont = new XFont("Segoe UI", 12, XFontStyle.Bold);
                var normalFont = new XFont("Segoe UI", 10, XFontStyle.Regular);

                gfx.DrawString("FACTURE CLIENT", titleFont, XBrushes.Black, new XRect(xStart, y, page.Width - xStart - 40, 30), XStringFormats.TopLeft);
                y += 34;

                // show connected user on the invoice
                try
                {
                    var userName = UserSession.FullName ?? UserSession.Username ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        gfx.DrawString($"Préparé par: {userName}", normalFont, XBrushes.Black, new XRect(40, 80, page.Width - 80, 20), XStringFormats.TopRight);
                    }
                }
                catch { }

                gfx.DrawString($"Client: {patientName}", normalFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
                y += 24;

                // table header
                y += 6;
                var left = 40;
                var totalWidth = page.Width.Point - 80;
                var colRefW = 160;
                var colDateW = 120;
                var colMethodW = 120;
                var colAmountW = 80;

                gfx.DrawString("Réf", headerFont, XBrushes.Black, new XRect(left, y, colRefW, 20), XStringFormats.TopLeft);
                gfx.DrawString("Date", headerFont, XBrushes.Black, new XRect(left + colRefW, y, colDateW, 20), XStringFormats.TopLeft);
                gfx.DrawString("Méthode", headerFont, XBrushes.Black, new XRect(left + colRefW + colDateW, y, colMethodW, 20), XStringFormats.TopLeft);
                gfx.DrawString("Montant", headerFont, XBrushes.Black, new XRect(left + colRefW + colDateW + colMethodW, y, colAmountW, 20), XStringFormats.TopLeft);
                y += 22;

                decimal sum = 0m;
                foreach (DataRow r in payments.Rows)
                {
                    var reference = payments.Columns.Contains("reference") ? r["reference"].ToString() : string.Empty;
                    var paidAt = payments.Columns.Contains("paid_at") ? Convert.ToDateTime(r["paid_at"]) : DateTime.MinValue;
                    var method = payments.Columns.Contains("method") ? r["method"].ToString() : string.Empty;
                    var amount = payments.Columns.Contains("amount") ? Convert.ToDecimal(r["amount"]) : 0m;

                    gfx.DrawString(reference, normalFont, XBrushes.Black, new XRect(left, y, colRefW, 18), XStringFormats.TopLeft);
                    gfx.DrawString(paidAt == DateTime.MinValue ? string.Empty : paidAt.ToString("yyyy-MM-dd"), normalFont, XBrushes.Black, new XRect(left + colRefW, y, colDateW, 18), XStringFormats.TopLeft);
                    gfx.DrawString(method, normalFont, XBrushes.Black, new XRect(left + colRefW + colDateW, y, colMethodW, 18), XStringFormats.TopLeft);
                    gfx.DrawString(amount.ToString("N2"), normalFont, XBrushes.Black, new XRect(left + colRefW + colDateW + colMethodW, y, colAmountW, 18), XStringFormats.TopLeft);
                    y += 18;
                    sum += amount;

                    // new page if needed
                    if (y > page.Height.Point - 120)
                    {
                        page = doc.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }
                }

                y += 12;
                gfx.DrawString($"Total: {sum:N2}", headerFont, XBrushes.Black, new XRect(left + colRefW + colDateW, y, colMethodW + colAmountW, 20), XStringFormats.TopLeft);

                gfx.DrawString("Merci pour votre confiance.", normalFont, XBrushes.Black, new XRect(40, page.Height - 80, page.Width - 80, 20), XStringFormats.Center);

                // include prepared by footer
                try
                {
                    var userName = UserSession.FullName ?? UserSession.Username ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        gfx.DrawString($"Préparé par: {userName}", normalFont, XBrushes.Gray, new XRect(40, page.Height - 60, page.Width - 80, 16), XStringFormats.TopLeft);
                    }
                }
                catch { }

                doc.Save(fs);
                fs.Flush();

                MessageBox.Show("Facture client générée avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur génération facture client: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PaiementControl_Load(object? sender, EventArgs e)
        {
            try
            {
                MainDashboard.UiHelpers.ConfigureGrid(dgv);
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                LoadData();
            }
            catch { }
        }

        private void LoadData()
        {
            try
            {
                // ensure MySql types are referenced without invalid IDisposable usage
                var _ = typeof(MySql.Data.MySqlClient.MySqlDbType);
            }
            catch { }

            try
            {
                // try to load payments table if exists
                var dt = new DataTable();
                try
                {
                    dt = hm.GetPaymentsTable();
                }
                catch { }
                dgv.DataSource = dt.DefaultView;
                ApplyFilter();
            }
            catch { }
        }

        private void ApplyFilter()
        {
            try
            {
                if (dgv.DataSource is DataView dv)
                {
                    var q = txtSearch.Text?.Trim().Replace("'", "''") ?? string.Empty;
                    if (string.IsNullOrEmpty(q))
                    {
                        dv.RowFilter = string.Empty;
                        return;
                    }

                    // build a filter that searches commonly used columns
                    var parts = new System.Collections.Generic.List<string>();
                    var cols = new[] { "reference", "patient_nom", "method", "amount" };
                    foreach (var c in cols)
                    {
                        // amount numeric -> try parse and filter specially
                        if (c == "amount" && decimal.TryParse(q, out var val))
                        {
                            parts.Add($"Convert([{c}], 'System.String') LIKE '%{val}%'");
                        }
                        else
                        {
                            parts.Add($"[{c}] LIKE '%{q}%'");
                        }
                    }

                    dv.RowFilter = string.Join(" OR ", parts);
                }
            }
            catch { }
        }

        private void PrintInvoice_Click(object? sender, EventArgs e)
        {
            if (dgv.CurrentRow == null)
            {
                MessageBox.Show("Veuillez sélectionner un paiement.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!(dgv.CurrentRow.DataBoundItem is DataRowView drv))
            {
                MessageBox.Show("Impossible de lire la ligne sélectionnée.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var row = drv.Row;
            try
            {
                var id = row.Table.Columns.Contains("id") ? row["id"] : row[0];
                var patient = row.Table.Columns.Contains("patient_nom") ? row["patient_nom"].ToString() : string.Empty;
                if (string.IsNullOrWhiteSpace(patient))
                {
                    MessageBox.Show("Impossible d'imprimer la facture : le paiement n'est pas lié à un client.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var reference = row.Table.Columns.Contains("reference") ? row["reference"].ToString() : string.Empty;
                var amount = row.Table.Columns.Contains("amount") ? Convert.ToDecimal(row["amount"]) : 0m;
                var currency = row.Table.Columns.Contains("currency") ? row["currency"].ToString() : "";
                var method = row.Table.Columns.Contains("method") ? row["method"].ToString() : string.Empty;
                var paidAt = row.Table.Columns.Contains("paid_at") ? Convert.ToDateTime(row["paid_at"]) : DateTime.Now;
                var notes = row.Table.Columns.Contains("notes") ? row["notes"].ToString() : string.Empty;

                using var sfd = new SaveFileDialog { Filter = "Fichiers PDF|*.pdf", FileName = $"facture_{id}.pdf" };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write);
                var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                // draw logo if present in assets
                try
                {
                    string logoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "medical.png.png");
                    if (!File.Exists(logoFile))
                    {
                        var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                        if (Directory.Exists(assetsDir))
                        {
                            var first = Directory.GetFiles(assetsDir);
                            if (first.Length > 0) logoFile = first[0];
                        }
                    }
                    if (File.Exists(logoFile))
                    {
                        using var img = XImage.FromFile(logoFile);
                        gfx.DrawImage(img, 40, 40, 100, 100);
                    }
                }
                catch { }

                var y = 40;
                var xStart = 160;
                var titleFont = new XFont("Segoe UI", 18, XFontStyle.Bold);
                var headerFont = new XFont("Segoe UI", 12, XFontStyle.Bold);
                var normalFont = new XFont("Segoe UI", 11, XFontStyle.Regular);

                gfx.DrawString("FACTURE", titleFont, XBrushes.Black, new XRect(xStart, y, page.Width - xStart - 40, 30), XStringFormats.TopLeft);
                y += 40;

                gfx.DrawString($"Référence: {reference}", normalFont, XBrushes.Black, new XRect(xStart, y, page.Width - xStart - 40, 20), XStringFormats.TopLeft);
                y += 22;
                gfx.DrawString($"Date: {paidAt:yyyy-MM-dd HH:mm}", normalFont, XBrushes.Black, new XRect(xStart, y, page.Width - xStart - 40, 20), XStringFormats.TopLeft);
                y += 30;

                gfx.DrawString("Détails du paiement", headerFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
                y += 26;

                gfx.DrawString($"Client: {patient}", normalFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"Méthode: {method}", normalFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"Montant: {amount:N2} {currency}", normalFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
                y += 18;
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    gfx.DrawString($"Notes: {notes}", normalFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 80), XStringFormats.TopLeft);
                    y += 80;
                }

                gfx.DrawString("Merci pour votre confiance.", normalFont, XBrushes.Black, new XRect(40, page.Height - 80, page.Width - 80, 20), XStringFormats.Center);

                doc.Save(fs);
                fs.Flush();

                MessageBox.Show("Facture PDF générée avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur génération facture: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var f = new PaiementForm(hm);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                LoadData();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                using var f = new PaiementForm(hm, id);
                if (f.ShowDialog(this) == DialogResult.OK) LoadData();
            }
            catch { }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                var res = MessageBox.Show("Voulez-vous supprimer ce paiement ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
                try { hm.DeletePayment(id); } catch (Exception ex) { MessageBox.Show("Erreur suppression paiement: " + ex.Message); }
                LoadData();
            }
            catch { }
        }
    }
}
