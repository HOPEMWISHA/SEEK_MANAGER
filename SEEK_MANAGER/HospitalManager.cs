
using MySql.Data.MySqlClient;
using SEEK_MANAGER;
using System;
using System.Data;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    public class HospitalManager
    {
        // Event raised when chambres (rooms) change so UI can refresh comboboxes/lists
        public static event Action? ChambresChanged;

        private static void RaiseChambresChanged()
        {
            try { ChambresChanged?.Invoke(); } catch { }
        }

        // Public helper to notify listeners from other types
        public static void NotifyChambresChanged()
        {
            RaiseChambresChanged();
        }

        private MySqlConnection GetConnection()
        {
            return MySqlDbManager.Instance.GetConnection();
        }

        // Return consultations filtered by period relative to reference date
        public DataTable GetConsultationsByPeriod(string period, DateTime referenceDate)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string where = "";
                switch ((period ?? "JOUR").ToUpperInvariant())
                {
                    case "JOUR":
                        where = "DATE(date_consultation) = @refdate";
                        break;
                    case "SEMAINE":
                        where = "YEARWEEK(date_consultation, 1) = YEARWEEK(@refdate, 1)";
                        break;
                    case "MOIS":
                        where = "YEAR(date_consultation) = YEAR(@refdate) AND MONTH(date_consultation) = MONTH(@refdate)";
                        break;
                    case "ANNEE":
                        where = "YEAR(date_consultation) = YEAR(@refdate)";
                        break;
                    default:
                        where = "DATE(date_consultation) = @refdate";
                        break;
                }

                string sql = $@"SELECT c.id_consultation, c.date_consultation, c.diagnostic, c.traitement,
                                       p.nom AS patient_nom, p.prenom AS patient_prenom,
                                       m.nom AS medecin_nom
                                FROM consultation c
                                LEFT JOIN patient p ON c.id_patient = p.id_patient
                                LEFT JOIN medecin m ON c.id_medecin = m.id_medecin
                                WHERE {where} ORDER BY c.date_consultation DESC";

                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@refdate", referenceDate.Date);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ==========================
        // ADMIN: Chambres & Users
        // ==========================

        // Ensure chambre and users tables and helpful triggers exist
        public void EnsureAdminSchema()
        {
            using (var con = GetConnection())
            {
                con.Open();
                // create chambre table
                string createChambre = @"CREATE TABLE IF NOT EXISTS chambre (
                    id_chambre INT AUTO_INCREMENT PRIMARY KEY,
                    numero VARCHAR(64) NOT NULL UNIQUE,
                    type VARCHAR(128),
                    statut VARCHAR(32) DEFAULT 'Libre',
                    role VARCHAR(64) DEFAULT 'Général'
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                var cmd = new MySqlCommand(createChambre, con);
                cmd.ExecuteNonQuery();
                // ensure role column exists for older installations
                try
                {
                    // Add role column if missing (older MySQL versions may not support IF NOT EXISTS)
                    try
                    {
                        string addRole = "ALTER TABLE chambre ADD COLUMN role VARCHAR(64) DEFAULT 'Général'";
                        var cmd2 = new MySqlCommand(addRole, con);
                        cmd2.ExecuteNonQuery();
                    }
                    catch { }
                }
                catch { /* IF NOT EXISTS may not be supported on all MySQL versions; ignore if fails */ }

                // create users table if missing (minimal fields for admin panel)
                string createUsers = @"CREATE TABLE IF NOT EXISTS users (
                    id_user INT AUTO_INCREMENT PRIMARY KEY,
                    full_name VARCHAR(200),
                    username VARCHAR(100) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    role VARCHAR(50) DEFAULT 'Utilisateur',
                    email VARCHAR(200)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                cmd = new MySqlCommand(createUsers, con);
                cmd.ExecuteNonQuery();

                // create a trigger to prevent assigning a patient to an already occupied room
                try
                {
                    string dropTrig = "DROP TRIGGER IF EXISTS before_insert_hospitalisation_check_chambre";
                    cmd = new MySqlCommand(dropTrig, con); cmd.ExecuteNonQuery();

                    string createTrig = @"CREATE TRIGGER before_insert_hospitalisation_check_chambre
                    BEFORE INSERT ON hospitalisation
                    FOR EACH ROW
                    BEGIN
                      DECLARE s VARCHAR(32);
                      SELECT statut INTO s FROM chambre WHERE numero = NEW.chambre LIMIT 1;
                      IF s = 'Occupée' THEN
                        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La chambre est déjà occupée.';
                      END IF;
                    END;";
                    cmd = new MySqlCommand(createTrig, con);
                    cmd.ExecuteNonQuery();
                }
                catch { /* triggers may fail on some MySQL versions; schema checks still help */ }

                // Ensure hospitalisation has a column to record which user performed the sortie (discharge)
                try
                {
                    try
                    {
                        string addSortiePar = "ALTER TABLE hospitalisation ADD COLUMN sortie_par VARCHAR(200) DEFAULT NULL";
                        var cmdSort = new MySqlCommand(addSortiePar, con);
                        cmdSort.ExecuteNonQuery();
                    }
                    catch { }

                    // Normalize invalid zero-dates to NULL to avoid MySQL rejecting them in strict modes
                    try
                    {
                        string nulldates = "UPDATE hospitalisation SET date_sortie = NULL WHERE date_sortie = '0000-00-00' OR date_sortie = ''";
                        var cmdNull = new MySqlCommand(nulldates, con);
                        cmdNull.ExecuteNonQuery();
                    }
                    catch { }

                    try
                    {
                        string modifyDate = "ALTER TABLE hospitalisation MODIFY COLUMN date_sortie DATE NULL";
                        var cmdModDate = new MySqlCommand(modifyDate, con);
                        cmdModDate.ExecuteNonQuery();
                    }
                    catch { }
                }
                catch { }
            }
        }

        public DataTable GetChambresTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT id_chambre, numero, type, statut, COALESCE(role, 'Général') AS role FROM chambre";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void AddChambre(string numero, string type, string statut, string role = "Général")
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "INSERT INTO chambre (numero, type, statut, role) VALUES (@num,@type,@stat,@role)";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@num", numero);
                cmd.Parameters.AddWithValue("@type", type ?? string.Empty);
                cmd.Parameters.AddWithValue("@stat", statut ?? "Libre");
                cmd.Parameters.AddWithValue("@role", role ?? "Général");
                cmd.ExecuteNonQuery();
            }
            // notify listeners
            RaiseChambresChanged();
        }

        public void UpdateChambre(int id, string numero, string type, string statut, string role = "Général")
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "UPDATE chambre SET numero=@num, type=@type, statut=@stat, role=@role WHERE id_chambre=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@num", numero);
                cmd.Parameters.AddWithValue("@type", type ?? string.Empty);
                cmd.Parameters.AddWithValue("@stat", statut ?? "Libre");
                cmd.Parameters.AddWithValue("@role", role ?? "Général");
                cmd.ExecuteNonQuery();
            }
            // notify listeners
            RaiseChambresChanged();
        }

        public void DeleteChambre(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "DELETE FROM chambre WHERE id_chambre=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // Assign a patient to a chambre, checking status and updating both hospitalisation and chambre tables
        public void AssignPatientToChambre(string numeroChambre, int idPatient, int idService, DateTime dateEntree)
        {
            using (var con = GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // check chambre status
                    string stSql = "SELECT statut FROM chambre WHERE numero=@num FOR UPDATE";
                    var stCmd = new MySqlCommand(stSql, con, tx);
                    stCmd.Parameters.AddWithValue("@num", numeroChambre);
                    var st = stCmd.ExecuteScalar() as string ?? "Libre";
                    if (string.Equals(st, "Occupée", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("La chambre est déjà occupée.");
                    }

                    // insert hospitalisation
                    MySqlCommand icmd;
                    if (idService <= 0)
                    {
                        // insert without id_service to avoid FK constraint when no service is provided
                        string insNoService = "INSERT INTO hospitalisation (chambre, id_patient, date_entree) VALUES (@ch,@idp,@ent)";
                        icmd = new MySqlCommand(insNoService, con, tx);
                        icmd.Parameters.AddWithValue("@ch", numeroChambre);
                        icmd.Parameters.AddWithValue("@idp", idPatient);
                        icmd.Parameters.AddWithValue("@ent", dateEntree);
                        icmd.ExecuteNonQuery();
                    }
                    else
                    {
                        string ins = "INSERT INTO hospitalisation (chambre, id_patient, id_service, date_entree) VALUES (@ch,@idp,@ids,@ent)";
                        icmd = new MySqlCommand(ins, con, tx);
                        icmd.Parameters.AddWithValue("@ch", numeroChambre);
                        icmd.Parameters.AddWithValue("@idp", idPatient);
                        icmd.Parameters.AddWithValue("@ids", idService);
                        icmd.Parameters.AddWithValue("@ent", dateEntree);
                        icmd.ExecuteNonQuery();
                    }

                    // update chambre statut
                    string upd = "UPDATE chambre SET statut='Occupée' WHERE numero=@num";
                    var ucmd = new MySqlCommand(upd, con, tx);
                    ucmd.Parameters.AddWithValue("@num", numeroChambre);
                    ucmd.ExecuteNonQuery();

                    tx.Commit();
                }
            }
        }

        public void ReleaseChambre(string numeroChambre)
        {
            using (var con = GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // set last hospitalisation's date_sortie if any
                    string updHosp = @"UPDATE hospitalisation h
                        SET h.date_sortie = NOW()
                        WHERE h.chambre = @num AND (h.date_sortie IS NULL OR h.date_sortie = '0000-00-00')
                        ORDER BY h.id_hospitalisation DESC LIMIT 1";
                    var hcmd = new MySqlCommand(updHosp, con, tx);
                    hcmd.Parameters.AddWithValue("@num", numeroChambre);
                    hcmd.ExecuteNonQuery();

                    // update chambre statut
                    string upd = "UPDATE chambre SET statut='Libre' WHERE numero=@num";
                    var ucmd = new MySqlCommand(upd, con, tx);
                    ucmd.Parameters.AddWithValue("@num", numeroChambre);
                    ucmd.ExecuteNonQuery();

                    tx.Commit();
                }
            }
        }

        // Users management
        public DataTable GetUsersTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT id_user, full_name, username, role, email FROM users";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void AddUser(string fullName, string username, string passwordHash, string role, string email)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "INSERT INTO users (full_name, username, password_hash, role, email) VALUES (@f,@u,@p,@r,@e)";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@f", fullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", passwordHash);
                cmd.Parameters.AddWithValue("@r", role ?? "Utilisateur");
                cmd.Parameters.AddWithValue("@e", email ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateUser(int id, string fullName, string username, string? passwordHash, string role, string email)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = passwordHash == null ?
                    "UPDATE users SET full_name=@f, username=@u, role=@r, email=@e WHERE id_user=@id" :
                    "UPDATE users SET full_name=@f, username=@u, password_hash=@p, role=@r, email=@e WHERE id_user=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@f", fullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@u", username);
                if (passwordHash != null) cmd.Parameters.AddWithValue("@p", passwordHash);
                cmd.Parameters.AddWithValue("@r", role ?? "Utilisateur");
                cmd.Parameters.AddWithValue("@e", email ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteUser(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "DELETE FROM users WHERE id_user=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // Return patients filtered by registration/creation date column if present
        public DataTable GetPatientsByRegistrationPeriod(string period, DateTime referenceDate)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // find a candidate date column
                string dateCol = null;
                foreach (var cand in new[] { "date_enregistrement", "created_at", "date_created", "date_inscription" })
                {
                    if (ColumnExists(con, "patient", cand)) { dateCol = cand; break; }
                }

                if (dateCol == null)
                {
                    // fallback: return full patients table
                    return GetPatientsFullTable();
                }

                string where = "";
                switch ((period ?? "JOUR").ToUpperInvariant())
                {
                    case "JOUR":
                        where = $"DATE({dateCol}) = @refdate";
                        break;
                    case "SEMAINE":
                        where = $"YEARWEEK({dateCol}, 1) = YEARWEEK(@refdate, 1)";
                        break;
                    case "MOIS":
                        where = $"YEAR({dateCol}) = YEAR(@refdate) AND MONTH({dateCol}) = MONTH(@refdate)";
                        break;
                    case "ANNEE":
                        where = $"YEAR({dateCol}) = YEAR(@refdate)";
                        break;
                    default:
                        where = $"DATE({dateCol}) = @refdate";
                        break;
                }

                string sql = $@"SELECT * FROM patient WHERE {where}";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@refdate", referenceDate.Date);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
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

        // Return consultations table (with patient and medecin names)
        public DataTable GetConsultationsTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"SELECT c.id_consultation, c.date_consultation, c.diagnostic, c.traitement,
                                      p.nom AS patient_nom,
                                      m.nom AS medecin_nom
                               FROM consultation c
                               LEFT JOIN patient p ON c.id_patient = p.id_patient
                               LEFT JOIN medecin m ON c.id_medecin = m.id_medecin";

                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Return full patients table (all columns)
        public DataTable GetPatientsFullTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "SELECT * FROM patient";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Return medecins with service name (full columns for medecin view)
        public DataTable GetMedecinsFullTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"SELECT m.id_medecin, m.nom, m.specialite, m.id_service,
                                      s.nom_service AS service_nom
                               FROM medecin m
                               LEFT JOIN service s ON m.id_service = s.id_service";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
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
                // Prevent duplicate full names (case-insensitive)
                string existsSql = "SELECT COUNT(*) FROM patient WHERE LOWER(nom)=LOWER(@nom) AND LOWER(prenom)=LOWER(@prenom)";
                var existsCmd = new MySqlCommand(existsSql, con);
                existsCmd.Parameters.AddWithValue("@nom", nom);
                existsCmd.Parameters.AddWithValue("@prenom", prenom);
                var count = Convert.ToInt32(existsCmd.ExecuteScalar());
                if (count > 0)
                {
                    throw new InvalidOperationException("Un patient avec le même nom et prénom existe déjà.");
                }

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
                // Prevent duplicate full names for other records
                string existsSql = "SELECT COUNT(*) FROM patient WHERE LOWER(nom)=LOWER(@nom) AND LOWER(prenom)=LOWER(@prenom) AND id_patient<>@id";
                var existsCmd = new MySqlCommand(existsSql, con);
                existsCmd.Parameters.AddWithValue("@nom", nom);
                existsCmd.Parameters.AddWithValue("@prenom", prenom);
                existsCmd.Parameters.AddWithValue("@id", id);
                var count = Convert.ToInt32(existsCmd.ExecuteScalar());
                if (count > 0)
                {
                    throw new InvalidOperationException("Un autre patient avec le même nom et prénom existe déjà.");
                }

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

        // =====================================================
        // PAIEMENT → Form : PaiementForm / Control : PaiementControl
        // =====================================================

        // Return payments table with patient name when available
        public DataTable GetPaymentsTable()
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"SELECT p.id, p.patient_id, CONCAT(pt.nom, ' ', pt.prenom) AS patient_nom,
                                      p.reference, p.amount, p.currency, p.method, p.paid_at, p.notes
                               FROM paiement p
                               LEFT JOIN patient pt ON p.patient_id = pt.id_patient
                               WHERE p.is_deleted IS NULL OR p.is_deleted = 0";
                var da = new MySqlDataAdapter(sql, con);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetPaymentById(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"SELECT p.id, p.patient_id, p.reference, p.amount, p.currency, p.method, p.paid_at, p.notes
                               FROM paiement p WHERE p.id = @id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Return payments for a specific patient (not including deleted)
        public DataTable GetPaymentsByPatientId(int patientId)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = @"SELECT p.id, p.patient_id, CONCAT(pt.nom, ' ', pt.prenom) AS patient_nom,
                                      p.reference, p.amount, p.currency, p.method, p.paid_at, p.notes
                               FROM paiement p
                               LEFT JOIN patient pt ON p.patient_id = pt.id_patient
                               WHERE (p.is_deleted IS NULL OR p.is_deleted = 0) AND p.patient_id = @pid";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@pid", patientId);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void AjouterPAIEMENT(int? patientId, string reference, decimal amount, string currency, string method, DateTime paidAt, string notes)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // validations
                if (amount <= 0) throw new InvalidOperationException("Le montant doit être supérieur à zéro.");
                if (paidAt > DateTime.Now) throw new InvalidOperationException("La date de paiement ne peut pas être dans le futur.");
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    string refExists = "SELECT COUNT(*) FROM paiement WHERE reference = @ref";
                    var rcmd = new MySqlCommand(refExists, con);
                    rcmd.Parameters.AddWithValue("@ref", reference);
                    var rcount = Convert.ToInt32(rcmd.ExecuteScalar());
                    if (rcount > 0) throw new InvalidOperationException("La référence du paiement existe déjà.");
                }

                string sql = @"INSERT INTO paiement (patient_id, reference, amount, currency, method, paid_at, created_at, notes, is_deleted)
                               VALUES (@pid, @ref, @amt, @cur, @method, @paid, NOW(), @notes, 0)";
                var cmd = new MySqlCommand(sql, con);
                if (patientId.HasValue)
                    cmd.Parameters.AddWithValue("@pid", patientId.Value);
                else
                    cmd.Parameters.AddWithValue("@pid", DBNull.Value);
                cmd.Parameters.AddWithValue("@ref", reference ?? string.Empty);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@cur", currency ?? "");
                cmd.Parameters.AddWithValue("@method", method ?? "");
                cmd.Parameters.AddWithValue("@paid", paidAt);
                cmd.Parameters.AddWithValue("@notes", notes ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        public void ModifierPAIEMENT(int id, int? patientId, string reference, decimal amount, string currency, string method, DateTime paidAt, string notes)
        {
            using (var con = GetConnection())
            {
                con.Open();
                // validations
                if (amount <= 0) throw new InvalidOperationException("Le montant doit être supérieur à zéro.");
                if (paidAt > DateTime.Now) throw new InvalidOperationException("La date de paiement ne peut pas être dans le futur.");
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    string refExists = "SELECT COUNT(*) FROM paiement WHERE reference = @ref AND id<>@id";
                    var rcmd = new MySqlCommand(refExists, con);
                    rcmd.Parameters.AddWithValue("@ref", reference);
                    rcmd.Parameters.AddWithValue("@id", id);
                    var rcount = Convert.ToInt32(rcmd.ExecuteScalar());
                    if (rcount > 0) throw new InvalidOperationException("La référence du paiement existe déjà pour un autre enregistrement.");
                }

                string sql = @"UPDATE paiement SET patient_id=@pid, reference=@ref, amount=@amt, currency=@cur,
                                     method=@method, paid_at=@paid, notes=@notes, updated_at=NOW()
                               WHERE id=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                if (patientId.HasValue)
                    cmd.Parameters.AddWithValue("@pid", patientId.Value);
                else
                    cmd.Parameters.AddWithValue("@pid", DBNull.Value);
                cmd.Parameters.AddWithValue("@ref", reference ?? string.Empty);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@cur", currency ?? "");
                cmd.Parameters.AddWithValue("@method", method ?? "");
                cmd.Parameters.AddWithValue("@paid", paidAt);
                cmd.Parameters.AddWithValue("@notes", notes ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        // The control calls DeletePayment; keep the English name for compatibility
        public void DeletePayment(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                string sql = "DELETE FROM paiement WHERE id=@id";
                var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
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
                using (var tx = con.BeginTransaction())
                {
                    // business rule: a patient cannot be admitted and discharged on the same day
                    if (dateSortie.HasValue && dateEntree.Date == dateSortie.Value.Date)
                    {
                        throw new InvalidOperationException("La date de sortie ne peut pas être la même que la date d'entrée.");
                    }

                    // date_sortie must be strictly in the future (not today and not in the past)
                    if (dateSortie.HasValue && dateSortie.Value.Date <= DateTime.Today)
                    {
                        throw new InvalidOperationException("La date de sortie doit être postérieure à la date actuelle.");
                    }

                    // check chambre status to prevent assigning into an already occupied room
                    try
                    {
                        // ensure there is no active hospitalisation for this chambre
                        string chkActive = "SELECT COUNT(*) FROM hospitalisation WHERE chambre=@num AND (date_sortie IS NULL OR DATE(date_sortie) >= CURDATE())";
                        var ccmd = new MySqlCommand(chkActive, con, tx);
                        ccmd.Parameters.AddWithValue("@num", chambre);
                        var cnt = Convert.ToInt32(ccmd.ExecuteScalar());
                        if (cnt > 0)
                        {
                            throw new InvalidOperationException("La chambre est déjà occupée (hospitalisation active).");
                        }
                    }
                    catch (InvalidOperationException) { throw; }
                    catch { /* ignore check failure, proceed */ }

                    bool hasMedecinColumn = ColumnExists(con, "hospitalisation", "id_medecin");
                    bool hasSortiePar = ColumnExists(con, "hospitalisation", "sortie_par");

                    string sql;
                    if (hasMedecinColumn && idMedecin.HasValue)
                    {
                        sql = @"INSERT INTO hospitalisation (chambre, id_patient, id_service, id_medecin, date_entree, date_sortie" + (hasSortiePar ? ", sortie_par)" : ")") +
                              " VALUES (@chambre, @idp, @ids, @idm, @entree, @sortie" + (hasSortiePar ? ", @sortie_par)" : ")");
                    }
                    else
                    {
                        sql = @"INSERT INTO hospitalisation (chambre, id_patient, id_service, date_entree, date_sortie" + (hasSortiePar ? ", sortie_par)" : ")") +
                              " VALUES (@chambre, @idp, @ids, @entree, @sortie" + (hasSortiePar ? ", @sortie_par)" : ")");
                    }

                    var cmd = new MySqlCommand(sql, con, tx);
                    cmd.Parameters.AddWithValue("@chambre", chambre);
                    cmd.Parameters.AddWithValue("@idp", idPatient);
                    cmd.Parameters.AddWithValue("@ids", idService);
                    if (hasMedecinColumn && idMedecin.HasValue)
                        cmd.Parameters.AddWithValue("@idm", idMedecin.Value);
                    // explicit date parameters
                    var pEntree = cmd.Parameters.Add("@entree", MySql.Data.MySqlClient.MySqlDbType.Date);
                    pEntree.Value = dateEntree.Date;
                    var pSortie = cmd.Parameters.Add("@sortie", MySql.Data.MySqlClient.MySqlDbType.Date);
                    pSortie.Value = (dateSortie.HasValue && dateSortie.Value.Year >= 1900) ? (object)dateSortie.Value.Date : DBNull.Value;

                    if (hasSortiePar)
                    {
                        var user = UserSession.FullName ?? UserSession.Username ?? string.Empty;
                        if (dateSortie.HasValue && !string.IsNullOrWhiteSpace(user)) cmd.Parameters.AddWithValue("@sortie_par", user);
                        else cmd.Parameters.AddWithValue("@sortie_par", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();

                    // update chambre statut to Occupée when patient assigned
                    try
                    {
                        var up = new MySqlCommand("UPDATE chambre SET statut='Occupée' WHERE numero=@num", con, tx);
                        up.Parameters.AddWithValue("@num", chambre);
                        up.ExecuteNonQuery();
                    }
                    catch { }

                    tx.Commit();
                }
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
                                            DateTime dateEntree, DateTime? dateSortie)
        {
            using (var con = GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Validate dates
                    if (dateSortie.HasValue && dateSortie.Value.Date == dateEntree.Date)
                        throw new InvalidOperationException("La date de sortie ne peut pas être la même que la date d'entrée.");
                    if (dateSortie.HasValue && dateSortie.Value.Date <= DateTime.Today)
                        throw new InvalidOperationException("La date de sortie doit être postérieure à la date actuelle.");

                    // Build SQL dynamically: do not update date_sortie if no valid value provided
                    var sb = new System.Text.StringBuilder();
                    sb.Append("UPDATE hospitalisation SET chambre=@chambre, id_patient=@idp, id_service=@ids, date_entree=@entree");
                    bool willUpdateSortie = dateSortie.HasValue && dateSortie.Value.Year >= 1900;
                    if (willUpdateSortie) sb.Append(", date_sortie=@sortie");
                    sb.Append(" WHERE id_hospitalisation=@id");

                    string sql = sb.ToString();
                    var cmd = new MySqlCommand(sql, con, tx);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@chambre", chambre);
                    cmd.Parameters.AddWithValue("@idp", idPatient);
                    cmd.Parameters.AddWithValue("@ids", idService);
                    var pEntree = cmd.Parameters.Add("@entree", MySql.Data.MySqlClient.MySqlDbType.Date);
                    pEntree.Value = dateEntree.Date;
                    if (willUpdateSortie)
                    {
                        var pSort = cmd.Parameters.Add("@sortie", MySql.Data.MySqlClient.MySqlDbType.Date);
                        pSort.Value = dateSortie.Value.Date;
                    }

                    cmd.ExecuteNonQuery();

                    // If sortie_par column exists, update it only after main update
                    try
                    {
                        if (ColumnExists(con, "hospitalisation", "sortie_par"))
                        {
                            var user = UserSession.FullName ?? UserSession.Username ?? string.Empty;
                            string upd = "UPDATE hospitalisation SET sortie_par=@sortie_par WHERE id_hospitalisation=@id";
                            var ucmd = new MySqlCommand(upd, con, tx);
                            if (willUpdateSortie && !string.IsNullOrWhiteSpace(user))
                                ucmd.Parameters.AddWithValue("@sortie_par", user);
                            else
                                ucmd.Parameters.AddWithValue("@sortie_par", DBNull.Value);
                            ucmd.Parameters.AddWithValue("@id", id);
                            ucmd.ExecuteNonQuery();
                        }
                    }
                    catch { }

                    // if chambre changed or patient released, ensure chambre statut updated appropriately
                    try
                    {
                        // set chambre to Occupée when hospitalisation has no sortie
                        if (!willUpdateSortie)
                        {
                            var upOcc = new MySqlCommand("UPDATE chambre SET statut='Occupée' WHERE numero=@num", con, tx);
                            upOcc.Parameters.AddWithValue("@num", chambre);
                            upOcc.ExecuteNonQuery();
                        }
                        else
                        {
                            // if sortie set, mark chambre as Libre
                            var upLib = new MySqlCommand("UPDATE chambre SET statut='Libre' WHERE numero=@num", con, tx);
                            upLib.Parameters.AddWithValue("@num", chambre);
                            upLib.ExecuteNonQuery();
                        }
                    }
                    catch { }

                    tx.Commit();
                }
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
                                      h.date_entree, h.date_sortie, h.sortie_par
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
                                      s.nom_service AS service_nom, m.nom AS medecin_nom, h.date_entree, h.date_sortie, h.sortie_par
                               FROM hospitalisation h
                               LEFT JOIN patient p ON h.id_patient = p.id_patient
                               LEFT JOIN service s ON h.id_service = s.id_service
                               LEFT JOIN medecin m ON h.id_medecin = m.id_medecin
                               WHERE {where} ORDER BY h.date_sortie DESC";
                }
                else
                {
                    sql = $@"SELECT h.id_hospitalisation, h.chambre, p.nom AS patient_nom, p.prenom AS patient_prenom,
                                      s.nom_service AS service_nom, h.date_entree, h.date_sortie, h.sortie_par
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