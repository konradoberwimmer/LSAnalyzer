using System.Collections.Generic;
using System.Collections.Immutable;
using LSAnalyzerAvalonia.Models;

namespace LSAnalyzerAvalonia.Services;

public interface IAppConfiguration
{
    public string LastInFileLocation { get; set; }
    
    public string LastOutFileLocation { get; set; }
    
    public string DatasetTypesConfigFilePath { get; }
    
    public bool RestoreDefaultDatasetTypesStorage();
    
    public List<DatasetType>? GetStoredDatasetTypes();

    public void StoreDatasetType(DatasetType datasetType);

    public void RemoveDatasetType(DatasetType datasetType);

    public List<string> PreservedPluginLocations { get; }
    
    public void StorePreservedPluginLocation(string preservedPluginLocation);
    
    public void RemovePreservedPluginLocation(string preservedPluginLocation);
}