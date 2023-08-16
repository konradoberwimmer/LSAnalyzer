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
    public class TestEmptyEnumerableToVisibilityCollapsed
    {
        [Theory, MemberData(nameof(ConvertTestData))]
        public void TestConvert(object testObject, Visibility expected)
        {
            EmptyEnumerableToVisibilityCollapsed emptyCollectionToVisibilityCollapsedConverter = new();

            Assert.Equal(expected, emptyCollectionToVisibilityCollapsedConverter.Convert(testObject, Type.GetType("Visibility")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> ConvertTestData =>
            new List<object[]>()
            {
                new object[] { new Variable(1, "Ulldrael", true), Visibility.Collapsed },
                new object[] { new List<string>(), Visibility.Collapsed },
                new object[] { new List<string>() { "Ulldrael der Gerechte" }, Visibility.Visible },
            };
    }
}
