using System.Globalization;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestVirtualVariableTypeToName
{
    [Fact]
    public void TestConvert()
    {
        VirtualVariableTypeToName converter = new();
        
        Assert.Empty(converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture) as string ?? "not a string");
        Assert.Empty(converter.Convert(new DateTime(), typeof(string), null, CultureInfo.InvariantCulture) as string ?? "not a string");
        Assert.Empty(converter.Convert(typeof(DateTime), typeof(string), null, CultureInfo.InvariantCulture) as string ?? "not a string");
        Assert.Equal("Combine", converter.Convert(typeof(VirtualVariableCombine), typeof(string), null, CultureInfo.InvariantCulture) as string ?? "not a string");
    }
}