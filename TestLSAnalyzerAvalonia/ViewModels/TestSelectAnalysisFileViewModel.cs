using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerDataReaderXlsx;
using Moq;

namespace TestLSAnalyzerAvalonia.ViewModels;

public class TestSelectAnalysisFileViewModel
{
    [Fact]
    public void TestConstructor()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl));
        
        Assert.Equal(2, viewModel.DataReaderPlugins.Count);
        Assert.Null(viewModel.SelectedDataReaderPlugin);
    }

    [Fact]
    public void TestIsReadyToLoad()
    {
        DataReaderCsv dataReaderCsv = new();
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        
        SelectAnalysisFileViewModel viewModel = new([ dataReaderCsv ], pluginService.Object, typeof(UserControl));
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.First();
        
        Assert.True(viewModel.IsReadyToLoad);
        
        ((DataReaderCsvViewModel)((DataReaderCsv)viewModel.SelectedDataReaderPlugin).ViewModel).SeparatorCharacter = string.Empty;
        
        Assert.False(viewModel.IsReadyToLoad);
        
        viewModel.SelectedDataReaderPlugin = viewModel.DataReaderPlugins.Last();
        
        Assert.True(viewModel.IsReadyToLoad);
    }
}