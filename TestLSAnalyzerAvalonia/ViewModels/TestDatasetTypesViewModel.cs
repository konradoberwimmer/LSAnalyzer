using System.Text.RegularExpressions;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using Moq;

namespace TestLSAnalyzerAvalonia.ViewModels;

public class TestDatasetTypesViewModel
{
    [Fact]
    public void TestConstructor()
    {
        // failed configuration
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.DatasetTypesConfigFilePath).Returns("/");
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns((List<DatasetType>?)null);
        appConfiguration.Setup(x => x.RestoreDefaultDatasetTypesStorage()).Returns(false);
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches(new Regex("at /!"), viewModel.Message);
        
        // restore default configuration
        appConfiguration.SetupSequence(x => x.GetStoredDatasetTypes()).Returns((List<DatasetType>?)null).Returns(DatasetType.CreateDefaultDatasetTypes());
        appConfiguration.Setup(x => x.RestoreDefaultDatasetTypesStorage()).Returns(true);
        
        viewModel = new(appConfiguration.Object);
        
        Assert.False(viewModel.ShowMessage);
        Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count, viewModel.DatasetTypes.Count);
        
        // existing configuration
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns([]);
        
        viewModel = new(appConfiguration.Object);
        
        Assert.False(viewModel.ShowMessage);
        Assert.Empty(viewModel.DatasetTypes);
    }
}