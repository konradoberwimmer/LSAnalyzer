using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IPluginCommons
{
    public PluginTypes PluginType { get; }
    
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
        
        [Display(Name="Data provider")]
        DataProvider,
    }

    public record Manifest
    {
        [JsonPropertyName("dll")]
        public required string Dll { get; init; }
        
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required PluginTypes Type { get; init; }
    }
}