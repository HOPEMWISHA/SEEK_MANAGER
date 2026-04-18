using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace SEEK_MANAGER
{
    public sealed class MySqlDbManager
    {
        private static MySqlDbManager instance = null;
        private readonly string connectionString;

        private MySqlDbManager()
        {
            // Build the connection string using the provider builder to avoid parsing issues
            var builder = new MySqlConnectionStringBuilder()
            {
                Server = "localhost",
                Port = 3306,
                Database = "HopitalDB",
                UserID = "root",
                Password = "Ndakola12@#123"
            };
            // Do not set SslMode explicitly here to avoid enum parsing issues across provider versions.
            connectionString = builder.ConnectionString;
        }

        public static MySqlDbManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MySqlDbManager();
                return instance;
            }
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
        // Verifies TCP reachability to the configured MySQL server and port.
        private bool IsPortOpen(out string error)
        {
            try
            {
                // parse server and port from connection string
                var cs = new MySqlConnectionStringBuilder(connectionString);
                string server = cs.Server;
                int port = (int)cs.Port;

                using (var tcp = new TcpClient())
                {
                    var ar = tcp.BeginConnect(server, port, null, null);
                    bool success = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                    if (!success)
                    {
                        error = $"Impossible d'atteindre {server}:{port} (timeout).";
                        return false;
                    }
                    tcp.EndConnect(ar);
                }

                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Try opening a connection to verify connectivity. Returns true when successful, false otherwise.
        public bool TestConnection(out string errorMessage)
        {
            // First check TCP reachability to server:port
            if (!IsPortOpen(out var portErr))
            {
                errorMessage = portErr;
                return false;
            }

            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    con.Close();
                }

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
