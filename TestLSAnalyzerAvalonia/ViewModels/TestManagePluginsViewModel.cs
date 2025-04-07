using System.Runtime.InteropServices;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerDataProviderDataverse;
using LSAnalyzerDataReaderXlsx;
using Moq;

namespace TestLSAnalyzerAvalonia.ViewModels;

[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
public class NonParallelCollection;

[Collection(nameof(NonParallelCollection))]
public class TestManagePluginsViewModel
{
    [Fact]
    public void TestConstructor()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([ new DataReaderXlsx() ]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([ new DataProviderDataverse() ]);

        ManagePluginsViewModel viewModel = new(pluginService.Object);
        
        Assert.Equal(2, viewModel.Plugins.Count);
    }

    [Fact]
    public void TestAddPluginCommand()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([]);
        
        ManagePluginsViewModel viewModel = new(pluginService.Object);
        
        viewModel.AddPluginCommand.Execute(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_here" ]));
        
        Assert.Empty(viewModel.Plugins);
        Assert.Matches("not found", viewModel.Message);
        
        viewModel.AddPluginCommand.Execute(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx_MissingManifest.zip" ]));
        
        Assert.Empty(viewModel.Plugins);
        Assert.Matches("not a valid.*plugin", viewModel.Message);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        viewModel.AddPluginCommand.Execute(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip" ]));
        viewModel.AddPluginCommand.Execute(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataProviderDataverse.zip" ]));
        
        pluginService.Verify(x => x.AddPlugin(It.IsAny<IPluginCommons>(), It.IsAny<string>()), Times.Exactly(2));
        Assert.Equal(2, viewModel.Plugins.Count);
        Assert.Matches($"Added.*{ nameof(DataProviderDataverse) }", viewModel.Message);

        // this raises code coverage to 100% but is a risky test
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
                
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var permissionsStored = File.GetUnixFileMode(localAppData);
        File.SetUnixFileMode(localAppData, UnixFileMode.UserRead);
        
        viewModel.AddPluginCommand.Execute(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip" ]));
        
        Assert.Equal(2, viewModel.Plugins.Count);
        Assert.Matches("not preserve", viewModel.Message);
        
        File.SetUnixFileMode(localAppData, permissionsStored);
    }
    
    [Fact]
    public void TestRemovePluginCommand()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([]);

        ManagePluginsViewModel viewModel = new(pluginService.Object);
        viewModel.Plugins.Add(new DataReaderXlsx());
        viewModel.Plugins.Add(new DataProviderDataverse());
        
        viewModel.RemovePluginCommand.Execute(viewModel.Plugins.First());
        
        pluginService.Verify(x => x.RemovePlugin(It.IsAny<IPluginCommons>()), Times.Once);
        Assert.Single(viewModel.Plugins);
        Assert.Matches($"Removed.*{ nameof(DataReaderXlsx) }", viewModel.Message);
        
        viewModel.RemovePluginCommand.Execute(viewModel.Plugins.First());

        pluginService.Verify(x => x.RemovePlugin(It.IsAny<IPluginCommons>()), Times.Exactly(2));
        Assert.Empty(viewModel.Plugins);
        Assert.Matches($"Removed.*{ nameof(DataProviderDataverse) }", viewModel.Message);
    }
}