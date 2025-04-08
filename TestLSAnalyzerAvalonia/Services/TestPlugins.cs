using System.Runtime.InteropServices;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerDataProviderDataverse;
using LSAnalyzerDataReaderXlsx;
using Moq;

namespace TestLSAnalyzerAvalonia.Services;

public class TestPlugins
{
    [Fact]
    public void TestConstructor()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx" ]),
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataProviderDataverse" ]),
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins" ])
        ]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        Assert.Single(plugins.DataReaderPlugins);
        Assert.Single(plugins.DataProviderPlugins);
    }
    
    [Fact]
    public void TestLoadPlugin()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        Assert.Null(plugins.LoadPlugin(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "not_here" ])));

        var exception = Record.Exception(() =>
            Assert.Null(plugins.LoadPlugin(Path.Combine([
                Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "not_a_zip_file"
            ]))));
        Assert.NotNull(exception);
        
        Assert.NotNull(plugins.LoadPlugin(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx.dll" ])));
    }

    [Fact]
    public void TestCreatePlugin()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        var assembly = plugins.LoadPlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx.dll"
        ]));
        
        Assert.NotNull(assembly);
        
        Assert.Null(plugins.CreatePlugin<IDataProviderPlugin>(assembly));

        Assert.NotNull(plugins.CreatePlugin<IDataReaderPlugin>(assembly));
    }

    [Theory]
    [ClassData(typeof(TestIsValidPluginData))]
    public void TestIsValidPlugin(IPlugins.Validity validity, IPluginCommons.Manifest? manifest, string fileName)
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        if (validity is IPlugins.Validity.PluginTypeUndefined or IPlugins.Validity.PluginNotCreatable or IPlugins.Validity.Valid && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Assert.Equal((validity, manifest), plugins.IsValidPluginZip(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", fileName ])
        ));
    }

    private class TestIsValidPluginData : TheoryData<IPlugins.Validity, IPluginCommons.Manifest?, string>
    {
        public TestIsValidPluginData()
        {
            Add(IPlugins.Validity.FileNotFound, null, "not_here");
            Add(IPlugins.Validity.FileNotZip, null, "not_a_zip_file");
            Add(IPlugins.Validity.ManifestNotFound, null, "LSAnalyzerDataReaderXlsx_MissingManifest.zip");
            Add(IPlugins.Validity.ManifestCorrupt, null, "LSAnalyzerDataReaderXlsx_CorruptManifest.zip");
            Add(IPlugins.Validity.DllNotFound, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXls.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx_WrongManifestMissingDll.zip");
            Add(IPlugins.Validity.AssemblyInaccessible, new IPluginCommons.Manifest { Dll = "not_a_zip_file", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx_WrongManifestBadDll.zip");
            Add(IPlugins.Validity.PluginTypeUndefined, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.Undefined }, "LSAnalyzerDataReaderXlsx_WrongManifestUndefinedType.zip");
            Add(IPlugins.Validity.PluginNotCreatable, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataProviderDataverse.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataProviderDataverse_WrongType.zip");
            Add(IPlugins.Validity.PluginNotCreatable, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.DataProvider }, "LSAnalyzerDataReaderXlsx_WrongType.zip");
            Add(IPlugins.Validity.Valid, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx.zip");
            Add(IPlugins.Validity.Valid, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataProviderDataverse.dll", Type = IPluginCommons.PluginTypes.DataProvider }, "LSAnalyzerDataProviderDataverse.zip");
        }
    }

    [Fact]
    public void TestPreservePlugin()
    {   
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        Assert.Null(plugins.PreservePlugin("/not_here"));

        Assert.Null(plugins.PreservePlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "not_a_zip_file"
        ])));

        Assert.NotNull(plugins.PreservePlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx.zip"
        ])));
    }

    [Fact]
    public void TestAddPlugin()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([]);
        
        Plugins plugins = new(appConfiguration.Object);
        
        plugins.AddPlugin(new DataReaderXlsx(), "/somewhereA");
        plugins.AddPlugin(new DataProviderDataverse(), "/somewhereB");
        
        Assert.Single(plugins.DataReaderPlugins);
        Assert.Single(plugins.DataProviderPlugins);
        
        appConfiguration.Verify(x => x.StorePreservedPluginLocation(It.Is<string>(location => location == "/somewhereA")), Times.Once);
        appConfiguration.Verify(x => x.StorePreservedPluginLocation(It.Is<string>(location => location == "/somewhereB")), Times.Once);
    }

    [Fact]
    public void TestRemovePlugin()
    {
        var tempDirectoryA = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectoryA);
        var tempDirectoryB = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectoryB);
        
        foreach(var file in Directory.GetFiles(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataReaderXlsx" ])))
            File.Copy(file, Path.Combine(tempDirectoryA, Path.GetFileName(file)));
        foreach(var file in Directory.GetFiles(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestServicesPlugins", "LSAnalyzerDataProviderDataverse" ])))
            File.Copy(file, Path.Combine(tempDirectoryB, Path.GetFileName(file)));
        
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.SetupGet(x => x.PreservedPluginLocations).Returns([ tempDirectoryA, tempDirectoryB ]);

        var mockPlugin = new Mock<IPluginCommons>();
        mockPlugin.SetupGet(x => x.PluginType).Returns(IPluginCommons.PluginTypes.Undefined);
        
        Plugins plugins = new(appConfiguration.Object);
        
        Directory.Delete(tempDirectoryA, true);
        
        plugins.RemovePlugin(mockPlugin.Object);
        plugins.RemovePlugin(plugins.DataReaderPlugins.First());
        plugins.RemovePlugin(plugins.DataProviderPlugins.First());
        
        Assert.Empty(plugins.DataReaderPlugins);
        Assert.Empty(plugins.DataProviderPlugins);
        
        appConfiguration.Verify(x => x.RemovePreservedPluginLocation(It.Is<string>(location => location == tempDirectoryA)), Times.Once);
        appConfiguration.Verify(x => x.RemovePreservedPluginLocation(It.Is<string>(location => location == tempDirectoryB)), Times.Once);
    }
}