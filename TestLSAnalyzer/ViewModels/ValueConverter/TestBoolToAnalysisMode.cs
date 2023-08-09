using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestBoolToAnalysisMode
    {
        [Theory]
        [InlineData(false, "Build BIFIEsurvey object for analyses")]
        [InlineData(true, "Keep BIFIEsurvey object")]
        public void TestConvert(bool boolVal, string expected)
        {
            var converter = new BoolToAnalysisMode();

            Assert.Equal(expected, converter.Convert(boolVal, Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }
    }
}
