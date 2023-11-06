using LSAnalyzer.Helper;
using LSAnalyzer.Models.ValidationAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LSAnalyzer.Models
{
    public class DatasetType : ObservableValidatorExtended, IChangeTracking
    {
        public int Id { get; set; }
        [MinLength(3, ErrorMessage = "Name must have length of at least three characters!")]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Weight variable is required! If there is none, add a constant of one to the dataset.")]
        public string Weight { get; set; }
        [Required(ErrorMessage = "Number of multiple imputations / plausible values is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of multiple imputations / plausible values has to be at least 1!")]
        public int? NMI { get; set; }
        [RequiredInsteadOf(nameof(PVvars), "Either indicator variable for multiple imputations or plausible value variables are requried! If there is no MI/PV involved, add a constant of one to the dataset.")]
        public string? MIvar { get; set; }
        public string? IDvar { get; set; }
        [MutuallyExclusive(nameof(MIvar), "Cannot specify both indicator variable for multiple imputations and plausible value variables!")]
        public string? PVvars { get; set; }
        [Required(ErrorMessage = "Number of replications is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of replications has to be at least 1!")]
        public int? Nrep { get; set; }
        [ValidRegex("Invalid regex pattern!")]
        public string? RepWgts { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Variance adjustment factor must not be negative!")]
        public double? FayFac { get; set; }
        [MutuallyExclusive(nameof(RepWgts), "Cannot specify both replicate weight variables and jackknife zone variables!")]
        public string? JKzone { get; set; }
        [MutuallyExclusive(nameof(RepWgts), "Cannot specify both replicate weight variables and jackknife zone variables!")]
        public string? JKrep { get; set; }
        public bool JKreverse { get; set; }


        private DatasetType? _savedState;
        [NotMapped]
        [JsonIgnore]
        public bool IsChanged 
        {
            get
            {
                if (_savedState == null)
                {
                    return false;
                }

                return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, new string[] { "Errors", "IsChanged" });
            }
        }

        public DatasetType()
        {
            Name = "New Dataset Type";
            Weight = string.Empty;
            JKreverse = false;
        }

        public DatasetType(DatasetType datasetType)
        {
            Id = datasetType.Id;
            Name = datasetType.Name;
            Description = datasetType.Description;
            Weight = datasetType.Weight;
            NMI = datasetType.NMI;
            MIvar = datasetType.MIvar;
            IDvar = datasetType.IDvar;
            PVvars = datasetType.PVvars;
            Nrep = datasetType.Nrep;
            RepWgts = datasetType.RepWgts;
            FayFac = datasetType.FayFac;
            JKzone = datasetType.JKzone;
            JKrep = datasetType.JKrep;
            JKreverse = datasetType.JKreverse;
        }

        public void AcceptChanges()
        {
            _savedState = new DatasetType(this);
        }

        public bool HasSystemVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (Weight == name || MIvar == name || IDvar == name)
            {
                return true;
            }

            var repwgtRegex = string.IsNullOrWhiteSpace(RepWgts) ? "lsanalyzer_repwgt_" : RepWgts;

            if (Regex.IsMatch(name, repwgtRegex))
            {
                return true;
            }

            return false;
        }

        public List<string> GetRegexNecessaryVariables()
        {
            List<string> regexNecessaryVariables = new();

            regexNecessaryVariables.Add("^" + Weight + "$");
            
            if (!string.IsNullOrWhiteSpace(MIvar)) regexNecessaryVariables.Add("^" + MIvar + "$");
            if (!string.IsNullOrWhiteSpace(IDvar)) regexNecessaryVariables.Add("^" + IDvar + "$");
            if (!string.IsNullOrWhiteSpace(PVvars))
            {
                string[] pvVarsSplit = PVvars.Split(';');
                foreach (var pvVar in pvVarsSplit) regexNecessaryVariables.Add(pvVar);
            }

            if (!string.IsNullOrWhiteSpace(RepWgts)) regexNecessaryVariables.Add(RepWgts);
            if (!string.IsNullOrWhiteSpace(JKzone)) regexNecessaryVariables.Add("^" + JKzone + "$");
            if (!string.IsNullOrWhiteSpace(JKrep)) regexNecessaryVariables.Add("^" + JKrep + "$");

            return regexNecessaryVariables;
        }

        public static List<DatasetType> CreateDefaultDatasetTypes()
        {
            return new List<DatasetType>
            {
                new DatasetType
                {
                    Id = 101, Name = "PIRLS until 2011 - student level", Description = "PIRLS until 2011 - student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "ASRREA;ASRLIT;ASRINF;ASRIIE;ASRRSI;(ASRIBM)",
                    Nrep = 75, FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 102, Name = "PIRLS since 2016 - student level", Description = "PIRLS since 2016 (reverse jackknife) - student level", 
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "ASRREA;ASRLIT;ASRINF;ASRIIE;ASRRSI;ASRIBM",
                    Nrep = 150, FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 103, Name = "PIRLS/TIMSS 2011 joint - student level", Description = "PIRLS/TIMSS 2011 joint - student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "ASMMAT;ASSSCI;ASRREA;ASMIBM;ASSIBM;ASRIBM",
                    Nrep = 75, FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 201, Name = "TIMSS 2003 - 4th grade student level", Description = "TIMSS 2003 - 4th grade student level",
                    Weight = "totwgt;senwgt", IDvar = "idstud",
                    NMI = 5, PVvars = "asmmat;asssci;asmalg;asmdap;asmfns;asmgeo;asmmea;asseas;asslis;assphy;asmapp;asmkno;asmrea;asmibm;assibm",
                    Nrep = 75, FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 202, Name = "TIMSS 2003 - 8th grade student level", Description = "TIMSS 2003 - 8th grade student level",
                    Weight = "totwgt;senwgt", IDvar = "idstud",
                    NMI = 5, PVvars = "bsmmat;bsssci;bsmalg;bsmdap;bsmfns;bsmgeo;bsmmea;bsseas;bsslis;bssphy;bssche;bsseri;bsmapp;bsmkno;bsmrea;bsmibm;bssibm",
                    Nrep = 75, FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 203, Name = "TIMSS 2007/2011 - 4th grade student level", Description = "TIMSS 2007/2011 - 4th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "ASMMAT;ASSSCI;ASMNUM;ASMGEO;ASMDAT;ASMKNO;ASMAPP;ASMREA;ASSLIF;ASSPHY;ASSEAR;ASSKNO;ASSAPP;ASSREA;ASMIBM;ASSIBM",
                    Nrep = 75, FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 204, Name = "TIMSS 2007/2011 - 8th grade student level", Description = "TIMSS 2007/2011 - 8th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "BSMMAT;BSSSCI;BSMALG;BSMAPP;BSMDAT;BSMGEO;BSMKNO;BSMNUM;BSMREA;BSSAPP;BSSBIO;BSSCHE;BSSEAR;BSSKNO;BSSPHY;BSSREA;BSMIBM;BSSIBM",
                    Nrep = 75, FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
                },
                new DatasetType
                {
                    Id = 205, Name = "TIMSS since 2015 - 4th grade student level", Description = "TIMSS since 2015 (reverse jackknife) - 4th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "ASMMAT;ASSSCI;ASMNUM;ASMGEO;ASMDAT;ASMKNO;ASMAPP;ASMREA;ASSLIF;ASSPHY;ASSEAR;ASSKNO;ASSAPP;ASSREA;(ASSENV);ASMIBM;ASSIBM",
                    Nrep = 150, FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 206, Name = "TIMSS since 2015 - 8th grade student level", Description = "TIMSS since 2015 (reverse jackknife) - 8th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvars = "BSMMAT;BSSSCI;BSMALG;BSMAPP;BSMDAT;BSMGEO;BSMKNO;BSMNUM;BSMREA;BSSAPP;BSSBIO;BSSCHE;BSSEAR;BSSKNO;BSSPHY;BSSREA;(BSSENV);BSMIBM;BSSIBM",
                    Nrep = 150, FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 301, Name = "PISA 2003-2012 - student level", Description = "PISA 2003-2012 - student level",
                    Weight = "W_FSTUWT",
                    NMI = 5, PVvars = "PV[0-9]+MATH$;PV[0-9]+READ$;PV[0-9]+SCIE;(PV[0-9]+MACC);(PV[0-9]+MACQ);(PV[0-9]+MACS);(PV[0-9]+MACU);(PV[0-9]+MAPE);(PV[0-9]+MAPF);(PV[0-9]+MAPI);(PV[0-9]+READ1);(PV[0-9]+READ2);(PV[0-9]+READ3);(PV[0-9]+READ4);(PV[0-9]+READ5);(PV[0-9]+INTR);(PV[0-9]+SUPP);(PV[0-9]+EPS);(PV[0-9]+ISI);(PV[0-9]+USE);(PV[0-9]+MATH1);(PV[0-9]+MATH2);(PV[0-9]+MATH3);(PV[0-9]+MATH4);(PV[0-9]+MATH5);(PV[0-9]+PROB)",
                    Nrep = 80, RepWgts = "W_FSTR", FayFac = 0.05, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 302, Name = "PISA since 2015 - student level", Description = "PISA since 2015 - student level",
                    Weight = "W_FSTUWT", IDvar = "CNTSTUID",
                    NMI = 10, PVvars = "PV[0-9]+MATH;PV[0-9]+READ;PV[0-9]+SCIE;(PV[0-9]+GLCM);(PV[0-9]+RCLI);(PV[0-9]+RCUN);(PV[0-9]+RCER);(PV[0-9]+RTSN);(PV[0-9]+RTML);(PV[0-9]+SCEP);(PV[0-9]+SCED);(PV[0-9]+SCID);(PV[0-9]+SKCO);(PV[0-9]+SKPE);(PV[0-9]+SSPH);(PV[0-9]+SSLI);(PV[0-9]+SSES)",
                    Nrep = 80, RepWgts = "W_FSTURWT", FayFac = 0.05, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 401, Name = "TALIS - principal level", Description = "TALIS - principal level",
                    Weight = "SCHWGT", IDvar = "IDSCHOOL",
                    NMI = 1,
                    Nrep = 100, RepWgts = "SRWGT", FayFac = 0.04, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 402, Name = "TALIS - teacher level", Description = "TALIS - teacher level",
                    Weight = "TCHWGT", IDvar = "IDTEACH",
                    NMI = 1,
                    Nrep = 100, RepWgts = "TRWGT", FayFac = 0.04, JKreverse = false,
                },
            };
        }
    }
}
