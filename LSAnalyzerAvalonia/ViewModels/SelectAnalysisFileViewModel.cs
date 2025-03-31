using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class SelectAnalysisFileViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<IDataReaderPlugin> _dataReaderPlugins = [];
    
    [ObservableProperty] private IDataReaderPlugin? _selectedDataReaderPlugin;
    partial void OnSelectedDataReaderPluginChanged(IDataReaderPlugin? value)
    {
        DummyOutput = value?.ReadDataFile(string.Empty).ToString() ?? string.Empty;
    }
    
    [ObservableProperty] private string _dummyOutput = string.Empty;
    
    [ExcludeFromCodeCoverage]
    public SelectAnalysisFileViewModel() // design-time only parameterless constructor
    {
        
    }

    public SelectAnalysisFileViewModel(Services.IPlugins plugins)
    {
        foreach (var dataReaderPlugin in plugins.DataReaderPlugins)
        {
            DataReaderPlugins.Add(dataReaderPlugin);    
        }
    }
}