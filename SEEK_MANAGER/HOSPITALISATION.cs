using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public partial class HOSPITALISATION : Form
    {
        private readonly HospitalManager hm;

        public HOSPITALISATION()
        {
            InitializeComponent();
            this.Size = new System.Drawing.Size(1024, 700);
            this.MinimumSize = this.Size;
            hm = new HospitalManager();

            guna2Button1.Click += Guna2Button1_Click;
            guna2Button2.Click += Guna2Button2_Click;
            guna2Button3.Click += Guna2Button3_Click;
            Load += HOSPITALISATION_Load;
            // wire designer search box + etat button
            guna2SearchBox.TextChanged += (s, e) => SearchService.Instance.Publish(guna2SearchBox.Text);
            SearchService.Instance.Subscribe(q =>
            {
                try
                {
                    if (guna2DataGridView1.DataSource is DataView dv)
                    {
                        // case-insensitive
                        dv.Table.CaseSensitive = false;
                        if (string.IsNullOrWhiteSpace(q))
                        {
                            dv.RowFilter = string.Empty;
                        }
                        else
                        {
                            var safe = q.Replace("'", "''");
                            // filter by patient name starting with the query (initial match)
                            dv.RowFilter = $"patient_nom LIKE '{safe}%'";
                        }
                    }
                }
                catch { }
            });

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
                    var lf = new ListForm(items, "Liste - Hospitalisations");
                    lf.ShowDialog(this);

                    if (guna2DataGridView1.CurrentRow == null) { MessageBox.Show("Sélectionnez une hospitalisation.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                    if (!int.TryParse(guna2DataGridView1.CurrentRow.Cells[0].Value?.ToString(), out int id)) { MessageBox.Show("Impossible de déterminer l'ID.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    var input = Microsoft.VisualBasic.Interaction.InputBox("Entrez la date de sortie (jj/mm/aaaa):", "Etat de sortie", DateTime.Today.ToShortDateString());
                    if (DateTime.TryParse(input, out DateTime sortie))
                    {
                        if (sortie.Date > DateTime.Today) { MessageBox.Show("La date de sortie ne peut pas être dans le futur.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        hm.SetDateSortie(id, sortie);
                        RefreshGrid();
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Preserve designer-defined styles for the action buttons and ETAT_DE_SORTIE.
            // Runtime overrides removed so controls keep local designer appearance.

            // populate fields when selecting a row
            guna2DataGridView1.CellClick += (s, e) => SyncFieldsWithSelectedRow();
            guna2DataGridView1.RowEnter += (s, e) => SyncFieldsWithSelectedRow();
            // refresh button removed
        }

        private void HOSPITALISATION_Load(object? sender, EventArgs e)
        {
            RefreshGrid();
            FillComboBoxes();
        }

        private void FillComboBoxes()
        {
            try
            {
                // Patients — display as "nom prenom", value = id_patient
                var dtP = hm.GetPatientsTable();
                var dtP2 = dtP.Clone();
                if (!dtP2.Columns.Contains("full_name")) dtP2.Columns.Add("full_name", typeof(string));
                foreach (DataRow r in dtP.Rows)
                {
                    var nr = dtP2.NewRow();
                    nr.ItemArray = r.ItemArray;
                    nr["full_name"] = (r["nom"]?.ToString() ?? string.Empty) + " " + (r["prenom"]?.ToString() ?? string.Empty);
                    dtP2.Rows.Add(nr);
                }
                guna2ComboBoxPatient.DataSource = dtP2.DefaultView;
                guna2ComboBoxPatient.DisplayMember = "full_name";
                // original id column is id_patient
                if (dtP2.Columns.Contains("id_patient")) guna2ComboBoxPatient.ValueMember = "id_patient";

                try { guna2ComboBoxPatient.TextChanged -= ComboFilter_Patient_Changed; } catch { }
                guna2ComboBoxPatient.TextChanged += ComboFilter_Patient_Changed;

                // Services — display name, value = id_service
                var dtS = hm.GetServicesTable();
                guna2ComboBoxService.DataSource = dtS.DefaultView;
                if (dtS.Columns.Contains("nom_service")) guna2ComboBoxService.DisplayMember = "nom_service";
                if (dtS.Columns.Contains("id_service")) guna2ComboBoxService.ValueMember = "id_service";
                guna2ComboBoxPatient.SelectedIndex = -1;
                guna2ComboBoxService.SelectedIndex = -1;
                try { guna2ComboBoxService.TextChanged -= ComboFilter_Service_Changed; } catch { }
                guna2ComboBoxService.TextChanged += ComboFilter_Service_Changed;
            }
            catch { }
        }

        private void ComboFilter_Patient_Changed(object? sender, EventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb == null) return;
                var dv = cb.DataSource as DataView;
                if (dv == null) return;
                var txt = cb.Text.Replace("'", "''");
                if (string.IsNullOrWhiteSpace(txt)) dv.RowFilter = string.Empty;
                else dv.RowFilter = $"full_name LIKE '%{txt}%" + "'";
            }
            catch { }
        }

        private void ComboFilter_Service_Changed(object? sender, EventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb == null) return;
                var dv = cb.DataSource as DataView;
                if (dv == null) return;
                var txt = cb.Text.Replace("'", "''");
                if (string.IsNullOrWhiteSpace(txt)) dv.RowFilter = string.Empty;
                else dv.RowFilter = $"nom_service LIKE '%{txt}%" + "'";
            }
            catch { }
        }

        private void RefreshAll()
        {
            try
            {
                RefreshGrid();
                FillComboBoxes();
            }
            catch { }
        }

        private void RefreshGrid()
        {
            try
            {
                hm.ChargerHOSPITALISATION(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                // ignore if method missing, but show other errors
                if (!(ex is System.Reflection.TargetInvocationException))
                    MessageBox.Show($"Erreur chargement hospitalisation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button1_Click(object? sender, EventArgs e)
        {
            try
            {
                string chambre = (guna2ComboBoxChambre.SelectedItem as string) ?? guna2ComboBoxChambre.Text ?? string.Empty;
                int idPatient = 0;
                int idService = 0;
                try { idPatient = Convert.ToInt32(guna2ComboBoxPatient.SelectedValue ?? 0); } catch { idPatient = 0; }
                try { idService = Convert.ToInt32(guna2ComboBoxService.SelectedValue ?? 0); } catch { idService = 0; }

                DateTime dateEntree;
                if (!DateTime.TryParse(guna2TextBox6.Text.Trim(), out dateEntree)) dateEntree = DateTime.Today;
                DateTime dateSortie;
                if (!DateTime.TryParse(guna2TextBox7.Text.Trim(), out dateSortie)) dateSortie = DateTime.Today;

                hm.AjouterHOSPITALISATION(chambre, idPatient, idService, dateEntree, dateSortie);
                MessageBox.Show("Hospitalisation ajoutée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur ajout hospitalisation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button2_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID hospitalisation invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string chambre = (guna2ComboBoxChambre.SelectedItem as string) ?? guna2ComboBoxChambre.Text ?? string.Empty;
                int idPatient = 0;
                int idService = 0;
                try { idPatient = Convert.ToInt32(guna2ComboBoxPatient.SelectedValue ?? 0); } catch { idPatient = 0; }
                try { idService = Convert.ToInt32(guna2ComboBoxService.SelectedValue ?? 0); } catch { idService = 0; }

                DateTime dateEntree;
                if (!DateTime.TryParse(guna2TextBox6.Text.Trim(), out dateEntree)) dateEntree = DateTime.Today;
                DateTime dateSortie;
                if (!DateTime.TryParse(guna2TextBox7.Text.Trim(), out dateSortie)) dateSortie = DateTime.Today;

                hm.ModifierHOSPITALISATION(id, chambre, idPatient, idService, dateEntree, dateSortie);
                MessageBox.Show("Hospitalisation modifiée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur modification hospitalisation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button3_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID hospitalisation invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show("Voulez-vous vraiment supprimer cette hospitalisation ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;

                hm.SupprimerHOSPITALISATION(id);
                MessageBox.Show("Hospitalisation supprimée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression hospitalisation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            try
            {
                guna2TextBox1.Text = string.Empty;
                // clear combo boxes for patient/service
                try { guna2ComboBoxPatient.SelectedIndex = -1; } catch { }
                try { guna2ComboBoxService.SelectedIndex = -1; } catch { }
                try { guna2ComboBoxChambre.SelectedIndex = -1; } catch { try { guna2ComboBoxChambre.Text = string.Empty; } catch { } }
                guna2TextBox6.Text = string.Empty;
                guna2TextBox7.Text = string.Empty;
                guna2TextBox1.Focus();
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

                guna2TextBox1.Text = GetCellValue(row, "id_hospitalisation") ?? GetCellValue(row, 0) ?? string.Empty;
                var chambreVal = GetCellValue(row, "chambre") ?? GetCellValue(row, 1) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(chambreVal))
                {
                    try { guna2ComboBoxChambre.SelectedItem = chambreVal; } catch { try { guna2ComboBoxChambre.Text = chambreVal; } catch { } }
                }
                // patient and service may be displayed as names; try to map back to ids if possible
                var patientName = GetCellValue(row, "patient_nom") ?? GetCellValue(row, 2);
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    try
                    {
                        for (int i = 0; i < guna2ComboBoxPatient.Items.Count; i++)
                        {
                            var drv = guna2ComboBoxPatient.Items[i] as DataRowView;
                            if (drv == null) continue;
                            var full = (drv.DataView.Table.Columns.Contains("full_name") ? drv["full_name"].ToString() : (drv["nom"]?.ToString() ?? string.Empty));
                            if (string.Equals(full, patientName, StringComparison.OrdinalIgnoreCase))
                            {
                                guna2ComboBoxPatient.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                var serviceName = GetCellValue(row, "service_nom") ?? GetCellValue(row, 3);
                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    try
                    {
                        for (int i = 0; i < guna2ComboBoxService.Items.Count; i++)
                        {
                            var drv = guna2ComboBoxService.Items[i] as DataRowView;
                            if (drv == null) continue;
                            var nom = drv.DataView.Table.Columns.Contains("nom_service") ? drv["nom_service"].ToString() : drv["nom"]?.ToString();
                            if (string.Equals(nom, serviceName, StringComparison.OrdinalIgnoreCase))
                            {
                                guna2ComboBoxService.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                var dateEntree = GetCellValue(row, "date_entree") ?? GetCellValue(row, 4);
                if (DateTime.TryParse(dateEntree, out var dte)) guna2TextBox6.Text = dte.ToShortDateString(); else guna2TextBox6.Text = dateEntree ?? string.Empty;

                var dateSortie = GetCellValue(row, "date_sortie") ?? GetCellValue(row, 5);
                if (DateTime.TryParse(dateSortie, out var dts)) guna2TextBox7.Text = dts.ToShortDateString(); else guna2TextBox7.Text = dateSortie ?? string.Empty;
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

        private void guna2Button1_Click_1(object sender, EventArgs e)
        {


        }
    }
}
