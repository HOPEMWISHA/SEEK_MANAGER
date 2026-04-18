using System;
using System.Collections.Generic;
using System.Text;

namespace SEEK_MANAGER
{
    internal class HOSPITALISATION_CS
    {
        public int IdHospitalisation { get; set; }
        public DateTime DateEntree { get; set; }
        public DateTime DateSortie { get; set; }
        public string Chambre { get; set; }
        public int IdPatient { get; set; }
        public int IdService { get; set; }
    }
}
