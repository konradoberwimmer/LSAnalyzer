using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

[JsonDerivedType(typeof(VirtualVariableCombine), typeDiscriminator: "combine")]
public abstract partial class VirtualVariable : ObservableValidatorExtended, IChangeTracking
{
    public abstract string TypeName { get; }

    public int Id { get; set; } = 0;
    
    [ObservableProperty]
    [MinLength(3, ErrorMessage = "Name must have length of at least three characters!")]
    private string _name = string.Empty;
    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [ObservableProperty]
    private string _label = string.Empty;
    partial void OnLabelChanged(string value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [ObservableProperty]
    private string _forFileName = string.Empty;
    partial void OnForFileNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }

    [ObservableProperty] 
    private int? _forDatasetTypeId = null;    
    partial void OnForDatasetTypeIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [ObservableProperty] 
    private bool _fromPlausibleValues = false;    
    partial void OnFromPlausibleValuesChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [JsonIgnore]
    public abstract string Info { get; }

    public abstract VirtualVariable Clone();

    public abstract void AcceptChanges();

    [JsonIgnore]
    public abstract bool IsChanged { get; }
}