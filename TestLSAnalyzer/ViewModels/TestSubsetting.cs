using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestSubsetting
    {
        [Fact]
        public void TestFillDatasetVariables()
        {
            AnalysisConfiguration dummyAnalysisConfiguration = new();

            var mockRservice = new Mock<Rservice>();
            mockRservice.Setup(rservice => rservice.GetCurrentDatasetVariables(dummyAnalysisConfiguration, true)).Returns(new List<Variable>()
            {
                new(1, "x", false),
                new(2, "y", false),
                new(3, "z", false),
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
            var mockRservice = new Mock<Rservice>();
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
            var mockRservice = new Mock<Rservice>();
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
        public async Task TestUseSubsetting()
        {
            var mockRservice = new Mock<Rservice>();
            mockRservice.Setup(rservice => rservice.TestSubsetting("invalid", null)).Returns(new SubsettingInformation() { ValidSubset = false });
            mockRservice.Setup(rservice => rservice.TestSubsetting("valid", null)).Returns(new SubsettingInformation() { ValidSubset = true });
            mockRservice.Setup(rservice => rservice.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<string?>())).Returns(true);

            Subsetting subsettingViewModel = new(mockRservice.Object, new Mock<Configuration>().Object);
            subsettingViewModel.AnalysisConfiguration = new() { ModeKeep = false, DatasetType = new() { Id = 1234 } };

            string? message = null;
            WeakReferenceMessenger.Default.Register<SetSubsettingExpressionMessage>(this, (r, m) =>
            {
                message = m.Value;
            });

            subsettingViewModel.UseSubsettingCommand.Execute(null);
            await Task.Delay(100);
            Assert.Null(message);
            Assert.Null(subsettingViewModel.SubsettingInformation);

            subsettingViewModel.SubsetExpression = "invalid";
            subsettingViewModel.UseSubsettingCommand.Execute(null);
            await Task.Delay(100);
            Assert.Null(message);
            Assert.NotNull(subsettingViewModel.SubsettingInformation);
            Assert.False(subsettingViewModel.SubsettingInformation.ValidSubset);

            subsettingViewModel.SubsetExpression = "valid";
            subsettingViewModel.UseSubsettingCommand.Execute(null);
            await Task.Delay(100);
            Assert.NotNull(message);
            Assert.Equal("valid", message);

            message = null;
            subsettingViewModel.AnalysisConfiguration = new() { ModeKeep = true, DatasetType = new() { Id = 1234 } };
            subsettingViewModel.UseSubsettingCommand.Execute(null);
            await Task.Delay(100);
            Assert.NotNull(message);
            Assert.Equal("valid", message);
        }

        [Fact]
        public async Task TestClearSubsetting()
        {
            var mockRservice = new Mock<Rservice>();
            mockRservice.Setup(rservice => rservice.TestSubsetting("valid", null)).Returns(new SubsettingInformation() { ValidSubset = true });
            mockRservice.Setup(rservice => rservice.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<string?>())).Returns(true);

            Subsetting subsettingViewModel = new(mockRservice.Object, new Mock<Configuration>().Object);
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
            await Task.Delay(100);
            Assert.True(messageReceived);
            Assert.NotNull(message);
            Assert.Equal("valid", message);

            messageReceived = false;
            message = null;
            subsettingViewModel.ClearSubsettingCommand.Execute(null);
            await Task.Delay(100);
            Assert.True(messageReceived);
            Assert.Null(message);
        }
    }
}
