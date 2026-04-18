using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class MedecinControl : UserControl
    {
        private MEDECINS formWrapped;
        public MedecinControl()
        {
            formWrapped = new MEDECINS();
            formWrapped.TopLevel = false;
            formWrapped.FormBorderStyle = FormBorderStyle.None;
            formWrapped.Dock = DockStyle.Fill;
            Controls.Add(formWrapped);
            formWrapped.Show();
        }
    }
}
