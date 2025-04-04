using System.Reflection;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerDataProviderDataverse;

public class DataProviderDataverse : IDataProviderPlugin
{
    public IPluginCommons.PluginTypes PluginType => IPluginCommons.PluginTypes.DataProvider;    
    
    public string DllName => "LSAnalyzerDataProviderDataverse.dll";
    
    public Version Version => Assembly.GetAssembly(typeof(DataProviderDataverse))!.GetName().Version ?? new Version(0, 0, 0);
    
    public string ClassName => GetType().Name;
    
    public string Description => "Fetch data from a (Harvard-like) dataverse";
    
    public string DisplayName => "Dataverse (Harvard-like)";
}