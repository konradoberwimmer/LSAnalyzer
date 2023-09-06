using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestBoolToRedGreen
    {
        [Theory, MemberData(nameof(ConvertTestData))]
        public void TestConvert(bool? boolVal, SolidColorBrush expected)
        {
            var converter = new BoolToRedGreen();

            Assert.Equal(expected, converter.Convert(boolVal, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object?[]> ConvertTestData =>
            new List<object?[]>()
            {
                new object?[] { null, Brushes.Red },
                new object?[] { false, Brushes.Red },
                new object?[] { true, Brushes.Green },
            };
    }
}
