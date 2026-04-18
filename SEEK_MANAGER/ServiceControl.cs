using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    // Replaces the previous wrapper: shows a left submenu of services (5 entries ensured in DB)
    // and a right panel with a detail UI to assign patients/medecins to the selected service.
    public class ServiceControl : UserControl
    {
        private readonly HospitalManager hm = new HospitalManager();
        private FlowLayoutPanel leftMenu;
        private Panel rightPanel;

        public ServiceControl()
        {
            Dock = DockStyle.Fill;

            // reset services to the 5 default ones chosen earlier
            hm.ResetServicesToDefaults();

            // layout: left menu (200px) + separator + details
            leftMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 220,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            Controls.Add(rightPanel);
            Controls.Add(leftMenu);

            Load += ServiceControl_Load;
        }

        private void ServiceControl_Load(object? sender, EventArgs e)
        {
            PopulateMenu();
            // select first service by default
            if (leftMenu.Controls.Count > 0 && leftMenu.Controls[0] is Button b)
                b.PerformClick();
        }

        private void PopulateMenu()
        {
            leftMenu.Controls.Clear();

            try
            {
                DataTable dt = hm.GetServicesTable();
                foreach (DataRow r in dt.Rows)
                {
                    int id = Convert.ToInt32(r["id_service"]);
                    string name = r["nom_service"].ToString();

                    var btn = new Button
                    {
                        Text = name,
                        Tag = id,
                        Width = leftMenu.ClientSize.Width - 16,
                        Height = 44,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(12, 0, 0, 0),
                        BackColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Margin = new Padding(4),
                        ForeColor = Color.FromArgb(33,37,41),
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245,245,245);
                    btn.Click += ServiceButton_Click;
                    leftMenu.Controls.Add(btn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de charger la liste des services : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ServiceButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                // visual highlight
                foreach (Control c in leftMenu.Controls)
                    if (c is Button bb) bb.BackColor = Color.White;
                b.BackColor = Color.FromArgb(230, 240, 255);

                ShowServiceDetail(id);
            }
        }

        private void ShowServiceDetail(int serviceId)
        {
            rightPanel.Controls.Clear();
            var detail = new ServiceDetailControl(serviceId);
            detail.Dock = DockStyle.Fill;
            rightPanel.Controls.Add(detail);
        }
    }
}
