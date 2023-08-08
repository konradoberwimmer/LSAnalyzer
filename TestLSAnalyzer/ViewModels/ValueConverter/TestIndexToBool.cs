using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestIndexToBool
    {
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        public void TestConvert(int index, bool expected)
        {
            var converter = new IndexToBool();

            Assert.Equal(expected, converter.Convert(index, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }
    }
}
