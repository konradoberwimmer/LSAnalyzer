using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class SelectAnalysisFileViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<IDataReaderPlugin> _dataReaderPlugins = [];
    
    [ObservableProperty] private IDataReaderPlugin? _selectedDataReaderPlugin;
    partial void OnSelectedDataReaderPluginChanged(IDataReaderPlugin? value)
    {
        OnPropertyChanged(nameof(IsReadyToLoad));
    }

    public bool IsReadyToLoad => SelectedDataReaderPlugin?.ViewModel.IsCompletelyFilled ?? false;
    
    [ExcludeFromCodeCoverage]
    public SelectAnalysisFileViewModel() // design-time only parameterless constructor
    {
        
    }

    public SelectAnalysisFileViewModel(List<IDataReaderPlugin> builtins, Services.IPlugins plugins, Type uiType)
    {
        foreach (var plugin in builtins.Concat(plugins.DataReaderPlugins))
        {
            plugin.CreateView(uiType);
            plugin.ViewModel.PropertyChanged += ListenSelectedDataReaderPluginViewModel;
            DataReaderPlugins.Add(plugin);
        }
    }

    private void ListenSelectedDataReaderPluginViewModel(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsReadyToLoad));
    }
}