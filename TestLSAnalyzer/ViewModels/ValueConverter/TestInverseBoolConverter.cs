using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestInverseBoolConverter
    {
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TestConvert(bool value, bool expected)
        {
            InverseBool inverseBoolConverter = new();

            Assert.Equal(expected, inverseBoolConverter.Convert(value, Type.GetType("bool")!, "", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TestConvertBack(bool value, bool expected)
        {
            InverseBool inverseBoolConverter = new();

            Assert.Equal(expected, inverseBoolConverter.ConvertBack(value, Type.GetType("bool")!, "", CultureInfo.InvariantCulture));
        }
    }
}
