using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestAtLeast
    {
        [Theory]
        [InlineData(1, 2, false)]
        [InlineData(2, 1, true)]
        [InlineData(2, 2, true)]
        [InlineData(2, 3, false)]
        public void TestConvert(int value, int atLeast, bool expected)
        {
            AtLeast atLeastConverter = new();

            Assert.Equal(expected, atLeastConverter.Convert(value, Type.GetType("bool")!, atLeast, CultureInfo.InvariantCulture));
        }
    }
}
