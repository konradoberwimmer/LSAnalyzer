using System;
using System.Reflection;
using System.Runtime.Loader;

namespace LSAnalyzerAvalonia;

public class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver? _resolver;

    public bool SetAssemblyDependencyResolverFromFullPath(string pluginPath)
    {
        try
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
            return true;
        }
        catch
        {
            return false;
        }
    } 

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_resolver == null) return null;
        
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        
        return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver == null) return IntPtr.Zero;
        
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        
        return libraryPath == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
    }
}