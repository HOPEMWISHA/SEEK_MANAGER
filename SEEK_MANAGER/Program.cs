using System.Windows.Forms;

namespace SEEK_MANAGER
{
    internal static class Program
    {
        
        [STAThread]
        static void Main()
        {
            
            ApplicationConfiguration.Initialize();
           
            if (!MySqlDbManager.Instance.TestConnection(out var err))
            {
                MessageBox.Show($"Impossible de se connecter à la base de données:\n{err}", "Erreur de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            
            try
            {
                var hm = new HospitalManager();
                hm.EnsureAdminSchema();
            }
            catch { }

            
            using (var lf = new LoginForm())
            {
                var dr = lf.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return; 
                }
            }


            
            try
            {
                if (UserSession.IsAdmin)
                {
                    Application.Run(new Admin.AdminDashboard());
                }
                else
                {
                    Application.Run(new MainDashboard());
                }
            }
            catch
            {
                Application.Run(new MainDashboard());
            }
        }
    }
}