using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestDataProviderTypeName
    {
        [Theory]
        [MemberData(nameof(TestCases))]
        public void TestConvert(object? value, string expected)
        {
            DataProviderTypeName converter = new();
            Assert.Equal(expected, converter.Convert(value, typeof(string), string.Empty, CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object?[]> TestCases =>
            new List<object?[]>
            {
                new object?[] { null, string.Empty },
                new object?[] { "aas", string.Empty },
                new object?[] { typeof(string), string.Empty },
                new object?[] { typeof(DataverseConfiguration), "Dataverse" },
            };
    }
}
