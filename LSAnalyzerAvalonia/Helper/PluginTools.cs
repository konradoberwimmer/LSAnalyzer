using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Helper;

public static class PluginTools
{
    private static DirectoryInfo? _tempDirectory;
    
    public static Validity IsValidPlugin(string pluginPath, IPluginCommons.PluginTypes pluginType)
    {
        if (!File.Exists(pluginPath)) return Validity.FileNotFound;
        
        _tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            ZipFile.ExtractToDirectory(pluginPath, _tempDirectory.FullName);
        }
        catch
        {
            CleanUp();
            return Validity.FileNotZip;
        }

        string manifestSource;
        try
        {
            manifestSource = File.ReadAllText(Path.Combine(_tempDirectory.FullName, "manifest.json"));
        } catch
        {
            CleanUp();
            return Validity.ManifestNotFound;
        }

        IPluginCommons.Manifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<IPluginCommons.Manifest>(manifestSource)!;
        }
        catch
        {
            CleanUp();
            return Validity.ManifestCorrupt;
        }

        if (!File.Exists(Path.Combine(_tempDirectory.FullName, manifest.Dll)))
        {
            CleanUp();
            return Validity.DllNotFound;
        }

        Assembly assembly;
        try
        {
            assembly = LoadPlugin(Path.Combine(_tempDirectory.FullName, manifest.Dll))!;
        }
        catch
        {
            CleanUp();
            return Validity.AssemblyInaccessible;
        }
        
        try
        {
            IPluginCommons? plugin;
            switch (pluginType)
            {
                case IPluginCommons.PluginTypes.Undefined:
                    CleanUp();
                    return Validity.PluginTypeUndefined;
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
            CleanUp();
            return Validity.PluginNotCreatable;
        }
        
        CleanUp();
        return Validity.Valid;
    }
    
    public static Assembly? LoadPlugin(string fullPath)
    {
        PluginLoadContext pluginLoadContext = new();

        if (!pluginLoadContext.SetAssemblyDependencyResolverFromFullPath(fullPath)) return null;
        
        return pluginLoadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(fullPath))) ;
    }

    public static T? CreatePlugin<T>(Assembly assembly) where T : class, IPluginCommons
    {
        var possibleType = assembly.GetTypes().FirstOrDefault(t => typeof(T).IsAssignableFrom(t));
        
        return possibleType is null ? null : Activator.CreateInstance(possibleType) as T;
    }

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

    private static void CleanUp()
    {
        _tempDirectory?.Delete(true);
    }
}