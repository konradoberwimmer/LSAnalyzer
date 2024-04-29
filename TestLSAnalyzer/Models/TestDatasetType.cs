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
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } }, Nrep = 1 }, false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1, RepWgts = "[" }, false},
                new object[] { new DatasetType() { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", Nrep = 1, RepWgts = "wgtrep[0-9]*" }, true},
                new object[] { DatasetType.CreateDefaultDatasetTypes().FirstOrDefault()!, true }
            };

        [Fact]
        public void TestIsChanged()
        {
            DatasetType datasetType = new()
            {
                Weight = "W_FSTUWT",
                MIvar = "mivar",
                PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } },
                RepWgts = "W_STURWT",
            };

            Assert.True(datasetType.IsChanged);

            datasetType.AcceptChanges();
            Assert.False(datasetType.IsChanged);

            datasetType.NMI = 10;
            Assert.True(datasetType.IsChanged);

            datasetType.AcceptChanges();
            Assert.False(datasetType.IsChanged);
        }

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
                PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } },
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
                PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true }, new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = false } },
                RepWgts = "W_STURWT",
            };

            var regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();

            Assert.Equal(3, regexNecessaryVariables.Count);
            Assert.Contains("^W_FSTUWT$", regexNecessaryVariables);
            Assert.Contains("ASRREA", regexNecessaryVariables);
            Assert.DoesNotContain("ASRINF", regexNecessaryVariables);
            Assert.Contains("W_STURWT", regexNecessaryVariables);
        }
    }
}
