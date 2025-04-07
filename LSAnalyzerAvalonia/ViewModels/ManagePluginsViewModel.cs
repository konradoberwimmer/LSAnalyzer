using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class ManagePluginsViewModel : ViewModelBase
{
    private Services.IPlugins _pluginService = null!;
    
    [ObservableProperty] private ObservableCollection<IPluginCommons> _plugins = [];
    
    [ExcludeFromCodeCoverage]
    public ManagePluginsViewModel() // design-time only parameterless constructor
    {
        
    }

    public ManagePluginsViewModel(Services.IPlugins pluginService)
    {
        _pluginService = pluginService;

        foreach (var dataReaderPlugin in _pluginService.DataReaderPlugins)
        {
            Plugins.Add(dataReaderPlugin);
        }
        
        foreach (var dataProviderPlugin in _pluginService.DataProviderPlugins)
        {
            Plugins.Add(dataProviderPlugin);
        }
    }

    [RelayCommand]
    private void AddPlugin(string path)
    {
        var (validity, manifest) = _pluginService.IsValidPlugin(path);

        if (validity is not Services.IPlugins.Validity.Valid)
        {
            var error = validity switch
            {
                Services.IPlugins.Validity.FileNotFound => $"File { path } not found.",
                _ => $"File { Path.GetFileName(path) } is not a valid LSAnalyzer plugin."
            };
            
            Message = "Failed: " + error;
            ShowMessage = true;
            return;
        }

        var preservedPluginFolder = _pluginService.PreservePlugin(path);

        if (preservedPluginFolder is null)
        {
            Message = "Failed: Could not preserve plugin.";
            ShowMessage = true;
            return;
        }
        
        // the following is null-safe because of validity check above
        
        var assembly = _pluginService.LoadPlugin(Path.Combine(preservedPluginFolder, manifest!.Dll))!;
        
        IPluginCommons plugin = null!;
        switch (manifest.Type)
        {
            case IPluginCommons.PluginTypes.DataReader:
                plugin = _pluginService.CreatePlugin<IDataReaderPlugin>(assembly)!;
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                plugin  = _pluginService.CreatePlugin<IDataProviderPlugin>(assembly)!;
                break;
        }
        
        _pluginService.AddPlugin(plugin, preservedPluginFolder);
        Plugins.Add(plugin);
        Message = $"Added plugin: {plugin.ClassName}";
        ShowMessage = true;
    }

    [RelayCommand]
    private void RemovePlugin(IPluginCommons plugin)
    {
        _pluginService.RemovePlugin(plugin);
        Plugins.Remove(plugin);

        Message = $"Removed plugin: { plugin.ClassName }";
        ShowMessage = true;
    }
}