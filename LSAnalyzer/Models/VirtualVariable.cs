using System.Text.Json.Serialization;

namespace LSAnalyzer.Models;

[JsonDerivedType(typeof(VirtualVariableCombine), typeDiscriminator: "combine")]
public abstract class VirtualVariable
{
    public abstract string TypeName { get; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Label { get; set; } = string.Empty;
    
    public string ForFileName { get; set; } = string.Empty;
    
    public int? ForDatasetTypeId { get; set; }
    
    public bool FromPlausibleValues { get; set; } = false;

    public abstract VirtualVariable Clone();
}