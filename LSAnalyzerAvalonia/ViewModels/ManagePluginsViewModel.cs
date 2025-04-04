using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzerAvalonia.Helper;
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
        var (validity, manifest) = PluginTools.IsValidPlugin(path);

        if (validity is not PluginTools.Validity.Valid)
        {
            var error = validity switch
            {
                PluginTools.Validity.FileNotFound => $"File { path } not found.",
                _ => $"File { Path.GetFileName(path) } is not a valid LSAnalyzer plugin."
            };
            
            Message = "Failed: " + error;
            ShowMessage = true;
            return;
        }

        var preservedPluginFolder = PluginTools.PreservePlugin(path);

        if (preservedPluginFolder is null)
        {
            Message = "Failed: Could not preserve plugin.";
            ShowMessage = true;
            return;
        }
        
        var assembly = PluginTools.LoadPlugin(Path.Combine(preservedPluginFolder, manifest!.Dll))!;
        
        switch (manifest.Type)
        {
            case IPluginCommons.PluginTypes.DataReader:
                var dataReaderPlugin = PluginTools.CreatePlugin<IDataReaderPlugin>(assembly)!;
                _pluginService.DataReaderPlugins.Add(dataReaderPlugin);
                Plugins.Add(dataReaderPlugin);
                Message = $"Added plugin: { dataReaderPlugin.ClassName }";
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                var dataProviderPlugin = PluginTools.CreatePlugin<IDataProviderPlugin>(assembly)!;
                _pluginService.DataProviderPlugins.Add(dataProviderPlugin);
                Plugins.Add(dataProviderPlugin);
                Message = $"Added plugin: { dataProviderPlugin.ClassName }";
                break;
        }
        
        ShowMessage = true;
    }

    [RelayCommand]
    private void RemovePlugin(IPluginCommons plugin)
    {
        switch (plugin.PluginType)
        {
            case IPluginCommons.PluginTypes.DataReader:
                _pluginService.DataReaderPlugins.Remove((plugin as IDataReaderPlugin)!);
                break;
            case IPluginCommons.PluginTypes.DataProvider:
                _pluginService.DataProviderPlugins.Remove((plugin as IDataProviderPlugin)!);
                break;
        }
        
        Plugins.Remove(plugin);

        Message = $"Removed plugin: { plugin.ClassName }";
        ShowMessage = true;
    }
}