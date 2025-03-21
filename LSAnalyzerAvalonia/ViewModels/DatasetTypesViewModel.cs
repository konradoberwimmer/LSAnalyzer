using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class DatasetTypesViewModel : ViewModelBase
{
    private IAppConfiguration _appConfiguration = null!;
    
    [ObservableProperty] private ObservableCollection<DatasetType> _datasetTypes = null!;

    [ExcludeFromCodeCoverage]
    public DatasetTypesViewModel() // design-time only parameterless constructor
    {
        
    }
    
    public DatasetTypesViewModel(IAppConfiguration appConfiguration)
    {
        _appConfiguration = appConfiguration;
        
        var storedDatasetTypes = _appConfiguration.GetStoredDatasetTypes();
        if (storedDatasetTypes == null)
        {
            if (!_appConfiguration.RestoreDefaultDatasetTypesStorage())
            {
                Message = $"Something is seriously wrong with the dataset types configuration file at {_appConfiguration.DatasetTypesConfigFilePath}!";
                ShowMessage = true;
                storedDatasetTypes = [];
            }
            else
            {
                storedDatasetTypes = _appConfiguration.GetStoredDatasetTypes();
            }
        }

        DatasetTypes = new(storedDatasetTypes!);
    }
}