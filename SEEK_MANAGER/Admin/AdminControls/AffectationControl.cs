using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER.Admin.AdminControls
{
    public class AffectationControl : UserControl
    {
        private Guna.UI2.WinForms.Guna2DataGridView dgvHospitalisations;
        private Guna.UI2.WinForms.Guna2ComboBox cbPatients, cbChambres;
        private Guna.UI2.WinForms.Guna2Button btnRelease, btnRefresh;
        private HospitalManager hm = new HospitalManager();

        public AffectationControl()
        {
            Dock = DockStyle.Fill;
            var top = new Panel { Dock = DockStyle.Top, Height = 96 };
            cbPatients = new Guna.UI2.WinForms.Guna2ComboBox { Left = 12, Top = 12, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cbChambres = new Guna.UI2.WinForms.Guna2ComboBox { Left = 392, Top = 12, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            btnRelease = new Guna.UI2.WinForms.Guna2Button { Left = 664, Top = 12, Text = "Libérer", Width = 120, AutoRoundedCorners = true };
            btnRefresh = new Guna.UI2.WinForms.Guna2Button { Left = 792, Top = 12, Text = "Actualiser", Width = 120, AutoRoundedCorners = true };
            top.Controls.Add(cbPatients); top.Controls.Add(cbChambres); top.Controls.Add(btnRelease); top.Controls.Add(btnRefresh);
            dgvHospitalisations = new Guna.UI2.WinForms.Guna2DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            Controls.Add(dgvHospitalisations); Controls.Add(top);

            btnRefresh.Click += (s, e) => LoadData();
            btnRelease.Click += (s, e) => Release();

            Load += (s, e) => LoadData();
            // subscribe to chambre changes so control can refresh if rooms updated elsewhere
            HospitalManager.ChambresChanged += OnChambresChanged;
        }

        private void OnChambresChanged()
        {
            try { if (IsHandleCreated) Invoke((Action)LoadData); else LoadData(); } catch { }
        }

        private void LoadData()
        {
            try
            {
                // patients
                var pts = hm.GetPatientsTable();
                cbPatients.Items.Clear();
                foreach (DataRow r in pts.Rows)
                {
                    cbPatients.Items.Add(new { Id = Convert.ToInt32(r["id_patient"]), Text = r["nom"].ToString() + " " + r["prenom"].ToString() });
                }
                if (cbPatients.Items.Count > 0) cbPatients.SelectedIndex = 0;

                // chambres (load only admin-created rooms)
                var chs = hm.GetChambresTable();
                cbChambres.DataSource = null;
                if (chs != null)
                {
                    cbChambres.DisplayMember = "numero";
                    cbChambres.ValueMember = "id_chambre";
                    cbChambres.DataSource = chs;
                }

                dgvHospitalisations.DataSource = hm.GetHospitalisationsByService(0).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement affectation: " + ex.Message);
            }
        }

        // Note: assign button removed per request. Use Release and admin flows.

        private void Release()
        {
            if (cbChambres.SelectedItem == null) { MessageBox.Show("Sélectionnez une chambre."); return; }
            var txtProp = cbChambres.SelectedItem.GetType().GetProperty("Numero");
            if (txtProp == null) return;
            var numero = txtProp.GetValue(cbChambres.SelectedItem).ToString();
            try
            {
                hm.ReleaseChambre(numero);
                MessageBox.Show("Chambre libérée.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur libération: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { HospitalManager.ChambresChanged -= OnChambresChanged; } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
