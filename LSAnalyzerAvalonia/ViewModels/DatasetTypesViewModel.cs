using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class DatasetTypesViewModel : ViewModelBase
{
    private IAppConfiguration _appConfiguration = null!;
    
    [ObservableProperty] private ObservableCollection<DatasetType> _datasetTypes = null!;
    
    [ObservableProperty] private DatasetType? _selectedDatasetType;

    [ExcludeFromCodeCoverage]
    public DatasetTypesViewModel() // design-time only parameterless constructor
    {
        DatasetTypes = [
            new DatasetType { Name = "Type A", AutoEncapsulateRegex = true, NMI = 10 }
        ];
        SelectedDatasetType = DatasetTypes.First();
        SelectedDatasetType.AcceptChanges();
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

        DatasetTypes = [];
        storedDatasetTypes!.OrderBy(d => d.Name).ToList().ForEach(d => {
            d.AcceptChanges();
            DatasetTypes.Add(d);
        });
    }
    
    [RelayCommand]
    private void NewDatasetType()
    {
        var minAvailableDatasetTypeId = 1;
        while (DatasetTypes.Any(dst => dst.Id == minAvailableDatasetTypeId))
        {
            minAvailableDatasetTypeId++;
        }

        DatasetType newDatasetType = new() { Id = minAvailableDatasetTypeId, Name = "New dataset type", JKreverse = false };

        DatasetTypes.Add(newDatasetType);
        SelectedDatasetType = newDatasetType;
    }
    
    [RelayCommand]
    private void SaveSelectedDatasetType()
    {
        if (SelectedDatasetType == null || !SelectedDatasetType.Validate())
        {
            return;
        }

        _appConfiguration.StoreDatasetType(SelectedDatasetType);
        SelectedDatasetType.AcceptChanges();
    }
    
    [RelayCommand]
    private void RemoveDatasetType()
    {
        if (SelectedDatasetType == null)
        {
            return;
        }
        
        _appConfiguration.RemoveDatasetType(SelectedDatasetType);
        
        Message = $"Removed { SelectedDatasetType.Name }";
        ShowMessage = true;
        
        DatasetTypes.Remove(SelectedDatasetType);
        SelectedDatasetType = null;
    }

    [RelayCommand]
    private void AddPlausibleValueVariable()
    {
        SelectedDatasetType?.PVvarsList.Add(new PlausibleValueVariable { Regex = string.Empty, DisplayName = string.Empty, Mandatory = false });
    }

    [RelayCommand]
    private void RemovePlausibleValueVariables(PlausibleValueVariable plausibleValueVariable)
    { 
        SelectedDatasetType?.PVvarsList.Remove(plausibleValueVariable);
    }
}