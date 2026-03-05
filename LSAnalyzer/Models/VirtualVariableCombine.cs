
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

public partial class VirtualVariableCombine : VirtualVariable
{
    public override string TypeName => "Combine";

    public enum CombinationFunction
    {
        Sum,
        Mean,
        FactorScores,
    }
    
    [ObservableProperty]
    private CombinationFunction _type = CombinationFunction.Mean;
    partial void OnTypeChanged(CombinationFunction value)
    {
        OnPropertyChanged(nameof(NaRemovalMakesSense));
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore] 
    public bool NaRemovalMakesSense => Type != CombinationFunction.FactorScores;
    
    [ObservableProperty]
    private bool _removeNa = true;
    partial void OnRemoveNaChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

    [ObservableProperty]
    [MinLength(1)]
    private ItemsChangeObservableCollection<Variable> _variables = [];
    partial void OnVariablesChanged(ItemsChangeObservableCollection<Variable> value)
    {
        Variables.CollectionChanged += delegate (object? sender, NotifyCollectionChangedEventArgs args)
        {
            OnPropertyChanged(nameof(FromPlausibleValues));
            OnPropertyChanged(nameof(IsChanged)); 
        };
        OnPropertyChanged(nameof(FromPlausibleValues));
        OnPropertyChanged(nameof(IsChanged));
    }

    public override bool FromPlausibleValues => Variables.Any(variable => variable.FromPlausibleValues);

    public override string Info => $"{Type.ToString().ToLower()}({string.Join(", ", Variables.Select(variable => variable.Name))}, rmNA = {(RemoveNa ? "T" : "F")})";

    private VirtualVariableCombine? _savedState;
    [JsonIgnore]
    public override bool IsChanged 
    {
        get
        {
            OnPropertyChanged(nameof(Info));
            
            if (_savedState is null) return true;
            
            return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, [ "Info", "IsChanged", "Errors", "Variables" ]) ||
                   !Variables.ElementObjectsEqual(_savedState.Variables);
        }
    }
    
    public override VirtualVariable Clone()
    {
        ItemsChangeObservableCollection<Variable> variables = [];

        foreach (var variable in Variables)
        {
            variables.Add(variable.Clone());
        }
        
        return new VirtualVariableCombine
        {
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Type = Type,
            RemoveNa = RemoveNa,
            Variables = variables,
        };
    }

    public override void AcceptChanges()
    {
        _savedState = new VirtualVariableCombine
        {
            Id = Id,
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Type = Type,
            RemoveNa = RemoveNa,
            Variables = [..Variables],
        };
        OnPropertyChanged(nameof(IsChanged));
    }
}