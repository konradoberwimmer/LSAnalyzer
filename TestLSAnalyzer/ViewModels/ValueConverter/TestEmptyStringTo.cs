using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestEmptyStringTo
{
    [Theory, MemberData(nameof(ConvertTestCases))]
    public void TestConvert(string? value, string? parameter, string expected)
    {
        EmptyStringTo emptyStringToConverter = new();
        Assert.Equal(expected, emptyStringToConverter.Convert(value, Type.GetType("string")!, parameter, CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object?[]> ConvertTestCases =>
        new List<object?[]>
        {
                new object?[] { null, "", null },
                new object?[] { 2, "not entry", 2 },
                new object?[] { "", null, "" },
                new object?[] { "", "no entry", "no entry" },
                new object?[] { "entry", "no entry", "entry" },
        };
}
