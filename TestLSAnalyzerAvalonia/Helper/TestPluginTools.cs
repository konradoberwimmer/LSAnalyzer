using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using LSAnalyzerAvalonia.Helper;
using LSAnalyzerAvalonia.IPlugins;

namespace TestLSAnalyzerAvalonia.Helper;

public class TestPluginTools
{
    [Fact]
    public void TestLoadPlugin()
    {
        Assert.Null(PluginTools.LoadPlugin(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_here" ])));

        var exception = Record.Exception(() =>
            Assert.Null(PluginTools.LoadPlugin(Path.Combine([
                Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_a_zip_file"
            ]))));
        Assert.NotNull(exception);
        
        Assert.NotNull(PluginTools.LoadPlugin(Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.dll" ])));
    }

    [Fact]
    public void TestCreatePlugin()
    {
        var assembly = PluginTools.LoadPlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.dll"
        ]));
        
        Assert.NotNull(assembly);
        
        Assert.Null(PluginTools.CreatePlugin<IDataProviderPlugin>(assembly));

        Assert.NotNull(PluginTools.CreatePlugin<IDataReaderPlugin>(assembly));
    }

    [Theory]
    [ClassData(typeof(TestIsValidPluginData))]
    public void TestIsValidPlugin(PluginTools.Validity validity, IPluginCommons.Manifest? manifest, string fileName)
    {
        if (validity is PluginTools.Validity.PluginTypeUndefined or PluginTools.Validity.PluginNotCreatable or PluginTools.Validity.Valid && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Assert.Equal((validity, manifest), PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", fileName ])
        ));
    }

    private class TestIsValidPluginData : TheoryData<PluginTools.Validity, IPluginCommons.Manifest?, string>
    {
        public TestIsValidPluginData()
        {
            Add(PluginTools.Validity.FileNotFound, null, "not_here");
            Add(PluginTools.Validity.FileNotZip, null, "not_a_zip_file");
            Add(PluginTools.Validity.ManifestNotFound, null, "LSAnalyzerDataReaderXlsx_MissingManifest.zip");
            Add(PluginTools.Validity.ManifestCorrupt, null, "LSAnalyzerDataReaderXlsx_CorruptManifest.zip");
            Add(PluginTools.Validity.DllNotFound, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXls.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx_WrongManifestMissingDll.zip");
            Add(PluginTools.Validity.AssemblyInaccessible, new IPluginCommons.Manifest { Dll = "not_a_zip_file", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx_WrongManifestBadDll.zip");
            Add(PluginTools.Validity.PluginTypeUndefined, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.Undefined }, "LSAnalyzerDataReaderXlsx_WrongManifestUndefinedType.zip");
            Add(PluginTools.Validity.PluginNotCreatable, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataProviderDataverse.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataProviderDataverse_WrongType.zip");
            Add(PluginTools.Validity.PluginNotCreatable, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.DataProvider }, "LSAnalyzerDataReaderXlsx_WrongType.zip");
            Add(PluginTools.Validity.Valid, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataReaderXlsx.dll", Type = IPluginCommons.PluginTypes.DataReader }, "LSAnalyzerDataReaderXlsx.zip");
            Add(PluginTools.Validity.Valid, new IPluginCommons.Manifest { Dll = "LSAnalyzerDataProviderDataverse.dll", Type = IPluginCommons.PluginTypes.DataProvider }, "LSAnalyzerDataProviderDataverse.zip");
        }
    }

    [Fact]
    public void TestPreservePlugin()
    {
        Assert.Null(PluginTools.PreservePlugin("/not_here"));

        Assert.Null(PluginTools.PreservePlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_a_zip_file"
        ])));

        Assert.NotNull(PluginTools.PreservePlugin(Path.Combine([
            Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip"
        ])));
    }
}