using System.Reflection;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using LSAnalyzer.ViewModels.VirtualVariableCreation;
using Moq;
using RDotNet;

namespace TestLSAnalyzer.ViewModels.VirtualVariableCreation;

[Collection("Sequential")]
public class TestEqualFrequencyBinning
{
    [Fact]
    public void TestCreateEqualFrequencyBinningModeKeep()
    {
        var configuration = new Mock<Configuration>();
        configuration.Setup(conf => conf.GetVirtualVariablesFor(It.IsAny<string>(), It.IsAny<DatasetType>())).Returns([]);
        
        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                RepWgts = "repwgt",
                FayFac = 1,
            },
            ModeKeep = true,
        };

        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");
        Assert.True(rservice.InjectAppFunctions());
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

        AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
        {
            Vars = new() { new(1, "x") },
            Percentiles = new() { 0.25, 0.50, 0.75 },
            CalculateSE = false,
            UseInterpolation = true,
        };

        var percentilesResult = rservice.CalculatePercentiles(analysisPercentiles)!.First()["stat"].AsDataFrame()["quant"].AsNumeric().ToList();

        VirtualVariables virtualVariables = new(configuration.Object, rservice);
        virtualVariables.AnalysisConfiguration = analysisConfiguration;
        virtualVariables.AvailableVariables = [..rservice.GetCurrentDatasetVariables(analysisConfiguration, [])!];
        
        EqualFrequencyBinning viewModel = new(virtualVariables, rservice);
        viewModel.Name = "TestEqualFrequencyBinning";
        viewModel.SelectedVariable = viewModel.Variables.First(var => var.Name == "x");
        viewModel.NumberOfBins = 4;
        
        viewModel.CreateEqualFrequencyBinningCommand.Execute(null);
        
        Assert.Single(virtualVariables.CurrentVirtualVariables);
        var newVirtualVariable = (virtualVariables.CurrentVirtualVariables.First() as VirtualVariableRecode)!;
        Assert.Equal("TestEqualFrequencyBinning", newVirtualVariable.Name);
        Assert.Equal(4, newVirtualVariable.Rules.Count);
        Assert.True(newVirtualVariable.Rules.First().Criteria.First().Type == VirtualVariableRecode.Term.TermType.AtMost);
        Assert.True(newVirtualVariable.Rules.Last().Criteria.First().Type == VirtualVariableRecode.Term.TermType.AtLeast);
        Assert.True(newVirtualVariable.Rules.Index().All(rule => 
            (rule.Index > 0 && rule.Item.Criteria.First().Value == percentilesResult[rule.Index - 1]) ||
            (rule.Index < 3 && rule.Item.Criteria.First().MaxValue == percentilesResult[rule.Index])
        ));
    }
    
    [Fact]
    public void TestCreateEqualFrequencyBinningModeBuild()
    {
        var configuration = new Mock<Configuration>();
        configuration.Setup(conf => conf.GetVirtualVariablesFor(It.IsAny<string>(), It.IsAny<DatasetType>())).Returns([]);
        
        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                RepWgts = "repwgt",
                FayFac = 1,
            },
            ModeKeep = false,
        };

        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");
        Assert.True(rservice.InjectAppFunctions());
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

        AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
        {
            Vars = new() { new(1, "y") },
            Percentiles = new() { 0.2, 0.4, 0.6, 0.8 },
            CalculateSE = false,
            UseInterpolation = true,
        };

        var percentilesResult = rservice.CalculatePercentiles(analysisPercentiles)!.First()["stat"].AsDataFrame()["quant"].AsNumeric().ToList();

        VirtualVariables virtualVariables = new(configuration.Object, rservice);
        virtualVariables.AnalysisConfiguration = analysisConfiguration;
        virtualVariables.AvailableVariables = [..rservice.GetCurrentDatasetVariables(analysisConfiguration, [])!];
        
        EqualFrequencyBinning viewModel = new(virtualVariables, rservice);
        viewModel.Name = "TestEqualFrequencyBinning";
        viewModel.SelectedVariable = viewModel.Variables.First(var => var.Name == "y");
        viewModel.NumberOfBins = 5;
        
        viewModel.CreateEqualFrequencyBinningCommand.Execute(null);
        
        Assert.Single(virtualVariables.CurrentVirtualVariables);
        var newVirtualVariable = (virtualVariables.CurrentVirtualVariables.First() as VirtualVariableRecode)!;
        Assert.Equal("TestEqualFrequencyBinning", newVirtualVariable.Name);
        Assert.Equal(5, newVirtualVariable.Rules.Count);
        Assert.True(newVirtualVariable.Rules.Index().All(rule => 
            (rule.Index > 0 && rule.Item.Criteria.First().Value == percentilesResult[rule.Index - 1]) ||
            (rule.Index < 4 && rule.Item.Criteria.First().MaxValue == percentilesResult[rule.Index])
        ));
    }
        
    public static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path)!;
        }
    }
}