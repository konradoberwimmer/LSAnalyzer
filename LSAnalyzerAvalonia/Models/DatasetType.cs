﻿using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.Helper;
using LSAnalyzerAvalonia.Models.ValidationAttributes;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LSAnalyzerAvalonia.Models;

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
                Id = 101, Name = "PIRLS until 2011 - student level", Group = "PIRLS/TIMSS", Description = "PIRLS until 2011 - student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() { 
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true},
                    new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true},
                    new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true},
                    new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Mandatory = true},
                    new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Mandatory = true},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = false},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 102, Name = "PIRLS since 2016 - student level", Group = "PIRLS/TIMSS", Description = "PIRLS since 2016 (reverse jackknife) - student level", 
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true},
                    new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true},
                    new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true},
                    new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Mandatory = true},
                    new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Mandatory = true},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = true},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 103, Name = "PIRLS/TIMSS 2011 joint - student level", Group = "PIRLS/TIMSS", Description = "PIRLS/TIMSS 2011 joint - student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = true},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = true},
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = true},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = true},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = true},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 111, Name = "PIRLS until 2011 - teacher data on student level", Group = "PIRLS/TIMSS", Description = "PIRLS until 2011 - teacher data on student level",
                Weight = "TCHWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = false},
                    new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = false},
                    new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = false},
                    new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Mandatory = false},
                    new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Mandatory = false},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = false},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 112, Name = "PIRLS since 2016 - teacher data on student level", Group = "PIRLS/TIMSS", Description = "PIRLS since 2016 (reverse jackknife) - teacher data on student level",
                Weight = "TCHWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = false},
                    new() { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = false},
                    new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = false},
                    new() { Regex = "ASRIIE", DisplayName = "ASRIIE", Mandatory = false},
                    new() { Regex = "ASRRSI", DisplayName = "ASRRSI", Mandatory = false},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = false},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 113, Name = "PIRLS/TIMSS 2011 joint - teacher data on student level", Group = "PIRLS/TIMSS", Description = "PIRLS/TIMSS 2011 joint - teacher data on student level",
                Weight = "TCHWGT;MATWGT;REAWGT;SCIWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = false},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = false},
                    new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = false},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = false},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = false},
                    new() { Regex = "ASRIBM", DisplayName = "ASRIBM", Mandatory = false},
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = false},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = false},
                    new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Mandatory = false},
                    new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Mandatory = false},
                    new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Mandatory = false},
                    new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Mandatory = false},
                    new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Mandatory = false},
                    new() { Regex = "ASMREA", DisplayName = "ASMREA", Mandatory = false},
                    new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Mandatory = false},
                    new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Mandatory = false},
                    new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Mandatory = false},
                    new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Mandatory = false},
                    new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Mandatory = false},
                    new() { Regex = "ASSREA", DisplayName = "ASSREA", Mandatory = false},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = false},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = false},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 121, Name = "PIRLS until 2011 - principal level", Group = "PIRLS/TIMSS", Description = "PIRLS until 2011 - principal level",
                Weight = "SCHWGT;STOTWGTU", IDvar = "IDSCHOOL",
                NMI = 1, PVvarsList = new() { },
                FayFac = 1, JKzone = "JKCZONE", JKrep = "JKCREP", JKreverse = false,
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
                Id = 123, Name = "PIRLS/TIMSS 2011 joint - principal level", Group = "PIRLS/TIMSS", Description = "PIRLS/TIMSS 2011 joint - principal level",
                Weight = "SCHWGT;STOTWGTU", IDvar = "IDSCHOOL",
                NMI = 1, PVvarsList = new() { },
                FayFac = 1, JKzone = "JKCZONE", JKrep = "JKCREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 201, Name = "TIMSS 2003 - 4th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2003 - 4th grade student level",
                Weight = "totwgt;senwgt", IDvar = "idstud",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "asmmat", DisplayName = "asmmat", Mandatory = true},
                    new() { Regex = "asssci", DisplayName = "asssci", Mandatory = true},
                    new() { Regex = "asmalg", DisplayName = "asmalg", Mandatory = true},
                    new() { Regex = "asmdap", DisplayName = "asmdap", Mandatory = true},
                    new() { Regex = "asmfns", DisplayName = "asmfns", Mandatory = true},
                    new() { Regex = "asmgeo", DisplayName = "asmgeo", Mandatory = true},
                    new() { Regex = "asmmea", DisplayName = "asmmea", Mandatory = true},
                    new() { Regex = "asseas", DisplayName = "asseas", Mandatory = true},
                    new() { Regex = "asslis", DisplayName = "asslis", Mandatory = true},
                    new() { Regex = "assphy", DisplayName = "assphy", Mandatory = true},
                    new() { Regex = "asmapp", DisplayName = "asmapp", Mandatory = true},
                    new() { Regex = "asmkno", DisplayName = "asmkno", Mandatory = true},
                    new() { Regex = "asmrea", DisplayName = "asmrea", Mandatory = true},
                    new() { Regex = "asmibm", DisplayName = "asmibm", Mandatory = true},
                    new() { Regex = "assibm", DisplayName = "assibm", Mandatory = true},
                },
                FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
            },
            new DatasetType
            {
                Id = 202, Name = "TIMSS 2003 - 8th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2003 - 8th grade student level",
                Weight = "totwgt;senwgt", IDvar = "idstud",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "bsmmat", DisplayName = "bsmmat", Mandatory = true},
                    new() { Regex = "bsssci", DisplayName = "bsssci", Mandatory = true},
                    new() { Regex = "bsmalg", DisplayName = "bsmalg", Mandatory = true},
                    new() { Regex = "bsmdap", DisplayName = "bsmdap", Mandatory = true},
                    new() { Regex = "bsmfns", DisplayName = "bsmfns", Mandatory = true},
                    new() { Regex = "bsmgeo", DisplayName = "bsmgeo", Mandatory = true},
                    new() { Regex = "bsmmea", DisplayName = "bsmmea", Mandatory = true},
                    new() { Regex = "bsseas", DisplayName = "bsseas", Mandatory = true},
                    new() { Regex = "bsslis", DisplayName = "bsslis", Mandatory = true},
                    new() { Regex = "bssphy", DisplayName = "bssphy", Mandatory = true},
                    new() { Regex = "bssche", DisplayName = "bssche", Mandatory = true},
                    new() { Regex = "bsseri", DisplayName = "bsseri", Mandatory = true},
                    new() { Regex = "bsmapp", DisplayName = "bsmapp", Mandatory = true},
                    new() { Regex = "bsmkno", DisplayName = "bsmkno", Mandatory = true},
                    new() { Regex = "bsmrea", DisplayName = "bsmrea", Mandatory = true},
                    new() { Regex = "bsmibm", DisplayName = "bsmibm", Mandatory = true},
                    new() { Regex = "bssibm", DisplayName = "bssibm", Mandatory = true},
                },
                FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
            },
            new DatasetType
            {
                Id = 203, Name = "TIMSS 2007/2011 - 4th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2007/2011 - 4th grade student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = true},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = true},
                    new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Mandatory = true},
                    new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Mandatory = true},
                    new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Mandatory = true},
                    new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Mandatory = true},
                    new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Mandatory = true},
                    new() { Regex = "ASMREA", DisplayName = "ASMREA", Mandatory = true},
                    new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Mandatory = true},
                    new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Mandatory = true},
                    new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Mandatory = true},
                    new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Mandatory = true},
                    new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Mandatory = true},
                    new() { Regex = "ASSREA", DisplayName = "ASSREA", Mandatory = true},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = true},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = true},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 204, Name = "TIMSS 2007/2011 - 8th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2007/2011 - 8th grade student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Mandatory = true},
                    new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Mandatory = true},
                    new() { Regex = "BSMALG", DisplayName = "BSMALG", Mandatory = true},
                    new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Mandatory = true},
                    new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Mandatory = true},
                    new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Mandatory = true},
                    new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Mandatory = true},
                    new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Mandatory = true},
                    new() { Regex = "BSMREA", DisplayName = "BSMREA", Mandatory = true},
                    new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Mandatory = true},
                    new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Mandatory = true},
                    new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Mandatory = true},
                    new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Mandatory = true},
                    new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Mandatory = true},
                    new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Mandatory = true},
                    new() { Regex = "BSSREA", DisplayName = "BSSREA", Mandatory = true},
                    new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Mandatory = true},
                    new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Mandatory = true},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 205, Name = "TIMSS since 2015 - 4th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 4th grade student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = true},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = true},
                    new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Mandatory = true},
                    new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Mandatory = true},
                    new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Mandatory = true},
                    new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Mandatory = true},
                    new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Mandatory = true},
                    new() { Regex = "ASMREA", DisplayName = "ASMREA", Mandatory = true},
                    new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Mandatory = true},
                    new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Mandatory = true},
                    new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Mandatory = true},
                    new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Mandatory = true},
                    new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Mandatory = true},
                    new() { Regex = "ASSREA", DisplayName = "ASSREA", Mandatory = true},
                    new() { Regex = "ASSENV", DisplayName = "ASSENV", Mandatory = false},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = true},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = true},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 206, Name = "TIMSS since 2015 - 8th grade student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 8th grade student level",
                Weight = "TOTWGT;SENWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Mandatory = true},
                    new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Mandatory = true},
                    new() { Regex = "BSMALG", DisplayName = "BSMALG", Mandatory = true},
                    new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Mandatory = true},
                    new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Mandatory = true},
                    new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Mandatory = true},
                    new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Mandatory = true},
                    new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Mandatory = true},
                    new() { Regex = "BSMREA", DisplayName = "BSMREA", Mandatory = true},
                    new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Mandatory = true},
                    new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Mandatory = true},
                    new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Mandatory = true},
                    new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Mandatory = true},
                    new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Mandatory = true},
                    new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Mandatory = true},
                    new() { Regex = "BSSREA", DisplayName = "BSSREA", Mandatory = true},
                    new() { Regex = "BSSENV", DisplayName = "BSSENV", Mandatory = false},
                    new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Mandatory = true},
                    new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Mandatory = true},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 211, Name = "TIMSS 2003 - 4th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2003 - 4th teacher data on grade student level",
                Weight = "tchwgt;matwgt;sciwgt", IDvar = "idstud",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "asmmat", DisplayName = "asmmat", Mandatory = false},
                    new() { Regex = "asssci", DisplayName = "asssci", Mandatory = false},
                    new() { Regex = "asmalg", DisplayName = "asmalg", Mandatory = false},
                    new() { Regex = "asmdap", DisplayName = "asmdap", Mandatory = false},
                    new() { Regex = "asmfns", DisplayName = "asmfns", Mandatory = false},
                    new() { Regex = "asmgeo", DisplayName = "asmgeo", Mandatory = false},
                    new() { Regex = "asmmea", DisplayName = "asmmea", Mandatory = false},
                    new() { Regex = "asseas", DisplayName = "asseas", Mandatory = false},
                    new() { Regex = "asslis", DisplayName = "asslis", Mandatory = false},
                    new() { Regex = "assphy", DisplayName = "assphy", Mandatory = false},
                    new() { Regex = "asmapp", DisplayName = "asmapp", Mandatory = false},
                    new() { Regex = "asmkno", DisplayName = "asmkno", Mandatory = false},
                    new() { Regex = "asmrea", DisplayName = "asmrea", Mandatory = false},
                    new() { Regex = "asmibm", DisplayName = "asmibm", Mandatory = false},
                    new() { Regex = "assibm", DisplayName = "assibm", Mandatory = false},
                },
                FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
            },
            new DatasetType
            {
                Id = 212, Name = "TIMSS 2003 - 8th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2003 - 8th grade teacher data on student level",
                Weight = "tchwgt;matwgt;sciwgt", IDvar = "idstud",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "bsmmat", DisplayName = "bsmmat", Mandatory = false},
                    new() { Regex = "bsssci", DisplayName = "bsssci", Mandatory = false},
                    new() { Regex = "bsmalg", DisplayName = "bsmalg", Mandatory = false},
                    new() { Regex = "bsmdap", DisplayName = "bsmdap", Mandatory = false},
                    new() { Regex = "bsmfns", DisplayName = "bsmfns", Mandatory = false},
                    new() { Regex = "bsmgeo", DisplayName = "bsmgeo", Mandatory = false},
                    new() { Regex = "bsmmea", DisplayName = "bsmmea", Mandatory = false},
                    new() { Regex = "bsseas", DisplayName = "bsseas", Mandatory = false},
                    new() { Regex = "bsslis", DisplayName = "bsslis", Mandatory = false},
                    new() { Regex = "bssphy", DisplayName = "bssphy", Mandatory = false},
                    new() { Regex = "bssche", DisplayName = "bssche", Mandatory = false},
                    new() { Regex = "bsseri", DisplayName = "bsseri", Mandatory = false},
                    new() { Regex = "bsmapp", DisplayName = "bsmapp", Mandatory = false},
                    new() { Regex = "bsmkno", DisplayName = "bsmkno", Mandatory = false},
                    new() { Regex = "bsmrea", DisplayName = "bsmrea", Mandatory = false},
                    new() { Regex = "bsmibm", DisplayName = "bsmibm", Mandatory = false},
                    new() { Regex = "bssibm", DisplayName = "bssibm", Mandatory = false},
                },
                FayFac = 1, JKzone = "jkzone", JKrep = "jkrep", JKreverse = false,
            },
            new DatasetType
            {
                Id = 213, Name = "TIMSS 2007/2011 - 4th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2007/2011 - 4th grade teacher data on student level",
                Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = false},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = false},
                    new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Mandatory = false},
                    new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Mandatory = false},
                    new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Mandatory = false},
                    new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Mandatory = false},
                    new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Mandatory = false},
                    new() { Regex = "ASMREA", DisplayName = "ASMREA", Mandatory = false},
                    new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Mandatory = false},
                    new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Mandatory = false},
                    new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Mandatory = false},
                    new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Mandatory = false},
                    new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Mandatory = false},
                    new() { Regex = "ASSREA", DisplayName = "ASSREA", Mandatory = false},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = false},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = false},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 214, Name = "TIMSS 2007/2011 - 8th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS 2007/2011 - 8th grade teacher data on student level",
                Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Mandatory = false},
                    new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Mandatory = false},
                    new() { Regex = "BSMALG", DisplayName = "BSMALG", Mandatory = false},
                    new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Mandatory = false},
                    new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Mandatory = false},
                    new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Mandatory = false},
                    new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Mandatory = false},
                    new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Mandatory = false},
                    new() { Regex = "BSMREA", DisplayName = "BSMREA", Mandatory = false},
                    new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Mandatory = false},
                    new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Mandatory = false},
                    new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Mandatory = false},
                    new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Mandatory = false},
                    new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Mandatory = false},
                    new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Mandatory = false},
                    new() { Regex = "BSSREA", DisplayName = "BSSREA", Mandatory = false},
                    new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Mandatory = false},
                    new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Mandatory = false},
                },
                FayFac = 1, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = false,
            },
            new DatasetType
            {
                Id = 215, Name = "TIMSS since 2015 - 4th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 4th grade teacher data on student level",
                Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "ASMMAT", DisplayName = "ASMMAT", Mandatory = false},
                    new() { Regex = "ASSSCI", DisplayName = "ASSSCI", Mandatory = false},
                    new() { Regex = "ASMNUM", DisplayName = "ASMNUM", Mandatory = false},
                    new() { Regex = "ASMGEO", DisplayName = "ASMGEO", Mandatory = false},
                    new() { Regex = "ASMDAT", DisplayName = "ASMDAT", Mandatory = false},
                    new() { Regex = "ASMKNO", DisplayName = "ASMKNO", Mandatory = false},
                    new() { Regex = "ASMAPP", DisplayName = "ASMAPP", Mandatory = false},
                    new() { Regex = "ASMREA", DisplayName = "ASMREA", Mandatory = false},
                    new() { Regex = "ASSLIF", DisplayName = "ASSLIF", Mandatory = false},
                    new() { Regex = "ASSPHY", DisplayName = "ASSPHY", Mandatory = false},
                    new() { Regex = "ASSEAR", DisplayName = "ASSEAR", Mandatory = false},
                    new() { Regex = "ASSKNO", DisplayName = "ASSKNO", Mandatory = false},
                    new() { Regex = "ASSAPP", DisplayName = "ASSAPP", Mandatory = false},
                    new() { Regex = "ASSREA", DisplayName = "ASSREA", Mandatory = false},
                    new() { Regex = "ASSENV", DisplayName = "ASSENV", Mandatory = false},
                    new() { Regex = "ASMIBM", DisplayName = "ASMIBM", Mandatory = false},
                    new() { Regex = "ASSIBM", DisplayName = "ASSIBM", Mandatory = false},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 216, Name = "TIMSS since 2015 - 8th grade teacher data on student level", Group = "PIRLS/TIMSS", Description = "TIMSS since 2015 (reverse jackknife) - 8th grade teacher data on student level",
                Weight = "TCHWGT;MATWGT;SCIWGT", IDvar = "IDSTUD",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "BSMMAT", DisplayName = "BSMMAT", Mandatory = false},
                    new() { Regex = "BSSSCI", DisplayName = "BSSSCI", Mandatory = false},
                    new() { Regex = "BSMALG", DisplayName = "BSMALG", Mandatory = false},
                    new() { Regex = "BSMAPP", DisplayName = "BSMAPP", Mandatory = false},
                    new() { Regex = "BSMDAT", DisplayName = "BSMDAT", Mandatory = false},
                    new() { Regex = "BSMGEO", DisplayName = "BSMGEO", Mandatory = false},
                    new() { Regex = "BSMKNO", DisplayName = "BSMKNO", Mandatory = false},
                    new() { Regex = "BSMNUM", DisplayName = "BSMNUM", Mandatory = false},
                    new() { Regex = "BSMREA", DisplayName = "BSMREA", Mandatory = false},
                    new() { Regex = "BSSAPP", DisplayName = "BSSAPP", Mandatory = false},
                    new() { Regex = "BSSBIO", DisplayName = "BSSBIO", Mandatory = false},
                    new() { Regex = "BSSCHE", DisplayName = "BSSCHE", Mandatory = false},
                    new() { Regex = "BSSEAR", DisplayName = "BSSEAR", Mandatory = false},
                    new() { Regex = "BSSKNO", DisplayName = "BSSKNO", Mandatory = false},
                    new() { Regex = "BSSPHY", DisplayName = "BSSPHY", Mandatory = false},
                    new() { Regex = "BSSREA", DisplayName = "BSSREA", Mandatory = false},
                    new() { Regex = "BSSENV", DisplayName = "BSSENV", Mandatory = false},
                    new() { Regex = "BSMIBM", DisplayName = "BSMIBM", Mandatory = false},
                    new() { Regex = "BSSIBM", DisplayName = "BSSIBM", Mandatory = false},
                },
                FayFac = 0.5, JKzone = "JKZONE", JKrep = "JKREP", JKreverse = true,
            },
            new DatasetType
            {
                Id = 221, Name = "TIMSS 2003 - principal level", Group = "PIRLS/TIMSS", Description = "TIMSS 2003 - principal level",
                Weight = "schwgt;stotwgtu", IDvar = "idschool",
                NMI = 1, PVvarsList = new() { },
                FayFac = 1, JKzone = "jkczone", JKrep = "jkcrep", JKreverse = false,
            },
            new DatasetType
            {
                Id = 223, Name = "TIMSS 2007/2011 - principal level", Group = "PIRLS/TIMSS", Description = "TIMSS 2007/2011 - principal level",
                Weight = "SCHWGT;STOTWGTU", IDvar = "IDSCHOOL",
                NMI = 1, PVvarsList = new() { },
                FayFac = 1, JKzone = "JKCZONE", JKrep = "JKCREP", JKreverse = false,
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
                Id = 301, Name = "PISA 2003-2012 - student level", Group = "PISA", Description = "PISA 2003-2012 - student level",
                Weight = "W_FSTUWT",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "PV[0-9]+MATH", DisplayName = "PVMATH", Mandatory = true},
                    new() { Regex = "PV[0-9]+READ", DisplayName = "PVREAD", Mandatory = true},
                    new() { Regex = "PV[0-9]+SCIE", DisplayName = "PVSCIE", Mandatory = true},
                    new() { Regex = "PV[0-9]+MACC", DisplayName = "PVMACC", Mandatory = false},
                    new() { Regex = "PV[0-9]+MACQ", DisplayName = "PVMACQ", Mandatory = false},
                    new() { Regex = "PV[0-9]+MACS", DisplayName = "PVMACS", Mandatory = false},
                    new() { Regex = "PV[0-9]+MACU", DisplayName = "PVMACU", Mandatory = false},
                    new() { Regex = "PV[0-9]+MAPE", DisplayName = "PVMAPE", Mandatory = false},
                    new() { Regex = "PV[0-9]+MAPF", DisplayName = "PVMAPF", Mandatory = false},
                    new() { Regex = "PV[0-9]+MAPI", DisplayName = "PVMAPI", Mandatory = false},
                    new() { Regex = "PV[0-9]+READ1", DisplayName = "PVREAD1", Mandatory = false},
                    new() { Regex = "PV[0-9]+READ2", DisplayName = "PVREAD2", Mandatory = false},
                    new() { Regex = "PV[0-9]+READ3", DisplayName = "PVREAD3", Mandatory = false},
                    new() { Regex = "PV[0-9]+READ4", DisplayName = "PVREAD4", Mandatory = false},
                    new() { Regex = "PV[0-9]+READ5", DisplayName = "PVREAD5", Mandatory = false},
                    new() { Regex = "PV[0-9]+INTR", DisplayName = "PVINTR", Mandatory = false},
                    new() { Regex = "PV[0-9]+SUPP", DisplayName = "PVSUPP", Mandatory = false},
                    new() { Regex = "PV[0-9]+EPS", DisplayName = "PVEPS", Mandatory = false},
                    new() { Regex = "PV[0-9]+ISI", DisplayName = "PVISI", Mandatory = false},
                    new() { Regex = "PV[0-9]+USE", DisplayName = "PVUSE", Mandatory = false},
                    new() { Regex = "PV[0-9]+MATH1", DisplayName = "PVMATH1", Mandatory = false},
                    new() { Regex = "PV[0-9]+MATH2", DisplayName = "PVMATH2", Mandatory = false},
                    new() { Regex = "PV[0-9]+MATH3", DisplayName = "PVMATH3", Mandatory = false},
                    new() { Regex = "PV[0-9]+MATH4", DisplayName = "PVMATH4", Mandatory = false},
                    new() { Regex = "PV[0-9]+MATH5", DisplayName = "PVMATH5", Mandatory = false},
                    new() { Regex = "PV[0-9]+PROB", DisplayName = "PVPROB", Mandatory = false},
                },
                RepWgts = "W_FSTR", FayFac = 0.05, JKreverse = false,
            },
            new DatasetType
            {
                Id = 302, Name = "PISA since 2015 - student level", Group = "PISA", Description = "PISA since 2015 - student level",
                Weight = "W_FSTUWT", IDvar = "CNTSTUID",
                NMI = 10, PVvarsList = new() {
                    new() { Regex = "PV[0-9]+MATH", DisplayName = "PVMATH", Mandatory = true},
                    new() { Regex = "PV[0-9]+READ", DisplayName = "PVREAD", Mandatory = true},
                    new() { Regex = "PV[0-9]+SCIE", DisplayName = "PVSCIE", Mandatory = true},
                    new() { Regex = "PV[0-9]+GLCM", DisplayName = "PVGLCM", Mandatory = false},
                    new() { Regex = "PV[0-9]+RCLI", DisplayName = "PVRCLI", Mandatory = false},
                    new() { Regex = "PV[0-9]+RCUN", DisplayName = "PVRCUN", Mandatory = false},
                    new() { Regex = "PV[0-9]+RCER", DisplayName = "PVRCER", Mandatory = false},
                    new() { Regex = "PV[0-9]+RTSN", DisplayName = "PVRTSN", Mandatory = false},
                    new() { Regex = "PV[0-9]+RTML", DisplayName = "PVRTML", Mandatory = false},
                    new() { Regex = "PV[0-9]+SCEP", DisplayName = "PVSCEP", Mandatory = false},
                    new() { Regex = "PV[0-9]+SCED", DisplayName = "PVSCED", Mandatory = false},
                    new() { Regex = "PV[0-9]+SCID", DisplayName = "PVSCID", Mandatory = false},
                    new() { Regex = "PV[0-9]+SKCO", DisplayName = "PVSKCO", Mandatory = false},
                    new() { Regex = "PV[0-9]+SKPE", DisplayName = "PVSKPE", Mandatory = false},
                    new() { Regex = "PV[0-9]+SSPH", DisplayName = "PVSSPH", Mandatory = false},
                    new() { Regex = "PV[0-9]+SSLI", DisplayName = "PVSSLI", Mandatory = false},
                    new() { Regex = "PV[0-9]+SSES", DisplayName = "PVSSES", Mandatory = false},
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
                    new() { Regex = "PVLIT", DisplayName = "PVLIT", Mandatory = true},
                    new() { Regex = "PVNUM", DisplayName = "PVNUM", Mandatory = true},
                    new() { Regex = "PVPSL", DisplayName = "PVPSL", Mandatory = true},
                },
                RepWgts = "SPFWT[1-9][0-9]?", FayFac = 1, JKreverse = false,
            },
            new DatasetType
            {
                Id = 601, Name = "ICILS - student level", Group = "ICILS", Description = "ICILS since 2013 - student level",
                Weight = "TOTWGTS",
                NMI = 5, PVvarsList = new() {
                    new() { Regex = "PV[0-9]+CIL", DisplayName = "PVCIL", Mandatory = true},
                    new() { Regex = "PV[0-9]+CT", DisplayName = "PVCT", Mandatory = false},
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