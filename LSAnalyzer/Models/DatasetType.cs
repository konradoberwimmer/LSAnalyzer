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

            if (Weight == name || MIvar == name)
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
            if (!string.IsNullOrWhiteSpace(PVvars))
            {
                string[] pvVarsSplit = PVvars.Split(';');
                foreach (var pvVar in pvVarsSplit) regexNecessaryVariables.Add("^" + pvVar);
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
                    Id = 1,
                    Name = "PIRLS 2016 - student level",
                    Description = "PIRLS 2016 - student level",
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvars = "ASRREA;ASRLIT;ASRINF;ASRIIE;ASRRSI;ASRIBM",
                    Nrep = 150,
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                new DatasetType
                {
                    Id = 2,
                    Name = "PISA 2018 - student level",
                    Description = "PISA 2018 - student level",
                    Weight = "W_FSTUWT",
                    NMI = 10,
                    PVvars = "PV[0-9]+MATH;PV[0-9]+READ;PV[0-9]+SCIE;PV[0-9]+GLCM;PV[0-9]+RCLI;PV[0-9]+RCUN;PV[0-9]+RCER;PV[0-9]+RTSN;PV[0-9]+RTML",
                    Nrep = 80,
                    RepWgts = "W_FSTURWT",
                    FayFac = 0.05,
                    JKreverse = false,
                },
            };
        }
    }
}
