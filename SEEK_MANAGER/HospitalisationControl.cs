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
        }
    }
}
