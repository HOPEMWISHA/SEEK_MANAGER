
using MySql.Data.MySqlClient;
using SEEK_MANAGER;
using System;
using System.Data;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class HospitalManager
    {
        private MySqlConnection GetConnection()
        {
            return MySqlDbManager.Instance.GetConnection();
        }

        // Lightweight data access methods for dashboard statistics
        public int GetTotalPatients()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT COUNT(*) FROM patient";
                var cmd = new MySqlCommand(sql, con);
                var res = cmd.ExecuteScalar();
                return Convert.ToInt32(res);
            }
        }

        public int GetTotalMedecins()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT COUNT(*) FROM medecin";
                var cmd = new MySqlCommand(sql, con);
                var res = cmd.ExecuteScalar();
                return Convert.ToInt32(res);
            }
        }

        public int GetTotalServices()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT COUNT(*) FROM service";
                var cmd = new MySqlCommand(sql, con);
                var res = cmd.ExecuteScalar();
                return Convert.ToInt32(res);
            }
        }

        public int GetConsultationsTodayCount()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT COUNT(*) FROM consultation WHERE DATE(date_consultation) = CURDATE()";
                var cmd = new MySqlCommand(sql, con);
                var res = cmd.ExecuteScalar();
                return Convert.ToInt32(res);
            }
        }

        public int GetActiveHospitalisationsCount()
        {
            using (var con = GetConnection())
            {
                con.Open();
                // active = date_sortie IS NULL OR date_sortie >= today
                string sql = "SELECT COUNT(*) FROM hospitalisation WHERE date_sortie IS NULL OR DATE(date_sortie) >= CURDATE()";
                var cmd = new MySqlCommand(sql, con);
                var res = cmd.ExecuteScalar();
                return Convert.ToInt32(res);
            }
        }

        // =====================================================
        // PATIENT  → Form : PATIENT / Class : PATIENT-CS
        // =====================================================

        public void AjouterPATIENT(string nom, string prenom, string sexe,
                                   DateTime dateNaissance, string tel, string adresse)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"INSERT INTO patient
                               (nom, prenom, sexe, date_naissance, telephone, adresse)
                               VALUES (@nom,@prenom,@sexe,@date,@tel,@adr)";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@prenom", prenom);
                cmd.Parameters.AddWithValue("@sexe", sexe);
                cmd.Parameters.AddWithValue("@date", dateNaissance);
                cmd.Parameters.AddWithValue("@tel", tel);
                cmd.Parameters.AddWithValue("@adr", adresse);

                cmd.ExecuteNonQuery();
            }
        }

        public void ModifierPATIENT(int id, string nom, string prenom, string sexe,
                                    DateTime dateNaissance, string tel, string adresse)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"UPDATE patient SET
                               nom=@nom, prenom=@prenom, sexe=@sexe,
                               date_naissance=@date, telephone=@tel, adresse=@adr
                               WHERE id_patient=@id";

                var cmd = new MySqlCommand(sql, con);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@prenom", prenom);
                cmd.Parameters.AddWithValue("@sexe", sexe);
                cmd.Parameters.AddWithValue("@date", dateNaissance);
                cmd.Parameters.AddWithValue("@tel", tel);
                cmd.Parameters.AddWithValue("@adr", adresse);

                cmd.ExecuteNonQuery();
            }
        }

        public void SupprimerPATIENT(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = "DELETE FROM patient WHERE id_patient=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
        }

        public void ChargerPATIENT(DataGridView dgv)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = "SELECT * FROM patient";

                var da = new MySqlDataAdapter(sql, con);
                DataTable dt = new DataTable();

                da.Fill(dt);

                dgv.DataSource = dt.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }

        // =====================================================
        // SERVICE  → Form : SERVICES / Class : SERVICE-CS
        // =====================================================

        public void AjouterSERVICE(string nomService, string description)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"INSERT INTO service (nom_service, description)
                               VALUES (@nom, @desc)";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@nom", nomService);
                cmd.Parameters.AddWithValue("@desc", description);

                cmd.ExecuteNonQuery();
            }
        }

        public void ModifierSERVICE(int id, string nomService, string description)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"UPDATE service
                               SET nom_service=@nom, description=@desc
                               WHERE id_service=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@nom", nomService);
                cmd.Parameters.AddWithValue("@desc", description);

                cmd.ExecuteNonQuery();
            }
        }

        public void SupprimerSERVICE(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = "DELETE FROM service WHERE id_service=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
        }

        public void ChargerSERVICE(DataGridView dgv)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = "SELECT * FROM service";

                var da = new MySqlDataAdapter(sql, con);
                DataTable dt = new DataTable();

                da.Fill(dt);
                dgv.DataSource = dt.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }

        // Helper: return services table
        public DataTable GetServicesTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT * FROM service";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetServiceById(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT * FROM service WHERE id_service=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Summary: count of sorties grouped by service for a period
        public DataTable GetEtatSortieSummary(string period, DateTime referenceDate)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string where = "";
                switch ((period ?? "JOUR").ToUpperInvariant())
                {
                    case "JOUR":
                        where = "DATE(date_sortie) = @refdate";
                        break;
                    case "SEMAINE":
                        where = "YEARWEEK(date_sortie, 1) = YEARWEEK(@refdate, 1)";
                        break;
                    case "MOIS":
                        where = "YEAR(date_sortie) = YEAR(@refdate) AND MONTH(date_sortie) = MONTH(@refdate)";
                        break;
                    case "ANNEE":
                        where = "YEAR(date_sortie) = YEAR(@refdate)";
                        break;
                    default:
                        where = "DATE(date_sortie) = @refdate";
                        break;
                }

                string sql = $@"SELECT COALESCE(s.nom_service, 'Non renseigné') AS service_nom,
                                       COUNT(*) AS sortie_count
                                FROM hospitalisation h
                                LEFT JOIN service s ON h.id_service = s.id_service
                                WHERE {where}
                                GROUP BY s.nom_service
                                ORDER BY sortie_count DESC";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@refdate", referenceDate.Date);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ensure at least 5 default services exist
        public void EnsureDefaultServices()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string countSql = "SELECT COUNT(*) FROM service";
                var cmd = new MySqlCommand(countSql, con);
                var res = Convert.ToInt32(cmd.ExecuteScalar());
                if (res >= 5) return;

                var defaults = new[] { "Urgences", "Cardiologie", "Pédiatrie", "Chirurgie", "Radiologie" };
                foreach (var d in defaults)
                {
                    string ins = "INSERT INTO service (nom_service, description) VALUES (@nom, @desc)";
                    var icmd = new MySqlCommand(ins, con);
                    icmd.Parameters.AddWithValue("@nom", d);
                    icmd.Parameters.AddWithValue("@desc", d + " - service par défaut");
                    try { icmd.ExecuteNonQuery(); } catch { }
                }
            }
        }

        public DataTable GetPatientsTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT id_patient, nom, prenom FROM patient";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetMedecinsByService(int idService)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT * FROM medecin WHERE id_service=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", idService);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetAllMedecins()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT * FROM medecin";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // return hospitalisations for a service (with patient and medecin names)
        public DataTable GetHospitalisationsByService(int idService)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // select hospitalisations with patient names and medecin name when available
                bool hasMedecin = ColumnExists(con, "hospitalisation", "id_medecin");
                string sql;
                if (hasMedecin)
                {
                    sql = @"SELECT h.id_hospitalisation, h.chambre, p.nom AS patient_nom, p.prenom AS patient_prenom,
                                      m.nom AS medecin_nom, h.date_entree, h.date_sortie
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               LEFT JOIN medecin m ON h.id_medecin = m.id_medecin
                               WHERE h.id_service = @id";
                }
                else
                {
                    sql = @"SELECT h.id_hospitalisation, h.chambre, p.nom AS patient_nom, p.prenom AS patient_prenom,
                                      h.date_entree, h.date_sortie
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               WHERE h.id_service = @id";
                }
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", idService);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Reset services table: delete first three existing services and keep only the 5 defaults specified
        public void ResetServicesToDefaults()
        {
            using (var con = GetConnection())
            {
                con.Open();

                var defaults = new[] { "Urgences", "Cardiologie", "Pédiatrie", "Chirurgie", "Radiologie" };
                foreach (var d in defaults)
                {
                    // insert only if not exists (match by name)
                    string existsSql = "SELECT COUNT(*) FROM service WHERE nom_service = @nom";
                    var existsCmd = new MySqlCommand(existsSql, con);
                    existsCmd.Parameters.AddWithValue("@nom", d);
                    var count = Convert.ToInt32(existsCmd.ExecuteScalar());
                    if (count == 0)
                    {
                        string ins = "INSERT INTO service (nom_service, description) VALUES (@nom, @desc)";
                        var icmd = new MySqlCommand(ins, con);
                        icmd.Parameters.AddWithValue("@nom", d);
                        icmd.Parameters.AddWithValue("@desc", d + " - service par défaut");
                        try { icmd.ExecuteNonQuery(); } catch { }
                    }
                }
            }
        }

        // =====================================================
        // MEDECINS → Form : MEDECINS / Class : MEDECINS-CS
        // =====================================================

        public void AjouterMEDECINS(string nom, string specialite, int idService)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"INSERT INTO medecin (nom, specialite, id_service)
                               VALUES (@nom,@spec,@idS)";

                var cmd = new MySqlCommand(sql, con);

                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@spec", specialite);
                cmd.Parameters.AddWithValue("@idS", idService);

                cmd.ExecuteNonQuery();
            }
        }

        public void ModifierMEDECINS(int id, string nom, string specialite, int idService)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = @"UPDATE medecin
                               SET nom=@nom, specialite=@spec,
                               id_service=@idS
                               WHERE id_medecin=@id";

                var cmd = new MySqlCommand(sql, con);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@spec", specialite);
                cmd.Parameters.AddWithValue("@idS", idService);

                cmd.ExecuteNonQuery();
            }
        }

        public void SupprimerMEDECINS(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();

                string sql = "DELETE FROM medecin WHERE id_medecin=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
        }

        public void ChargerMEDECINS(DataGridView dgv)
        {
            using (var con = GetConnection())
            {
                con.Open();

                // include service name for display
                string sql = @"SELECT m.id_medecin, m.nom, m.specialite, m.id_service,
                                      s.nom_service AS service_nom
                               FROM medecin m
                               LEFT JOIN service s ON m.id_service = s.id_service";

                var da = new MySqlDataAdapter(sql, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }

        // =====================================================
        // HOSPITALISATION → Form : HOSPITALISATION
        // =====================================================

        // original signature kept for compatibility; delegates to nullable overload with optional medecin
        public void AjouterHOSPITALISATION(string chambre, int idPatient, int idService,
                                            DateTime dateEntree, DateTime dateSortie)
        {
            AjouterHOSPITALISATION(chambre, idPatient, idService, dateEntree, (DateTime?)dateSortie, null);
        }

        // Nullable overload: allows passing null for dateSortie and optional medecin id
        public void AjouterHOSPITALISATION(string chambre, int idPatient, int idService,
                                            DateTime dateEntree, DateTime? dateSortie, int? idMedecin)
        {
            using (var con = GetConnection())
            {
                con.Open();

                bool hasMedecinColumn = ColumnExists(con, "hospitalisation", "id_medecin");

                string sql;
                if (hasMedecinColumn && idMedecin.HasValue)
                {
                    sql = @"INSERT INTO hospitalisation (chambre, id_patient, id_service, id_medecin, date_entree, date_sortie)
                               VALUES (@chambre, @idp, @ids, @idm, @entree, @sortie)";
                }
                else
                {
                    sql = @"INSERT INTO hospitalisation (chambre, id_patient, id_service, date_entree, date_sortie)
                               VALUES (@chambre, @idp, @ids, @entree, @sortie)";
                }

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@chambre", chambre);
                cmd.Parameters.AddWithValue("@idp", idPatient);
                cmd.Parameters.AddWithValue("@ids", idService);
                if (hasMedecinColumn && idMedecin.HasValue)
                    cmd.Parameters.AddWithValue("@idm", idMedecin.Value);
                cmd.Parameters.AddWithValue("@entree", dateEntree);
                if (dateSortie.HasValue)
                    cmd.Parameters.AddWithValue("@sortie", dateSortie.Value);
                else
                    cmd.Parameters.AddWithValue("@sortie", DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        // helper to test if a column exists for a given open connection
        private bool ColumnExists(MySqlConnection con, string tableName, string columnName)
        {
            try
            {
                string sql = "SHOW COLUMNS FROM `" + tableName + "` LIKE @col";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@col", columnName);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public void ModifierHOSPITALISATION(int id, string chambre, int idPatient, int idService,
                                            DateTime dateEntree, DateTime dateSortie)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"UPDATE hospitalisation SET chambre=@chambre, id_patient=@idp, id_service=@ids,
                               date_entree=@entree, date_sortie=@sortie WHERE id_hospitalisation=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@chambre", chambre);
                cmd.Parameters.AddWithValue("@idp", idPatient);
                cmd.Parameters.AddWithValue("@ids", idService);
                cmd.Parameters.AddWithValue("@entree", dateEntree);
                cmd.Parameters.AddWithValue("@sortie", dateSortie);

                cmd.ExecuteNonQuery();
            }
        }

        public void SupprimerHOSPITALISATION(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "DELETE FROM hospitalisation WHERE id_hospitalisation=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void ChargerHOSPITALISATION(DataGridView dgv)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // join patient and service to display names instead of ids
                string sql = @"SELECT h.id_hospitalisation, h.chambre,
                                      p.nom AS patient_nom,
                                      s.nom_service AS service_nom,
                                      h.date_entree, h.date_sortie
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               LEFT JOIN service s ON h.id_service = s.id_service";

                var da = new MySqlDataAdapter(sql, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }

        // =====================================================
        // CONSULTATION → Form : CONSULTATION
        // =====================================================

        public void AjouterCONSULTATION(DateTime dateConsultation, string diagnostic, string traitement,
                                         int idPatient, int idMedecin)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"INSERT INTO consultation (date_consultation, diagnostic, traitement, id_patient, id_medecin)
                               VALUES (@datec, @diag, @trait, @idp, @idm)";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@datec", dateConsultation);
                cmd.Parameters.AddWithValue("@diag", diagnostic);
                cmd.Parameters.AddWithValue("@trait", traitement);
                cmd.Parameters.AddWithValue("@idp", idPatient);
                cmd.Parameters.AddWithValue("@idm", idMedecin);

                cmd.ExecuteNonQuery();
            }
        }

        public void ModifierCONSULTATION(int id, DateTime dateConsultation, string diagnostic, string traitement,
                                         int idPatient, int idMedecin)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"UPDATE consultation SET date_consultation=@datec, diagnostic=@diag, traitement=@trait,
                               id_patient=@idp, id_medecin=@idm WHERE id_consultation=@id";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@datec", dateConsultation);
                cmd.Parameters.AddWithValue("@diag", diagnostic);
                cmd.Parameters.AddWithValue("@trait", traitement);
                cmd.Parameters.AddWithValue("@idp", idPatient);
                cmd.Parameters.AddWithValue("@idm", idMedecin);

                cmd.ExecuteNonQuery();
            }
        }

        public void SupprimerCONSULTATION(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "DELETE FROM consultation WHERE id_consultation=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void ChargerCONSULTATION(DataGridView dgv)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // include patient and medecin names for display
                string sql = @"SELECT c.id_consultation, c.date_consultation, c.diagnostic, c.traitement,
                                      p.nom AS patient_nom,
                                      m.nom AS medecin_nom
                               FROM consultation c
                               LEFT JOIN patient p ON c.id_patient = p.id_patient
                               LEFT JOIN medecin m ON c.id_medecin = m.id_medecin";

                var da = new MySqlDataAdapter(sql, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt.DefaultView;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }

        // Set only the date_sortie for a hospitalisation
        public void SetDateSortie(int idHospitalisation, DateTime dateSortie)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "UPDATE hospitalisation SET date_sortie=@sortie WHERE id_hospitalisation=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@sortie", dateSortie);
                cmd.Parameters.AddWithValue("@id", idHospitalisation);
                cmd.ExecuteNonQuery();
            }
        }

        // Return hospitalisation rows filtered by period relative to a reference date
        // period: "JOUR", "SEMAINE", "MOIS", "ANNEE"
        public DataTable GetEtatSortie(string period, DateTime referenceDate)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string where = "";
                switch ((period ?? "JOUR").ToUpperInvariant())
                {
                    case "JOUR":
                        where = "DATE(date_sortie) = @refdate";
                        break;
                    case "SEMAINE":
                        // week starting Monday
                        where = "YEARWEEK(date_sortie, 1) = YEARWEEK(@refdate, 1)";
                        break;
                    case "MOIS":
                        where = "YEAR(date_sortie) = YEAR(@refdate) AND MONTH(date_sortie) = MONTH(@refdate)";
                        break;
                    case "ANNEE":
                        where = "YEAR(date_sortie) = YEAR(@refdate)";
                        break;
                    default:
                        where = "DATE(date_sortie) = @refdate";
                        break;
                }
                // If the hospitalisation table does not have id_medecin column, don't join medecin table
                bool hasMedecin = ColumnExists(con, "hospitalisation", "id_medecin");

                string sql;
                if (hasMedecin)
                {
                    sql = $@"SELECT h.id_hospitalisation, h.chambre, p.nom AS patient_nom, p.prenom AS patient_prenom,
                                      s.nom_service AS service_nom, m.nom AS medecin_nom, h.date_entree, h.date_sortie
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               LEFT JOIN service s ON h.id_service = s.id_service
                               LEFT JOIN medecin m ON h.id_medecin = m.id_medecin
                               WHERE {where} ORDER BY h.date_sortie DESC";
                }
                else
                {
                    sql = $@"SELECT h.id_hospitalisation, h.chambre, p.nom AS patient_nom, p.prenom AS patient_prenom,
                                      s.nom_service AS service_nom, h.date_entree, h.date_sortie
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               LEFT JOIN service s ON h.id_service = s.id_service
                               WHERE {where} ORDER BY h.date_sortie DESC";
                }

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@refdate", referenceDate.Date);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
    }
}