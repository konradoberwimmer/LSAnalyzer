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
}