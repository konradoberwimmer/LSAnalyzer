using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzer.ViewModels;

public partial class ConfigDatasetTypes : ObservableObject
{
    private readonly Configuration _configuration;

    [ObservableProperty]
    private ObservableCollection<DatasetType> _datasetTypes = [];

    [ObservableProperty]
    private DatasetType? _selectedDatasetType;

    public List<string> UnsavedDatasetTypeNames => DatasetTypes.Where(dst => dst.IsChanged).Select(dst => dst.Name).ToList();

    [ExcludeFromCodeCoverage]
    public ConfigDatasetTypes()
    {
        // design-time only parameter-less constructor
        _configuration = new Configuration();
    }

    public ConfigDatasetTypes(Configuration configuration)
    {
        _configuration = configuration;
        DatasetTypes = [];
        try
        {
            var storedDatasetTypes = _configuration.GetStoredDatasetTypes();
            if (storedDatasetTypes != null)
            {
                storedDatasetTypes.OrderBy(d => d.Name).ToList().ForEach(d => {
                    d.AcceptChanges();
                    DatasetTypes.Add(d);
                });
            }
        }
        catch
        {
            // ignored
        }
    }

    [RelayCommand]
    private void NewDatasetType()
    {
        int minAvailableDatasetTypeId = 1;
        while (DatasetTypes.Where(dst => dst.Id == minAvailableDatasetTypeId).Any())
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

        _configuration.StoreDatasetType(SelectedDatasetType);
        SelectedDatasetType.AcceptChanges();
    }

    [RelayCommand]
    private void RemoveDatasetType()
    {
        if (SelectedDatasetType == null)
        {
            return;
        }

        _configuration.RemoveDatasetType(SelectedDatasetType);
        _configuration.RemoveRecentSubsettingExpressions(SelectedDatasetType.Id);
        _configuration.RemoveRecentFilesByDatasetTypeId(SelectedDatasetType.Id);

        DatasetTypes.Remove(SelectedDatasetType);
        SelectedDatasetType = null;
    }

    [RelayCommand]
    private void ImportDatasetType(string? filename)
    {
        if (filename == null)
        {
            return;
        }

        try
        {
            var fileContent = File.ReadAllText(filename);
            var newDatasetType = JsonSerializer.Deserialize<DatasetType>(fileContent);
            
            if (newDatasetType == null)
            {
                WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid file"));
                return;
            }

            int minAvailableDatasetTypeId = 1;
            while (DatasetTypes.Any(dst => dst.Id == minAvailableDatasetTypeId))
            {
                minAvailableDatasetTypeId++;
            }

            newDatasetType.Id = minAvailableDatasetTypeId;

            string newDatasetTypeOriginalName = newDatasetType.Name;
            int newDatasetTypeNameCounter = 1;
            while (DatasetTypes.Any(dst => dst.Name == newDatasetType.Name))
            {
                newDatasetType.Name = newDatasetTypeOriginalName + " (" + newDatasetTypeNameCounter + ")";
                newDatasetTypeNameCounter++;
            }

            if (!newDatasetType.Validate())
            {
                WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid dataset type"));
                return;
            }

            _configuration.StoreDatasetType(newDatasetType);

            DatasetTypes.Add(newDatasetType);
            newDatasetType.AcceptChanges();
            SelectedDatasetType = newDatasetType;

            WeakReferenceMessenger.Default.Send(new SuccessImportDatasetTypeMessage(newDatasetType.Name));
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid file"));
        }
    }

    [RelayCommand]
    private void ExportDatasetType(string? filename)
    {
        if (SelectedDatasetType == null || !SelectedDatasetType.Validate() || filename == null)
        {
            return;
        }

        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        File.WriteAllText(filename, JsonSerializer.Serialize(SelectedDatasetType, jsonSerializerOptions));
    }
}

internal class FailureImportDatasetTypeMessage : ValueChangedMessage<string>
{
    public FailureImportDatasetTypeMessage(string message) : base(message)
    {

    }
}

internal class SuccessImportDatasetTypeMessage : ValueChangedMessage<string>
{
    public SuccessImportDatasetTypeMessage(string message) : base(message)
    {

    }
}
