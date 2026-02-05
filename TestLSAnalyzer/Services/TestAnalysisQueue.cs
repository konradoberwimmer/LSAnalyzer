using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Moq;
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

        var countAnalysisQueueCountChangedMessages = 0;
        WeakReferenceMessenger.Default.Register<AnalysisQueue.AnalysisQueueCountChangedMessage>(this, (_, _) =>
        {
            countAnalysisQueueCountChangedMessages++;
        });
        
        analysisQueue.Add(analysisPresentationViewModelUnivar);
        analysisQueue.Add(analysisPresentationViewModelMeanDiff);
        analysisQueue.Add(analysisPresentationViewModelCorr);
        
        Policy.Handle<TrueException>().WaitAndRetry(2000, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.True(
                analysisPresentationViewModelUnivar.Analysis.Result.Count > 0 && 
                analysisPresentationViewModelMeanDiff.Analysis.Result.Count > 0 &&  
                analysisPresentationViewModelCorr.Analysis.Result.Count > 0)
            );

        Policy.Handle<EqualException>().WaitAndRetry(200, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.Equal(6, countAnalysisQueueCountChangedMessages));
        
        Assert.Equal(0, analysisQueue.Count);
    }

    [Fact]
    public void TestInterruptAnalysis()
    {
        var rservice = new Mock<IRservice>();
        rservice
            .Setup(rs => rs.CalculateCorr(It.IsAny<AnalysisCorr>()))
            .Callback((AnalysisCorr _) => Thread.Sleep(100));
        
        AnalysisQueue analysisQueue = new(rservice.Object);

        AnalysisPresentation analysisPresentationNotInQueue = new()
        {
            Analysis = new AnalysisCorr(new AnalysisConfiguration())
        };
        
        analysisQueue.InterruptAnalysis(analysisPresentationNotInQueue);
        
        rservice.Verify(rs => rs.SendUserInterrupt(), Times.Never);
        
        AnalysisPresentation analysisPresentationFirstInQueue = new()
        {
            Analysis = new AnalysisCorr(new AnalysisConfiguration())
        };
        
        AnalysisPresentation analysisPresentationSecondInQueue = new()
        {
            Analysis = new AnalysisCorr(new AnalysisConfiguration())
        };
        
        analysisQueue.Add(analysisPresentationFirstInQueue);
        analysisQueue.Add(analysisPresentationSecondInQueue);
        
        // 100ms timeframe until first in queue is finished
        
        analysisQueue.InterruptAnalysis(analysisPresentationSecondInQueue);
        
        rservice.Verify(rs => rs.SendUserInterrupt(), Times.Never);
        
        analysisQueue.InterruptAnalysis(analysisPresentationFirstInQueue);
        
        rservice.Verify(rs => rs.SendUserInterrupt(), Times.Once);
    }

    [Fact]
    public void TestAnalysisWillNotStartWhenNotRequestedByMainWindow()
    {
        var rservice = new Mock<IRservice>();
        
        AnalysisQueue analysisQueue = new(rservice.Object);

        AnalysisPresentation analysisPresentation = new(new AnalysisCorr(new AnalysisConfiguration()), new MainWindow());
        
        var countAnalysisQueueCountChangedMessages = 0;
        WeakReferenceMessenger.Default.Register<AnalysisQueue.AnalysisQueueCountChangedMessage>(this, (_, _) =>
        {
            countAnalysisQueueCountChangedMessages++;
        });
        
        analysisQueue.Add(analysisPresentation);
        
        Policy.Handle<EqualException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.Equal(0, analysisQueue.Count));
        
        rservice.Verify(rs => rs.CalculateCorr(It.IsAny<AnalysisCorr>()), Times.Never);
        
        Assert.Equal(2, countAnalysisQueueCountChangedMessages);
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