using System.Runtime.InteropServices;
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
    [InlineData(PluginTools.Validity.FileNotFound, "not_here")]
    [InlineData(PluginTools.Validity.FileNotZip, "not_a_zip_file")]
    [InlineData(PluginTools.Validity.ManifestNotFound, "LSAnalyzerDataReaderXlsx_MissingManifest.zip")]
    [InlineData(PluginTools.Validity.ManifestCorrupt, "LSAnalyzerDataReaderXlsx_CorruptManifest.zip")]
    [InlineData(PluginTools.Validity.DllNotFound, "LSAnalyzerDataReaderXlsx_WrongManifestMissingDll.zip")]
    [InlineData(PluginTools.Validity.AssemblyInaccessible, "LSAnalyzerDataReaderXlsx_WrongManifestBadDll.zip")]
    [InlineData(PluginTools.Validity.PluginTypeUndefined, "LSAnalyzerDataReaderXlsx_WrongManifestUndefinedType.zip")]
    [InlineData(PluginTools.Validity.PluginNotCreatable, "LSAnalyzerDataProviderDataverse_WrongType.zip")]
    [InlineData(PluginTools.Validity.PluginNotCreatable, "LSAnalyzerDataReaderXlsx_WrongType.zip")]
    [InlineData(PluginTools.Validity.Valid, "LSAnalyzerDataReaderXlsx.zip")]
    [InlineData(PluginTools.Validity.Valid, "LSAnalyzerDataProviderDataverse.zip")]
    public void TestIsValidPlugin(PluginTools.Validity validity, string fileName)
    {
        if (validity is PluginTools.Validity.PluginTypeUndefined or PluginTools.Validity.PluginNotCreatable or PluginTools.Validity.Valid && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        Assert.Equal(validity, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", fileName ])
        ));
    }
}