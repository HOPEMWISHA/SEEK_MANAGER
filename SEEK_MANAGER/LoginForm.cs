using System;
using System.Drawing;
using System.Windows.Forms;
using BCrypt.Net;

namespace SEEK_MANAGER
{
    public class LoginForm : Form
    {
        private Guna.UI2.WinForms.Guna2TextBox txtUsername;
        private Guna.UI2.WinForms.Guna2TextBox txtEmail;
        private Guna.UI2.WinForms.Guna2TextBox txtPassword;
        private Guna.UI2.WinForms.Guna2Button btnLogin;
        private Guna.UI2.WinForms.Guna2Button btnRegister;
        private Guna.UI2.WinForms.Guna2Button btnToggle;
        private Label lblTitle;

        private HospitalManager hm = new HospitalManager();

        public LoginForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Connexion - SEEK_MANAGER";
            Size = new Size(520, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            lblTitle = new Label { Text = "SEEK_MANAGER - Connexion", Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };

            txtUsername = new Guna.UI2.WinForms.Guna2TextBox { PlaceholderText = "Nom d'utilisateur", Location = new Point(40, 80), Size = new Size(420, 44) };
            txtEmail = new Guna.UI2.WinForms.Guna2TextBox { PlaceholderText = "Email (pour inscription)", Location = new Point(40, 130), Size = new Size(420, 44), Visible = false };
            txtPassword = new Guna.UI2.WinForms.Guna2TextBox { PlaceholderText = "Mot de passe", UseSystemPasswordChar = true, Location = new Point(40, 180), Size = new Size(420, 44) };

            btnLogin = new Guna.UI2.WinForms.Guna2Button { Text = "Se connecter", Location = new Point(40, 240), Size = new Size(200, 48), FillColor = Color.FromArgb(52,152,219) };
            btnRegister = new Guna.UI2.WinForms.Guna2Button { Text = "Créer un compte", Location = new Point(260, 240), Size = new Size(200, 48), FillColor = Color.FromArgb(46,204,113) };
            btnToggle = new Guna.UI2.WinForms.Guna2Button { Text = "S'inscrire", Location = new Point(40, 300), Size = new Size(420, 40), FillColor = Color.FromArgb(155,89,182) };

            btnLogin.Click += BtnLogin_Click;
            btnRegister.Click += BtnRegister_Click;
            btnToggle.Click += BtnToggle_Click;

            Controls.Add(lblTitle);
            Controls.Add(txtUsername);
            Controls.Add(txtEmail);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(btnRegister);
            Controls.Add(btnToggle);

            // default: hide register controls
            btnRegister.Visible = false;
        }

        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            // toggle register mode
            var registering = txtEmail.Visible;
            if (!registering)
            {
                // switch to register
                txtEmail.Visible = true;
                btnRegister.Visible = true;
                btnLogin.Visible = false;
                btnToggle.Text = "Se connecter";
            }
            else
            {
                // switch to login
                txtEmail.Visible = false;
                btnRegister.Visible = false;
                btnLogin.Visible = true;
                btnToggle.Text = "S'inscrire";
            }
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var email = txtEmail.Text.Trim();
                var password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Tous les champs sont requis pour l'inscription.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // hash password
                string hash = BCrypt.Net.BCrypt.HashPassword(password);

                using (var con = MySqlDbManager.Instance.GetConnection())
                {
                    con.Open();
                    var sql = "INSERT INTO users (username, password_hash, full_name, email) VALUES (@u,@p,@f,@e)";
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", hash);
                    cmd.Parameters.AddWithValue("@f", username);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Compte créé avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // switch back to login
                BtnToggle_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du compte : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Veuillez fournir nom d'utilisateur et mot de passe.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var con = MySqlDbManager.Instance.GetConnection())
                {
                    con.Open();
                    var sql = "SELECT id_user, password_hash, full_name FROM users WHERE username=@u LIMIT 1";
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@u", username);
                    var r = cmd.ExecuteReader();
                    if (!r.Read())
                    {
                        MessageBox.Show("Utilisateur introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        r.Close();
                        return;
                    }

                    var id = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                    var hash = r.IsDBNull(1) ? string.Empty : r.GetString(1);
                    var fullName = r.FieldCount > 2 && !r.IsDBNull(2) ? r.GetString(2) : username;
                    r.Close();

                    if (BCrypt.Net.BCrypt.Verify(password, hash))
                    {
                        // set session
                        UserSession.UserId = id;
                        UserSession.Username = username;
                        UserSession.FullName = fullName;

                        MessageBox.Show("Connexion réussie.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Mot de passe incorrect.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
