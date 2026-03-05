using System.Globalization;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestEnumCamelCase
{
    [Theory, MemberData(nameof(TestEnumCamelCaseCases))]
    public void TestConvert(object? value, object? expected)
    {
        EnumCamelCase converter = new();
        
        Assert.Equal(expected, converter.Convert(value, typeof(object), null, CultureInfo.InvariantCulture));
    }
    
    public static IEnumerable<object?[]> TestEnumCamelCaseCases =>
        new List<object?[]>
        {
            new object?[] { null, null },
            new object?[] { 1, 1 },
            new object?[] { VirtualVariableCombine.CombinationFunction.Mean, "Mean" },
            new object?[] { VirtualVariableCombine.CombinationFunction.FactorScores, "Factor scores" },
        };
}