using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class ConsultationControl : UserControl
    {
        private CONSULTATION formWrapped;
        public ConsultationControl()
        {
            formWrapped = new CONSULTATION();
            formWrapped.TopLevel = false;
            formWrapped.FormBorderStyle = FormBorderStyle.None;
            formWrapped.Dock = DockStyle.Fill;
            Controls.Add(formWrapped);
            formWrapped.Show();
        }
    }
}
