using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class PatientControl : UserControl
    {
        private PATIENT formWrapped;
        public PatientControl()
        {
            formWrapped = new PATIENT();
            formWrapped.TopLevel = false;
            formWrapped.FormBorderStyle = FormBorderStyle.None;
            formWrapped.Dock = DockStyle.Fill;
            Controls.Add(formWrapped);
            formWrapped.Show();
        }
    }
}
