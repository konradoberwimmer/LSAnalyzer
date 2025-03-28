using LSAnalyzerAvalonia.Models;

namespace TestLSAnalyzerAvalonia.Models;

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
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one" }, true},
            new object[] { new DatasetType { Name = "AB", Weight = "TOTWGT", NMI = 1, MIvar = "one" }, false},
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } } }, false},
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, PVvarsList = new() { new() { Regex = "[", DisplayName = "ASRREA", Mandatory = true } } }, false},
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", RepWgts = "[" }, false},
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", RepWgts = "wgtrep[0-9]*" }, true},
            new object[] { new DatasetType { Name = "New dataset type", Weight = "TOTWGT", NMI = 1, MIvar = "one", RepWgts = string.Empty, JKzone = "JKZONE", JKrep = "JKREP"}, true},
            new object[] { DatasetType.CreateDefaultDatasetTypes().FirstOrDefault()!, true },
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
        DatasetType datasetType = new();
        
        var regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();
        
        Assert.Single(regexNecessaryVariables);
        Assert.Equal("^$", regexNecessaryVariables.First());
        
        datasetType.Weight = "W_FSTUWT";
        datasetType.IDvar = "IDSTUD";
        
        regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();
        
        Assert.Equal(2, regexNecessaryVariables.Count);
        Assert.Equal("^W_FSTUWT$", regexNecessaryVariables.First());

        datasetType.MIvar = "micnt";
        datasetType.PVvarsList = new()
        {
            new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true },
            new() { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = false }
        };
        
        regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();
        
        Assert.Equal(4, regexNecessaryVariables.Count);
        
        datasetType.RepWgts = "W_STURWT";
        datasetType.JKzone = "JKZONE";
        datasetType.JKrep = "JKREP";
        
        regexNecessaryVariables = datasetType.GetRegexNecessaryVariables();

        Assert.Equal(7, regexNecessaryVariables.Count);
        
        Assert.Contains("^W_FSTUWT$", regexNecessaryVariables);
        Assert.Contains("^IDSTUD$", regexNecessaryVariables);
        Assert.Contains("^micnt$", regexNecessaryVariables);
        Assert.Contains("ASRREA", regexNecessaryVariables);
        Assert.DoesNotContain("ASRINF", regexNecessaryVariables);
        Assert.Contains("W_STURWT", regexNecessaryVariables);
        Assert.Contains("^JKZONE$", regexNecessaryVariables);
        Assert.Contains("^JKREP$", regexNecessaryVariables);
    }
}