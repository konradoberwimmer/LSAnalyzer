using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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

            var assembly = LoadPlugin(Path.Combine(preservedPluginLocation, manifest.Dll));
            
            if (assembly == null) continue;

            switch (manifest.Type)
            {
                case IPluginCommons.PluginTypes.DataReader:
                    var dataReaderPlugin = CreatePlugin<IDataReaderPlugin>(assembly);
                    if (dataReaderPlugin == null) continue;
                    _dataReaderPlugins.Add(dataReaderPlugin);
                    break;
                case IPluginCommons.PluginTypes.DataProvider:
                    var dataProviderPlugin = CreatePlugin<IDataProviderPlugin>(assembly);
                    if (dataProviderPlugin == null) continue;
                    _dataProviderPlugins.Add(dataProviderPlugin);
                    break;
            }
            
        }
    }
    
    public (IPlugins.Validity, IPluginCommons.Manifest?) IsValidPlugin(string pluginPath)
    {
        if (!File.Exists(pluginPath)) return (IPlugins.Validity.FileNotFound, null);
        
        var tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            ZipFile.ExtractToDirectory(pluginPath, tempDirectory.FullName);
        }
        catch
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.FileNotZip, null);
        }

        string manifestSource;
        try
        {
            manifestSource = File.ReadAllText(Path.Combine(tempDirectory.FullName, "manifest.json"));
        } catch
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.ManifestNotFound, null);
        }

        IPluginCommons.Manifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<IPluginCommons.Manifest>(manifestSource)!;
        }
        catch
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.ManifestCorrupt, null);
        }

        if (!File.Exists(Path.Combine(tempDirectory.FullName, manifest.Dll)))
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.DllNotFound, manifest);
        }

        Assembly assembly;
        try
        {
            assembly = LoadPlugin(Path.Combine(tempDirectory.FullName, manifest.Dll))!;
        }
        catch
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.AssemblyInaccessible, manifest);
        }
        
        try
        {
            IPluginCommons? plugin;
            switch (manifest.Type)
            {
                case IPluginCommons.PluginTypes.Undefined:
                    tempDirectory?.Delete(true);
                    return (IPlugins.Validity.PluginTypeUndefined, manifest);
                case IPluginCommons.PluginTypes.DataReader:
                    plugin = CreatePlugin<IDataReaderPlugin>(assembly);
                    if (plugin == null) throw new Exception("Failed to create plugin");
                    break;
                case IPluginCommons.PluginTypes.DataProvider:
                    plugin = CreatePlugin<IDataProviderPlugin>(assembly);
                    if (plugin == null) throw new Exception("Failed to create plugin");
                    break;
            }
        } catch
        {
            tempDirectory?.Delete(true);
            return (IPlugins.Validity.PluginNotCreatable, manifest);
        }
        
        tempDirectory?.Delete(true);
        return (IPlugins.Validity.Valid, manifest);
    }
    
    public Assembly? LoadPlugin(string fullPath)
    {
        PluginLoadContext pluginLoadContext = new();

        if (!pluginLoadContext.SetAssemblyDependencyResolverFromFullPath(fullPath)) return null;
        
        return pluginLoadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(fullPath))) ;
    }

    public T? CreatePlugin<T>(Assembly assembly) where T : class, IPluginCommons
    {
        var possibleType = assembly.GetTypes().FirstOrDefault(t => typeof(T).IsAssignableFrom(t));
        
        return possibleType is null ? null : Activator.CreateInstance(possibleType) as T;
    }

    public string? PreservePlugin(string pluginPath)
    {
        if (!File.Exists(pluginPath)) return null;
        
        try
        {
            var destination = Directory.CreateDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "lsanalyzer_plugins", 
                Path.GetRandomFileName()
            ));
            ZipFile.ExtractToDirectory(pluginPath, destination.FullName);
            return destination.FullName;
        } catch
        {
            return null;
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