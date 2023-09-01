using LSAnalyzer.ViewModels;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static LSAnalyzer.ViewModels.SelectAnalysisFile;

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

        [Fact]
        public void TestConvertWithImpossibleValues() 
        {
            EnumToBoolean enumToBooleanConverter = new();

            Assert.Equal(DependencyProperty.UnsetValue, enumToBooleanConverter.Convert(SelectAnalysisFile.AnalysisModes.Keep, Type.GetType("bool")!, 1, CultureInfo.InvariantCulture));

            Assert.Equal(DependencyProperty.UnsetValue, enumToBooleanConverter.Convert(SelectAnalysisFile.AnalysisModes.Keep, Type.GetType("bool")!, "seppi", CultureInfo.InvariantCulture));
        }
    }
}
