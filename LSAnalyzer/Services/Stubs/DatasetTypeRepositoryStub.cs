using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services.Stubs;

public class DatasetTypeRepositoryStub : IDatasetTypeRepository
{
    public (IDatasetTypeRepository.FetchResult, List<DatasetTypeCollection>) FetchDatasetTypeCollections(string url)
    {
        throw new System.NotImplementedException();
    }

    public (IDatasetTypeRepository.FetchResult result, DatasetType? datasetType) FetchDatasetType(string baseUrl, string fileName)
    {
        throw new System.NotImplementedException();
    }
}