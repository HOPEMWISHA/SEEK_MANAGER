using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public partial class MEDECINS : Form
    {
        private readonly HospitalManager hm;
        private Guna.UI2.WinForms.Guna2ComboBox serviceCombo;
        public MEDECINS()
        {
            InitializeComponent();
            this.Size = new System.Drawing.Size(1024, 700);
            this.MinimumSize = this.Size;

            hm = new HospitalManager();

            // use the combobox added in the designer (guna2ComboBoxService)
            try
            {
                // only reference the designer combo so code can use it; do not override designer styles here
                serviceCombo = guna2ComboBoxService;
            }
            catch { }

            // Ensure default services exist and populate combo
            try
            {
                hm.EnsureDefaultServices();
                ReloadServiceCombo();
            }
            catch { }



            // wire buttons and events
            guna2Button1.Click += Guna2Button1_Click;
            guna2Button2.Click += Guna2Button2_Click;
            guna2Button3.Click += Guna2Button3_Click;
            Load += MEDECINS_Load;
            Activated += (s, e) => ReloadServiceCombo();

            // wire designer search
            try
            {
                guna2SearchBox.TextChanged += (s, e) => SearchService.Instance.Publish(guna2SearchBox.Text);
                SearchService.Instance.Subscribe(q =>
                {
                    try
                    {
                        if (guna2DataGridView1.DataSource is DataView dv)
                        {
                            if (string.IsNullOrWhiteSpace(q)) dv.RowFilter = string.Empty;
                            else dv.RowFilter = $"nom LIKE '%{q.Replace("'", "''")}%' OR specialite LIKE '%{q.Replace("'", "''")}%'";
                        }
                    }
                    catch { }
                });
            }
            catch { }

            guna2EtatSortie.Click += (s, e) =>
            {
                try
                {
                    var items = new System.Collections.Generic.List<string>();
                    foreach (DataGridViewRow row in guna2DataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var cells = new System.Collections.Generic.List<string>();
                        foreach (DataGridViewCell c in row.Cells)
                            cells.Add(c.Value?.ToString() ?? string.Empty);
                        items.Add(string.Join(" | ", cells));
                    }
                    var lf = new ListForm(items, "Liste - Medecins");
                    lf.ShowDialog(this);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Do not override button texts, fonts or colors here so designer-local styles remain intact.

            // Populate fields when selecting a row for quick editing
            guna2DataGridView1.CellClick += (s, e) => SyncFieldsWithSelectedRow();
            guna2DataGridView1.RowEnter += (s, e) => SyncFieldsWithSelectedRow();
        }

        private void ReloadServiceCombo()
        {
            try
            {
                if (serviceCombo == null) return;
                var src = hm.GetServicesTable();
                if (src == null || src.Rows.Count == 0)
                {
                    try { hm.EnsureDefaultServices(); } catch { }
                    src = hm.GetServicesTable();
                }
                if (src != null)
                {
                    var dt2 = new DataTable();
                    dt2.Columns.Add("display", typeof(string));
                    dt2.Columns.Add("id", typeof(int));
                    foreach (DataRow r2 in src.Rows)
                    {
                        try
                        {
                            int sid = Convert.ToInt32(r2["id_service"]);
                            var sname = r2.Table.Columns.Contains("nom_service") ? (r2["nom_service"]?.ToString() ?? "") : r2[0]?.ToString() ?? "";
                            dt2.Rows.Add($"{sid} - {sname}", sid);
                        }
                        catch { }
                    }
                    var sel = serviceCombo.SelectedValue;
                    serviceCombo.DisplayMember = "display";
                    serviceCombo.ValueMember = "id";
                    serviceCombo.DataSource = dt2;
                    try { if (sel != null) serviceCombo.SelectedValue = sel; else if (serviceCombo.Items.Count > 0) serviceCombo.SelectedIndex = 0; } catch { if (serviceCombo.Items.Count > 0) serviceCombo.SelectedIndex = 0; }
                }
            }
            catch { }
        }

        private void ClearFields()
        {
            try
            {
                guna2TextBox1.Text = string.Empty; // ID_MEDECINS
                guna2TextBox2.Text = string.Empty; // NOM_COMPLET
                guna2TextBox3.Text = string.Empty; // SPECIALITE
                if (serviceCombo != null) serviceCombo.SelectedIndex = -1;
                guna2TextBox2.Focus();
            }
            catch
            {
            }
        }

        private void SyncFieldsWithSelectedRow()
        {
            try
            {
                var row = guna2DataGridView1.CurrentRow;
                if (row == null) return;

                guna2TextBox1.Text = GetCellValue(row, "id_medecin") ?? GetCellValue(row, 0) ?? string.Empty;
                guna2TextBox2.Text = GetCellValue(row, "nom") ?? GetCellValue(row, 1) ?? string.Empty;
                guna2TextBox3.Text = GetCellValue(row, "specialite") ?? GetCellValue(row, 2) ?? string.Empty;
                // service id: select in combobox by id (preferred) so display shows "id - name"
                try
                {
                    var idServiceStr = GetCellValue(row, "id_service") ?? GetCellValue(row, 3);
                    if (!string.IsNullOrWhiteSpace(idServiceStr) && serviceCombo != null)
                    {
                        if (int.TryParse(idServiceStr, out var sid))
                        {
                            try { serviceCombo.SelectedValue = sid; }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        private string? GetCellValue(DataGridViewRow row, string columnName)
        {
            try
            {
                if (row.DataGridView != null && row.DataGridView.Columns.Contains(columnName))
                {
                    var v = row.Cells[columnName].Value;
                    return v?.ToString();
                }
            }
            catch { }
            return null;
        }

        private string? GetCellValue(DataGridViewRow row, int index)
        {
            try
            {
                if (index >= 0 && index < row.Cells.Count)
                {
                    var v = row.Cells[index].Value;
                    return v?.ToString();
                }
            }
            catch { }
            return null;
        }

        private void MEDECINS_Load(object? sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            try
            {
                hm.ChargerMEDECINS(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement medecins: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button1_Click(object? sender, EventArgs e)
        {
            try
            {
                string nom = guna2TextBox2.Text.Trim();
                string spec = guna2TextBox3.Text.Trim();
                int idService = 0;
                try { idService = Convert.ToInt32(serviceCombo?.SelectedValue ?? 0); } catch { }
                hm.AjouterMEDECINS(nom, spec, idService);
                MessageBox.Show("Medecin ajouté.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur ajout medecin: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button2_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string nom = guna2TextBox2.Text.Trim();
                string spec = guna2TextBox3.Text.Trim();
                int idService = 0;
                try { idService = Convert.ToInt32(serviceCombo?.SelectedValue ?? 0); } catch { }
                hm.ModifierMEDECINS(id, nom, spec, idService);
                MessageBox.Show("Medecin modifié.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur mod medecin: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button3_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                hm.SupprimerMEDECINS(id);
                MessageBox.Show("Medecin supprimé.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression medecin: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
