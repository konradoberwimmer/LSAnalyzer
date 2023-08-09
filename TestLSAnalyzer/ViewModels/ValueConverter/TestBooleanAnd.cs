using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestBooleanAnd
    {
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void TestConvert(bool value1, bool value2, bool expected)
        {
            var converter = new BooleanAnd();

            Assert.Equal(expected, converter.Convert(new object[] { value1, value2 }, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(1, false, false)]
        [InlineData(0, true, false)]
        [InlineData(1, true, true)]
        public void TestConvertWithInt(int value1, bool value2, bool expected)
        {
            var converter = new BooleanAnd();

            Assert.Equal(expected, converter.Convert(new object[] { value1, value2 }, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("", false, false)]
        [InlineData("0", false, false)]
        [InlineData("1", true, false)]
        public void TestConvertWithUncastable(string value1, bool value2, bool expected)
        {
            var converter = new BooleanAnd();

            Assert.Equal(expected, converter.Convert(new object[] { value1, value2 }, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }
    }
}
