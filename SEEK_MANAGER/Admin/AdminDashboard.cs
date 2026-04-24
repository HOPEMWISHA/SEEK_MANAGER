using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SEEK_MANAGER.Admin
{
    public class AdminDashboard : Form
    {
        private TabControl tabs;
        public AdminDashboard()
        {
            Text = "Admin Dashboard";
            // open admin dashboard maximized so it occupies the entire screen
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            // header with title and logout
            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(250, 250, 250), Padding = new Padding(12) };
            var title = new Label { Text = "ADMIN - SEEK_MANAGER", Dock = DockStyle.Left, Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true };
            header.Controls.Add(title);

            // top menu buttons (restore management buttons)
            var menuPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                Width = 680,
                Padding = new Padding(6),
                BackColor = Color.Transparent
            };

            var btnMedecins = new Guna.UI2.WinForms.Guna2Button { Text = "Médecins", Width = 120, FillColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, AutoRoundedCorners = true, Margin = new Padding(6) };
            var btnUsers = new Guna.UI2.WinForms.Guna2Button { Text = "Utilisateurs", Width = 120, FillColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, AutoRoundedCorners = true, Margin = new Padding(6) };
            var btnChambres = new Guna.UI2.WinForms.Guna2Button { Text = "Chambres", Width = 120, FillColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, AutoRoundedCorners = true, Margin = new Padding(6) };
            var btnLogout = new Guna.UI2.WinForms.Guna2Button { Text = "Déconnexion", Width = 120, FillColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, AutoRoundedCorners = true, Margin = new Padding(6) };

            menuPanel.Controls.Add(btnMedecins);
            menuPanel.Controls.Add(btnUsers);
            menuPanel.Controls.Add(btnChambres);
            menuPanel.Controls.Add(btnLogout);

            header.Controls.Add(menuPanel);

            Controls.Add(header);

            // tabs area
            this.tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(new TabPage("Chambres") { Name = "tabChambres" });
            tabs.TabPages.Add(new TabPage("Utilisateurs") { Name = "tabUsers" });
            tabs.TabPages.Add(new TabPage("Médecins") { Name = "tabMedecins" });
            // ensure tab pages have padding so their content sits below the header/menu
            foreach (TabPage tp in tabs.TabPages)
            {
                tp.Padding = new Padding(12, header.Height + 12, 12, 12);
            }
            Controls.Add(tabs);

            // wire menu buttons to select tabs
            btnUsers.Click += (s, e) => { try { tabs.SelectedTab = tabs.TabPages["tabUsers"]; } catch { } };
            btnMedecins.Click += (s, e) => { try { tabs.SelectedTab = tabs.TabPages["tabMedecins"]; } catch { } };
            btnChambres.Click += (s, e) => { try { tabs.SelectedTab = tabs.TabPages["tabChambres"]; } catch { } };
            btnLogout.Click += BtnLogout_Click;

            // Ensure schema exists
            try { new HospitalManager().EnsureAdminSchema(); } catch { }

            // Embed user controls (use guna-styled controls inside)
            var chCtrl = new AdminControls.ChambresControl { Dock = DockStyle.Fill };
            tabs.TabPages["tabChambres"].Controls.Add(chCtrl);

            // Affectation tab removed per admin UI simplification

            var uCtrl = new AdminControls.UsersControl { Dock = DockStyle.Fill };
            tabs.TabPages["tabUsers"].Controls.Add(uCtrl);

            // medecins control uses existing MedecinControl
            try
            {
                var mCtrl = new SEEK_MANAGER.MedecinControl { Dock = DockStyle.Fill };
                tabs.TabPages["tabMedecins"].Controls.Add(mCtrl);
            }
            catch { }

            // Ensure header stays visible above tabs
            try { header.BringToFront(); tabs.SendToBack(); } catch { }
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            // clear session
            UserSession.UserId = null;
            UserSession.Username = null;
            UserSession.FullName = null;
            UserSession.Role = null;

            // hide admin dashboard and show main dashboard (home)
            try
            {
                this.Hide();
                var main = new MainDashboard();
                main.FormClosed += (s, ev) => this.Close();
                main.Show();
            }
            catch
            {
                this.Close();
            }
        }
    }
}
