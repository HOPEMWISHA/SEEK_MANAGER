using System;
using System.Collections.Generic;
using System.Text;

namespace SEEK_MANAGER
{
    internal class PATIENT_CS
    {
        public int IdPatient { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Sexe { get; set; }
        public DateTime DateNaissance { get; set; }
        public string Telephone { get; set; }
        public string Adresse { get; set; }
    }
}
