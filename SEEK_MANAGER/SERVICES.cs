using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public partial class SERVICES : Form
    {
        private readonly HospitalManager hm;

        public SERVICES()
        {
            InitializeComponent();
            this.Size = new System.Drawing.Size(1024, 700);
            this.MinimumSize = this.Size;
            hm = new HospitalManager();

            guna2Button1.Click += Guna2Button1_Click;
            guna2Button2.Click += Guna2Button2_Click;
            guna2Button3.Click += Guna2Button3_Click;
            Load += SERVICES_Load;
            // wire designer search box and etat button
            guna2SearchBox.TextChanged += (s, e) => SearchService.Instance.Publish(guna2SearchBox.Text);
            SearchService.Instance.Subscribe(q =>
            {
                try
                {
                    if (guna2DataGridView1.DataSource is DataView dv)
                    {
                        if (string.IsNullOrWhiteSpace(q)) dv.RowFilter = string.Empty;
                        else dv.RowFilter = $"nom_service LIKE '%{q.Replace("'", "''")}%' OR description LIKE '%{q.Replace("'", "''")}%';";
                    }
                }
                catch { }

            });
            guna2EtatSortie.Click += (s, e) =>
            {
                try
                {
                    using var f = new EtatSortieForm(hm, summary: true);
                    f.ShowDialog(this);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // style designer etat button
            guna2EtatSortie.Text = "🚪  ETAT_DE_SORTIE";

            try
            {
                guna2Button1.Text = "➕  AJOUTER";
                guna2Button1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);

                guna2Button2.Text = "✏️  MODIFIER";
                guna2Button2.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);

                guna2Button3.Text = "🗑️  SUPPRIMER";
                guna2Button3.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            }
            catch { }

            // unify action button colors for add/modify/delete and style ETAT_DE_SORTIE to Guna accent
            try { guna2Button1.FillColor = Color.FromArgb(52, 152, 219); guna2Button1.BorderRadius = 0; } catch { }
            try { guna2Button2.FillColor = Color.FromArgb(52, 152, 219); guna2Button2.BorderRadius = 0; } catch { }
            try { guna2Button3.FillColor = Color.FromArgb(52, 152, 219); guna2Button3.BorderRadius = 0; } catch { }
            try { guna2EtatSortie.FillColor = Color.FromArgb(46, 204, 113); guna2EtatSortie.BorderRadius = 8; guna2EtatSortie.ForeColor = Color.White; guna2EtatSortie.Font = new Font("Segoe UI", 12F, FontStyle.Bold); } catch { }

            // populate fields when a row is clicked/entered for quick edit
            guna2DataGridView1.CellClick += (s, e) => SyncFieldsWithSelectedRow();
            guna2DataGridView1.RowEnter += (s, e) => SyncFieldsWithSelectedRow();
            // refresh button removed
        }

        private void SERVICES_Load(object sender, EventArgs e)
        {
            RefreshGrid();
            // ensure DataGridView shows service names properly (already done in HospitalManager)
        }

        private void RefreshGrid()
        {
            try
            {
                hm.ChargerSERVICE(guna2DataGridView1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des services : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guna2Button1_Click(object? sender, EventArgs e)
        {
            try
            {
                string nom = guna2TextBox2.Text.Trim();
                string desc = guna2TextBox3.Text.Trim();
                hm.AjouterSERVICE(nom, desc);
                MessageBox.Show("Service ajouté.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur ajout service: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string desc = guna2TextBox3.Text.Trim();
                hm.ModifierSERVICE(id, nom, desc);
                MessageBox.Show("Service modifié.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur modification service: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                hm.SupprimerSERVICE(id);
                MessageBox.Show("Service supprimé.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression service: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            try
            {
                guna2TextBox1.Text = string.Empty; // ID
                guna2TextBox2.Text = string.Empty; // NOM_SERVICE
                guna2TextBox3.Text = string.Empty; // DESCRIPTION
                guna2TextBox2.Focus();
            }
            catch
            {
                // ignore errors during clearing
            }
        }

        private void SyncFieldsWithSelectedRow()
        {
            try
            {
                var row = guna2DataGridView1.CurrentRow;
                if (row == null) return;

                guna2TextBox1.Text = GetCellValue(row, "id_service") ?? GetCellValue(row, 0) ?? string.Empty;
                guna2TextBox2.Text = GetCellValue(row, "nom_service") ?? GetCellValue(row, 1) ?? string.Empty;
                guna2TextBox3.Text = GetCellValue(row, "description") ?? GetCellValue(row, 2) ?? string.Empty;
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

        private void guna2Button1_Click(object sender, EventArgs e)
        {

        }
    }
}
