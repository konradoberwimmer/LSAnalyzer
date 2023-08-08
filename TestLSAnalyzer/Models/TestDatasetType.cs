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
    }
}
