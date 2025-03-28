using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
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
    
    public List<string> UnsavedDatasetTypeNames => DatasetTypes.Where(dst => dst.IsChanged).Select(dst => dst.Name).ToList();

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
                DisplayMessage($"Something is seriously wrong with the dataset types configuration file at {_appConfiguration.DatasetTypesConfigFilePath}!");
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
        
        DisplayMessage($"Removed { SelectedDatasetType.Name }");
        
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

    [RelayCommand]
    private void ExportDatasetType(string file)
    {
        if (SelectedDatasetType == null || !SelectedDatasetType.Validate())
        {
            return;
        }

        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        File.WriteAllText(file, JsonSerializer.Serialize(SelectedDatasetType, jsonSerializerOptions));
        
        _appConfiguration.LastOutFileLocation = Path.GetDirectoryName(file)!;
        DisplayMessage($"Exported { SelectedDatasetType.Name } to { file }");
    }

    [RelayCommand]
    private void ImportDatasetType(string file)
    {
        try
        {
            var fileContent = File.ReadAllText(file);
            var newDatasetType = JsonSerializer.Deserialize<DatasetType>(fileContent);
                
            if (newDatasetType == null)
            {
                DisplayMessage("Invalid file");
                return;
            }

            var minAvailableDatasetTypeId = 1;
            while (DatasetTypes.Any(dst => dst.Id == minAvailableDatasetTypeId))
            {
                minAvailableDatasetTypeId++;
            }
            newDatasetType.Id = minAvailableDatasetTypeId;

            var newDatasetTypeOriginalName = newDatasetType.Name;
            var newDatasetTypeNameCounter = 1;
            while (DatasetTypes.Any(dst => dst.Name == newDatasetType.Name))
            {
                newDatasetType.Name = newDatasetTypeOriginalName + " (" + newDatasetTypeNameCounter + ")";
                newDatasetTypeNameCounter++;
            }

            if (!newDatasetType.Validate())
            {
                DisplayMessage("Invalid dataset type");
                return;
            }

            _appConfiguration.StoreDatasetType(newDatasetType);

            DatasetTypes.Add(newDatasetType);
            newDatasetType.AcceptChanges();
            SelectedDatasetType = newDatasetType;

            _appConfiguration.LastInFileLocation = Path.GetDirectoryName(file)!;
            DisplayMessage($"Imported { newDatasetType.Name } from { file }");
        }
        catch (Exception)
        {
            DisplayMessage("Invalid file");
        }
    }
}