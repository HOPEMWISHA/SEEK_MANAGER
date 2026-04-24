using System;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class HospitalisationControl : UserControl
    {
        private HOSPITALISATION formWrapped;

        public HospitalisationControl()
        {
            formWrapped = new HOSPITALISATION();
            formWrapped.TopLevel = false;
            formWrapped.FormBorderStyle = FormBorderStyle.None;
            formWrapped.Dock = DockStyle.Fill;
            Controls.Add(formWrapped);
            formWrapped.Show();

            // load chambres into the chambre combobox when the form is shown
            formWrapped.Load += (s, e) => TryLoadChambres();
            formWrapped.VisibleChanged += (s, e) => { if (formWrapped.Visible) TryLoadChambres(); };
            formWrapped.GotFocus += (s, e) => TryLoadChambres();

            // subscribe to global chambres changed event to refresh combobox immediately
            HospitalManager.ChambresChanged += TryLoadChambres;
        }

        private void TryLoadChambres()
        {
            try
            {
                var hm = new HospitalManager();
                var dt = hm.GetChambresTable();
                var cb = formWrapped.Controls["guna2ComboBoxChambre"] as Guna.UI2.WinForms.Guna2ComboBox;
                if (cb == null) return;

                cb.DataSource = null;
                if (dt == null) return;

                if (dt.Columns.Contains("id_chambre") && dt.Columns.Contains("numero"))
                {
                    cb.DisplayMember = "numero";
                    cb.ValueMember = "id_chambre";
                    cb.DataSource = dt;
                    cb.SelectedIndex = cb.Items.Count > 0 ? 0 : -1;
                }
                else
                {
                    cb.Items.Clear();
                    foreach (System.Data.DataRow r in dt.Rows)
                        cb.Items.Add(r["numero"].ToString());
                    cb.SelectedIndex = cb.Items.Count > 0 ? 0 : -1;
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { HospitalManager.ChambresChanged -= TryLoadChambres; } catch { }
                try { formWrapped?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
