using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class DatasetType
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Weight { get; set; }
        public int? NMI { get; set; }
        public int? Nrep { get; set; }
        public string? RepWgts { get; set; }
        public double? FayFac { get; set; }
        public string? JKzone { get; set; }
        public string? JKrep { get; set; }
        public bool? JKreverse { get; set; }
        public string? PVvars { get; set; }

        public DatasetType()
        {
            Name = "New Dataset Type";
        }

        public static List<DatasetType> CreateDefaultDatasetTypes()
        {
            return new List<DatasetType>
            {
                new DatasetType
                {
                    Name = "PIRLS 2016 - student level",
                    Description = "PIRLS 2016 - student level",
                    Weight = "TOTWGT",
                    NMI = 5,
                    Nrep = 150,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                    FayFac = 0.5,
                    PVvars = "ASRREA;ASRLIT;ASRINF;ASRIIE;ASRRSI;ASRIBM"
                }
            };
        }
    }
}
