using System.Collections.Generic;
using LSAnalyzerAvalonia.Models;

namespace LSAnalyzerAvalonia.Services;

public interface IAppConfiguration
{
    public string DatasetTypesConfigFilePath { get; }
    
    public bool RestoreDefaultDatasetTypesStorage();
    
    public List<DatasetType>? GetStoredDatasetTypes();

    public void StoreDatasetType(DatasetType datasetType);

    public void RemoveDatasetType(DatasetType datasetType);
}