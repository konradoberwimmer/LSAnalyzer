using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Moq;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.ViewModels;

public class TestSubsetting
{
    [Fact]
    public void TestFillDatasetVariables()
    {
        AnalysisConfiguration dummyAnalysisConfiguration = new();

        var mockRservice = new Mock<IRservice>();
        mockRservice.Setup(rservice => rservice.GetCurrentDatasetVariables(dummyAnalysisConfiguration, It.IsAny<List<VirtualVariable>>(), true)).Returns(new List<Variable>()
        {
            new(1, "x"),
            new(2, "y"),
            new(3, "z"),
        });
        
        Subsetting subsettingViewModel = new(mockRservice.Object, new Mock<Configuration>().Object);

        Assert.Empty(subsettingViewModel.AvailableVariables);

        subsettingViewModel.AnalysisConfiguration = dummyAnalysisConfiguration;

        Assert.Equal(3, subsettingViewModel.AvailableVariables.Count);
        Assert.Contains("y", subsettingViewModel.AvailableVariables.Select(x => x.Name));
    }

    [Fact]
    public void TestSetCurrentSubsetting()
    {
        var mockRservice = new Mock<IRservice>();
        Subsetting subsettingViewModel = new(mockRservice.Object, new Mock<Configuration>().Object);

        Assert.False(subsettingViewModel.IsCurrentlySubsetting);
        Assert.Null(subsettingViewModel.SubsetExpression);

        subsettingViewModel.SetCurrentSubsetting("expression");

        Assert.True(subsettingViewModel.IsCurrentlySubsetting);
        Assert.NotNull(subsettingViewModel.SubsetExpression);
    }

    [Fact]
    public void TestTestSubsetting()
    {
        var mockRservice = new Mock<IRservice>();
        mockRservice.Setup(rservice => rservice.TestSubsetting("invalid", null)).Returns(new SubsettingInformation() { ValidSubset = false });
        mockRservice.Setup(rservice => rservice.TestSubsetting("valid", null)).Returns(new SubsettingInformation() { ValidSubset = true });

        Subsetting subsettingViewModel = new(mockRservice.Object, new Mock<Configuration>().Object);
        
        subsettingViewModel.SubsetExpression = "invalid";
        subsettingViewModel.TestSubsettingCommand.Execute(null);
        
        Policy.Handle<NotNullException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(subsettingViewModel.SubsettingInformation));
        Assert.False(subsettingViewModel.SubsettingInformation!.ValidSubset);

        subsettingViewModel.SubsetExpression = "valid";
        Assert.Null(subsettingViewModel.SubsettingInformation);
        subsettingViewModel.TestSubsettingCommand.Execute(null);
        
        Policy.Handle<NotNullException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(subsettingViewModel.SubsettingInformation));
        Assert.True(subsettingViewModel.SubsettingInformation!.ValidSubset);
    }

    [Fact]
    public void TestUseSubsetting()
    {
        var mockRservice = new Mock<IRservice>();
        mockRservice.Setup(rservice => rservice.TestSubsetting("invalid", null)).Returns(new SubsettingInformation() { ValidSubset = false });
        mockRservice.Setup(rservice => rservice.TestSubsetting("valid", null)).Returns(new SubsettingInformation() { ValidSubset = true });
        mockRservice.Setup(rservice => rservice.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), It.IsAny<string?>())).Returns(true);

        var configuration = new Mock<Configuration>();
        configuration.Setup(conf => conf.GetVirtualVariablesFor(It.IsAny<string>(), It.IsAny<DatasetType>())).Returns([]).Verifiable();
        
        Subsetting subsettingViewModel = new(mockRservice.Object, configuration.Object);
        subsettingViewModel.AnalysisConfiguration = new() { ModeKeep = false, DatasetType = new() { Id = 1234 } };

        string? message = null;
        WeakReferenceMessenger.Default.Register<SetSubsettingExpressionMessage>(this, (r, m) =>
        {
            message = m.Value;
        });

        subsettingViewModel.UseSubsettingCommand.Execute(null);
        Assert.Null(message);
        Assert.Null(subsettingViewModel.SubsettingInformation);

        subsettingViewModel.SubsettingInformation = new SubsettingInformation { ValidSubset = true };
        subsettingViewModel.SubsetExpression = "invalid";
        subsettingViewModel.UseSubsettingCommand.Execute(null);
        
        Policy.Handle<FalseException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.False(subsettingViewModel.SubsettingInformation?.ValidSubset));
        Assert.Null(message);
        Assert.NotNull(subsettingViewModel.SubsettingInformation);
        
        subsettingViewModel.SubsetExpression = "valid";
        subsettingViewModel.UseSubsettingCommand.Execute(null);
        
        Policy.Handle<NotNullException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(message));
        Assert.Equal("valid", message);

        message = null;
        subsettingViewModel.AnalysisConfiguration = new() { ModeKeep = true, DatasetType = new() { Id = 1234 } };
        subsettingViewModel.UseSubsettingCommand.Execute(null);
        
        Policy.Handle<NotNullException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(message));
        Assert.Equal("valid", message);
        
        configuration.Verify();
    }

    [Fact]
    public void TestClearSubsetting()
    {
        var mockRservice = new Mock<IRservice>();
        mockRservice.Setup(rservice => rservice.TestSubsetting("valid", null)).Returns(new SubsettingInformation() { ValidSubset = true });
        mockRservice.Setup(rservice => rservice.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), It.IsAny<string?>())).Returns(true);

        var configuration = new Mock<Configuration>();
        configuration.Setup(conf => conf.GetVirtualVariablesFor(It.IsAny<string>(), It.IsAny<DatasetType>())).Returns([]).Verifiable();
        
        Subsetting subsettingViewModel = new(mockRservice.Object, configuration.Object);
        subsettingViewModel.AnalysisConfiguration = new() { ModeKeep = true, DatasetType = new() { Id = 1234 } };

        bool messageReceived = false;
        string? message = null;
        WeakReferenceMessenger.Default.Register<SetSubsettingExpressionMessage>(this, (r, m) =>
        {
            messageReceived = true;
            message = m.Value;
        });

        subsettingViewModel.SubsetExpression = "valid";
        subsettingViewModel.UseSubsettingCommand.Execute(null);
        
        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageReceived));
        Assert.NotNull(message);
        Assert.Equal("valid", message);

        messageReceived = false;
        message = null;
        subsettingViewModel.ClearSubsettingCommand.Execute(null);
        
        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageReceived));
        Assert.Null(message);
        
        configuration.Verify();
    }
}
