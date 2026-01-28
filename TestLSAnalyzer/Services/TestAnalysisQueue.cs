using System.Reflection;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.Services;

[Collection("Sequential")]
public class TestAnalysisQueue
{
    [Fact]
    public void TestAddMultiple()
    {
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
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 0.5));
        
        AnalysisUnivar analysisUnivar = new(analysisConfiguration)
        {
            Vars = new() { new(1, "x", false), new(1, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
        };
        AnalysisPresentation analysisPresentationViewModelUnivar = new(analysisUnivar);
        
        AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
        {
            Vars = new() { new(1, "x", false), new(1, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateSeparately = false,
        };
        AnalysisPresentation analysisPresentationViewModelMeanDiff = new(analysisMeanDiff);
        
        AnalysisCorr analysisCorr = new(analysisConfiguration)
        {
            Vars = new() { new(1, "x", false), new(1, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
        };
        AnalysisPresentation analysisPresentationViewModelCorr = new(analysisCorr);

        AnalysisQueue analysisQueue = new(rservice);
        
        analysisQueue.Add(analysisPresentationViewModelUnivar);
        analysisQueue.Add(analysisPresentationViewModelMeanDiff);
        analysisQueue.Add(analysisPresentationViewModelCorr);
        
        Policy.Handle<TrueException>().WaitAndRetry(2000, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.True(
                analysisPresentationViewModelUnivar.Analysis.Result.Count > 0 && 
                analysisPresentationViewModelMeanDiff.Analysis.Result.Count > 0 &&  
                analysisPresentationViewModelCorr.Analysis.Result.Count > 0)
            );
        
        Assert.Equal(0, analysisQueue.Count);
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