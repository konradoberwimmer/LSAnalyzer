using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System.Collections.ObjectModel;
using System.Globalization;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestIsZero
    {
        [Theory, MemberData(nameof(ConvertTestData))]
        public void TestConvert(object? testObject, bool expected)
        {
            IsZero isZeroConverter = new();

            Assert.Equal(expected, isZeroConverter.Convert(testObject, Type.GetType("bool")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object?[]> ConvertTestData =>
            new List<object?[]>
            {
                new object?[] { null, false },
                new object?[] { 2, false },
                new object?[] { 0, true },
            };
    }
}
