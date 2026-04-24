using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER.Admin.AdminControls
{
    public class UsersControl : UserControl
    {
        private Guna.UI2.WinForms.Guna2DataGridView dgv;
        private Guna.UI2.WinForms.Guna2Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private HospitalManager hm = new HospitalManager();

        public UsersControl()
        {
            Dock = DockStyle.Fill;
            dgv = new Guna.UI2.WinForms.Guna2DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            // add top spacer so content is below admin header
            var topSpacer = new Panel { Dock = DockStyle.Top, Height = 72 };
            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft };
            btnAdd = new Guna.UI2.WinForms.Guna2Button { Text = "Ajouter", Width = 120, AutoRoundedCorners = true };
            btnEdit = new Guna.UI2.WinForms.Guna2Button { Text = "Modifier", Width = 120, AutoRoundedCorners = true };
            btnDelete = new Guna.UI2.WinForms.Guna2Button { Text = "Supprimer", Width = 120, AutoRoundedCorners = true };
            btnRefresh = new Guna.UI2.WinForms.Guna2Button { Text = "Actualiser", Width = 120, AutoRoundedCorners = true };
            panel.Controls.Add(btnRefresh); panel.Controls.Add(btnDelete); panel.Controls.Add(btnEdit); panel.Controls.Add(btnAdd);

            Controls.Add(dgv); Controls.Add(panel); Controls.Add(topSpacer);

            btnRefresh.Click += (s, e) => LoadData();
            btnAdd.Click += (s, e) => AddUser();
            btnEdit.Click += (s, e) => EditUser();
            btnDelete.Click += (s, e) => DeleteUser();

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = hm.GetUsersTable();
                dgv.DataSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement utilisateurs: " + ex.Message);
            }
        }

        private void AddUser()
        {
            using var f = new Forms.UserForm();
            f.ShowDialog(this);
            if (f.Saved) LoadData();
        }

        private void EditUser()
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                using var f = new Forms.UserForm(id);
                f.ShowDialog(this);
                if (f.Saved) LoadData();
            }
            catch { }
        }

        private void DeleteUser()
        {
            if (dgv.CurrentRow == null) return;
            try
            {
                var id = Convert.ToInt32(dgv.CurrentRow.Cells[0].Value);
                var res = MessageBox.Show("Voulez-vous supprimer cet utilisateur ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
                hm.DeleteUser(id);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur suppression utilisateur: " + ex.Message);
            }
        }
    }
}
