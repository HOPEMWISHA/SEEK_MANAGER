using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class PaiementForm : Form
    {
        private readonly HospitalManager hm;
        private readonly int? editId;

        private ComboBox cbPatient;
        private TextBox txtReference;
        private NumericUpDown nudAmount;
        private TextBox txtCurrency;
        private TextBox txtMethod;
        private DateTimePicker dtPaidAt;
        private TextBox txtNotes;
        private Button btnSave;
        private Button btnCancel;

        public PaiementForm(HospitalManager hm, int? id = null)
        {
            this.hm = hm ?? throw new ArgumentNullException(nameof(hm));
            this.editId = id;

            Text = id.HasValue ? "Modifier Paiement" : "Ajouter Paiement";
            Width = 420;
            Height = 360;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            InitializeComponents();

            Load += PaiementForm_Load;
        }

        private void InitializeComponents()
        {
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lblPatient = new Label { Text = "Patient", Left = 10, Top = 10, Width = 100 };
            cbPatient = new ComboBox { Left = 120, Top = 10, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblRef = new Label { Text = "Référence", Left = 10, Top = 45, Width = 100 };
            txtReference = new TextBox { Left = 120, Top = 45, Width = 260 };

            var lblAmount = new Label { Text = "Montant", Left = 10, Top = 80, Width = 100 };
            nudAmount = new NumericUpDown { Left = 120, Top = 80, Width = 120, DecimalPlaces = 2, Maximum = 1000000, Minimum = 0 };

            var lblCurrency = new Label { Text = "Devise", Left = 10, Top = 115, Width = 100 };
            txtCurrency = new TextBox { Left = 120, Top = 115, Width = 120, Text = "USD" };

            var lblMethod = new Label { Text = "Méthode", Left = 10, Top = 150, Width = 100 };
            txtMethod = new TextBox { Left = 120, Top = 150, Width = 260 };

            var lblPaidAt = new Label { Text = "Date Paiement", Left = 10, Top = 185, Width = 100 };
            dtPaidAt = new DateTimePicker { Left = 120, Top = 185, Width = 200, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm:ss" };

            var lblNotes = new Label { Text = "Notes", Left = 10, Top = 220, Width = 100 };
            txtNotes = new TextBox { Left = 120, Top = 220, Width = 260, Height = 50, Multiline = true };

            btnSave = new Button { Text = "Enregistrer", Left = 200, Top = 285, Width = 100, Height = 30 };
            btnCancel = new Button { Text = "Annuler", Left = 310, Top = 285, Width = 80, Height = 30 };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            main.Controls.AddRange(new Control[] { lblPatient, cbPatient, lblRef, txtReference,
                lblAmount, nudAmount, lblCurrency, txtCurrency, lblMethod, txtMethod,
                lblPaidAt, dtPaidAt, lblNotes, txtNotes, btnSave, btnCancel });

            // show current user on the form (bottom-left)
            var lblUser = new Label { Text = "Utilisateur : " + (UserSession.FullName ?? UserSession.Username ?? ""), Left = 10, Top = 320, Width = 380, ForeColor = Color.FromArgb(80,80,80) };
            main.Controls.Add(lblUser);

            Controls.Add(main);
        }

        private void PaiementForm_Load(object? sender, EventArgs e)
        {
            try
            {
                // load patients list
                var dt = hm.GetPatientsTable();
                cbPatient.Items.Clear();
                cbPatient.Items.Add(new ComboboxItem { Id = null, Text = "-- Aucun --" });
                foreach (DataRow r in dt.Rows)
                {
                    cbPatient.Items.Add(new ComboboxItem { Id = Convert.ToInt32(r["id_patient"]), Text = r["nom"].ToString() + " " + r["prenom"].ToString() });
                }
                if (cbPatient.Items.Count > 0) cbPatient.SelectedIndex = 0;

                if (editId.HasValue)
                {
                    var p = hm.GetPaymentById(editId.Value);
                    if (p.Rows.Count > 0)
                    {
                        var row = p.Rows[0];
                        // try to select patient
                        if (row["patient_id"] != DBNull.Value)
                        {
                            var pid = Convert.ToInt32(row["patient_id"]);
                            for (int i = 0; i < cbPatient.Items.Count; i++)
                            {
                                if (cbPatient.Items[i] is ComboboxItem it && it.Id is int && (int)it.Id == pid)
                                {
                                    cbPatient.SelectedIndex = i; break;
                                }
                            }
                        }
                        txtReference.Text = row["reference"].ToString();
                        nudAmount.Value = Convert.ToDecimal(row["amount"]);
                        txtCurrency.Text = row["currency"].ToString();
                        txtMethod.Text = row["method"].ToString();
                        dtPaidAt.Value = Convert.ToDateTime(row["paid_at"]);
                        txtNotes.Text = row["notes"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement paiement: " + ex.Message);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                int? pid = null;
                if (cbPatient.SelectedItem is ComboboxItem it && it.Id is int) pid = (int)it.Id;
                var reference = txtReference.Text.Trim();
                var amount = nudAmount.Value;
                var currency = txtCurrency.Text.Trim();
                var method = txtMethod.Text.Trim();
                var paidAt = dtPaidAt.Value;
                var notes = txtNotes.Text.Trim();

                if (editId.HasValue)
                {
                    hm.ModifierPAIEMENT(editId.Value, pid, reference, amount, currency, method, paidAt, notes);
                }
                else
                {
                    hm.AjouterPAIEMENT(pid, reference, amount, currency, method, paidAt, notes);
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur enregistrement paiement: " + ex.Message);
            }
        }

        private class ComboboxItem
        {
            public object Id { get; set; }
            public string Text { get; set; }
            public override string ToString() => Text;
        }
    }
}
