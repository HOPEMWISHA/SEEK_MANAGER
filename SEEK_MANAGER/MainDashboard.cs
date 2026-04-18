using System;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    // Rebuilt MainDashboard: clean, responsive layout using TableLayoutPanel and proper docking.
    public class MainDashboard : Form
    {
        private Color primaryColor;
        private Panel sidebar;
        private Panel header;
        private Panel mainContainer;
        private TableLayoutPanel cardsGrid;
        private Panel detailsPanel;
        private PictureBox mainImage;

        private Button btnPatient, btnMedecin, btnConsultation, btnHospitalisation, btnService;
        private HospitalManager hm = new HospitalManager();
        private UserControl currentControl;

        public MainDashboard()
        {
            Text = "SEEK_MANAGER";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            InitializeComponents();
            LoadDashboardData();
            // show main image by default until user selects a module
            ShowMainImage();
        }

        private void InitializeComponents()
        {
            // Sidebar
            primaryColor = Color.FromArgb(52, 152, 219);
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = primaryColor,
                Padding = new Padding(0)
            };

            // Header
            header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(12)
            };
            var title = new Label { Name = "lblTitle", Text = "SEEK_MANAGER", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41), Dock = DockStyle.Left, AutoSize = true };
            header.Controls.Add(title);

            // dashboard button (go home) and dark mode toggle
            var btnHome = new Button { Text = "Dashboard", Dock = DockStyle.Right, Width = 120, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnHome.FlatAppearance.BorderSize = 0;
            btnHome.Click += (s, e) => { LoadDashboardData(); ShowMainImage(); SetActiveButton(null); };
            header.Controls.Add(btnHome);

            var btnDark = new Button { Name = "btnDarkMode", Text = "🌙", Dock = DockStyle.Right, Width = 48, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat };
            btnDark.FlatAppearance.BorderSize = 0;
            btnDark.Click += ToggleDarkMode;
            header.Controls.Add(btnDark);

            /* show connected user and logout
            var lblUser = new Label { Name = "lblUser", Text = UserSession.Username ?? string.Empty, Dock = DockStyle.Left, AutoSize = true, ForeColor = Color.FromArgb(33,37,41), Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
            header.Controls.Add(lblUser);

            var btnLogout = new Button { Text = "Déconnexion", Dock = DockStyle.Right, Width = 120, BackColor = Color.FromArgb(231,76,60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => 
            {
                // clear session and show login form
                UserSession.UserId = null;
                UserSession.Username = null;
                UserSession.FullName = null;

                using (var lf = new LoginForm())
                {
                    var dr = lf.ShowDialog(this);
                    if (dr != DialogResult.OK)
                    {
                        // close app if user cancels login
                        Application.Exit();
                        return;
                    }
                }

                // update displayed username
                var lbl = header.Controls["lblUser"] as Label;
                if (lbl != null) lbl.Text = UserSession.Username ?? string.Empty;
            };
            header.Controls.Add(btnLogout);
            */
            // Main container: cards + details
            // remove extra padding so main image can fill full available width
            mainContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };

            // Cards grid (example: 4 columns) - adapts with percent widths
            cardsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 180,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            for (int i = 0; i < 4; i++) cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // details panel fills remaining area and hosts modules
            // remove padding so image/control can occupy full width
            detailsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, Padding = new Padding(0) };

            // main image shown in details area before any control is loaded
            // StretchImage so it fills left and right space; image may be scaled to fit
            mainImage = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.StretchImage };
            try
            {
                var imgFileName = "medical.png.png";
                var imgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", imgFileName);
                if (!System.IO.File.Exists(imgPath))
                {
                    string dir = AppDomain.CurrentDomain.BaseDirectory;
                    for (int i = 0; i < 6 && !System.IO.File.Exists(imgPath); i++)
                    {
                        dir = System.IO.Path.GetDirectoryName(dir) ?? dir;
                        imgPath = System.IO.Path.Combine(dir, "assets", imgFileName);
                    }
                }
                if (System.IO.File.Exists(imgPath))
                {
                    try { mainImage.Image = Image.FromFile(imgPath); } catch { mainImage.Image = null; }
                }
            }
            catch { }

            mainContainer.Controls.Add(detailsPanel);
            mainContainer.Controls.Add(cardsGrid);

            // add mainImage into details panel as initial content
            detailsPanel.Controls.Add(mainImage);

            // Sidebar buttons (stacked)
            btnPatient = MakeSidebarButton("PATIENT");
            btnMedecin = MakeSidebarButton("MEDECIN");
            btnConsultation = MakeSidebarButton("CONSULTATION");
            btnHospitalisation = MakeSidebarButton("HOSPITALISATION");
            btnService = MakeSidebarButton("SERVICE");

            btnPatient.Click += (s, e) => { SetActiveButton(btnPatient); LoadUserControl(new PatientControl()); };
            btnMedecin.Click += (s, e) => { SetActiveButton(btnMedecin); LoadUserControl(new MedecinControl()); };
            btnConsultation.Click += (s, e) => { SetActiveButton(btnConsultation); LoadUserControl(new ConsultationControl()); };
            btnHospitalisation.Click += (s, e) => { SetActiveButton(btnHospitalisation); LoadUserControl(new HospitalisationControl()); };
            btnService.Click += (s, e) => { SetActiveButton(btnService); LoadUserControl(new ServiceControl()); };

            var sidebarLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
            // increase top area for logo/image
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // logo/title area (expanded)
            for (int i = 0; i < 5; i++) sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var logoPanel = new Panel { Dock = DockStyle.Fill, BackColor = primaryColor };
            // add picture box centered and fill horizontally
            var pb = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, Dock = DockStyle.Fill, Padding = new Padding(0) };
            try
            {
                // try to load an embedded or file image if present
                // prefer actual filename present in your output folder
                var imgFileName = "medical.png.png"; // use double-extension as requested
                var imgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", imgFileName);
                if (!System.IO.File.Exists(imgPath))
                {
                    // try to locate assets folder by walking up a few parent directories
                    string dir = AppDomain.CurrentDomain.BaseDirectory;
                    for (int i = 0; i < 6 && !System.IO.File.Exists(imgPath); i++)
                    {
                        dir = System.IO.Path.GetDirectoryName(dir) ?? dir;
                        imgPath = System.IO.Path.Combine(dir, "assets", imgFileName);
                    }
                }
                if (System.IO.File.Exists(imgPath))
                {
                    try { pb.Image = Image.FromFile(imgPath); } catch { pb.Image = null; }
                }
                else
                {
                    // fallback: draw a simple cross bitmap so UI is not empty
                    var bmp = new Bitmap(120, 120);
                    using (var g = Graphics.FromImage(bmp))
                    {
                    g.Clear(primaryColor);
                        using var pen = new Pen(Color.White, 12);
                        g.DrawLine(pen, 60, 20, 60, 100);
                        g.DrawLine(pen, 20, 60, 100, 60);
                    }
                    pb.Image = bmp;
                }
                // debug message removed
            }
            catch { }
            logoPanel.Controls.Add(pb);
            sidebarLayout.Controls.Add(logoPanel, 0, 0);
            sidebarLayout.Controls.Add(btnPatient, 0, 1);
            sidebarLayout.Controls.Add(btnMedecin, 0, 2);
            sidebarLayout.Controls.Add(btnConsultation, 0, 3);
            sidebarLayout.Controls.Add(btnHospitalisation, 0, 4);
            sidebarLayout.Controls.Add(btnService, 0, 5);
            sidebar.Controls.Add(sidebarLayout);

            // Add to form
            Controls.Add(mainContainer);
            Controls.Add(header);
            Controls.Add(sidebar);

            // Basic form styling
            BackColor = Color.White;
        }

        public void RefreshDashboardStats()
        {
            LoadDashboardData();
        }

        private void ToggleDarkMode(object? s, EventArgs e)
        {
            bool isDark = BackColor == Color.White ? false : true;
            if (!isDark)
            {
                // switch to dark -> use light (white) backgrounds per user request (no pure black)
                BackColor = Color.FromArgb(245, 245, 245);
                header.BackColor = Color.FromArgb(250, 250, 250);
                sidebar.BackColor = Color.FromArgb(52, 152, 219);
                // title dark
                var lbl = header.Controls["lblTitle"] as Label;
                if (lbl != null) lbl.ForeColor = Color.FromArgb(33, 37, 41);
            }
            else
            {
                // switch to light - explicitly set white/light backgrounds
                BackColor = Color.White;
                header.BackColor = Color.FromArgb(250, 250, 250);
                sidebar.BackColor = Color.FromArgb(52, 152, 219);
                var lbl = header.Controls["lblTitle"] as Label;
                if (lbl != null) lbl.ForeColor = Color.FromArgb(33, 37, 41);
                // ensure dashboard button blue-green in light mode
                foreach (Control c in header.Controls)
                {
                    if (c is Button b && b.Text == "Dashboard") b.BackColor = Color.FromArgb(52, 152, 219);
                }
            }
        }

        private Button MakeSidebarButton(string text)
        {
            var b = new Button
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = primaryColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Margin = new Padding(0);
            // ensure no dark mouse-down/hover effect uses black - use primary/accent only
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 130, 180);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 152, 219);
            b.MouseEnter += (s, e) => b.BackColor = Color.FromArgb(70, 130, 180);
            b.MouseLeave += (s, e) => { if (b != null) b.BackColor = primaryColor; };
            return b;
        }

        private void SetActiveButton(Button active)
        {
            // reset -> use primary color instead of dark/black
            foreach (Control c in sidebar.Controls)
            {
                if (c is TableLayoutPanel tl)
                {
                    foreach (Control child in tl.Controls)
                        if (child is Button bb) bb.BackColor = primaryColor;
                }
            }
            if (active != null) active.BackColor = Color.FromArgb(70, 130, 180);
        }

        private void LoadUserControl(UserControl control)
        {
            if (currentControl != null)
            {
                detailsPanel.Controls.Remove(currentControl);
                currentControl.Dispose();
                currentControl = null;
            }
            // hide main image when loading a control
            try { if (mainImage != null) mainImage.Visible = false; } catch { }
            currentControl = control;
            control.Dock = DockStyle.Fill;
            detailsPanel.Controls.Add(control);
            // normalize Guna controls inside the loaded module to have consistent sizes/styles
            try { NormalizeGunaControls(control); } catch { }
        }

        // Ensure all Guna comboboxes/textboxes/buttons inside a control follow the app style and sizes
        private void NormalizeGunaControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                // recursively normalize
                try { NormalizeGunaControls(c); } catch { }

                // Guna2ComboBox -> ensure height matches textboxes and remove radii
                if (c.GetType().FullName == "Guna.UI2.WinForms.Guna2ComboBox")
                {
                    dynamic cb = c;
                    try { cb.BorderRadius = 0; } catch { }
                    try { cb.ItemHeight = 30; } catch { }
                    try { cb.Size = new System.Drawing.Size(cb.Width, 53); } catch { }
                    try { cb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown; } catch { }
                    try { cb.FillColor = Color.White; } catch { }
                    try { cb.ForeColor = Color.FromArgb(33, 37, 41); } catch { }
                }

                // Guna2TextBox -> remove radius
                if (c.GetType().FullName == "Guna.UI2.WinForms.Guna2TextBox")
                {
                    dynamic tb = c;
                    try { tb.BorderRadius = 0; } catch { }
                }

                // Guna2Button -> remove radius and unify colors
                if (c.GetType().FullName == "Guna.UI2.WinForms.Guna2Button")
                {
                    dynamic btn = c;
                    try { btn.BorderRadius = 0; } catch { }
                    try { btn.FillColor = primaryColor; } catch { }
                }
            }
        }

        private void ShowMainImage()
        {
            try
            {
                // remove current control if any
                if (currentControl != null)
                {
                    detailsPanel.Controls.Remove(currentControl);
                    currentControl.Dispose();
                    currentControl = null;
                }
                if (mainImage != null)
                {
                    mainImage.Visible = true;
                    // bring to front
                    mainImage.BringToFront();
                }
            }
            catch { }
        }

        private void LoadDashboardData()
        {
            cardsGrid.Controls.Clear();

            // palette
            var palette = new[] { Color.FromArgb(52, 152, 219), Color.FromArgb(46, 204, 113), Color.FromArgb(155, 89, 182), Color.FromArgb(241, 196, 15) };

            int totalPatients = 0, totalMedecins = 0, consultToday = 0, activeHosp = 0;
            try
            {
                totalPatients = hm.GetTotalPatients();
                totalMedecins = hm.GetTotalMedecins();
                consultToday = hm.GetConsultationsTodayCount();
                activeHosp = hm.GetActiveHospitalisationsCount();
            }
            catch { }

            var data = new (string title, string value, Color color)[]
            {
                ("Total Patients", totalPatients.ToString(), palette[0]),
                ("Total Médecins", totalMedecins.ToString(), palette[1]),
                ("Consultations Aujourd'hui", consultToday.ToString(), palette[2]),
                ("Hospitalisations Actives", activeHosp.ToString(), palette[3])
            };

            for (int i = 0; i < data.Length; i++)
            {
                var card = CreateStatCard(data[i].title, data[i].value, data[i].color);
                cardsGrid.Controls.Add(card, i, 0);
            }
        }

        private Control CreateStatCard(string title, string value, Color accent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.White };
            panel.Padding = new Padding(12);

            var leftAccent = new Panel { Width = 6, Dock = DockStyle.Left, BackColor = accent };
            panel.Controls.Add(leftAccent);

            var titleLbl = new Label { Text = title, Font = new Font("Segoe UI", 9F), ForeColor = Color.DimGray, Dock = DockStyle.Top, Height = 20 };
            var valueLbl = new Label { Text = value, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = accent, Dock = DockStyle.Bottom, Height = 48, TextAlign = ContentAlignment.MiddleLeft };

            panel.Controls.Add(valueLbl);
            panel.Controls.Add(titleLbl);

            // subtle border
            panel.BorderStyle = BorderStyle.None;

            return panel;
        }

        // Utility to apply consistent DataGridView styling across modules
        public static class UiHelpers
        {
            public static void ConfigureGrid(DataGridView dgv)
            {
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 50);
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                dgv.RowTemplate.Height = 28;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.BackgroundColor = Color.White;
                dgv.GridColor = Color.FromArgb(230, 230, 230);
                dgv.BorderStyle = BorderStyle.None;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
            }
        }
    }
}
