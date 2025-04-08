using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;

namespace TestLSAnalyzerAvalonia.Services;

public class TestAppConfiguration
{
    [Fact]
    public void TestLastInAndOutFileLocation()
    {
        AppConfiguration appConfiguration = new("/", string.Empty);
        
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastInFileLocation);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastOutFileLocation);

        appConfiguration.LastInFileLocation = "/somewhere/else";
        appConfiguration.LastOutFileLocation = "/somewhere/else";
        
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastInFileLocation);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastOutFileLocation);

        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_user_settings.json"), "abc/def");
        
        appConfiguration = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_user_settings.json"), string.Empty);
        
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastInFileLocation);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appConfiguration.LastOutFileLocation);
        
        appConfiguration.LastInFileLocation = "/somewhere/else";
        appConfiguration.LastOutFileLocation = "/somewhere/else";
        
        Assert.Equal("/somewhere/else", appConfiguration.LastInFileLocation);
        Assert.Equal("/somewhere/else", appConfiguration.LastOutFileLocation);
    }
    
    [Fact]
    public void TestRestoreDefaultDatasetTypesStorage()
    {
        AppConfiguration appConfiguration = new(string.Empty, "/");
        
        Assert.False(appConfiguration.RestoreDefaultDatasetTypesStorage());
        
        appConfiguration = new(string.Empty, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_storage_lsanalyzer_dataset_types.json"));
        
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
        AppConfiguration appConfiguration = new(string.Empty, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "doesnotexist_lsanalyzer_dataset_types.json"));
        
        Assert.Null(appConfiguration.GetStoredDatasetTypes());
        
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_dataset_types.json"), "abc/def");
        
        appConfiguration = new(string.Empty, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_corrupt_lsanalyzer_dataset_types.json"));

        Assert.Null(appConfiguration.GetStoredDatasetTypes());
        
        appConfiguration = new(string.Empty, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_get_lsanalyzer_dataset_types.json"));
        Assert.True(appConfiguration.RestoreDefaultDatasetTypesStorage());

        var defaultDatasetTypes = DatasetType.CreateDefaultDatasetTypes();
        var storedDatasetTypes = appConfiguration.GetStoredDatasetTypes();
        
        Assert.NotNull(storedDatasetTypes);
        
        Assert.Equal(defaultDatasetTypes.Count, storedDatasetTypes.Count);
    }

    [Fact]
    public void TestStoreDatasetType()
    {
        AppConfiguration appConfiguration = new(string.Empty, "/not_here.json");
        
        // assert: will not throw error
        appConfiguration.StoreDatasetType(new DatasetType() { Id = 15 });

        var testFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_store_lsanalyzer_dataset_types.json");
        appConfiguration = new(string.Empty, testFileName);
        appConfiguration.RestoreDefaultDatasetTypesStorage();
        
        appConfiguration.StoreDatasetType(new DatasetType { Id = 123454321, Name = "test_store", Weight = "wgt_store" });

        var fileContent = File.ReadAllText(testFileName);
        
        Assert.Matches("123454321", fileContent);
        Assert.Matches("test_store", fileContent);
        Assert.Matches("wgt_store", fileContent);
        
        appConfiguration.StoreDatasetType(new DatasetType { Id = 123454321, Name = "test_again_store", Weight = "wgt_again_store" });
        
        fileContent = File.ReadAllText(testFileName);
        
        Assert.DoesNotMatch("test_store", fileContent);
        Assert.DoesNotMatch("wgt_store", fileContent);
        Assert.Matches("test_again_store", fileContent);
        Assert.Matches("wgt_again_store", fileContent);
    }
    
    [Fact]
    public void TestRemoveDatasetType()
    {
        AppConfiguration appConfiguration = new(string.Empty, "/not_here.json");
        
        // assert: will not throw error
        appConfiguration.RemoveDatasetType(new DatasetType() { Id = 15 });

        var testFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_remove_lsanalyzer_dataset_types.json");
        appConfiguration = new(string.Empty, testFileName);
        appConfiguration.RestoreDefaultDatasetTypesStorage();
        
        appConfiguration.StoreDatasetType(new DatasetType { Id = 123454321, Name = "test_store", Weight = "wgt_store" });

        var fileContent = File.ReadAllText(testFileName);
        
        Assert.Matches("123454321", fileContent);
        
        appConfiguration.RemoveDatasetType(new DatasetType { Id = 123454321 });
        
        fileContent = File.ReadAllText(testFileName);
        
        Assert.DoesNotMatch("123454321", fileContent);
    }

    [Fact]
    public void TestStoreAndRemoveSanitizeCorruptConfigFile()
    {
        var corruptConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_corruptest_lsanalyzer_dataset_types.json");
        AppConfiguration appConfiguration = new(string.Empty, corruptConfigFile);
        
        File.WriteAllText(corruptConfigFile, "abc/def");
        
        appConfiguration.StoreDatasetType(new DatasetType { Id = 123454321 });
        
        var datasetTypes = appConfiguration.GetStoredDatasetTypes();
        
        Assert.NotNull(datasetTypes);
        Assert.Single(datasetTypes);
        Assert.Equal(123454321, datasetTypes.First().Id);
        
        File.WriteAllText(corruptConfigFile, "abc/def");
        
        appConfiguration.RemoveDatasetType(new DatasetType { Id = 123454321 });
        
        datasetTypes = appConfiguration.GetStoredDatasetTypes();
        
        Assert.NotNull(datasetTypes);
        Assert.Empty(datasetTypes);
    }

    [Fact]
    public void TestPreservedPluginLocations()
    {
        AppConfiguration appConfiguration = new(string.Empty, string.Empty);
        
        Assert.Empty(appConfiguration.PreservedPluginLocations);
    }

    [Fact]
    public void TestStorePreservedPluginLocation()
    {
        var testFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_store_lsanalyzer_preserved_plugin_location.json");
        
        if (File.Exists(testFileName)) File.Delete(testFileName);
        
        AppConfiguration appConfiguration = new(testFileName, string.Empty);
        
        appConfiguration.StorePreservedPluginLocation("/somewhereA");
        Assert.Single(appConfiguration.PreservedPluginLocations);
        
        appConfiguration = new("/", string.Empty);
        
        appConfiguration.StorePreservedPluginLocation("/somewhereB");
        Assert.Empty(appConfiguration.PreservedPluginLocations);
    }
    
    [Fact]
    public void TestRemovePreservedPluginLocation()
    {
        var testFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_remove_lsanalyzer_preserved_plugin_location.json");
        
        if (File.Exists(testFileName)) File.Delete(testFileName);
        
        AppConfiguration appConfiguration = new(testFileName, string.Empty);
        
        appConfiguration.StorePreservedPluginLocation("/somewhereA");
        Assert.Single(appConfiguration.PreservedPluginLocations);
        
        appConfiguration.RemovePreservedPluginLocation("/somewhereA");
        Assert.Empty(appConfiguration.PreservedPluginLocations);
        
        appConfiguration = new("/", string.Empty);

        var exception = Record.Exception(() => { appConfiguration.RemovePreservedPluginLocation("/somewhereB"); });
        Assert.Null(exception);
    }
}