using System.Text.RegularExpressions;
using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;
using LSAnalyzerAvalonia.IPlugins;
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
    public void TestGuessDatasetTypeCommand()
    {
        var dataReaderCsv = new Mock<IDataReaderPlugin>();
        dataReaderCsv.SetupGet(reader => reader.ViewModel).Returns(new DataReaderCsvViewModel());
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv.Object ], pluginService.Object, typeof(UserControl), appConfiguration.Object);
        
        viewModel.GuessDatasetTypeCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.ShowMessage);
        
        viewModel.FilePath = "/some/crazy/path";
        viewModel.GuessDatasetTypeCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.ShowMessage);
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.First();
        dataReaderCsv.Setup(reader => reader.ReadFileHeader(It.IsAny<string>())).Returns((false, []));
        viewModel.GuessDatasetTypeCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Failed to read file", viewModel.Message);
        
        viewModel.ShowMessage = false;
        dataReaderCsv.Setup(reader => reader.ReadFileHeader(It.IsAny<string>())).Returns((true, [ "a", "b", "c" ]));
        viewModel.GuessDatasetTypeCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("No compatible dataset type", viewModel.Message);
        
        viewModel.ShowMessage = false;
        dataReaderCsv.Setup(reader => reader.ReadFileHeader(It.IsAny<string>())).Returns((true, [ "TCHWGT", "IDTEACH", "TRWGT01", "TRWGT02", "TRWGT03" ]));
        viewModel.GuessDatasetTypeCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedDatasetType); // has to be TALIS teacher level
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.ShowMessage);

        viewModel.SelectedDatasetType = null;
        dataReaderCsv.Setup(reader => reader.ReadFileHeader(It.IsAny<string>())).Returns((true, [ "TCHWGT", "MATWGT", "SCIWGT", "IDSTUD", "JKZONE", "JKREP" ]));
        viewModel.GuessDatasetTypeCommand.Execute(null);

        Assert.Null(viewModel.SelectedDatasetType);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Multiple compatible dataset types", viewModel.Message);
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