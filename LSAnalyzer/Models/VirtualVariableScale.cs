using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

public partial class VirtualVariableScale : VirtualVariable
{
    public override string TypeName => "Scale";
    
    public override bool FromPlausibleValues => InputVariable?.FromPlausibleValues ?? false;

    public override string Info
    {
        get
        {
            var additionalInfo = Type switch
            {
                ScaleType.Linear => $", mean = {Mean:#.##}, sd = {Sd:#.##}",
                ScaleType.Logarithmic => $", logbase = {LogBase:#.##}, center = {(Center ? "T" : "F")}",
                _ => throw new ArgumentOutOfRangeException()
            };
            return $"{Type.ToString().ToLower()}({InputVariable?.Name ?? string.Empty}{additionalInfo})";
        }
    }

    public enum ScaleType
    {
        Linear,
        Logarithmic,
        Dichotomization
    }
    
    [ObservableProperty]
    private ScaleType _type = ScaleType.Linear;
    partial void OnTypeChanged(ScaleType value)
    {
        OnPropertyChanged(nameof(MeanMakesSense));
        OnPropertyChanged(nameof(SdMakesSense));
        OnPropertyChanged(nameof(LogBaseMakesSense));
        OnPropertyChanged(nameof(CenterMakesSense));
    }

    [ObservableProperty] 
    private double _mean = 0.0;

    [JsonIgnore]
    public bool MeanMakesSense => Type == ScaleType.Linear;
    
    [ObservableProperty]
    private double _sd = 1.0;

    [JsonIgnore]
    public bool SdMakesSense => Type == ScaleType.Linear;

    [ObservableProperty] 
    private double _logBase = 10.0;
    
    [JsonIgnore]
    public bool LogBaseMakesSense => Type == ScaleType.Logarithmic;
    
    [ObservableProperty]
    private bool _center = false;

    [JsonIgnore] 
    public bool CenterMakesSense => Type == ScaleType.Logarithmic;
    
    [ObservableProperty] 
    private Variable? _inputVariable;

    [ObservableProperty] 
    private Variable? _weightVariable;

    [ObservableProperty] 
    private Variable? _miVariable;
    
    public override VirtualVariable Clone()
    {
        return new VirtualVariableScale()
        {
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Type = Type,
            Mean = Mean,
            Sd = Sd,
            LogBase = LogBase,
            Center = Center,
            InputVariable = InputVariable?.Clone(),
            WeightVariable = WeightVariable?.Clone(),
            MiVariable = MiVariable?.Clone(),
        };
    }

    private VirtualVariableScale? _savedState;
    
    public override void AcceptChanges()
    {
        _savedState = new VirtualVariableScale()
        {
            Id = Id,
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Type = Type,
            Mean = Mean,
            Sd = Sd,
            LogBase = LogBase,
            Center = Center,
            InputVariable = InputVariable,
            WeightVariable = WeightVariable,
            MiVariable = MiVariable,
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
            
            return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, [ "Info", "IsChanged", "Errors", "InputVariable" ]) ||
                   (InputVariable == null) != (_savedState.InputVariable == null) ||
                   (InputVariable != null && !ObjectTools.PublicInstancePropertiesEqual(InputVariable!, _savedState.InputVariable!, [])) ||
                   (WeightVariable == null) != (_savedState.WeightVariable == null) ||
                   (WeightVariable != null && !ObjectTools.PublicInstancePropertiesEqual(WeightVariable!, _savedState.WeightVariable!, [])) ||
                   (MiVariable == null) != (_savedState.MiVariable == null) ||
                   (MiVariable != null && !ObjectTools.PublicInstancePropertiesEqual(MiVariable!, _savedState.MiVariable!, []));
        }
    }
}