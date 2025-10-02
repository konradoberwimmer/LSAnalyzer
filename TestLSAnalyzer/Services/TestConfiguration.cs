using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Moq;

namespace TestLSAnalyzer.Services;

public class TestConfiguration
{
    [Fact]
    public void TestGetStoredRecentSubsettingExpressions()
    {
        Configuration configuration = new();
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
        
        SystemSettings systemSettings = new(new Mock<Rservice>().Object, configuration, new Mock<Logging>().Object);
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
}