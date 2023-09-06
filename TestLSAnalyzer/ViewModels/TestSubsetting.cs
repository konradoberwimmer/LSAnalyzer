using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels
{
    public class TestSubsetting
    {
        [Fact]
        public void TestFillDatasetVariables()
        {
            AnalysisConfiguration dummyAnalysisConfiguration = new();

            var mockRservice = new Mock<Rservice>();
            mockRservice.Setup(rservice => rservice.GetCurrentDatasetVariables(dummyAnalysisConfiguration)).Returns(new List<Variable>()
            {
                new(1, "x", false),
                new(2, "y", false),
                new(3, "z", false),
            });
            
            Subsetting subsettingViewModel = new(mockRservice.Object);

            Assert.Empty(subsettingViewModel.AvailableVariables);

            subsettingViewModel.AnalysisConfiguration = dummyAnalysisConfiguration;

            Assert.Equal(3, subsettingViewModel.AvailableVariables.Count);
            Assert.Contains("y", subsettingViewModel.AvailableVariables.Select(x => x.Name));
        }

        [Fact]
        public void TestSetCurrentSubsetting()
        {
            var mockRservice = new Mock<Rservice>();
            Subsetting subsettingViewModel = new(mockRservice.Object);

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

            Subsetting subsettingViewModel = new(mockRservice.Object);
            
            subsettingViewModel.SubsetExpression = "invalid";
            subsettingViewModel.TestSubsettingCommand.Execute(null);
            Assert.NotNull(subsettingViewModel.SubsettingInformation);
            Assert.False(subsettingViewModel.SubsettingInformation.ValidSubset);

            subsettingViewModel.SubsetExpression = "valid";
            Assert.Null(subsettingViewModel.SubsettingInformation);
            subsettingViewModel.TestSubsettingCommand.Execute(null);
            Assert.NotNull(subsettingViewModel.SubsettingInformation);
            Assert.True(subsettingViewModel.SubsettingInformation.ValidSubset);
        }
    }
}
