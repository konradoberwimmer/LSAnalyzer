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
    
    private readonly List<(string location, IDataReaderPlugin plugin)> _dataReaderPlugins = [];

    public ImmutableList<IDataReaderPlugin> DataReaderPlugins => _dataReaderPlugins.Select(t => t.plugin).ToImmutableList();
    
    private readonly List<(string location, IDataProviderPlugin plugin)> _dataProviderPlugins = [];
    
    public ImmutableList<IDataProviderPlugin> DataProviderPlugins => _dataProviderPlugins.Select(t => t.plugin).ToImmutableList();

    public Plugins(IAppConfiguration appConfiguration)
    {
        _appConfiguration = appConfiguration;

        foreach (var preservedPluginLocation in _appConfiguration.PreservedPluginLocations)
        {
            var (validity, manifest, plugin) = IsValidPluginExtracted(new DirectoryInfo(preservedPluginLocation));
            
            if (validity != IPlugins.Validity.Valid) continue;

            // the following is null-safe because of validity check above
            
            switch (manifest!.Type)
            {
                case IPluginCommons.PluginTypes.DataReader:
                    _dataReaderPlugins.Add((location: preservedPluginLocation, plugin: (plugin as IDataReaderPlugin)!));
                    break;
                case IPluginCommons.PluginTypes.DataProvider:
                    _dataProviderPlugins.Add((location: preservedPluginLocation, plugin: (plugin as IDataProviderPlugin)!));
                    break;
            }
        }
    }
    
    public (IPlugins.Validity, IPluginCommons.Manifest?) IsValidPluginZip(string pluginPath)
    {
        if (!File.Exists(pluginPath)) return (IPlugins.Validity.FileNotFound, null);
        
        var tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            ZipFile.ExtractToDirectory(pluginPath, tempDirectory.FullName);
        }
        catch
        {
            tempDirectory.Delete(true);
            return (IPlugins.Validity.FileNotZip, null);
        }

        var (validity, manifest, _) = IsValidPluginExtracted(tempDirectory);
        
        tempDirectory.Delete(true);
        return (validity, manifest);
    }

    public (IPlugins.Validity, IPluginCommons.Manifest?, IPluginCommons?) IsValidPluginExtracted(DirectoryInfo pluginDirectory)
    { 
        string manifestSource;
        try
        {
            manifestSource = File.ReadAllText(Path.Combine(pluginDirectory.FullName, "manifest.json"));
        } catch
        {
            return (IPlugins.Validity.ManifestNotFound, null, null);
        }

        IPluginCommons.Manifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<IPluginCommons.Manifest>(manifestSource)!;
        } catch
        {
            return (IPlugins.Validity.ManifestCorrupt, null, null);
        }

        if (!File.Exists(Path.Combine(pluginDirectory.FullName, manifest.Dll)))
        {
            return (IPlugins.Validity.DllNotFound, manifest, null);
        }

        Assembly assembly;
        try
        {
            assembly = LoadPlugin(Path.Combine(pluginDirectory.FullName, manifest.Dll))!;
        } catch
        {
            return (IPlugins.Validity.AssemblyInaccessible, manifest, null);
        }
        
        try
        {
            IPluginCommons? plugin = null;
            switch (manifest.Type)
            {
                case IPluginCommons.PluginTypes.Undefined:
                    return (IPlugins.Validity.PluginTypeUndefined, manifest, null);
                case IPluginCommons.PluginTypes.DataReader:
                    plugin = CreatePlugin<IDataReaderPlugin>(assembly);
                    break;
                case IPluginCommons.PluginTypes.DataProvider:
                    plugin = CreatePlugin<IDataProviderPlugin>(assembly);
                    break;
            }
            
            if (plugin == null) throw new Exception("Failed to create plugin");
        
            return (IPlugins.Validity.Valid, manifest, plugin);
        } catch
        {
            return (IPlugins.Validity.PluginNotCreatable, manifest, null);
        }
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
                _dataReaderPlugins.Add((location, plugin: (IDataReaderPlugin)plugin));
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                _dataProviderPlugins.Add((location, (IDataProviderPlugin)plugin));
                break;
        }

        _appConfiguration.StorePreservedPluginLocation(location);
    }

    public void RemovePlugin(IPluginCommons plugin)
    {
        string preservedPluginLocation;
        
        switch (plugin.PluginType)
        {
            case IPluginCommons.PluginTypes.DataReader:
                preservedPluginLocation = _dataReaderPlugins.FirstOrDefault(t => t.plugin == (IDataReaderPlugin)plugin).location ?? string.Empty;
                _dataReaderPlugins.RemoveAll(t => t.plugin == (IDataReaderPlugin)plugin);
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                preservedPluginLocation = _dataProviderPlugins.FirstOrDefault(t => t.plugin == (IDataProviderPlugin)plugin).location ?? string.Empty;
                _dataProviderPlugins.RemoveAll(t => t.plugin == (IDataProviderPlugin)plugin);
                break;
            case IPluginCommons.PluginTypes.Undefined:
            default:
                return;
        }
        
        _appConfiguration.RemovePreservedPluginLocation(preservedPluginLocation);

        try
        {
            Directory.Delete(preservedPluginLocation, true);
        } catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}