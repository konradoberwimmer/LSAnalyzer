using LSAnalyzerAvalonia.Helper;

namespace TestLSAnalyzerAvalonia.Helper;

public class TestStringFormats
{
    [Theory, MemberData(nameof(GetMaxRelevantDigitsTestCases))]
    public void TestGetMaxRelevantDigits(double[] values, int maxDigits, int expected)
    {
        Assert.Equal(expected, StringFormats.GetMaxRelevantDigits(values, maxDigits));
    }
    public static IEnumerable<object[]> GetMaxRelevantDigitsTestCases =>
        new List<object[]>
        {
            new object[] { new double[] { 1.000, -2.000, 3.000 }, 3, 0},
            new object[] { new double[] { 1.000, -2.100, 3.000 }, 3, 1},
            new object[] { new double[] { 1.000, -2.000, 3.030 }, 3, 2},
            new object[] { new double[] { 1.123, -2.000, 3.030 }, 3, 3},
            new object[] { new double[] { 1.200, -2.322, 3.020 }, 2, 2},
        };

    [Theory, MemberData(nameof(EncapsulateRegexTestCases))]
    public void TestEncapsulateRegex(string? regex, bool encapsulate, string? expected)
    {
        Assert.Equal(expected, StringFormats.EncapsulateRegex(regex, encapsulate));
    }

    public static IEnumerable<object?[]> EncapsulateRegexTestCases =>
        new List<object?[]>
        {
            new object?[] { null, true, null },
            new object?[] { "myRegex", false, "myRegex" },
            new object?[] { "myRegex", true, "^myRegex$" },
            new object?[] { "^myRegex$", true, "^myRegex$" },
        };
}