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

    [Fact]
    public void TestIsValidPlugin()
    {
        Assert.Equal(PluginTools.Validity.FileNotFound, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_here" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.FileNotZip, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "not_a_zip_file" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.ManifestNotFound, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx_MissingManifest.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.ManifestCorrupt, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx_CorruptManifest.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.DllNotFound, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx_WrongManifestMissingDll.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.AssemblyInaccessible, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx_WrongManifestBadDll.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.PluginTypeUndefined, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip" ]), 
            IPluginCommons.PluginTypes.Undefined
        ));
        
        Assert.Equal(PluginTools.Validity.PluginNotCreatable, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataProviderDataverse.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.PluginNotCreatable, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip" ]), 
            IPluginCommons.PluginTypes.DataProvider
        ));
        
        Assert.Equal(PluginTools.Validity.Valid, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataReaderXlsx.zip" ]), 
            IPluginCommons.PluginTypes.DataReader
        ));
        
        Assert.Equal(PluginTools.Validity.Valid, PluginTools.IsValidPlugin(
            Path.Combine([ Directory.GetCurrentDirectory(), "_testFiles", "TestPluginTools", "LSAnalyzerDataProviderDataverse.zip" ]), 
            IPluginCommons.PluginTypes.DataProvider
        ));
    }
}