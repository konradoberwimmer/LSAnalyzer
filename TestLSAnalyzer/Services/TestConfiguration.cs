using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.Services.Stubs;
using LSAnalyzer.ViewModels;
using Moq;

namespace TestLSAnalyzer.Services;

[Collection("Sequential")]
public class TestConfiguration
{
    [Theory, ClassData(typeof(TestGetRLocationData))]
    public void TestGetRLocation(string? storedLocation, bool createStoredLocation, string? defaultLocation, string? expected)
    {
        var mockedSettingsService = new Mock<ISettingsService>();
        mockedSettingsService.Setup(s => s.RLocation).Returns(storedLocation);

        if (createStoredLocation)
        {
            Directory.CreateDirectory(storedLocation!);
        }
        
        var mockedRegistryService = new Mock<IRegistryService>();
        mockedRegistryService.Setup(s => s.GetDefaultRLocation()).Returns(defaultLocation);
        
        Configuration configuration = new(string.Empty, null, mockedSettingsService.Object, mockedRegistryService.Object);
        
        var result = configuration.GetRLocation();
        
        Assert.Equal(expected, result?.rHome);
        if (expected is not null)
        {
            Assert.Equal(Path.Combine(expected, "bin", "x64"), result?.rPath);
        }
    }

    public class TestGetRLocationData : TheoryData<string?, bool, string?, string?>
    {
        public TestGetRLocationData()
        {
            Add(null, false, null, null);
            var actualTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Add(actualTempPath, true, null, actualTempPath);
            var nonExistingPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Add(nonExistingPath, false, null, null);
            Add(nonExistingPath, false, @"C:\myRinstallation", "C:\\myRinstallation");
        }
    }
    
    [Fact]
    public void TestGetStoredRecentSubsettingExpressions()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentSubsettingExpressions = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentSubsettingExpressions(1);
        configuration.RemoveRecentSubsettingExpressions(2);
        
        Assert.Empty(configuration.GetStoredRecentSubsettingExpressions(1));
        
        configuration.StoreRecentSubsettingExpression(1, "test");
        
        Assert.Single(configuration.GetStoredRecentSubsettingExpressions(1));
        Assert.Empty(configuration.GetStoredRecentSubsettingExpressions(2));
    }
    
    [Fact]
    public void TestStoreRecentSubsettingExpression()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentSubsettingExpressions = 3;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentSubsettingExpressions(1);
        
        configuration.StoreRecentSubsettingExpression(1, "test");
        
        Assert.Single(configuration.GetStoredRecentSubsettingExpressions(1));
        
        configuration.StoreRecentSubsettingExpression(1, "test again");
        
        Assert.Equal(2, configuration.GetStoredRecentSubsettingExpressions(1).Count);
        Assert.Equal("test again", configuration.GetStoredRecentSubsettingExpressions(1).First());
        
        configuration.StoreRecentSubsettingExpression(1, "test");
        
        Assert.Equal(2, configuration.GetStoredRecentSubsettingExpressions(1).Count);
        Assert.Equal("test", configuration.GetStoredRecentSubsettingExpressions(1).First());
        
        configuration.StoreRecentSubsettingExpression(1, "test another");
        configuration.StoreRecentSubsettingExpression(1, "test yet another");
        
        Assert.Equal(3, configuration.GetStoredRecentSubsettingExpressions(1).Count);
        
        systemSettings.NumberRecentSubsettingExpressions = 0;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        Assert.Empty(configuration.GetStoredRecentSubsettingExpressions(1));
        
        configuration.StoreRecentSubsettingExpression(1, "test");
        
        Assert.Empty(configuration.GetStoredRecentSubsettingExpressions(1));
    }
    
    [Fact]
    public void TestRemoveRecentSubsettingExpressions()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentSubsettingExpressions = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.StoreRecentSubsettingExpression(3, "test");
        
        Assert.NotEmpty(configuration.GetStoredRecentSubsettingExpressions(3));
        
        configuration.RemoveRecentSubsettingExpressions(3);
        
        Assert.Empty(configuration.GetStoredRecentSubsettingExpressions(3));
    }

    [Fact]
    public void TestTrimRecentSubsettingExpressions()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentSubsettingExpressions = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentSubsettingExpressions(4);
        
        configuration.StoreRecentSubsettingExpression(4, "test");
        configuration.StoreRecentSubsettingExpression(4, "test again");
        configuration.StoreRecentSubsettingExpression(4, "test again again");
        configuration.StoreRecentSubsettingExpression(4, "test again again again");
        
        Assert.Equal(4, configuration.GetStoredRecentSubsettingExpressions(4).Count);
        
        configuration.TrimRecentSubsettingExpressions(2);
        
        Assert.Equal(2, configuration.GetStoredRecentSubsettingExpressions(4).Count);
        Assert.Equal("test again again again", configuration.GetStoredRecentSubsettingExpressions(4).First());
        Assert.Equal("test again again", configuration.GetStoredRecentSubsettingExpressions(4).Last());
    }
    
    [Fact]
    public void TestGetStoredRecentFiles()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentFilesByDataProviderId(1);
        configuration.RemoveRecentFilesByDataProviderId(2);
        
        Assert.Empty(configuration.GetStoredRecentFiles(1));
        
        configuration.StoreRecentFile(1, new() { FileName = "test", DatasetTypeId = 12 });
        
        Assert.Single(configuration.GetStoredRecentFiles(1));
        Assert.Empty(configuration.GetStoredRecentFiles(2));
    }
    
    [Fact]
    public void TestStoreRecentFile()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 3;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentFilesByDataProviderId(1);
        
        configuration.StoreRecentFile(1, new() { FileName = "test", DatasetTypeId = 12, ModeKeep = false });
        
        Assert.Single(configuration.GetStoredRecentFiles(1));
        
        configuration.StoreRecentFile(1, new() { FileName = "test again", DatasetTypeId = 13 });
        
        Assert.Equal(2, configuration.GetStoredRecentFiles(1).Count);
        Assert.True(configuration.GetStoredRecentFiles(1).First().IsEqualFile(new() { FileName = "test again", DatasetTypeId = 13 }));
        
        configuration.StoreRecentFile(1, new() { FileName = "test", DatasetTypeId = 12, ModeKeep = true });
        
        Assert.Equal(2, configuration.GetStoredRecentFiles(1).Count);
        Assert.True(configuration.GetStoredRecentFiles(1).First().IsEqualFile(new() { FileName = "test", DatasetTypeId = 12, ModeKeep = true }));
        
        configuration.StoreRecentFile(1, new() { FileName = "test another", DatasetTypeId = 2 });
        configuration.StoreRecentFile(1, new() { FileName = "test yet another", DatasetTypeId = 4 });
        
        Assert.Equal(3, configuration.GetStoredRecentFiles(1).Count);
        
        systemSettings.NumberRecentFiles = 0;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        Assert.Empty(configuration.GetStoredRecentFiles(1));
        
        configuration.StoreRecentFile(1, new() { FileName = "test", DatasetTypeId = 12 });
        
        Assert.Empty(configuration.GetStoredRecentFiles(1));
    }
    
    [Fact]
    public void TestRemoveRecentFilesByDataProviderId()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.StoreRecentFile(3, new() { FileName = "test", DatasetTypeId = 1, Weight = "wgt" });
        
        Assert.NotEmpty(configuration.GetStoredRecentFiles(3));
        
        configuration.RemoveRecentFilesByDataProviderId(3);
        
        Assert.Empty(configuration.GetStoredRecentFiles(3));
    }
    
    [Fact]
    public void TestRemoveRecentFilesByDatasetTypeId()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentFilesByDataProviderId(17);
        configuration.StoreRecentFile(17, new() { FileName = "test", DatasetTypeId = 42, Weight = "wgt" });
        configuration.StoreRecentFile(17, new() { FileName = "test other", DatasetTypeId = 43, Weight = "wgt" });

        Assert.Equal(2, configuration.GetStoredRecentFiles(17).Count);
        
        configuration.RemoveRecentFilesByDatasetTypeId(42);
        
        Assert.Single(configuration.GetStoredRecentFiles(17));
    }
    
    [Fact]
    public void TestRemoveRecentFile()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentFilesByDataProviderId(66);
        configuration.StoreRecentFile(66, new() { FileName = "test", DatasetTypeId = 42, Weight = "wgt" });
        configuration.StoreRecentFile(66, new() { FileName = "test other", DatasetTypeId = 43, Weight = "wgt", ConvertCharacters = true });

        Assert.Equal(2, configuration.GetStoredRecentFiles(66).Count);

        configuration.RemoveRecentFile(new() { FileName = "test other", DatasetTypeId = 43, Weight = "wgt", ConvertCharacters = false });
        
        Assert.Single(configuration.GetStoredRecentFiles(66));
    }

    [Fact]
    public void TestTrimRecentFiles()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.RemoveRecentFilesByDataProviderId(4);
        
        configuration.StoreRecentFile(4, new() { FileName = "test", DatasetTypeId = 1 });
        configuration.StoreRecentFile(4, new() { FileName = "test again", DatasetTypeId = 1 });
        configuration.StoreRecentFile(4, new() { FileName = "test again again", DatasetTypeId = 1 });
        configuration.StoreRecentFile(4, new() { FileName = "test again again again", DatasetTypeId = 1 });
        
        Assert.Equal(4, configuration.GetStoredRecentFiles(4).Count);
        
        configuration.TrimRecentFiles(2);
        
        Assert.Equal(2, configuration.GetStoredRecentFiles(4).Count);
        Assert.True(configuration.GetStoredRecentFiles(4).First().IsEqualFile(new() { FileName = "test again again again", DatasetTypeId = 1 }));
        Assert.True(configuration.GetStoredRecentFiles(4).Last().IsEqualFile(new() { FileName = "test again again", DatasetTypeId = 1 }));
    }

    [Fact]
    public void TestRecentFileForAnalysisIntricacies()
    {
        Configuration.RecentFileForAnalysis rf1 = new() { FileName = "C:\\meine_Daten.csv", DatasetTypeId = 2, Weight = "wgt", ConvertCharacters = true };
        Configuration.RecentFileForAnalysis rf2 = new() { FileName = "C:\\meine_Daten.csv", DatasetTypeId = 2, Weight = "wgt", ConvertCharacters = false };
        Configuration.RecentFileForAnalysis rf3 = new() { FileName = "C:\\meine_Daten.csv", DatasetTypeId = 2, Weight = "otherwgt", ConvertCharacters = true };
        
        Assert.True(rf1.IsEqualFile(rf2));
        Assert.False(rf1.IsEqualFile(rf3));
        
        Assert.Equal("meine_Daten.csv - C:\\ (wgt)", rf1.DisplayString);

        rf1.FormatFileName = f => "File: " + f;
        
        Assert.Equal("File: C:\\meine_Daten.csv (wgt)", rf1.DisplayString);
    }

    [Fact]
    public void TestGetMatchingDataProviderConfiguration()
    {
        MockedConfiguration mockedConfiguration = new();
        
        Assert.Null(mockedConfiguration.GetMatchingDataProviderConfiguration(new DataverseConfiguration
        {
            Id = 12,
            Name = "no match",
            Url = "https://nomatch.com",
            ApiToken = "token",
        }));
        
        Assert.NotNull(mockedConfiguration.GetMatchingDataProviderConfiguration(new DataverseConfiguration
        {
            Id = 21,
            Name = "match even though only url corresponds",
            Url = "https://match.org",
            ApiToken = "",
        }));
    }
    
    [Fact]
    public void TestEverythingWithRecentBatchAnalyzeFiles()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object, new DatasetTypeRepositoryStub(), new SettingsServiceStub());
        systemSettings.NumberRecentFiles = 20;
        systemSettings.SaveSettingsCommand.Execute(null);
        
        configuration.TrimRecentBatchAnalyzeFiles(0);
        
        Assert.Empty(configuration.GetStoredRecentBatchAnalyzeFiles());
        
        configuration.StoreRecentBatchAnalyzeFile(@"C:\somewhere\file.json");
        
        Assert.Single(configuration.GetStoredRecentBatchAnalyzeFiles());
        
        configuration.StoreRecentBatchAnalyzeFile(@"C:\somewhere\newest_file.json");
        
        Assert.Equal(2, configuration.GetStoredRecentBatchAnalyzeFiles().Count);
        Assert.Equal(@"C:\somewhere\newest_file.json", configuration.GetStoredRecentBatchAnalyzeFiles().First());
        Assert.Equal(@"C:\somewhere\file.json", configuration.GetStoredRecentBatchAnalyzeFiles().Last());
        
        configuration.StoreRecentBatchAnalyzeFile(@"C:\somewhere\not_existing_file.json");
        
        Assert.Equal(3, configuration.GetStoredRecentBatchAnalyzeFiles().Count);
        
        configuration.RemoveRecentBatchAnalyzeFile(@"C:\somewhere\not_existing_file.json");
        
        Assert.Equal(2, configuration.GetStoredRecentBatchAnalyzeFiles().Count);
        Assert.Equal(@"C:\somewhere\newest_file.json", configuration.GetStoredRecentBatchAnalyzeFiles().First());
        Assert.Equal(@"C:\somewhere\file.json", configuration.GetStoredRecentBatchAnalyzeFiles().Last());
        
        configuration.TrimRecentBatchAnalyzeFiles(2);
        
        Assert.Equal(2, configuration.GetStoredRecentBatchAnalyzeFiles().Count);
        
        configuration.TrimRecentBatchAnalyzeFiles(0);
        
        Assert.Empty(configuration.GetStoredRecentBatchAnalyzeFiles());
    }

    [Fact]
    public void TestGetVirtualVariablesFor()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupSequence(service => service.VirtualVariables)
            .Returns([])
            .Returns([ new VirtualVariableCombine { ForFileName = "other_file.sav" } ])
            .Returns([ new VirtualVariableCombine { ForFileName = "other_file.sav", ForDatasetTypeId = 12 }, new VirtualVariableCombine { ForFileName = "some_file.csv" }])
            .Returns([ new VirtualVariableCombine { ForFileName = "other_file.sav", ForDatasetTypeId = 12 }, new VirtualVariableCombine { ForFileName = "some_file.csv" }])
            .Returns([ new VirtualVariableCombine { ForFileName = "other_file.sav", ForDatasetTypeId = 12 }, new VirtualVariableCombine { ForFileName = "some_file.csv" }]);

        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());

        var noVirtualVariablesAtAll = configuration.GetVirtualVariablesFor("some_file.csv", new DatasetType());
        Assert.Empty(noVirtualVariablesAtAll);
        
        var noVirtualVariableForFile = configuration.GetVirtualVariablesFor("some_file.csv", new DatasetType());
        Assert.Empty(noVirtualVariableForFile);
        
        var virtualVariableFromFileName = configuration.GetVirtualVariablesFor("some_file.csv", new DatasetType());
        Assert.Single(virtualVariableFromFileName);
        
        var virtualVariableFromDatasetType = configuration.GetVirtualVariablesFor("new_file.csv", new DatasetType { Id = 12 });
        Assert.Single(virtualVariableFromDatasetType);
        
        var virtualVariablesFromBoth = configuration.GetVirtualVariablesFor("some_file.csv", new DatasetType { Id = 12 });
        Assert.Equal(2, virtualVariablesFromBoth.Count);
    }

    [Fact]
    public void TestGetNextVirtualVariableIdFirst()
    {
        List<VirtualVariable> virtualVariables = [];
        
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupGet(service => service.VirtualVariables).Returns(virtualVariables);
        
        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());
        
        Assert.Equal(1, configuration.GetNextVirtualVariableId());
    }
    
    [Fact]
    public void TestGetNextVirtualVariableId()
    {
        List<VirtualVariable> virtualVariables =
            [new VirtualVariableCombine { Id = 21 }, new VirtualVariableCombine { Id = 7 }];
        
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupGet(service => service.VirtualVariables).Returns(virtualVariables);
        
        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());
        
        Assert.Equal(22, configuration.GetNextVirtualVariableId());
    }

    [Fact]
    public void TestRemoveVirtualVariable()
    {
        List<VirtualVariable> virtualVariables =
            [new VirtualVariableCombine { Id = 21, ForFileName = "some_file.sav" }];
        
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupGet(service => service.VirtualVariables).Returns(virtualVariables);

        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());

        settingsService.SetupSet(service => service.VirtualVariables = It.Is<List<VirtualVariable>>(list => list.Count == 1)).Verifiable();

        configuration.RemoveVirtualVariable(new VirtualVariableCombine { Id = 34 });

        settingsService.SetupSet(service => service.VirtualVariables = It.Is<List<VirtualVariable>>(list => list.Count == 0)).Verifiable();

        configuration.RemoveVirtualVariable(new VirtualVariableCombine { Id = 21 });
        
        settingsService.VerifyAll();
    }

    [Fact]
    public void TestStoreVirtualVariable()
    {
        List<VirtualVariable> virtualVariables =
            [new VirtualVariableCombine { Id = 21, ForFileName = "some_file.sav" }];
        
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupGet(service => service.VirtualVariables).Returns(virtualVariables);
        
        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());

        settingsService.SetupSet(service => service.VirtualVariables = It.Is<List<VirtualVariable>>(list => list.Count == 1)).Verifiable();

        configuration.StoreVirtualVariable(new VirtualVariableCombine { Id = 21 });
        
        settingsService.SetupSet(service => service.VirtualVariables = It.Is<List<VirtualVariable>>(list => list.Count == 2)).Verifiable();

        configuration.StoreVirtualVariable(new VirtualVariableCombine { Id = 34 });
        
        settingsService.VerifyAll();
    }

    class MockedConfiguration : Configuration
    {
        public override List<IDataProviderConfiguration> GetDataProviderConfigurations()
        {
            return
            [
                new DataverseConfiguration { Id = 12, Name = "no match", Url = "https://nomatch.org", ApiToken = "token" },
                new DataverseConfiguration { Id = 13, Name = "match", Url = "https://match.org", ApiToken = "token" },
            ];
        }
    }
}