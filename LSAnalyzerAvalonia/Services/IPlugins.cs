using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Services;

public interface IPlugins
{
    public ImmutableList<IDataReaderPlugin> DataReaderPlugins { get; }
    
    public ImmutableList<IDataProviderPlugin> DataProviderPlugins { get; }

    public (Validity, IPluginCommons.Manifest?) IsValidPlugin(string pluginPath);

    public Assembly? LoadPlugin(string fullPath);

    public T? CreatePlugin<T>(Assembly assembly) where T : class, IPluginCommons;

    public string? PreservePlugin(string pluginPath);
    
    public void AddPlugin(IPluginCommons plugin, string location);
    
    public void RemovePlugin(IPluginCommons plugin);

    public enum Validity
    {
        Valid,
        FileNotFound,
        FileNotZip,
        ManifestNotFound,
        ManifestCorrupt,
        DllNotFound,
        AssemblyInaccessible,
        PluginTypeUndefined,
        PluginNotCreatable,
    }
}