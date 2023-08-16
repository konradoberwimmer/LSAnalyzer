using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestVariablesToString
    {
        [Theory, MemberData(nameof(ConvertTestCases))]
        public void TestConvert(List<Variable> variables, string expected)
        {
            VariablesToString variablesToStringConverter = new();
            Assert.Equal(expected, variablesToStringConverter.Convert(variables, Type.GetType("string")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> ConvertTestCases =>
            new List<object[]>
            {
                new object[] { new List<Variable>(), "" },
                new object[] { new List<Variable>() { new(1, "x1", false) }, "x1" },
                new object[] { new List<Variable>() { new(1, "x1", false), new(1, "x2", false) }, "x1, x2" },
                new object[] { new List<Variable>() { new(1, "x1", false), new(1, "x2", false), new(1, "x3", false) }, "x1, x2, x3" },
            };
    }
}
