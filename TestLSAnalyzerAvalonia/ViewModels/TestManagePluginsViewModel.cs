using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Office2010.Excel;
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
    public void TestAddPluginCommandFileNotFound()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([]);

        var pathFileNotFound =
            Path.Combine([Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "not_here"]);
        
        pluginService
            .Setup(x => x.IsValidPlugin(It.Is<string>(v => v == pathFileNotFound))).Returns((IPlugins.Validity.FileNotFound, null));
        
        ManagePluginsViewModel viewModel = new(pluginService.Object);
        
        viewModel.AddPluginCommand.Execute(pathFileNotFound);
        
        Assert.Empty(viewModel.Plugins);
        Assert.Matches("not found", viewModel.Message);
    }
    
    
    [Fact]
    public void TestAddPluginCommandMissingManifest()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([]);
        
        var pathMissingManifest =
            Path.Combine([Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx_MissingManifest.zip"]);
        
        pluginService
            .Setup(x => x.IsValidPlugin(It.Is<string>(v => v == pathMissingManifest))).Returns((IPlugins.Validity.ManifestNotFound, null));
        
        ManagePluginsViewModel viewModel = new(pluginService.Object);
        
        viewModel.AddPluginCommand.Execute(pathMissingManifest);
        
        Assert.Empty(viewModel.Plugins);
        Assert.Matches("not a valid.*plugin", viewModel.Message);
    }
    
    
    [Fact]
    public void TestAddPluginCommandCannotPreserve()
    {
        var pluginService = new Mock<IPlugins>();
        pluginService.SetupGet(x => x.DataReaderPlugins).Returns([]);
        pluginService.SetupGet(x => x.DataProviderPlugins).Returns([]);

        var pathCannotPreserve =
            Path.Combine([Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "cannot_preserve"]);
        
        pluginService
            .Setup(x => x.IsValidPlugin(It.Is<string>(v => v == pathCannotPreserve))).Returns((IPlugins.Validity.Valid, new IPluginCommons.Manifest { Dll = string.Empty, Type = IPluginCommons.PluginTypes.DataProvider }));
        pluginService
            .Setup(x => x.PreservePlugin(It.Is<string>(v => v == pathCannotPreserve))).Returns((string?)null);
        
        ManagePluginsViewModel viewModel = new(pluginService.Object);
        
        viewModel.AddPluginCommand.Execute(pathCannotPreserve);
        
        Assert.Empty(viewModel.Plugins);
        Assert.Matches("not preserve", viewModel.Message);
    }
    
    
    [Fact]
    public void TestAddPluginCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins pluginService = new(appConfiguration.Object);

        var pathDataReaderXlsx =
            Path.Combine([Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx.zip"]);
        var pathDataProviderDataverse =
            Path.Combine([Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataProviderDataverse.zip"]);
        
        ManagePluginsViewModel viewModel = new(pluginService);
        
        viewModel.AddPluginCommand.Execute(pathDataReaderXlsx);
        viewModel.AddPluginCommand.Execute(pathDataProviderDataverse);
        
        Assert.Equal(2, viewModel.Plugins.Count);
        Assert.Matches($"Added.*{ nameof(DataProviderDataverse) }", viewModel.Message);
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