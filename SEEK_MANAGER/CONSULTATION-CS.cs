using System;
using System.Collections.Generic;
using System.Text;

namespace SEEK_MANAGER
{
    internal class CONSULTATION_CS
    {
        public int IdConsultation { get; set; }
        public DateTime DateConsultation { get; set; }
        public string Diagnostic { get; set; }
        public string Traitement { get; set; }
        public int IdPatient { get; set; }
        public int IdMedecin { get; set; }
    }
}
