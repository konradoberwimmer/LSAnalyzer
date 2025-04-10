using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class SelectAnalysisFileViewModel : ViewModelBase
{
    private readonly Type _uiType = null!;
    
    [ObservableProperty] private ObservableCollection<IDataReaderPlugin> _dataReaderPlugins = [];
    
    [ObservableProperty] private IDataReaderPlugin? _selectedDataReaderPlugin;
    partial void OnSelectedDataReaderPluginChanged(IDataReaderPlugin? value)
    {
        value?.CreateView(_uiType);
    }
    
    [ExcludeFromCodeCoverage]
    public SelectAnalysisFileViewModel() // design-time only parameterless constructor
    {
        
    }

    public SelectAnalysisFileViewModel(Services.IPlugins plugins, Type uiType)
    {
        foreach (var dataReaderPlugin in plugins.DataReaderPlugins)
        {
            DataReaderPlugins.Add(dataReaderPlugin);    
        }
        
        _uiType = uiType;
    }
}