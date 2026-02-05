using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System.Reflection;
using LSAnalyzer.Services.Stubs;
using Moq;
using Polly;
using Xunit.Sdk;
using BatchAnalyzeViewModel = LSAnalyzer.ViewModels.BatchAnalyze;

namespace TestLSAnalyzer.ViewModels;

public class TestBatchAnalyze
{
    [Fact]
    public void TestSelectedRecentBatchAnalyzeFile()
    {
        BatchAnalyzeService batchAnalyzeService = new(new RserviceStub(), Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = @"C:\anywhere.json",
            SelectedRecentBatchAnalyzeFile = null
        };

        Assert.Equal(@"C:\anywhere.json", batchAnalyzeViewModel.FileName);

        var sentMessage = false;
        WeakReferenceMessenger.Default.Register<RecentFileInvalidMessage>(this, (_,_) => sentMessage = true);

        batchAnalyzeViewModel.SelectedRecentBatchAnalyzeFile = @"F:\surely_not_here.json";
        
        Assert.Equal(@"C:\anywhere.json", batchAnalyzeViewModel.FileName);
        Assert.True(sentMessage);
        
        var tempFile = Path.GetTempFileName();
        
        batchAnalyzeViewModel.SelectedRecentBatchAnalyzeFile = tempFile;
        
        Assert.Equal(tempFile, batchAnalyzeViewModel.FileName);
    }
    
    [Fact]
    public void TestRunBatchSendsFailureMessageOnInvalidJSON()
    {
        BatchAnalyzeService batchAnalyzeService = new(new RserviceStub(), Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>());

        bool messageSent = false;
        WeakReferenceMessenger.Default.Register<BatchAnalyze.BatchAnalyzeFailureMessage>(this, (_, _) =>
        {
            messageSent = true; 
        });

        var tmpFile = Path.Combine(Path.GetTempPath(), "stupid.json");
        if (File.Exists(tmpFile))
        {
            File.Delete(tmpFile);
        }

        var fileStream = File.Create(tmpFile);
        var streamWriter = new StreamWriter(fileStream);
        streamWriter.WriteLine("{ WTF! }");
        streamWriter.Flush();
        streamWriter.Close();
        fileStream.Close();

        batchAnalyzeViewModel.FileName = tmpFile;
        
        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        
        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageSent));
    }

    [Fact]
    public void TestRunBatchInvokesService()
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new DatasetType
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = true,
        };

        var configuration = new Mock<Configuration>();

        var batchAnalyzeService = new Mock<IBatchAnalyzeService>();
        
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService.Object, configuration.Object)
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = true,
            CurrentConfiguration = analysisConfiguration
        };

        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        
        configuration.Verify(conf => conf.StoreRecentBatchAnalyzeFile(It.IsAny<string>()), Times.Once);

        batchAnalyzeViewModel.RunBatchCommand.Execute(null);
        
        batchAnalyzeService.Verify(service => service.RunBatch(It.IsAny<IEnumerable<BatchAnalyze.BatchEntry>>(), It.IsAny<bool>(), It.IsAny<AnalysisConfiguration>()), Times.Once);
    }

    [Fact]
    public void TestRunBatchWorksWithAndWithoutViewSettings()
    {
        AnalysisPresentation dummyAnalysisPresentation = new();
        
        BatchAnalyzeService batchAnalyzeService = new(new RserviceStub(), Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat_v1_2.json"),
            UseCurrentFile = false,
            CurrentConfiguration = null
        };

        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        
        Assert.NotEmpty(batchAnalyzeViewModel.AnalysesTable.First().Analysis.ViewSettings);
        var alternativeView = new AnalysisPresentation(new AnalysisCorr(new AnalysisConfiguration()));
        alternativeView.ApplyDeserializedViewSettings(batchAnalyzeViewModel.AnalysesTable.First().Analysis.ViewSettings);
        Assert.Contains(alternativeView.ViewSettings, viewSetting => !dummyAnalysisPresentation.ViewSettings[viewSetting.Key].Equals(viewSetting.Value));

        batchAnalyzeViewModel.FileName =
            Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json");
        
        Assert.Empty(batchAnalyzeViewModel.AnalysesTable);

        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        
        Assert.NotEmpty(batchAnalyzeViewModel.AnalysesTable.First().Analysis.ViewSettings);
        var defaultView = new AnalysisPresentation(new AnalysisCorr(new AnalysisConfiguration()));
        defaultView.ApplyDeserializedViewSettings(batchAnalyzeViewModel.AnalysesTable.First().Analysis.ViewSettings);
        Assert.DoesNotContain(defaultView.ViewSettings, viewSetting => !dummyAnalysisPresentation.ViewSettings[viewSetting.Key].Equals(viewSetting.Value));
    }
    
    [Fact]
    public void TestTransferResultsSendMessages()
    {
        BatchAnalyzeService batchAnalyzeService = new(new RserviceStub(), Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = false,
            CurrentConfiguration = null
        };
        
        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);

        foreach (var entry in batchAnalyzeViewModel.AnalysesTable)
        {
            entry.Success = true;
            entry.PreparedPresentation = new AnalysisPresentation();
        }

        var analysisReadyMessagesSent = 0;
        WeakReferenceMessenger.Default.Register<BatchAnalyze.BatchAnalyzeAnalysisReadyMessage>(this, (_, _) =>
        {
            analysisReadyMessagesSent++;
        });

        batchAnalyzeViewModel.TransferResultsCommand.Execute(null);

        Assert.Equal(batchAnalyzeViewModel.AnalysesTable.Count, analysisReadyMessagesSent);
    }
    
    [Fact]
    public void TestTransferResultsIgnoresUnselected()
    {
        BatchAnalyzeService batchAnalyzeService = new(new RserviceStub(), Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = false,
            CurrentConfiguration = null
        };
        
        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        
        foreach (var entry in batchAnalyzeViewModel.AnalysesTable)
        {
            entry.Success = true;
            entry.PreparedPresentation = new AnalysisPresentation();
        }
        
        batchAnalyzeViewModel.AnalysesTable.First().Selected = false;
        
        var analysisReadyMessagesSent = 0;
        WeakReferenceMessenger.Default.Register<BatchAnalyze.BatchAnalyzeAnalysisReadyMessage>(this, (_, _) =>
        {
            analysisReadyMessagesSent++;
        });

        batchAnalyzeViewModel.TransferResultsCommand.Execute(null);

        Assert.Equal(batchAnalyzeViewModel.AnalysesTable.Count - 1, analysisReadyMessagesSent);
    }

    [Fact]
    public void TestAbortBatch()
    {
        var batchAnalyzeService = new Mock<IBatchAnalyzeService>();
        
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService.Object, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = false,
            CurrentConfiguration = null
        };
        
        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        batchAnalyzeViewModel.RunBatchCommand.Execute(null);
        batchAnalyzeViewModel.AbortBatchCommand.Execute(null);
     
        batchAnalyzeService.Verify(service => service.RunBatch(It.IsAny<IEnumerable<BatchAnalyze.BatchEntry>>(), It.IsAny<bool>(), It.IsAny<AnalysisConfiguration>()), Times.Once);
        batchAnalyzeService.Verify(service => service.AbortBatch(), Times.Once);
    }
    
    [Fact]
    public void TestClearAnalysisData()
    {
        BatchAnalyzeViewModel batchAnalyzeViewModel = new()
        {
            AnalysesTable = new ObservableCollection<BatchAnalyze.BatchEntry>([new BatchAnalyze.BatchEntry() { Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings { Analysis = new AnalysisCorr(new AnalysisConfiguration()), ViewSettings = []}}]),
            IsBusy = true,
            FinishedAllCalculations = true
        };
        
        batchAnalyzeViewModel.ClearAnalysisData();
        
        Assert.Empty(batchAnalyzeViewModel.AnalysesTable);
        Assert.False(batchAnalyzeViewModel.IsBusy);
        Assert.False(batchAnalyzeViewModel.FinishedAllCalculations);
    }

    private static string AssemblyDirectory
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
