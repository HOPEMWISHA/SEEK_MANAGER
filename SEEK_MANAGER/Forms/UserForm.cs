using System;
using System.Drawing;
using System.Windows.Forms;
using BCrypt.Net;

namespace SEEK_MANAGER.Forms
{
    public class UserForm : Form
    {
        private Guna.UI2.WinForms.Guna2TextBox txtFullName, txtUsername, txtEmail, txtPassword;
        private Guna.UI2.WinForms.Guna2ComboBox cbRole;
        private Guna.UI2.WinForms.Guna2Button btnSave, btnCancel;
        public bool Saved { get; private set; }
        public string ResultFullName => txtFullName.Text.Trim();
        public string ResultUsername => txtUsername.Text.Trim();
        public string ResultEmail => txtEmail.Text.Trim();
        public string ResultRole => cbRole.SelectedItem?.ToString() ?? "Utilisateur";
        private int? id;
        private HospitalManager hm = new HospitalManager();

        public UserForm(int? id = null)
        {
            this.id = id;
            Initialize();
            if (id.HasValue) LoadUser(id.Value);
        }

        private void Initialize()
        {
            Text = id.HasValue ? "Modifier utilisateur" : "Ajouter utilisateur";
            Width = 420; Height = 340; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            txtFullName = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 12, Width = 360, PlaceholderText = "Nom complet" };
            txtUsername = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 52, Width = 360, PlaceholderText = "Nom d'utilisateur" };
            txtPassword = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 92, Width = 360, PlaceholderText = "Mot de passe (laisser vide pour ne pas changer)", PasswordChar = '\u25CF' };
            txtEmail = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 132, Width = 360, PlaceholderText = "Email" };
            cbRole = new Guna.UI2.WinForms.Guna2ComboBox { Left = 12, Top = 172, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cbRole.Items.AddRange(new[] { "Admin", "Utilisateur" });
            cbRole.SelectedIndex = 1;

            btnSave = new Guna.UI2.WinForms.Guna2Button { Text = "Enregistrer", Left = 196, Top = 212, Width = 100 };
            btnCancel = new Guna.UI2.WinForms.Guna2Button { Text = "Annuler", Left = 302, Top = 212, Width = 90 };
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { Saved = false; Close(); };

            Controls.Add(txtFullName); Controls.Add(txtUsername); Controls.Add(txtPassword); Controls.Add(txtEmail); Controls.Add(cbRole); Controls.Add(btnSave); Controls.Add(btnCancel);
        }

        private void LoadUser(int id)
        {
            try
            {
                var dt = hm.GetUsersTable();
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["id_user"]) == id)
                    {
                        txtFullName.Text = r["full_name"].ToString();
                        txtUsername.Text = r["username"].ToString();
                        txtEmail.Text = r["email"].ToString();
                        cbRole.SelectedItem = r["role"].ToString();
                        break;
                    }
                }
            }
            catch { }
        }

        private void Save()
        {
            try
            {
                var full = txtFullName.Text.Trim();
                var user = txtUsername.Text.Trim();
                var email = txtEmail.Text.Trim();
                var role = cbRole.SelectedItem?.ToString() ?? "Utilisateur";
                var pass = txtPassword.Text;
                if (string.IsNullOrWhiteSpace(user)) { MessageBox.Show("Le nom d'utilisateur est requis."); return; }

                string hash = null;
                if (!string.IsNullOrEmpty(pass)) hash = BCrypt.Net.BCrypt.HashPassword(pass);

                if (id.HasValue)
                {
                    hm.UpdateUser(id.Value, full, user, hash, role, email);
                }
                else
                {
                    if (string.IsNullOrEmpty(pass)) { MessageBox.Show("Le mot de passe est requis pour un nouvel utilisateur."); return; }
                    hm.AddUser(full, user, hash, role, email);
                }

                Saved = true; Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur sauvegarde utilisateur: " + ex.Message);
            }
        }
    }
}
