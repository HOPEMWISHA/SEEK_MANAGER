using System;
using System.Data;
using Guna.UI2.WinForms;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class ServiceDetailControl : UserControl
    {
        private readonly int serviceId;
        private readonly HospitalManager hm = new HospitalManager();

        private Label lblTitle;
        private ComboBox cbPatients;
        private ComboBox cbMedecins;
        private TextBox txtNotes;
        private Guna2TextBox txtSearch;
        private Button btnAssignHospital;
        private Guna2DataGridView _hospitalisationGrid;

        public ServiceDetailControl(int serviceId)
        {
            this.serviceId = serviceId;
            Initialize();
            Load += ServiceDetailControl_Load;
        }

        private void Initialize()
        {
            BackColor = Color.White;
            Padding = new Padding(12);

            lblTitle = new Label { Font = new Font("Segoe UI", 14F, FontStyle.Bold), Dock = DockStyle.Top, Height = 36 };

            var panel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 180, ColumnCount = 2 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            cbPatients = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cbMedecins = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };

            panel.Controls.Add(new Label { Text = "Patient:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            panel.Controls.Add(cbPatients, 1, 0);

            panel.Controls.Add(new Label { Text = "Médecin:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            panel.Controls.Add(cbMedecins, 1, 1);

            panel.Controls.Add(new Label { Text = "Notes / Détails:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            panel.Controls.Add(txtNotes, 1, 2);

            btnAssignHospital = new Button { Text = "Affecter / Hospitaliser", Height = 40, Dock = DockStyle.Fill, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAssignHospital.FlatAppearance.BorderSize = 0;
            btnAssignHospital.Click += BtnAssignHospital_Click;

            // Etat sortie button placed beside the assign button (split space)
            var btnEtatSortie = new Button { Text = "État de sortie", Height = 40, Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.FromArgb(33,37,41), FlatStyle = FlatStyle.Flat };
            btnEtatSortie.FlatAppearance.BorderSize = 0;
            btnEtatSortie.Click += (s, e) =>
            {
                try
                {
                    var table = hm.GetHospitalisationsByService(serviceId);
                    using var f = new EtatSortieForm(hm, table, $"État de sortie - {lblTitle.Text}");
                    f.FallbackLoader = (period, refDate) => hm.GetHospitalisationsByService(serviceId);
                    f.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossible d'ouvrir l'état de sortie: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // container to host both buttons side-by-side
            var btnContainer = new TableLayoutPanel { Dock = DockStyle.Top, Height = 44, ColumnCount = 2 };
            btnContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnContainer.Controls.Add(btnAssignHospital, 0, 0);
            btnContainer.Controls.Add(btnEtatSortie, 1, 0);

            // local search box — publish queries so it behaves like HOSPITALISATION search
            txtSearch = new Guna2TextBox { Dock = DockStyle.Top, Height = 36, PlaceholderText = "Rechercher patient...", Margin = new Padding(0, 8, 0, 8) };
            txtSearch.TextChanged += (s, e) =>
            {
                try { SearchService.Instance.Publish(txtSearch.Text); } catch { }
            };

            // data grid to show hospitalisations for this service (Guna2)
            var dgv = new Guna2DataGridView { Dock = DockStyle.Fill, Height = 240 };
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.FromArgb(230, 230, 230);
            dgv.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(100, 88, 255);
            dgv.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgv.ThemeStyle.RowsStyle.BackColor = Color.White;
            dgv.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(231, 229, 255);

            Controls.Add(dgv);
            Controls.Add(btnContainer);
            Controls.Add(panel);
            Controls.Add(txtSearch);
            Controls.Add(lblTitle);

            // store reference for reload
            _hospitalisationGrid = dgv;
        }

        private void ServiceDetailControl_Load(object? sender, EventArgs e)
        {
            try
            {
                var s = hm.GetServiceById(serviceId);
                lblTitle.Text = s?.Rows.Count > 0 ? s.Rows[0]["nom_service"].ToString() : "Service";

                // populate patients combobox from DB and bind to value
                var pts = hm.GetPatientsTable();
                cbPatients.Items.Clear();
                if (pts != null)
                {
                    var dtPatients = new DataTable();
                    dtPatients.Columns.Add("display", typeof(string));
                    dtPatients.Columns.Add("id", typeof(int));
                    foreach (DataRow r in pts.Rows)
                    {
                        try
                        {
                            var idp = Convert.ToInt32(r["id_patient"]);
                            var nom = r.Table.Columns.Contains("nom") ? (r["nom"]?.ToString() ?? "") : "";
                            var prenom = r.Table.Columns.Contains("prenom") ? (r["prenom"]?.ToString() ?? "") : "";
                            dtPatients.Rows.Add($"{idp} - {nom} {prenom}", idp);
                        }
                        catch { }
                    }
                    cbPatients.DisplayMember = "display";
                    cbPatients.ValueMember = "id";
                    cbPatients.DataSource = dtPatients;
                    if (cbPatients.Items.Count > 0) cbPatients.SelectedIndex = 0;
                }

                // populate medecins combobox for this service (bind to id_medecin)
                var meds = hm.GetMedecinsByService(serviceId);
                if (meds == null || meds.Rows.Count == 0)
                {
                    meds = hm.GetAllMedecins();
                }
                cbMedecins.Items.Clear();
                if (meds != null)
                {
                    var dtM = new DataTable();
                    dtM.Columns.Add("display", typeof(string));
                    dtM.Columns.Add("id", typeof(int));
                    foreach (DataRow r in meds.Rows)
                    {
                        try
                        {
                            int idm = 0;
                            if (r.Table.Columns.Contains("id_medecin") && r["id_medecin"] != DBNull.Value)
                                idm = Convert.ToInt32(r["id_medecin"]);
                            else if (r.Table.Columns.Count > 0 && int.TryParse(r[0]?.ToString(), out var tmp))
                                idm = tmp;
                            var name = r.Table.Columns.Contains("nom") ? (r["nom"]?.ToString() ?? "") : (r.ItemArray.Length > 0 ? r[0]?.ToString() ?? "" : "");
                            dtM.Rows.Add($"{idm} - {name}", idm);
                        }
                        catch { }
                    }
                    cbMedecins.DisplayMember = "display";
                    cbMedecins.ValueMember = "id";
                    cbMedecins.DataSource = dtM;
                    if (cbMedecins.Items.Count > 0) cbMedecins.SelectedIndex = 0;
                }

                // populate hospitalisations grid
                var hosp = hm.GetHospitalisationsByService(serviceId);
                if (_hospitalisationGrid != null)
                {
                    _hospitalisationGrid.DataSource = hosp;
                }

                // subscribe to global search service so filtering behaves like HOSPITALISATION
                try
                {
                    SearchService.Instance.Subscribe(ApplyGlobalSearch);
                }
                catch { }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " - " + ex.InnerException.Message;
                MessageBox.Show($"Erreur chargement détail service: {msg}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyGlobalSearch(string q)
        {
            try
            {
                if (_hospitalisationGrid == null) return;
                if (!(_hospitalisationGrid.DataSource is DataView dv)) return;
                dv.Table.CaseSensitive = false;
                var safe = (q ?? string.Empty).Trim().Replace("'", "''");
                if (string.IsNullOrWhiteSpace(safe))
                {
                    dv.RowFilter = string.Empty;
                    return;
                }
                dv.RowFilter = $"patient_nom LIKE '{safe}%';";
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { SearchService.Instance.Unsubscribe(ApplyGlobalSearch); } catch { }
            }
            base.Dispose(disposing);
        }

        private void BtnAssignHospital_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbPatients.SelectedValue == null || cbMedecins.SelectedValue == null)
                {
                    MessageBox.Show("Sélectionnez un patient et un médecin.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int idPatient = Convert.ToInt32(cbPatients.SelectedValue);
                int idMedecin = Convert.ToInt32(cbMedecins.SelectedValue);

                DateTime now = DateTime.Now;
                // Use nullable sortie (null means not set) and pass medecin id if available
                hm.AjouterHOSPITALISATION("---", idPatient, serviceId, now, (DateTime?)null, idMedecin);

                // refresh grid
                var hosp2 = hm.GetHospitalisationsByService(serviceId);
                if (_hospitalisationGrid != null) _hospitalisationGrid.DataSource = hosp2;

                MessageBox.Show("Patient affecté / hospitalisé au service.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affectation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ComboboxItem
        {
            public string Text { get; }
            public int Value { get; }
            public ComboboxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
