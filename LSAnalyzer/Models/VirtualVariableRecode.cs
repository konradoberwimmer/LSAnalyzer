using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

public partial class VirtualVariableRecode : VirtualVariable
{
    public enum ElseAction
    {
        Copy,
        Missing,
        Set,
    }
    
    public override string TypeName => "Recode";

    [ObservableProperty] 
    private ObservableCollection<Variable> _variables;
    partial void OnVariablesChanged(ObservableCollection<Variable> value)
    {
        Variables.CollectionChanged += delegate
        {
            if (Variables.Count != 1 && Else == ElseAction.Copy)
            {
                Else = ElseAction.Missing;
            }
            
            OnPropertyChanged(nameof(CanAddRule));
            OnPropertyChanged(nameof(CannotAddRule));
            OnPropertyChanged(nameof(ElseValueMakesSense));
            OnPropertyChanged(nameof(IsChanged)); 
        };
        OnPropertyChanged(nameof(CanAddRule));
        OnPropertyChanged(nameof(CannotAddRule));
        OnPropertyChanged(nameof(ElseValueMakesSense));
        OnPropertyChanged(nameof(IsChanged)); 
    }
    
    [ObservableProperty]
    private ItemsChangeObservableCollection<Rule> _rules;
    partial void OnRulesChanged(ItemsChangeObservableCollection<Rule> value)
    {
        Rules.CollectionChanged += delegate
        {
            OnPropertyChanged(nameof(IsChanged)); 
        };
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore] 
    public bool CanAddRule => Variables.Count > 0;
    
    [JsonIgnore] 
    public bool CannotAddRule => Variables.Count == 0;
    
    [ObservableProperty]
    private ElseAction _else = ElseAction.Missing;
    partial void OnElseChanged(ElseAction value)
    {
        if (value == ElseAction.Copy && Variables.Count != 1)
        {
            Else = ElseAction.Missing;
        }
        
        OnPropertyChanged(nameof(ElseValueMakesSense));
        OnPropertyChanged(nameof(IsChanged));
    }

    [ObservableProperty]
    private double _elseValue = 0.0;

    [JsonIgnore] 
    public bool ElseValueMakesSense => Else == ElseAction.Set;
    
    public override bool FromPlausibleValues => Variables.Any(var => var.FromPlausibleValues);

    public override string Info
    {
        get
        {
            var variables = Variables.Count switch
            {
                0 => string.Empty,
                1 => Variables.FirstOrDefault()?.Name + ", ",
                >= 2 => $"[{string.Join(",", Variables.Select(var => var.Name).ToList())}], ",
                _ => throw new ArgumentOutOfRangeException()
            };

            var elseAction = Else switch
            {
                ElseAction.Copy => "else=copy",
                ElseAction.Missing => "else=NA",
                ElseAction.Set => $"else={ElseValue.ToString(CultureInfo.InvariantCulture)}",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var recodes = string.Join(";", Rules.Select(rule => rule.Info).ToList().Append(elseAction));
            
            return $"recode({variables}'{recodes}')";
        }
    } 

    private VirtualVariableRecode? _savedState = null;

    public VirtualVariableRecode()
    {
        Variables = [];
        Rules = [];
    }
    
    public override VirtualVariable Clone()
    {
        return new VirtualVariableRecode
        {
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Variables = [..Variables.Select(variable => variable.Clone()).ToList()],
            Rules = [..Rules.Select(rule => (rule.Clone() as Rule)!).ToList()],
            Else = Else,
        };
    }

    public override void AcceptChanges()
    {
        _savedState = new VirtualVariableRecode
        {
            Id = Id,
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Variables = [..Variables.Select(variable => variable.Clone()).ToList()],
            Rules = [..Rules.Select(rule => (rule.Clone() as Rule)!).ToList()],
            Else = Else,
        };
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore]
    public override bool IsChanged
    {
        get
        {
            OnPropertyChanged(nameof(Info));

            if (_savedState is null) return true;

            return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, ["IsChanged", "Errors", "Variables", "Rules"]) ||
                   !Variables.ElementObjectsEqual(_savedState.Variables) ||
                   !Rules.ElementObjectsEqual(_savedState.Rules, ["Errors", "Criteria"]) ||
                   !Rules.Index().All(tupleRule => tupleRule.Item.Criteria.ElementObjectsEqual(_savedState.Rules[tupleRule.Index].Criteria, ["Errors"]));
        }
    }

    public void AddVariable(Variable variable)
    {
        foreach (var rule in Rules)
        {
            rule.Criteria.Add(new Term { VariableIndex = Variables.Count });
        }
        
        Variables.Add(variable);
    }

    public void RemoveLastVariable()
    {
        if (Variables.Count == 0) return;

        foreach (var rule in Rules)
        {
            rule.Criteria.Remove(rule.Criteria.Last());
        }
        
        Variables.Remove(Variables.Last());

        if (Variables.Count == 0)
        {
            Rules.Clear();
        }
    }

    public void AddRule()
    {
        if (Variables.Count == 0) return;
        
        Rule rule = new();

        for (var i = 0; i < Variables.Count; i++)
        {
            rule.Criteria.Add(new Term { VariableIndex = i });
        }
        
        Rules.Add(rule);
    }
    
    public bool ValidateDeep()
    {
        var validVirtualVariable = Validate();

        var validRules = true;
        foreach (var rule in Rules)
        {
            validRules = validRules && rule.ValidateDeep();
        }

        return validVirtualVariable && validRules;
    }

    public partial class Term : ObservableValidatorExtended, ICloneable
    {
        public enum TermType
        {
            Missing,
            Exactly,
            Between,
            AtLeast,
            AtMost,
        }
        
        public int VariableIndex { get; set; } = -1;
        
        [ObservableProperty]
        private TermType _type = TermType.Exactly;
        partial void OnTypeChanged(TermType value)
        {
            OnPropertyChanged(nameof(ValueMakesSense));
            OnPropertyChanged(nameof(MaxValueMakesSense));
        }

        [ObservableProperty] 
        private double _value = 0.0;

        [JsonIgnore]
        public bool ValueMakesSense => Type != TermType.Missing && Type != TermType.AtMost;

        [ObservableProperty] 
        private double _maxValue = 1.0;
        
        [JsonIgnore]
        public bool MaxValueMakesSense => Type is TermType.Between or TermType.AtMost;

        [JsonIgnore]
        public string Info
        {
            get
            {
                return Type switch
                {
                    TermType.Missing => "NA",
                    TermType.Exactly => Value.ToString("0.##", CultureInfo.InvariantCulture),
                    TermType.Between => $"{Value.ToString("0.##", CultureInfo.InvariantCulture)}-{MaxValue.ToString("0.##", CultureInfo.InvariantCulture)}",
                    TermType.AtLeast => $"\u2265{Value.ToString("0.##", CultureInfo.InvariantCulture)}",
                    TermType.AtMost => $"\u2264{MaxValue.ToString("0.##", CultureInfo.InvariantCulture)}",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public object Clone()
        {
            return new Term
            {
                VariableIndex = VariableIndex,
                Type = Type,
                Value = Value,
                MaxValue = MaxValue,
            };
        }
    }
    
    public partial class Rule : ObservableValidatorExtended, ICloneable
    {
        [ObservableProperty] 
        [MinLength(1)]
        private ItemsChangeObservableCollection<Term> _criteria = [];
        partial void OnCriteriaChanged(ItemsChangeObservableCollection<Term> value)
        {
            Criteria.CollectionChanged += delegate
            {
                OnPropertyChanged(nameof(IsChanged)); 
            };
            OnPropertyChanged(nameof(IsChanged));
        }

        [ObservableProperty] 
        private bool _resultNa = false;
        partial void OnResultNaChanged(bool value)
        {
            OnPropertyChanged(nameof(ResultValueMakesSense));
        }

        [ObservableProperty] 
        private double _resultValue = 0.0;

        [JsonIgnore]
        public bool ResultValueMakesSense => !ResultNa;

        [JsonIgnore]
        public string Info
        {
            get
            {
                var criteria = Criteria.Count <= 1 ? Criteria.FirstOrDefault()?.Info : $"[{string.Join(",", Criteria.Select(crit => crit.Info).ToList())}]";

                var result = $"{(ResultNa ? "NA" : ResultValue.ToString("0.##", CultureInfo.InvariantCulture))}";

                return $"{criteria}={result}";
            }
        }
        
        public object Clone()
        {
            return new Rule
            {
                Criteria = [..Criteria.Select(criterion => (criterion.Clone() as Term)!).ToList()],
                ResultNa = ResultNa,
                ResultValue = ResultValue,
            };
        }

        public bool ValidateDeep()
        {
            var validRule = Validate();

            var validCriteria = true;
            foreach (var criterion in Criteria)
            {
                validRule = validRule && criterion.Validate();
            }
            
            return validRule && validCriteria;
        }
    }
}