using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestNullToVisibilityCollapsed
    {
        [Theory, MemberData(nameof(ConvertTestData))]
        public void TestConvert(object? testObject, Visibility expected)
        {
            NullToVisibilityCollapsed nullToVisibilityCollapsedConverter = new();

            Assert.Equal(expected, nullToVisibilityCollapsedConverter.Convert(testObject, Type.GetType("Visibility")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object?[]> ConvertTestData =>
            new List<object?[]>()
            {
                new object?[] { null, Visibility.Collapsed },
                new object?[] { new Variable(1, "Ulldrael", true), Visibility.Visible },
                new object?[] { new List<string>() { "Ulldrael der Gerechte" }, Visibility.Visible },
            };
    }
}
