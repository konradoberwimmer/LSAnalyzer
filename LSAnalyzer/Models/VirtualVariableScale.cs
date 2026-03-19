using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
                ScaleType.Linear => $", mean = {Mean.ToString("0.##", CultureInfo.InvariantCulture)}, sd = {Sd.ToString("0.##", CultureInfo.InvariantCulture)}",
                ScaleType.Logarithmic => $", logbase = {LogBase.ToString("0.##", CultureInfo.InvariantCulture)}, center = {(Center ? "T" : "F")}",
                _ => throw new ArgumentOutOfRangeException()
            };
            return $"{Type.ToString().ToLower()}({InputVariable?.Name ?? string.Empty}{additionalInfo})";
        }
    }

    public enum ScaleType
    {
        Linear,
        Logarithmic
    }
    
    [ObservableProperty]
    private ScaleType _type = ScaleType.Linear;
    partial void OnTypeChanged(ScaleType value)
    {
        OnPropertyChanged(nameof(IsChanged));
        
        OnPropertyChanged(nameof(MeanMakesSense));
        OnPropertyChanged(nameof(SdMakesSense));
        OnPropertyChanged(nameof(LogBaseMakesSense));
        OnPropertyChanged(nameof(CenterMakesSense));
    }

    [ObservableProperty] 
    private double _mean = 0.0;
    partial void OnMeanChanged(double value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore]
    public bool MeanMakesSense => Type == ScaleType.Linear;
    
    [ObservableProperty]
    private double _sd = 1.0;
    partial void OnSdChanged(double value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore]
    public bool SdMakesSense => Type == ScaleType.Linear;

    [ObservableProperty] 
    [Range(1e-10, double.MaxValue)]
    private double _logBase = 10.0;
    partial void OnLogBaseChanged(double value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [JsonIgnore]
    public bool LogBaseMakesSense => Type == ScaleType.Logarithmic;
    
    [ObservableProperty]
    private bool _center = false;
    partial void OnCenterChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore] 
    public bool CenterMakesSense => Type == ScaleType.Logarithmic;
    
    [ObservableProperty]
    [Required]
    private Variable? _inputVariable;
    partial void OnInputVariableChanged(Variable? value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

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