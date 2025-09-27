using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestAnalysisConfiguration
{
    [Theory, ClassData(typeof(IsEqualTestCases))]
    public void TestIsEqual(AnalysisConfiguration a, AnalysisConfiguration b, bool expected)
    {
        Assert.Equal(a.IsEqual(b), expected);
    }
    
    class IsEqualTestCases : TheoryData<AnalysisConfiguration, AnalysisConfiguration, bool>
    {
        public IsEqualTestCases()
        {
            Add(new AnalysisConfiguration { DatasetType = new() }, new AnalysisConfiguration { DatasetType = new() }, true);
            Add(new AnalysisConfiguration(), new AnalysisConfiguration(), false); // unequal because no dataset type specified yet
            Add(new AnalysisConfiguration { DatasetType = new() }, new AnalysisConfiguration { DatasetType = new(), FileName = "test.sav" }, false);
            Add(new AnalysisConfiguration { DatasetType = new() { Id = 1 } }, new AnalysisConfiguration { DatasetType = new() { Id = 2 } }, false);
            Add(new AnalysisConfiguration { DatasetType = new() { Id = 1 } }, new AnalysisConfiguration { DatasetType = new() { Id = 1 } }, true);
            Add(new AnalysisConfiguration { DatasetType = new() { Id = 1, PVvarsList = new() { new() { DisplayName = "PV", Regex = "PV*", Mandatory = true } } } }, 
                new AnalysisConfiguration { DatasetType = new() { Id = 1, PVvarsList = new() { new() { DisplayName = "PV", Regex = "PV*", Mandatory = false } } } }, 
                false);
            Add(new AnalysisConfiguration { DatasetType = new() { Id = 1, PVvarsList = new() { new() { DisplayName = "PV", Regex = "PV*", Mandatory = true } } } }, 
                new AnalysisConfiguration { DatasetType = new() { Id = 1, PVvarsList = new() { new() { DisplayName = "PV", Regex = "PV*", Mandatory = true } } } }, 
                true);
        }
    }
}