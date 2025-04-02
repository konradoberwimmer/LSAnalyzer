using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IPluginCommons
{
    public static virtual PluginTypes PluginType => PluginTypes.Undefined;
    
    public string DllName { get; }
    
    public Version Version { get; }
    
    public string ClassName { get; }
    
    public string Description { get; }
    
    public string DisplayName { get; }

    public enum PluginTypes
    {
        Undefined,
        
        [Display(Name="Data reader")]
        DataReader,
        
        [Display(Name="Data reader")]
        DataProvider,
    }

    public record Manifest
    {
        [JsonPropertyName("dll")]
        public required string Dll { get; set; }
    }
}