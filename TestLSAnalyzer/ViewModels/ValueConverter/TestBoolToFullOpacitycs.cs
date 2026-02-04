using LSAnalyzer.ViewModels.ValueConverter;
using System.Globalization;
using System.Windows.Media;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestBoolToFullOpacity
{
    [Theory, MemberData(nameof(ConvertTestData))]
    public void TestConvert(bool? boolVal, double expected)
    {
        var converter = new BoolToFullOpacity();

        Assert.Equal(expected, converter.Convert(boolVal, typeof(double), "", CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object?[]> ConvertTestData =>
        new List<object?[]>()
        {
            new object?[] { null, 0.5 },
            new object?[] { false, 0.5 },
            new object?[] { true, 1.0 },
        };
}
