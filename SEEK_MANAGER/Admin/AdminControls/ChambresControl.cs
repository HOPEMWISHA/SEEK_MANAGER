using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER.Admin.AdminControls
{
    public class ChambresControl : UserControl
    {
        private Guna.UI2.WinForms.Guna2DataGridView dgv;
        private Guna.UI2.WinForms.Guna2Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private HospitalManager hm = new HospitalManager();

        public ChambresControl()
        {
            Dock = DockStyle.Fill;
            dgv = new Guna.UI2.WinForms.Guna2DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            // place controls below the top header/menu by adding a top margin panel
            var topSpacer = new Panel { Dock = DockStyle.Top, Height = 72 }; // reserved space under admin header
            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft };
            btnAdd = new Guna.UI2.WinForms.Guna2Button { Text = "Ajouter", Width = 120, AutoRoundedCorners = true };
            btnEdit = new Guna.UI2.WinForms.Guna2Button { Text = "Modifier", Width = 120, AutoRoundedCorners = true };
            btnDelete = new Guna.UI2.WinForms.Guna2Button { Text = "Supprimer", Width = 120, AutoRoundedCorners = true };
            btnRefresh = new Guna.UI2.WinForms.Guna2Button { Text = "Actualiser", Width = 120, AutoRoundedCorners = true };
            // Add a role selector when adding a room: managed inside ChambreForm
            panel.Controls.Add(btnRefresh); panel.Controls.Add(btnDelete); panel.Controls.Add(btnEdit); panel.Controls.Add(btnAdd);

            Controls.Add(dgv); Controls.Add(panel); Controls.Add(topSpacer);

            btnRefresh.Click += (s, e) => LoadData();
            btnAdd.Click += (s, e) => AddChambre(); // Adding a new room
            btnEdit.Click += (s, e) => EditChambre(); // Editing an existing room
            btnDelete.Click += (s, e) => DeleteChambre();

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var con = MySqlDbManager.Instance.GetConnection())
                {
                    con.Open();
                    string sql = "SELECT id_chambre, numero, type, statut, COALESCE(role, 'Général') AS role FROM chambre";
                    var da = new MySql.Data.MySqlClient.MySqlDataAdapter(sql, con);
                    var dt = new DataTable();
                    da.Fill(dt);
                    dgv.DataSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement chambres: " + ex.Message);
            }
        }

        private void AddChambre()
        {
            using var f = new Forms.ChambreForm();
            f.ShowDialog(this);
            if (f.Saved) LoadData(); // Adding a new room
        }

        private void EditChambre()
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                using var f = new Forms.ChambreForm(id);
                f.ShowDialog(this);
                if (f.Saved) LoadData();
            }
            catch { }
        }

        private void DeleteChambre()
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                var res = MessageBox.Show("Voulez-vous supprimer cette chambre ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
                using var con = MySqlDbManager.Instance.GetConnection();
                con.Open();
                var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM chambre WHERE id_chambre=@id", con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                LoadData();
                // notify listeners
                HospitalManager.NotifyChambresChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur suppression chambre: " + ex.Message);
            }
        }
    }
}
