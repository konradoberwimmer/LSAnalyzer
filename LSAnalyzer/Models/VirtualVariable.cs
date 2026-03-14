using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

[JsonDerivedType(typeof(VirtualVariableCombine), typeDiscriminator: "combine")]
[JsonDerivedType(typeof(VirtualVariableScale), typeDiscriminator: "scale")]
public abstract partial class VirtualVariable : ObservableValidatorExtended, IChangeTracking
{
    public abstract string TypeName { get; }

    public int Id { get; set; } = 0;
    
    [ObservableProperty]
    [Required]
    [RegularExpression("[a-zA-Z][a-zA-Z0-9_]{2,}", ErrorMessage = "Name must start with a letter and consist of letters, digits and underscores (at least 3)!")]
    private string _name = string.Empty;
    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    
    [ObservableProperty]
    [RegularExpression("^[^\"'`´]*$", ErrorMessage = "The label must not contain any kind of quote character.")]
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
    
    public abstract bool FromPlausibleValues { get; }    
    
    [JsonIgnore]
    public abstract string Info { get; }

    public abstract VirtualVariable Clone();

    public abstract void AcceptChanges();

    [JsonIgnore]
    public abstract bool IsChanged { get; }
}