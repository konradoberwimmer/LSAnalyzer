using System.Text.RegularExpressions;
using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerDataReaderXlsx;
using Moq;
using Xunit.Sdk;

namespace TestLSAnalyzerAvalonia.ViewModels;

public class TestSelectAnalysisFileViewModel
{
    [Fact]
    public void TestConstructor()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        var appConfiguration = new Mock<IAppConfiguration>();
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        Assert.Equal(2, viewModel.DataReaderPlugins.Count);
        Assert.Null(viewModel.SelectedDataReaderPlugin);
    }

    [Fact]
    public void TestChoosingFilePathSelectsDataReaderPlugin()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        Assert.Null(viewModel.SelectedDataReaderPlugin);
        
        viewModel.FilePath = "/some/crazy/path";
        
        Assert.Null(viewModel.SelectedDataReaderPlugin);
        
        viewModel.FilePath = "/some/crazy/path/to.a.not.csv/file.pdf";
        
        Assert.Null(viewModel.SelectedDataReaderPlugin);
        
        viewModel.FilePath = "/some/crazy/path/to.a.csv/mycsv.csv";
        
        Assert.NotNull(viewModel.SelectedDataReaderPlugin);
        Assert.Equal(viewModel.SelectedDataReaderPlugin, viewModel.DataReaderPlugins.First());;
    }

    [Fact]
    public void TestSelectedDatasetTypeUpdatesWeights()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.Empty(viewModel.Weights);
        Assert.Null(viewModel.SelectedWeight);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First();
        
        Assert.NotEmpty(viewModel.Weights);
        Assert.NotNull(viewModel.SelectedWeight);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First(dst => Regex.IsMatch(dst.Weight, "^[^;]*;[^;]*$"));
        
        Assert.Equal(2, viewModel.Weights.Count);
    }

    [Fact]
    public void TestIsReadyToLoad()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        Assert.False(viewModel.IsReadyToLoad);
        
        viewModel.FilePath = "/some/crazy/path";
        
        Assert.False(viewModel.IsReadyToLoad);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First();
        
        Assert.False(viewModel.IsReadyToLoad);
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.First();
        
        Assert.True(viewModel.IsReadyToLoad);
        
        ((DataReaderCsvViewModel)((DataReaderCsv)viewModel.SelectedDataReaderPlugin).ViewModel).SeparatorCharacter = string.Empty;
        
        Assert.False(viewModel.IsReadyToLoad);
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.Last();
        
        Assert.True(viewModel.IsReadyToLoad);
    }

    [Fact]
    public void TestUnregisterPluginListeners()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.First();

        Assert.PropertyChanged(viewModel, nameof(viewModel.IsReadyToLoad), () => 
            ((DataReaderCsvViewModel)((DataReaderCsv)viewModel.SelectedDataReaderPlugin).ViewModel).SeparatorCharacter = string.Empty);
        
        viewModel.UnregisterPluginListeners();
        
        Assert.Throws<PropertyChangedException>(() =>
            Assert.PropertyChanged(viewModel, nameof(viewModel.IsReadyToLoad), () => 
                ((DataReaderCsvViewModel)((DataReaderCsv)viewModel.SelectedDataReaderPlugin).ViewModel).SeparatorCharacter = ","));
    }
}