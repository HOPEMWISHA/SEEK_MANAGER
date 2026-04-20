using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    using System.Linq;
    public partial class CONSULTATION : Form
    {
        private readonly HospitalManager hm;

        public CONSULTATION()
        {
            InitializeComponent();
            this.Size = new System.Drawing.Size(1024, 700);
            this.MinimumSize = this.Size;
            hm = new HospitalManager();

            guna2Button1.Click += Guna2Button1_Click;
            guna2Button2.Click += Guna2Button2_Click;
            guna2Button3.Click += Guna2Button3_Click;
            Load += CONSULTATION_Load;
            // wire designer search box + etat
            guna2SearchBox.TextChanged += (s, e) => SearchService.Instance.Publish(guna2SearchBox.Text);
            SearchService.Instance.Subscribe(q =>
            {
                try
                {
                    if (guna2DataGridView1.DataSource is DataView dv)
                    {
                        dv.Table.CaseSensitive = false;
                        if (string.IsNullOrWhiteSpace(q)) dv.RowFilter = string.Empty;
                        else
                        {
                            var safe = q.Replace("'", "''");
                            // filter consultations by patient name initial
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
                    DataTable? dt = null;
                    try { if (guna2DataGridView1.DataSource is DataView dv) dt = dv.ToTable(); else if (guna2DataGridView1.DataSource is DataTable dt2) dt = dt2.Copy(); } catch { dt = null; }
                    if (dt == null) { try { dt = hm.GetConsultationsTable(); } catch { dt = new DataTable(); } }
                    using var f = new EtatSortieForm(hm, dt ?? new DataTable(), "CONSULTATION - État de sortie");
                    f.FallbackLoader = () => { try { return hm.GetConsultationsTable(); } catch { return null; } };
                    f.ShowDialog(this);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Apply Guna style to ETAT_DE_SORTIE
            try
            {
                guna2EtatSortie.FillColor = System.Drawing.Color.FromArgb(46, 204, 113);
                guna2EtatSortie.ForeColor = System.Drawing.Color.White;
                guna2EtatSortie.BorderRadius = 8;
                guna2EtatSortie.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
                guna2EtatSortie.Size = new System.Drawing.Size(288, 68);
            }
            catch { }
            // refresh button removed
        }

        private void CONSULTATION_Load(object? sender, EventArgs e)
        {
            RefreshGrid();
            FillComboBoxes();
        }

        // refresh button removed; use RefreshGrid() and FillComboBoxes() directly if needed

        private void FillComboBoxes()
        {
            try
            {
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
                if (dtP2.Columns.Contains("id_patient")) guna2ComboBoxPatient.ValueMember = "id_patient";

                // allow filtering while typing
                try { guna2ComboBoxPatient.TextChanged -= ComboFilter_Patient_Changed; } catch { }
                guna2ComboBoxPatient.TextChanged += ComboFilter_Patient_Changed;
                try { guna2ComboBoxMedecin.TextChanged -= ComboFilter_Medecin_Changed; } catch { }
                guna2ComboBoxMedecin.TextChanged += ComboFilter_Medecin_Changed;

                var dtM = hm.GetAllMedecins();
                if (dtM == null || dtM.Columns.Count == 0)
                {
                    guna2ComboBoxMedecin.DataSource = null;
                    guna2ComboBoxMedecin.DisplayMember = string.Empty;
                    guna2ComboBoxMedecin.ValueMember = string.Empty;
                }
                else
                {
                    // choose a display column
                    string? displayCol = null;
                    foreach (var c in new[] { "nom", "nom_medecin", "full_name", "nom_complet", "name" })
                        if (dtM.Columns.Contains(c)) { displayCol = c; break; }

                    // if no single name column, try composing from nom + prenom
                    if (displayCol == null && (dtM.Columns.Contains("nom") || dtM.Columns.Contains("prenom")))
                    {
                        if (!dtM.Columns.Contains("full_name")) dtM.Columns.Add("full_name", typeof(string));
                        foreach (DataRow r in dtM.Rows)
                        {
                            var nom = r.Table.Columns.Contains("nom") ? r["nom"]?.ToString() ?? string.Empty : string.Empty;
                            var prenom = r.Table.Columns.Contains("prenom") ? r["prenom"]?.ToString() ?? string.Empty : string.Empty;
                            r["full_name"] = (nom + " " + prenom).Trim();
                        }
                        displayCol = "full_name";
                    }

                    // fallback to first string column
                    if (displayCol == null)
                    {
                        displayCol = dtM.Columns.Cast<DataColumn>().FirstOrDefault(cc => cc.DataType == typeof(string))?.ColumnName;
                    }

                    // choose a value column
                    string? valueCol = null;
                    foreach (var v in new[] { "id_medecin", "id", "medecin_id" })
                        if (dtM.Columns.Contains(v)) { valueCol = v; break; }

                    // build explicit display table showing "id - name" so id is visible
                    var dtOut = new DataTable();
                    dtOut.Columns.Add("display", typeof(string));
                    dtOut.Columns.Add("id", typeof(int));
                    foreach (DataRow r in dtM.Rows)
                    {
                        try
                        {
                            int idm = 0;
                            if (!string.IsNullOrWhiteSpace(valueCol) && dtM.Columns.Contains(valueCol) && r[valueCol] != DBNull.Value)
                                idm = Convert.ToInt32(r[valueCol]);
                            else if (dtM.Columns.Contains("id_medecin") && r["id_medecin"] != DBNull.Value) idm = Convert.ToInt32(r["id_medecin"]);
                            string name = string.Empty;
                            if (!string.IsNullOrWhiteSpace(displayCol) && dtM.Columns.Contains(displayCol)) name = r[displayCol]?.ToString() ?? string.Empty;
                            else if (dtM.Columns.Contains("nom")) name = r["nom"]?.ToString() ?? string.Empty;
                            dtOut.Rows.Add($"{idm} - {name}", idm);
                        }
                        catch { }
                    }
                    guna2ComboBoxMedecin.DisplayMember = "display";
                    guna2ComboBoxMedecin.ValueMember = "id";
                    guna2ComboBoxMedecin.DataSource = dtOut;
                }
                guna2ComboBoxPatient.SelectedIndex = -1;
                guna2ComboBoxMedecin.SelectedIndex = -1;
            }
            catch { }
        }

        private void ComboFilter_Patient_Changed(object? sender, EventArgs e)
        {
            // guard against re-entrancy: changing the RowFilter may trigger TextChanged again
            var cb = sender as ComboBox;
            if (cb == null) return;
            try
            {
                cb.TextChanged -= ComboFilter_Patient_Changed;
                var dv = GetDataViewFromCombo(cb);
                if (dv == null) return;
                dv.Table.CaseSensitive = false;
                var txt = (cb.Text ?? string.Empty).Replace("'", "''");
                // choose a column that exists and is suitable for text filtering
                string? col = null;
                if (!string.IsNullOrWhiteSpace(cb.DisplayMember) && dv.Table.Columns.Contains(cb.DisplayMember)) col = cb.DisplayMember;
                if (col == null)
                {
                    // common fallbacks
                    foreach (var candidate in new[] { "full_name", "patient_nom", "nom", "prenom", "name" })
                        if (dv.Table.Columns.Contains(candidate)) { col = candidate; break; }
                }
                if (col == null)
                {
                    // try any string column
                    foreach (DataColumn c in dv.Table.Columns)
                    {
                        if (c.DataType == typeof(string)) { col = c.ColumnName; break; }
                    }
                }
                if (col == null) return;
                var colEscaped = "[" + col.Replace("]", "]]") + "]";
                try
                {
                    if (string.IsNullOrWhiteSpace(txt)) dv.RowFilter = string.Empty;
                    else dv.RowFilter = $"{colEscaped} LIKE '%{txt}%'";
                }
                catch { try { dv.RowFilter = string.Empty; } catch { } }
            }
            finally
            {
                try { cb.TextChanged += ComboFilter_Patient_Changed; } catch { }
            }
        }

        private void ComboFilter_Medecin_Changed(object? sender, EventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null) return;
            try
            {
                cb.TextChanged -= ComboFilter_Medecin_Changed;
                var dv = GetDataViewFromCombo(cb);
                if (dv == null) return;
                dv.Table.CaseSensitive = false;
                var txt = (cb.Text ?? string.Empty).Replace("'", "''");
                string? col = null;
                if (!string.IsNullOrWhiteSpace(cb.DisplayMember) && dv.Table.Columns.Contains(cb.DisplayMember)) col = cb.DisplayMember;
                if (col == null)
                {
                    foreach (var candidate in new[] { "nom", "full_name", "medecin_nom", "prenom", "name" })
                        if (dv.Table.Columns.Contains(candidate)) { col = candidate; break; }
                }
                if (col == null)
                {
                    foreach (DataColumn c in dv.Table.Columns)
                    {
                        if (c.DataType == typeof(string)) { col = c.ColumnName; break; }
                    }
                }
                if (col == null) return;
                // escape column name for RowFilter (handle spaces/special chars)
                var colEscaped = "[" + col.Replace("]", "]]") + "]";
                try
                {
                    if (string.IsNullOrWhiteSpace(txt)) dv.RowFilter = string.Empty;
                    else dv.RowFilter = $"{colEscaped} LIKE '%{txt}%'";
                }
                catch { try { dv.RowFilter = string.Empty; } catch { } }
            }
            finally
            {
                try { cb.TextChanged += ComboFilter_Medecin_Changed; } catch { }
            }
        }

        // Helper: obtain a DataView from common ComboBox DataSource types
        private DataView? GetDataViewFromCombo(ComboBox cb)
        {
            try
            {
                if (cb.DataSource is DataView dv) return dv;
                if (cb.DataSource is DataTable dt) return dt.DefaultView;
                if (cb.DataSource is BindingSource bs)
                {
                    if (bs.List is DataView bdv) return bdv;
                    if (bs.DataSource is DataTable bdt) return bdt.DefaultView;
                }
            }
            catch { }
            return null;
        }

        private void RefreshGrid()
        {
            try
            {
                hm.ChargerCONSULTATION(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement consultation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button1_Click(object? sender, EventArgs e)
        {
            try
            {
                DateTime datec;
                if (!DateTime.TryParse(guna2TextBox6.Text.Trim(), out datec)) datec = DateTime.Today;
                string diag = guna2TextBox7.Text.Trim();
                string trait = guna2TextBox5.Text.Trim();
                int idPatient = 0, idMedecin = 0;
                try { idPatient = Convert.ToInt32(guna2ComboBoxPatient.SelectedValue ?? 0); } catch { }
                try { idMedecin = Convert.ToInt32(guna2ComboBoxMedecin.SelectedValue ?? 0); } catch { }

                hm.AjouterCONSULTATION(datec, diag, trait, idPatient, idMedecin);
                MessageBox.Show("Consultation ajoutée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur ajout consultation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button2_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID consultation invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DateTime datec;
                if (!DateTime.TryParse(guna2TextBox6.Text.Trim(), out datec)) datec = DateTime.Today;
                string diag = guna2TextBox7.Text.Trim();
                string trait = guna2TextBox5.Text.Trim();
                int idPatient = 0, idMedecin = 0;
                try { idPatient = Convert.ToInt32(guna2ComboBoxPatient.SelectedValue ?? 0); } catch { }
                try { idMedecin = Convert.ToInt32(guna2ComboBoxMedecin.SelectedValue ?? 0); } catch { }

                hm.ModifierCONSULTATION(id, datec, diag, trait, idPatient, idMedecin);
                MessageBox.Show("Consultation modifiée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur modification consultation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button3_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID consultation invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show("Voulez-vous vraiment supprimer cette consultation ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;

                hm.SupprimerCONSULTATION(id);
                MessageBox.Show("Consultation supprimée.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression consultation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            try
            {
                guna2TextBox1.Text = string.Empty;
                guna2ComboBoxPatient.SelectedIndex = -1;
                guna2ComboBoxMedecin.SelectedIndex = -1;
                guna2TextBox5.Text = string.Empty;
                guna2TextBox6.Text = string.Empty;
                guna2TextBox7.Text = string.Empty;
                guna2TextBox1.Focus();
            }
            catch
            {
            }
        }

        private void CONSULTATION_Load_1(object sender, EventArgs e)
        {

        }
    }
}
