using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using LSAnalyzerAvalonia.Helper;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Services;

public class Plugins : IPlugins
{
    private readonly IAppConfiguration _appConfiguration;
    
    private readonly List<IDataReaderPlugin> _dataReaderPlugins = [];

    public ImmutableList<IDataReaderPlugin> DataReaderPlugins => _dataReaderPlugins.ToImmutableList();
    
    private readonly List<IDataProviderPlugin> _dataProviderPlugins = [];
    
    public ImmutableList<IDataProviderPlugin> DataProviderPlugins => _dataProviderPlugins.ToImmutableList();

    public Plugins(IAppConfiguration appConfiguration)
    {
        _appConfiguration = appConfiguration;

        foreach (var preservedPluginLocation in _appConfiguration.PreservedPluginLocations)
        {
            if (!File.Exists(Path.Combine(preservedPluginLocation, "manifest.json"))) continue;

            IPluginCommons.Manifest? manifest = null;
            try
            {
                var manifestJson = File.ReadAllText(Path.Combine(preservedPluginLocation, "manifest.json"));
                manifest = JsonSerializer.Deserialize<IPluginCommons.Manifest>(manifestJson);
            } catch
            {
                continue;
            }

            var assembly = PluginTools.LoadPlugin(Path.Combine(preservedPluginLocation, manifest.Dll));
            
            if (assembly == null) continue;

            switch (manifest.Type)
            {
                case IPluginCommons.PluginTypes.DataReader:
                    var dataReaderPlugin = PluginTools.CreatePlugin<IDataReaderPlugin>(assembly);
                    if (dataReaderPlugin == null) continue;
                    _dataReaderPlugins.Add(dataReaderPlugin);
                    break;
                case IPluginCommons.PluginTypes.DataProvider:
                    var dataProviderPlugin = PluginTools.CreatePlugin<IDataProviderPlugin>(assembly);
                    if (dataProviderPlugin == null) continue;
                    _dataProviderPlugins.Add(dataProviderPlugin);
                    break;
            }
            
        }
    }
    
    public void AddPlugin(IPluginCommons plugin, string location)
    {
        switch (plugin.PluginType)
        {
            case IPluginCommons.PluginTypes.DataReader:
                _dataReaderPlugins.Add((IDataReaderPlugin)plugin);
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                _dataProviderPlugins.Add((IDataProviderPlugin)plugin);
                break;
        }

        _appConfiguration.StorePreservedPluginLocation(location);
    }

    public void RemovePlugin(IPluginCommons plugin)
    {
        switch (plugin.PluginType)
        {
            case IPluginCommons.PluginTypes.DataReader:
                _dataReaderPlugins.Remove((IDataReaderPlugin)plugin);
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                _dataProviderPlugins.Remove((IDataProviderPlugin)plugin);
                break;
        }
    }
}