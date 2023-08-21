using LSAnalyzer.ViewModels;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestEnumToBoolean
    {
        [Theory]
        [InlineData(SelectAnalysisFile.AnalysisModes.Keep, "Keep", true)]
        [InlineData(SelectAnalysisFile.AnalysisModes.Keep, "Build", false)]
        [InlineData(SelectAnalysisFile.AnalysisModes.Build, "Keep", false)]
        [InlineData(SelectAnalysisFile.AnalysisModes.Build, "Build", true)]
        public void TestConvert(SelectAnalysisFile.AnalysisModes analysisMode, string parameter, bool expected)
        {
            EnumToBoolean enumToBooleanConverter = new();

            Assert.Equal(expected, enumToBooleanConverter.Convert(analysisMode, Type.GetType("bool")!, parameter, CultureInfo.InvariantCulture));
        }
    }
}
