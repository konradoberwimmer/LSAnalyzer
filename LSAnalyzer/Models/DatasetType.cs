using LSAnalyzer.Helper;
using LSAnalyzer.Models.ValidationAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LSAnalyzer.Models
{
    public class DatasetType : ObservableValidatorExtended
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
        public bool? JKreverse { get; set; }

        public DatasetType()
        {
            Name = "New Dataset Type";
            Weight = string.Empty;
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
