using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public partial class PATIENT : Form
    {
        private readonly HospitalManager hm;

        public PATIENT()
        {
            InitializeComponent();
            // ensure consistent interface size
            this.Size = new System.Drawing.Size(1024, 700);
            this.MinimumSize = this.Size;
            hm = new HospitalManager();

            // Wire button click events
            guna2Button1.Click += Guna2Button1_Click; // Ajouter
            guna2Button2.Click += Guna2Button2_Click; // Modifier
            guna2Button3.Click += Guna2Button3_Click; // Supprimer
            // wire designer search box and etat button
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
                            dv.RowFilter = $"nom LIKE '{safe}%'";
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
                    var lf = new ListForm(items, "Liste - Patients");
                    lf.ShowDialog(this);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            // Preserve designer-defined styles for ETAT_DE_SORTIE and action buttons.
            // Runtime overrides removed so the controls load with the local designer appearance.

            // populate fields when a row is clicked or entered
            guna2DataGridView1.CellClick += (s, e) => SyncFieldsWithSelectedRow();
            guna2DataGridView1.RowEnter += (s, e) => SyncFieldsWithSelectedRow();
        }

        private void PATIENT_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            try
            {
                hm.ChargerPATIENT(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des patients : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button1_Click(object? sender, EventArgs e)
        {
            // Ajouter
            try
            {
                string nom = guna2TextBox6.Text.Trim();
                string postnom = guna2TextBox7.Text.Trim();
                string prenom = guna2TextBox8.Text.Trim();
                string adresse = guna2TextBox3.Text.Trim();
                string tel = guna2TextBox5.Text.Trim();
                // date_naissance is in guna2TextBox4
                DateTime dateNaissance = DateTime.MinValue;
                if (!DateTime.TryParse(guna2TextBox4.Text.Trim(), out dateNaissance))
                {
                    // if parse fails, use today's date as fallback
                    dateNaissance = DateTime.Today;
                }

                // Validate date of birth: cannot be in the future
                if (dateNaissance.Date > DateTime.Today)
                {
                    MessageBox.Show("Date de naissance invalide : une personne ne peut pas être née dans le futur.", "Erreur de saisie", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // sexe from combo
                var sexe = (guna2ComboBoxSexe.SelectedItem as string) ?? guna2ComboBoxSexe.Text ?? string.Empty;

                // Validate required fields
                if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(sexe) || string.IsNullOrWhiteSpace(adresse))
                {
                    MessageBox.Show("Tous les champs obligatoires doivent être remplis (nom, prénom, sexe, date de naissance, adresse).", "Champs requis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hm.AjouterPATIENT(nom + " " + postnom, prenom, sexe, dateNaissance, tel, adresse);

                MessageBox.Show("Patient ajouté avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout du patient : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button2_Click(object? sender, EventArgs e)
        {
            // Modifier
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID patient invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string nom = guna2TextBox6.Text.Trim();
                string postnom = guna2TextBox7.Text.Trim();
                string prenom = guna2TextBox8.Text.Trim();
                string adresse = guna2TextBox3.Text.Trim();
                string tel = guna2TextBox5.Text.Trim();
                var sexe = (guna2ComboBoxSexe.SelectedItem as string) ?? guna2ComboBoxSexe.Text ?? string.Empty;
                DateTime dateNaissance = DateTime.MinValue;
                if (!DateTime.TryParse(guna2TextBox4.Text.Trim(), out dateNaissance))
                {
                    dateNaissance = DateTime.Today;
                }

                // Validate date of birth: cannot be in the future
                if (dateNaissance.Date > DateTime.Today)
                {
                    MessageBox.Show("Date de naissance invalide : une personne ne peut pas être née dans le futur.", "Erreur de saisie", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(sexe) || string.IsNullOrWhiteSpace(adresse))
                {
                    MessageBox.Show("Tous les champs obligatoires doivent être remplis (nom, prénom, sexe, date de naissance, adresse).", "Champs requis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                hm.ModifierPATIENT(id, nom + " " + postnom, prenom, sexe, dateNaissance, tel, adresse);

                MessageBox.Show("Patient modifié avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification du patient : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button3_Click(object? sender, EventArgs e)
        {
            // Supprimer
            try
            {
                if (!int.TryParse(guna2TextBox1.Text.Trim(), out int id))
                {
                    MessageBox.Show("ID patient invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show("Voulez-vous vraiment supprimer ce patient ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes)
                    return;

                hm.SupprimerPATIENT(id);
                MessageBox.Show("Patient supprimé.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du patient : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            try
            {
                guna2TextBox1.Text = string.Empty; // ID
                guna2TextBox6.Text = string.Empty; // NOM
                guna2TextBox7.Text = string.Empty; // POST_NOM
                guna2TextBox8.Text = string.Empty; // PRENOM
                guna2TextBox4.Text = string.Empty; // DATE_NAISSENCE
                guna2TextBox5.Text = string.Empty; // TELEPHONE
                guna2TextBox3.Text = string.Empty; // ADDRESS
                guna2ComboBoxSexe.SelectedIndex = -1;
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

                // id
                var idVal = GetCellValue(row, "id_patient") ?? GetCellValue(row, 0);
                guna2TextBox1.Text = idVal ?? string.Empty;

                // nom may contain both nom + postnom in DB; attempt to split
                var fullName = GetCellValue(row, "nom") ?? GetCellValue(row, 1) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    var parts = fullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    guna2TextBox6.Text = parts.Length > 0 ? parts[0] : fullName;
                    guna2TextBox7.Text = parts.Length > 1 ? parts[1] : string.Empty;
                }

                // prenom
                guna2TextBox8.Text = GetCellValue(row, "prenom") ?? GetCellValue(row, 2) ?? string.Empty;

                // date_naissance
                var date = GetCellValue(row, "date_naissance") ?? GetCellValue(row, 4);
                if (DateTime.TryParse(date, out var dt)) guna2TextBox4.Text = dt.ToShortDateString(); else guna2TextBox4.Text = date ?? string.Empty;

                // telephone & adresse
                guna2TextBox5.Text = GetCellValue(row, "telephone") ?? GetCellValue(row, 5) ?? string.Empty;
                guna2TextBox3.Text = GetCellValue(row, "adresse") ?? GetCellValue(row, 6) ?? string.Empty;
                // sexe
                var sexeVal = GetCellValue(row, "sexe");
                if (!string.IsNullOrWhiteSpace(sexeVal))
                {
                    try { guna2ComboBoxSexe.SelectedItem = sexeVal; } catch { guna2ComboBoxSexe.Text = sexeVal; }
                }
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
    }
}
