using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestBoolToVisibility
    {
        [Theory]
        [InlineData(false, Visibility.Hidden)]
        [InlineData(true, Visibility.Visible)]
        public void TestConvert(bool boolVal, Visibility expected)
        {
            var converter = new BoolToVisibility();

            Assert.Equal(expected, converter.Convert(boolVal, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }
    }
}
