using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestNullToBool
    {
        [Theory]
        [InlineData(1, true)]
        [InlineData("a", true)]
        [InlineData(null, false)]
        public void TestConvert(object? value, bool expected) 
        {
            NullToBool converter = new();
            Assert.Equal(expected, converter.Convert(value, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }
    }
}
