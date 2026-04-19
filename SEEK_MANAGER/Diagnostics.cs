using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SEEK_MANAGER
{
    public static class Diagnostics
    {
        public static List<string> RunChecks()
        {
            var results = new List<string>();

            // 1) DB connectivity
            try
            {
                if (MySqlDbManager.Instance.TestConnection(out var err))
                {
                    results.Add("DB: OK (connection successful)");
                }
                else
                {
                    results.Add($"DB: FAILED - {err}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"DB: EXCEPTION - {ex.Message}");
            }

            // 2) users table existence
            try
            {
                using var con = MySqlDbManager.Instance.GetConnection();
                con.Open();
                try
                {
                    using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users", con);
                    var cnt = cmd.ExecuteScalar();
                    results.Add($"Users table: OK (rows={cnt})");
                }
                catch (MySqlException mex)
                {
                    results.Add($"Users table: MISSING or unreadable ({mex.Message})");
                }
            }
            catch (Exception ex)
            {
                results.Add($"Users table: EXCEPTION - {ex.Message}");
            }

            // 3) patient.age column presence
            try
            {
                using var con2 = MySqlDbManager.Instance.GetConnection();
                con2.Open();
                var q = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'patient' AND COLUMN_NAME = 'age'";
                using var cmd2 = new MySqlCommand(q, con2);
                var found = cmd2.ExecuteScalar();
                if (found != null)
                    results.Add("Patient.age: PRESENT");
                else
                    results.Add("Patient.age: MISSING");
            }
            catch (Exception ex)
            {
                results.Add($"Patient.age: EXCEPTION - {ex.Message}");
            }

            // 4) logo file presence
            try
            {
                var found = false;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8 && !found; i++)
                {
                    var p = Path.Combine(baseDir, "assets", "medical.png.png");
                    if (File.Exists(p)) { found = true; break; }
                    baseDir = Path.GetDirectoryName(baseDir) ?? baseDir;
                }
                results.Add(found ? "Logo: FOUND (assets/medical.png.png)" : "Logo: NOT FOUND (assets/medical.png.png)");
            }
            catch (Exception ex)
            {
                results.Add($"Logo: EXCEPTION - {ex.Message}");
            }

            // 5) HOSPITALISATION guna2EtatSortie click handler check
            try
            {
                bool hasHandler = false;
                try
                {
                    var hospType = typeof(HOSPITALISATION);
                    using var hosp = Activator.CreateInstance(hospType) as Form;
                    if (hosp != null)
                    {
                        var f = hospType.GetField("guna2EtatSortie", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (f != null)
                        {
                            var ctrl = f.GetValue(hosp) as Control;
                            if (ctrl != null)
                            {
                                var eventsProp = typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
                                var list = eventsProp?.GetValue(ctrl) as EventHandlerList;
                                if (list != null)
                                {
                                    var clickEventKey = typeof(Control).GetField("EventClick", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                                    if (clickEventKey != null)
                                    {
                                        var del = list[clickEventKey] as Delegate;
                                        if (del != null)
                                            hasHandler = del.GetInvocationList().Length > 0;
                                    }
                                }
                            }
                        }
                        hosp.Dispose();
                    }
                }
                catch { }
                results.Add(hasHandler ? "EtatSortie Click handler: ATTACHED" : "EtatSortie Click handler: NOT ATTACHED or could not verify");
            }
            catch (Exception ex)
            {
                results.Add($"EtatSortie handler: EXCEPTION - {ex.Message}");
            }

            try { foreach (var r in results) Debug.WriteLine("DIAG: " + r); } catch { }
            return results;
        }
    }
}
