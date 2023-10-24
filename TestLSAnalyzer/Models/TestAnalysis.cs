using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestAnalysis
    {
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
                new object[] { new AnalysisUnivar(new()), new Dictionary<string, object?>() { { "Analysis:", "Univariate" }, { "Dependent variable:", null }, { "Type of percentiles:", null }, { "Mode:", "Build BIFIEdata object for analyses" }, } },
                new object[] { new AnalysisLinreg(new()) { Dependent = new(1, "y", false) }, new Dictionary<string, object?>() { { "Analysis:", "Linear regression" }, { "Dependent variable:", "y" }, } },
                new object[] { new AnalysisPercentiles(new()) { CalculateSE = true, MimicIdbAnalyzer = true }, new Dictionary<string, object?>() { { "Analysis:", "Percentiles" }, { "Type of percentiles:", "With standard errors and with interpolation (mimic BIFIE.ecdf, quanttype = 1)" }, } },
            };

        [Theory, MemberData(nameof(VariableLabelsTestCases))]
        public void TestVariableLabels(Analysis analysis, Dictionary<string, string> expectedVariableLabels)
        {
            var variableLabels = analysis.VariableLabels;
            foreach (var key in expectedVariableLabels.Keys)
            {
                Assert.True(variableLabels.ContainsKey(key));
                Assert.Equal(expectedVariableLabels[key], variableLabels[key]);
            }
        }

        public static IEnumerable<object[]> VariableLabelsTestCases =>
            new List<object[]>
            {
                new object[] { new AnalysisUnivar(new()) { Vars = new() { new(1, "y", false) { Label = "dependent" } }, }, new Dictionary<string, string>() { { "y", "dependent" }, } },
                new object[] { new AnalysisUnivar(new()) { Vars = new() { new(1, "y", false) { Label = "dependent" } }, GroupBy = new() { new(2, "x", false) { Label = "independent" } }, }, new Dictionary<string, string>() { { "y", "dependent" }, { "x", "independent" }, } },
                new object[] { new AnalysisLinreg(new()) { Vars = new() { new(2, "x", false) { Label = "independent" } }, GroupBy = new() { new(3, "cat", false) { Label = "groups" } }, Dependent = new(1, "y", false) { Label = "dependent" } }, new Dictionary<string, string>() { { "y", "dependent" }, { "x", "independent" }, { "cat", "groups" }, } },
            };
    }
}
