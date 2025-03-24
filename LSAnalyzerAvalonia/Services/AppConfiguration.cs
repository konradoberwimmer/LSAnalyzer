using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LSAnalyzerAvalonia.Models;

namespace LSAnalyzerAvalonia.Services;

public class AppConfiguration(string datasetTypesConfigFilePath) : IAppConfiguration
{
    public string DatasetTypesConfigFilePath => datasetTypesConfigFilePath;
    
    public bool RestoreDefaultDatasetTypesStorage()
    {
        try
        {
            File.Delete(datasetTypesConfigFilePath);
            var defaultDatasetTypes = DatasetType.CreateDefaultDatasetTypes();
            File.WriteAllText(datasetTypesConfigFilePath, JsonSerializer.Serialize(defaultDatasetTypes));
        } catch (Exception e) when (e is DirectoryNotFoundException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
        
        return true;
    }
    
    public List<DatasetType>? GetStoredDatasetTypes()
    {
        if (!File.Exists(datasetTypesConfigFilePath))
        {
            return null;
        }

        var fileContent = File.ReadAllText(datasetTypesConfigFilePath);
        List<DatasetType> storedDatasetTypes = [];
        try
        {
            storedDatasetTypes = JsonSerializer.Deserialize<List<DatasetType>>(fileContent) ?? storedDatasetTypes;
        }
        catch (JsonException)
        {
            return null;
        }
        
        return storedDatasetTypes;
    }

    public void StoreDatasetType(DatasetType datasetType)
    {
        if (!File.Exists(datasetTypesConfigFilePath))
        {
            return;
        }

        var fileContent = File.ReadAllText(datasetTypesConfigFilePath);
        List<DatasetType> storedDatasetTypes = [];
        try
        {
            storedDatasetTypes = JsonSerializer.Deserialize<List<DatasetType>>(fileContent) ?? storedDatasetTypes;
        } catch (JsonException) { }

        storedDatasetTypes.RemoveAll(dst => dst.Id == datasetType.Id);
        storedDatasetTypes.Add(datasetType);

        File.WriteAllText(datasetTypesConfigFilePath, JsonSerializer.Serialize(storedDatasetTypes));
    }

    public void RemoveDatasetType(DatasetType datasetType)
    {
        if (!File.Exists(datasetTypesConfigFilePath))
        {
            return;
        }

        var fileContent = File.ReadAllText(datasetTypesConfigFilePath);
        List<DatasetType> storedDatasetTypes = [];
        try
        {
            storedDatasetTypes = JsonSerializer.Deserialize<List<DatasetType>>(fileContent) ?? storedDatasetTypes;
        } catch (JsonException) { }

        storedDatasetTypes.RemoveAll(dst => dst.Id == datasetType.Id);

        File.WriteAllText(datasetTypesConfigFilePath, JsonSerializer.Serialize(storedDatasetTypes));
    }
}