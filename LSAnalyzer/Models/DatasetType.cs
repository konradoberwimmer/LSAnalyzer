using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;
using LSAnalyzer.Models.ValidationAttributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LSAnalyzer.Models
{
    public partial class DatasetType : ObservableValidatorExtended, IChangeTracking
    {
        public int Id { get; set; }
        [MinLength(3, ErrorMessage = "Name must have length of at least three characters!")]
        [ObservableProperty] private string _name;
        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ObservableProperty] private string _group = string.Empty;
        partial void OnGroupChanged(string value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ObservableProperty] private string? _description;
        partial void OnDescriptionChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ObservableProperty] private bool _autoEncapsulateRegex = false;
        partial void OnAutoEncapsulateRegexChanged(bool value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [Required(ErrorMessage = "Weight variable is required! If there is none, add a constant of one to the dataset.")]
        [ObservableProperty] private string _weight;
        partial void OnWeightChanged(string value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [Required(ErrorMessage = "Number of multiple imputations / plausible values is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of multiple imputations / plausible values has to be at least 1!")]
        [ObservableProperty] private int? _NMI;
        partial void OnNMIChanged(int? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [MutuallyExclusive(nameof(PVvarsList), "Cannot specify both indicator variable for multiple imputations and plausible value variables!")]
        [ObservableProperty] private string? _MIvar;
        partial void OnMIvarChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ObservableProperty] private string? _IDvar;
        partial void OnIDvarChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ValidItems("Invalid plausible values!")]
        [ObservableProperty] private ItemsChangeObservableCollection<PlausibleValueVariable> _PVvarsList;
        partial void OnPVvarsListChanged(ItemsChangeObservableCollection<PlausibleValueVariable> value)
        {
            PVvarsList.CollectionChanged += delegate (object? sender, NotifyCollectionChangedEventArgs args) 
            { 
                OnPropertyChanged(nameof(IsChanged)); 
            };
            OnPropertyChanged(nameof(IsChanged));
        }
        [ValidRegex("Invalid regex pattern!")]
        [ObservableProperty] private string? _repWgts;
        partial void OnRepWgtsChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [Range(0, double.MaxValue, ErrorMessage = "Variance adjustment factor must not be negative!")]
        [ObservableProperty] private double? _fayFac;
        partial void OnFayFacChanged(double? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [MutuallyExclusive(nameof(RepWgts), "Cannot specify both replicate weight variables and jackknife zone variables!")]
        [ObservableProperty] private string? _JKzone;
        partial void OnJKzoneChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [MutuallyExclusive(nameof(RepWgts), "Cannot specify both replicate weight variables and jackknife zone variables!")]
        [ObservableProperty] private string? _JKrep;
        partial void OnJKrepChanged(string? value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        [ObservableProperty] private bool _JKreverse;
        partial void OnJKreverseChanged(bool value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }

        private DatasetType? _savedState;
        [NotMapped]
        [JsonIgnore]
        public bool IsChanged 
        {
            get
            {
                if (_savedState == null)
                {
                    return true;
                }

                return !PVvarsList.ElementObjectsEqual(_savedState.PVvarsList, new string[] { "Errors" }) || !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, new string[] { "PVvarsList", "Errors", "IsChanged" });
            }
        }

        public DatasetType()
        {
            Name = "New Dataset Type";
            Weight = string.Empty;
            JKreverse = false;
            PVvarsList = new();
        }

        public DatasetType(DatasetType datasetType)
        {
            Id = datasetType.Id;
            Name = datasetType.Name;
            Group = datasetType.Group;
            AutoEncapsulateRegex = datasetType.AutoEncapsulateRegex;
            Description = datasetType.Description;
            Weight = datasetType.Weight;
            NMI = datasetType.NMI;
            MIvar = datasetType.MIvar;
            IDvar = datasetType.IDvar;
            PVvarsList = new();
            foreach (var pvvar in datasetType.PVvarsList)
            {
                PVvarsList.Add(new(pvvar));
            }
            RepWgts = datasetType.RepWgts;
            FayFac = datasetType.FayFac;
            JKzone = datasetType.JKzone;
            JKrep = datasetType.JKrep;
            JKreverse = datasetType.JKreverse;
        }

        public void AcceptChanges()
        {
            _savedState = new DatasetType(this);
            OnPropertyChanged(nameof(IsChanged));
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
            foreach (var pvVar in PVvarsList.Where(pvvar => pvvar.Mandatory).Select(pvvar => pvvar.Regex))
            {
                regexNecessaryVariables.Add(pvVar);
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
                    Id = 102, Name = "PIRLS since 2016 - student level", Group = "PIRLS/TIMSS", Description = "PIRLS since 2016 (reverse jackknife) - student level", 
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "ASRREA", DisplayName = "ASRREA", Label = "PLAUSIBLE VALUE: OVERALL READING", Mandatory = true},
                        new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Label = "PLAUSIBLE VALUE: LITERARY PURPOSE", Mandatory = true},
                        new() { Regex = "ASRINF", DisplayName = "ASRINF", Label = "PLAUSIBLE VALUE: INFORMATIONAL PURPOSE", Mandatory = true},
                        new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Label = "PLAUSIBLE VALUE: INTERPRETING PROCESS", Mandatory = true},
                        new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Label = "PLAUSIBLE VALUE: STRAIGHTFORWARD PROCESS", Mandatory = true},
                        new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Label = "INT. READING SCALE BENCHMARK REACHED", Mandatory = true},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 112, Name = "PIRLS since 2016 - teacher data on student level", Group = "PIRLS/TIMSS", Description = "PIRLS since 2016 (reverse jackknife) - teacher data on student level",
                    Weight = "TCHWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "ASRREA", DisplayName = "ASRREA", Label = "PLAUSIBLE VALUE: OVERALL READING", Mandatory = false},
                        new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Label = "PLAUSIBLE VALUE: LITERARY PURPOSE", Mandatory = false},
                        new() { Regex = "ASRINF", DisplayName = "ASRINF", Label = "PLAUSIBLE VALUE: INFORMATIONAL PURPOSE", Mandatory = false},
                        new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Label = "PLAUSIBLE VALUE: INTERPRETING PROCESS", Mandatory = false},
                        new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Label = "PLAUSIBLE VALUE: STRAIGHTFORWARD PROCESS", Mandatory = false},
                        new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Label = "INT. READING SCALE BENCHMARK REACHED", Mandatory = false},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 122, Name = "PIRLS since 2016 - principal level", Group = "PIRLS/TIMSS", Description = "PIRLS since 2016 (reverse jackknife) - principal level",
                    Weight = "SCHWGT;STOTWGTU", IDvar = "IDSCHOOL",
                    NMI = 1, PVvarsList = new() { },
                    FayFac = 0.5, JKzone = "JKCZONE", JKrep = "JKCREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 205, Name = "TIMSS since 2015 - 4th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 4th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Label = "PLAUSIBLE VALUE MATHEMATICS", Mandatory = true},
                        new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Label = "PLAUSIBLE VALUE SCIENCE", Mandatory = true},
                        new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Label = "PV NUMBER", Mandatory = true},
                        new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Label = "PV GEOMETRY", Mandatory = true},
                        new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Label = "PV DATA DISPLAY", Mandatory = true},
                        new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Label = "PV MATH KNOWING", Mandatory = true},
                        new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Label = "PV MATH APPLYING", Mandatory = true},
                        new() { Regex = "ASMREA", DisplayName = "ASMREA", Label = "PV MATH REASONING", Mandatory = true},
                        new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Label = "PV LIFE SCIENCE", Mandatory = true},
                        new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Label = "PV PHYSICS", Mandatory = true},
                        new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Label = "PV EARTH SCIENCE", Mandatory = true},
                        new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Label = "PV SCIENCE KNOWING", Mandatory = true},
                        new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Label = "PV SCIENCE APPLYING", Mandatory = true},
                        new() { Regex = "ASSREA", DisplayName = "ASSREA", Label = "PV SCIENCE REASONING", Mandatory = true},
                        new() { Regex = "ASSENV", DisplayName = "ASSENV", Label = "PLAUSIBLE VALUE ENVIRONMENTAL AWARENESS", Mandatory = false},
                        new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Label = "INTERN. MATH BENCH REACHED WITH PV", Mandatory = true},
                        new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Label = "INTERN. SCI BENCH REACHED WITH PV", Mandatory = true},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 206, Name = "TIMSS since 2015 - 8th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 8th grade student level",
                    Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Label = "PLAUSIBLE VALUE MATHEMATICS", Mandatory = true},
                        new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Label = "PLAUSIBLE VALUE SCIENCE", Mandatory = true},
                        new() { Regex = "BSMALG", DisplayName = "BSMALG", Label = "PV ALGEBRA", Mandatory = true},
                        new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Label = "PV MATH APPLYING", Mandatory = true},
                        new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Label = "PV DATA AND PROBABILITY", Mandatory = true},
                        new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Label = "PV GEOMETRY", Mandatory = true},
                        new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Label = "PV MATH KNOWING", Mandatory = true},
                        new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Label = "PV NUMBER", Mandatory = true},
                        new() { Regex = "BSMREA", DisplayName = "BSMREA", Label = "PV MATH REASONING", Mandatory = true},
                        new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Label = "PV SCIENCE APPLYING", Mandatory = true},
                        new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Label = "PV BIOLOGY", Mandatory = true},
                        new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Label = "PV CHEMISTRY", Mandatory = true},
                        new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Label = "PV EARTH SCIENCE", Mandatory = true},
                        new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Label = "PV SCIENCE KNOWING", Mandatory = true},
                        new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Label = "PV PHYSICS", Mandatory = true},
                        new() { Regex = "BSSREA", DisplayName = "BSSREA", Label = "PV SCIENCE REASONING", Mandatory = true},
                        new() { Regex = "BSSENV", DisplayName = "BSSENV", Label = "PLAUSIBLE VALUE ENVIRONMENTAL AWARENESS", Mandatory = false},
                        new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Label = "INTERN. MATH BENCH REACHED WITH PV", Mandatory = true},
                        new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Label = "INTERN. SCI BENCH REACHED WITH PV", Mandatory = true},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 215, Name = "TIMSS since 2015 - 4th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 4th grade teacher data on student level",
                    Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Label = "PLAUSIBLE VALUE MATHEMATICS", Mandatory = true},
                        new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Label = "PLAUSIBLE VALUE SCIENCE", Mandatory = true},
                        new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Label = "PV NUMBER", Mandatory = true},
                        new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Label = "PV GEOMETRY", Mandatory = true},
                        new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Label = "PV DATA DISPLAY", Mandatory = true},
                        new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Label = "PV MATH KNOWING", Mandatory = true},
                        new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Label = "PV MATH APPLYING", Mandatory = true},
                        new() { Regex = "ASMREA", DisplayName = "ASMREA", Label = "PV MATH REASONING", Mandatory = true},
                        new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Label = "PV LIFE SCIENCE", Mandatory = true},
                        new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Label = "PV PHYSICS", Mandatory = true},
                        new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Label = "PV EARTH SCIENCE", Mandatory = true},
                        new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Label = "PV SCIENCE KNOWING", Mandatory = true},
                        new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Label = "PV SCIENCE APPLYING", Mandatory = true},
                        new() { Regex = "ASSREA", DisplayName = "ASSREA", Label = "PV SCIENCE REASONING", Mandatory = true},
                        new() { Regex = "ASSENV", DisplayName = "ASSENV", Label = "PLAUSIBLE VALUE ENVIRONMENTAL AWARENESS", Mandatory = false},
                        new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Label = "INTERN. MATH BENCH REACHED WITH PV", Mandatory = true},
                        new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Label = "INTERN. SCI BENCH REACHED WITH PV", Mandatory = true},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 216, Name = "TIMSS since 2015 - 8th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 8th grade teacher data on student level",
                    Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Label = "PLAUSIBLE VALUE MATHEMATICS", Mandatory = true},
                        new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Label = "PLAUSIBLE VALUE SCIENCE", Mandatory = true},
                        new() { Regex = "BSMALG", DisplayName = "BSMALG", Label = "PV ALGEBRA", Mandatory = true},
                        new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Label = "PV MATH APPLYING", Mandatory = true},
                        new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Label = "PV DATA AND PROBABILITY", Mandatory = true},
                        new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Label = "PV GEOMETRY", Mandatory = true},
                        new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Label = "PV MATH KNOWING", Mandatory = true},
                        new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Label = "PV NUMBER", Mandatory = true},
                        new() { Regex = "BSMREA", DisplayName = "BSMREA", Label = "PV MATH REASONING", Mandatory = true},
                        new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Label = "PV SCIENCE APPLYING", Mandatory = true},
                        new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Label = "PV BIOLOGY", Mandatory = true},
                        new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Label = "PV CHEMISTRY", Mandatory = true},
                        new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Label = "PV EARTH SCIENCE", Mandatory = true},
                        new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Label = "PV SCIENCE KNOWING", Mandatory = true},
                        new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Label = "PV PHYSICS", Mandatory = true},
                        new() { Regex = "BSSREA", DisplayName = "BSSREA", Label = "PV SCIENCE REASONING", Mandatory = true},
                        new() { Regex = "BSSENV", DisplayName = "BSSENV", Label = "PLAUSIBLE VALUE ENVIRONMENTAL AWARENESS", Mandatory = false},
                        new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Label = "INTERN. MATH BENCH REACHED WITH PV", Mandatory = true},
                        new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Label = "INTERN. SCI BENCH REACHED WITH PV", Mandatory = true},
                    },
                    FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 225, Name = "TIMSS since 2015 - principal level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - principal level",
                    Weight = "SCHWGT;STOTWGTU", IDvar = "IDSCHOOL",
                    NMI = 1, PVvarsList = new() { },
                    FayFac = 0.5, JKzone = "JKCZONE", JKrep = "JKCREP", JKreverse = true,
                },
                new DatasetType
                {
                    Id = 302, Name = "PISA since 2015 - student level", Group = "PISA", Description = "PISA since 2015 - student level",
                    Weight = "W_FSTUWT", IDvar = "CNTSTUID",
                    NMI = 10, PVvarsList = new() {
                        new() { Regex = "PV[0-9]+MATH", DisplayName = "PVMATH", Label = "Plausible Value in Mathematics", Mandatory = true},
                        new() { Regex = "PV[0-9]+READ", DisplayName = "PVREAD", Label = "Plausible Value in Reading", Mandatory = true},
                        new() { Regex = "PV[0-9]+SCIE", DisplayName = "PVSCIE", Label = "Plausible Value in Science", Mandatory = true},
                        new() { Regex = "PV[0-9]+GLCM", DisplayName = "PVGLCM", Label = "Plausible Value in Global Competency", Mandatory = false},
                        new() { Regex = "PV[0-9]+RCLI", DisplayName = "PVRCLI", Label = "Plausible Value in Cognitive Process Subscale of Reading - Locate Information", Mandatory = false},
                        new() { Regex = "PV[0-9]+RCUN", DisplayName = "PVRCUN", Label = "Plausible Value in Cognitive Process Subscale of Reading - Understand", Mandatory = false},
                        new() { Regex = "PV[0-9]+RCER", DisplayName = "PVRCER", Label = "Plausible Value in Cognitive Process Subscale of Reading - Evaluate and Reflect", Mandatory = false},
                        new() { Regex = "PV[0-9]+RTSN", DisplayName = "PVRTSN", Label = "Plausible Value in Text Structure Subscale of Reading - Single", Mandatory = false},
                        new() { Regex = "PV[0-9]+RTML", DisplayName = "PVRTML", Label = "Plausible Value in Text Structure Subscale of Reading - Multiple", Mandatory = false},
                        new() { Regex = "PV[0-9]+SCEP", DisplayName = "PVSCEP", Label = "Plausible Value in Competency Subscale of Science - Explain Phenomena Scientifically", Mandatory = false},
                        new() { Regex = "PV[0-9]+SCED", DisplayName = "PVSCED", Label = "Plausible Value in Competency Subscale of Science - Evaluate and Design Scientific Enquiry", Mandatory = false},
                        new() { Regex = "PV[0-9]+SCID", DisplayName = "PVSCID", Label = "Plausible Value in Competency Subscale of Science - Interpret Data and Evidence Scientifically", Mandatory = false},
                        new() { Regex = "PV[0-9]+SKCO", DisplayName = "PVSKCO", Label = "Plausible Value in Knowledge Subscale of Science - Content", Mandatory = false},
                        new() { Regex = "PV[0-9]+SKPE", DisplayName = "PVSKPE", Label = "Plausible Value in Knowledge Subscale of Science - Procedural & Epistemic", Mandatory = false},
                        new() { Regex = "PV[0-9]+SSPH", DisplayName = "PVSSPH", Label = "Plausible Value in System Subscale of Science - Physical", Mandatory = false},
                        new() { Regex = "PV[0-9]+SSLI", DisplayName = "PVSSLI", Label = "Plausible Value in System Subscale of Science - Living", Mandatory = false},
                        new() { Regex = "PV[0-9]+SSES", DisplayName = "PVSSES", Label = "Plausible Value in System Subscale of Science - Earth & Science", Mandatory = false},
                        new() { Regex = "PV[0-9]+MCCR", DisplayName = "PVMCCR", Label = "Plausible Value in Content Subscale of Mathematics - Change and Relationships", Mandatory = false },
                        new() { Regex = "PV[0-9]+MCQN", DisplayName = "PVMCQN", Label = "Plausible Value in Content Subscale of Mathematics - Quantity", Mandatory = false },
                        new() { Regex = "PV[0-9]+MCSS", DisplayName = "PVMCSS", Label = "Plausible Value in Content Subscale of Mathematics - Space and Shape", Mandatory = false },
                        new() { Regex = "PV[0-9]+MCUD", DisplayName = "PVMCUD", Label = "Plausible Value in Content Subscale of Mathematics - Uncertainty and Data", Mandatory = false },
                        new() { Regex = "PV[0-9]+MPEM", DisplayName = "PVMPEM", Label = "Plausible Value in Process Subscale of Mathematics - Employing Mathematical Concepts, Facts, and Procedures", Mandatory = false },
                        new() { Regex = "PV[0-9]+MPFS", DisplayName = "PVMPFS", Label = "Plausible Value in Process Subscale of Mathematics - Formulating Situations Mathematically", Mandatory = false },
                        new() { Regex = "PV[0-9]+MPIN", DisplayName = "PVMPIN", Label = "Plausible Value in Process Subscale of Mathematics - Interpreting, Applying, and Evaluating Mathematical Outcomes", Mandatory = false },
                        new() { Regex = "PV[0-9]+MPRE", DisplayName = "PVMPRE", Label = "Plausible Value in Process Subscale of Mathematics - Reasoning", Mandatory = false },
                    },
                    RepWgts = "W_FSTURWT", FayFac = 0.05, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 401, Name = "TALIS - principal level", Group = "TALIS", Description = "TALIS - principal level",
                    Weight = "SCHWGT", IDvar = "IDSCHOOL",
                    NMI = 1,
                    RepWgts = "SRWGT", FayFac = 0.04, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 402, Name = "TALIS - teacher level", Group = "TALIS", Description = "TALIS - teacher level",
                    Weight = "TCHWGT", IDvar = "IDTEACH",
                    NMI = 1,
                    RepWgts = "TRWGT", FayFac = 0.04, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 501, Name = "PIAAC", Group = "PIAAC", Description = "PIAAC since 2011 - person level",
                    Weight = "SPFWT0",
                    NMI = 10, PVvarsList = new() {
                        new() { Regex = "PVLIT", DisplayName = "PVLIT", Label = "Literacy scale score - Plausible value", Mandatory = true},
                        new() { Regex = "PVNUM", DisplayName = "PVNUM", Label = "Numeracy scale score - Plausible value", Mandatory = true},
                        new() { Regex = "PVPSL", DisplayName = "PVPSL", Label = "Problem-solving scale score - Plausible value", Mandatory = false},
                        new() { Regex = "PVAPS", DisplayName = "PVAPS", Label = "Adaptive Problem Solving scale score - Plausible value", Mandatory = false},
                    },
                    RepWgts = "SPFWT[1-9][0-9]?", FayFac = 1, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 601, Name = "ICILS - student level", Group = "ICILS", Description = "ICILS since 2013 - student level",
                    Weight = "TOTWGTS",
                    NMI = 5, PVvarsList = new() {
                        new() { Regex = "PV[0-9]+CIL", DisplayName = "PVCIL", Label = "Computer and Information Literacy - PV", Mandatory = true},
                        new() { Regex = "PV[0-9]+CT", DisplayName = "PVCT", Label = "Computational Thinking - PV", Mandatory = false},
                    },
                    RepWgts = "SRWGT[0-9]+", FayFac = 1, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 611, Name = "ICILS - teacher level", Group = "ICILS", Description = "ICILS since 2013 - teacher level",
                    Weight = "TOTWGTT",
                    NMI = 1, PVvarsList = new() { },
                    RepWgts = "TRWGT[0-9]+", FayFac = 1, JKreverse = false,
                },
                new DatasetType
                {
                    Id = 621, Name = "ICILS - school level", Group = "ICILS", Description = "ICILS since 2013 - school level",
                    Weight = "TOTWGTC",
                    NMI = 1, PVvarsList = new() { },
                    RepWgts = "CRWGT[0-9]+", FayFac = 1, JKreverse = false,
                },
            };
        }
    }
}
