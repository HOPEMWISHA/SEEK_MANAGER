using System.Windows.Forms;

namespace SEEK_MANAGER
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            // Verify database connectivity before starting the app
            if (!MySqlDbManager.Instance.TestConnection(out var err))
            {
                MessageBox.Show($"Impossible de se connecter à la base de données:\n{err}", "Erreur de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // show login before main dashboard
            using (var lf = new LoginForm())
            {
                var dr = lf.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return; // user cancelled or failed to login
                }
            }
            Application.Run(new MainDashboard());
        }
    }
}