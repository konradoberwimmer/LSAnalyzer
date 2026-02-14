using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public interface IDatasetTypeRepository
{
    public (FetchResult result, List<DatasetTypeCollection> datasetTypeCollections) FetchDatasetTypeCollections(string url);
    
    public (FetchResult result, DatasetType? datasetType) FetchDatasetType(string baseUrl, string fileName);
    
    public enum FetchResult
    {
        NotFound,
        Malformed,
        Success,
    }
}