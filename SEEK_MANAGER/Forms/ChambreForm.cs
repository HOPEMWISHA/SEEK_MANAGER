using System;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER.Forms
{
    public class ChambreForm : Form
    {
        private Guna.UI2.WinForms.Guna2TextBox txtNumero, txtType;
        private Guna.UI2.WinForms.Guna2ComboBox cbStatut, cbRole;
        private Guna.UI2.WinForms.Guna2Button btnSave, btnCancel;
        public bool Saved { get; private set; }
        public string ResultNumero => txtNumero.Text.Trim();
        public string ResultType => txtType.Text.Trim();
        public string ResultStatut => cbStatut.SelectedItem?.ToString() ?? "Libre";
        public string ResultRole => cbRole.SelectedItem?.ToString() ?? "Général";
        private int? id;
        private HospitalManager hm = new HospitalManager();

        public ChambreForm(int? id = null)
        {
            this.id = id;
            Initialize();
            if (id.HasValue) LoadChambre(id.Value);
        }

        private void Initialize()
        {
            Text = id.HasValue ? "Modifier chambre" : "Ajouter chambre";
            Width = 420; Height = 260; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            txtNumero = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 12, Width = 380, Height = 30 };
            txtType = new Guna.UI2.WinForms.Guna2TextBox { Left = 12, Top = 52, Width = 380, Height = 30 };
            cbStatut = new Guna.UI2.WinForms.Guna2ComboBox { Left = 12, Top = 92, Width = 380, Height = 36, DropDownStyle = ComboBoxStyle.DropDownList };
            cbStatut.Items.AddRange(new[] { "Libre", "Occupée" });
            cbStatut.SelectedIndex = 0;
            // make role combo box a bit larger as requested
            cbRole = new Guna.UI2.WinForms.Guna2ComboBox { Left = 12, Top = 136, Width = 380, Height = 40, DropDownStyle = ComboBoxStyle.DropDownList };
            cbRole.Items.AddRange(new[] { "Général", "VIP", "Soins Intensifs", "Autre" });
            cbRole.SelectedIndex = 0;
            btnSave = new Guna.UI2.WinForms.Guna2Button { Text = "Enregistrer", Left = 196, Top = 184, Width = 100, Height = 36 };
            btnCancel = new Guna.UI2.WinForms.Guna2Button { Text = "Annuler", Left = 302, Top = 184, Width = 90, Height = 36 };
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { Saved = false; Close(); };
            Controls.Add(txtNumero); Controls.Add(txtType); Controls.Add(cbStatut); Controls.Add(cbRole); Controls.Add(btnSave); Controls.Add(btnCancel);
        }

        private void LoadChambre(int id)
        {
            try
            {
                var dt = hm.GetChambresTable();
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["id_chambre"]) == id)
                    {
                        txtNumero.Text = r["numero"].ToString();
                        txtType.Text = r["type"].ToString();
                        // set statut if present, default to Libre
                        if (r.Table.Columns.Contains("statut"))
                        {
                            var st = r["statut"].ToString();
                            try { cbStatut.SelectedItem = st; } catch { }
                        }
                        // try to populate role if present
                        if (r.Table.Columns.Contains("role"))
                        {
                            var role = r["role"].ToString();
                            try { cbRole.SelectedItem = role; } catch { }
                        }
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
                var numero = txtNumero.Text.Trim();
                var type = txtType.Text.Trim();
                var statut = cbStatut.SelectedItem?.ToString() ?? "Libre";
                var role = cbRole.SelectedItem?.ToString() ?? "Général";
                if (string.IsNullOrWhiteSpace(numero)) { MessageBox.Show("Le numéro est requis."); return; }
                if (id.HasValue)
                    hm.UpdateChambre(id.Value, numero, type, statut, role);
                else
                    hm.AddChambre(numero, type, statut, role);
                // notify others
                HospitalManager.NotifyChambresChanged();
                Saved = true; Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur sauvegarde chambre: " + ex.Message);
            }
        }
    }
}
