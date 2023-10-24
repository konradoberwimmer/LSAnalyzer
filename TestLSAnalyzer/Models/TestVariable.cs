using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestVariable
    {
        [Theory]
        [InlineData("x", null, "x")]
        [InlineData("y", "dependent variable", "y (dependent variable)")]
        public void TestInfo(string name, string? label, string expected)
        {
            Variable variable = new Variable(1, name, false);
            variable.Label = label;

            Assert.Equal(expected, variable.Info);
        }

        [Theory, MemberData(nameof(MetaInformationTestCases))]
        public void TestMetaInformation(Analysis analysis, Dictionary<string, object?> expectedMetaInformation)
        {
            var metaInformation = analysis.MetaInformation;
            foreach (var key in expectedMetaInformation.Keys)
            {
                Assert.True(metaInformation.ContainsKey(key));
                Assert.Equal(expectedMetaInformation[key], metaInformation[key]);
            }
        }

        public static IEnumerable<object[]> MetaInformationTestCases =>
            new List<object[]>
            {
                new object[] { new AnalysisUnivar(new AnalysisConfiguration()), new Dictionary<string, object?>() { { "Analysis:", "Univariate" }, { "Dependent variable:", null }, { "Type of percentiles:", null }, { "Mode:", "Build BIFIEdata object for analyses" }, } },
                new object[] { new AnalysisLinreg(new AnalysisConfiguration()) { Dependent = new(1, "y", false) }, new Dictionary<string, object?>() { { "Analysis:", "Linear regression" }, { "Dependent variable:", "y" }, } },
                new object[] { new AnalysisPercentiles(new AnalysisConfiguration()) { CalculateSE = true, MimicIdbAnalyzer = true }, new Dictionary<string, object?>() { { "Analysis:", "Percentiles" }, { "Type of percentiles:", "With standard errors and with interpolation (mimic BIFIE.ecdf, quanttype = 1)" }, } },
            };
    }
}
