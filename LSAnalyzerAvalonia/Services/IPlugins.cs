using System.Collections.Generic;
using System.Collections.Immutable;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Services;

public interface IPlugins
{
    public ImmutableList<IDataReaderPlugin> DataReaderPlugins { get; }
    
    public ImmutableList<IDataProviderPlugin> DataProviderPlugins { get; }
    
    public void AddPlugin(IPluginCommons plugin, string location);
    
    public void RemovePlugin(IPluginCommons plugin);
}