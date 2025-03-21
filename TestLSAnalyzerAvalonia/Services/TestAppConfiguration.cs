using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;

namespace TestLSAnalyzerAvalonia.Services;

public class TestAppConfiguration
{
    [Fact]
    public void TestRestoreDefaultDatasetTypesStorage()
    {
        AppConfiguration appConfiguration = new("/");
        
        Assert.False(appConfiguration.RestoreDefaultDatasetTypesStorage());
        
        appConfiguration = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_storage_lsanalyzer_dataset_types.json"));
        
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_storage_lsanalyzer_dataset_types.json"), appConfiguration.DatasetTypesConfigFilePath);
        
        Assert.True(appConfiguration.RestoreDefaultDatasetTypesStorage());

        var defaultDatasetTypes = DatasetType.CreateDefaultDatasetTypes();
        var storedDatasetTypes = appConfiguration.GetStoredDatasetTypes();
        
        Assert.NotNull(storedDatasetTypes);
        
        Assert.Equal(defaultDatasetTypes.Count, storedDatasetTypes.Count);
    }

    [Fact]
    public void TestGetStoredDatasetTypes()
    {
        AppConfiguration appConfiguration = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "doesnotexist_lsanalyzer_dataset_types.json"));
        
        Assert.Null(appConfiguration.GetStoredDatasetTypes());
        
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_dataset_types.json"), "abc/def");
        
        appConfiguration = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_dataset_types.json"));

        Assert.Null(appConfiguration.GetStoredDatasetTypes());
        
        appConfiguration = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_get_lsanalyzer_dataset_types.json"));
        Assert.True(appConfiguration.RestoreDefaultDatasetTypesStorage());

        var defaultDatasetTypes = DatasetType.CreateDefaultDatasetTypes();
        var storedDatasetTypes = appConfiguration.GetStoredDatasetTypes();
        
        Assert.NotNull(storedDatasetTypes);
        
        Assert.Equal(defaultDatasetTypes.Count, storedDatasetTypes.Count);
    }
}