using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class ManagePluginsViewModel : ViewModelBase
{
    private Services.IPlugins _pluginService = null!;
    
    [ObservableProperty] private ObservableCollection<IPluginCommons> _plugins = [];
    
    [ObservableProperty] private IPluginCommons _selectedPlugin = null!;
    
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
    }

    [RelayCommand]
    private void AddPlugin(string path)
    {
        
    }
}