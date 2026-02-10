using LSAnalyzer.Services;

namespace TestLSAnalyzer.Services;

public class TestDatasetTypeRepository
{
    [Fact]
    public void TestRepositoryInvalidUrl()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 1 };

        var result = datasetTypeRepository.FetchDatasetTypeCollections("https://www.thisisnotanlsanalyzer.com/repository.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.NotFound, result.result);
        Assert.Empty(result.datasetTypeCollections);
        
        var result2 = datasetTypeRepository.FetchDatasetTypeCollections("https://konradoberwimmer.github.io/LSAnalyzer/invalid_file.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.NotFound, result2.result);
        Assert.Empty(result2.datasetTypeCollections);
    }
    
    [Fact]
    public void TestRepositoryInvalidFile()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 5 };

        var result = datasetTypeRepository.FetchDatasetTypeCollections("https://konradoberwimmer.github.io/LSAnalyzer/501_piaac.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.Malformed, result.result);
        Assert.Empty(result.datasetTypeCollections);
    }
    
    [Fact]
    public void TestRepositoryValid()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 5 };

        var result = datasetTypeRepository.FetchDatasetTypeCollections("https://konradoberwimmer.github.io/LSAnalyzer/index.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.Success, result.result);
        Assert.NotEmpty(result.datasetTypeCollections);
    }
    
    [Fact]
    public void TestDatasetTypeInvalidUrl()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 1 };

        var result = datasetTypeRepository.FetchDatasetType("https://www.thisisnotlsanalyzerrepository.com/", "501_piaac.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.NotFound, result.result);
        Assert.Null(result.datasetType);
        
        var result2 = datasetTypeRepository.FetchDatasetType("https://konradoberwimmer.github.io/LSAnalyzer/", "invalid_file.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.NotFound, result2.result);
        Assert.Null(result2.datasetType);
    }
    
    [Fact]
    public void TestDatasetTypeInvalidFile()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 5 };

        var result = datasetTypeRepository.FetchDatasetType("https://konradoberwimmer.github.io/LSAnalyzer/", "index.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.Malformed, result.result);
        Assert.Null(result.datasetType);
    }
    
    [Fact]
    public void TestDatasetTypeValid()
    {
        DatasetTypeRepository datasetTypeRepository = new() { TimeoutInSeconds = 5 };

        var result = datasetTypeRepository.FetchDatasetType("https://konradoberwimmer.github.io/LSAnalyzer/", "501_piaac.json");
        
        Assert.Equal(IDatasetTypeRepository.FetchResult.Success, result.result);
        Assert.NotNull(result.datasetType);
    }
}