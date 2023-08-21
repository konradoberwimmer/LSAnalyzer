using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models
{
    public class TestDatasetType
    {
        [Theory, MemberData(nameof(ValidityTestCases))]
        public void TestValidity(DatasetType testCase, bool expectedValidity)
        {
            Assert.Equal(testCase.Validate(), expectedValidity);
        }

        public static IEnumerable<object[]> ValidityTestCases =>
            new List<object[]>
            {
                new object[] { new DatasetType(), false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1 }, true},
                new object[] { new DatasetType() { Name = "AB", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1 }, false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", PVvars = "ASRREA", Nrep = 1 }, false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1, RepWgts = "[" }, false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1, RepWgts = "wgtrep[0-9]*" }, true},
                new object[] { DatasetType.CreateDefaultDatasetTypes().FirstOrDefault()!, true }
            };

        [Theory]
        [InlineData("", false)]
        [InlineData("W_FSTUWT", true)]
        [InlineData("W_STURWT", true)]
        [InlineData("mivar", true)]
        [InlineData("mimimi", false)]
        public void TestHasSystemVariable(string name, bool expectedHasSystemVariable)
        {
            DatasetType datasetType = new()
            {
                Weight = "W_FSTUWT",
                MIvar = "mivar",
                PVvars = "ASRREA",
                RepWgts = "W_STURWT",
            };

            Assert.Equal(expectedHasSystemVariable, datasetType.HasSystemVariable(name));
        }

        [Fact]
        public void TestGetRegexNecessaryVariables()
        {
            DatasetType datasetType = new()
            {
                Weight = "W_FSTUWT",
                PVvars = "ASRREA;ASRINF",
                RepWgts = "W_STURWT",
            };

            var regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();

            Assert.Equal(4, regexNecessaryVariables.Count);
            Assert.Contains("^W_FSTUWT$", regexNecessaryVariables);
            Assert.Contains("^ASRREA", regexNecessaryVariables);
            Assert.Contains("W_STURWT", regexNecessaryVariables);
        }
    }
}
