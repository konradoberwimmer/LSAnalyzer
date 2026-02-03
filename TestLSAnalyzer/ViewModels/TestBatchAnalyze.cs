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

[Collection("Sequential")]
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
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        BatchAnalyzeService batchAnalyzeService = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
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
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = true,
        };

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

        var configuration = new Mock<Configuration>();
        
        BatchAnalyzeService batchAnalyzeService = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, configuration.Object)
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = true,
            CurrentConfiguration = analysisConfiguration
        };

        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        batchAnalyzeViewModel.RunBatchCommand.Execute(null);

        Policy.Handle<NotNullException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(batchAnalyzeViewModel.AnalysesTable));
        Assert.Equal(4, batchAnalyzeViewModel.AnalysesTable.Count);
        
        configuration.Verify(conf => conf.StoreRecentBatchAnalyzeFile(It.IsAny<string>()), Times.Once);
        
        Policy.Handle<Exception>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() =>
            {
                foreach (var entry in batchAnalyzeViewModel.AnalysesTable)
                {
                    Assert.True(entry.Success);
                }
            });
    }

    [Fact]
    public void TestRunBatchWorksWithViewSettings()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = true,
        };

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

        BatchAnalyzeService batchAnalyzeService = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat_v1_2.json"),
            UseCurrentFile = true,
            CurrentConfiguration = analysisConfiguration
        };

        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        batchAnalyzeViewModel.RunBatchCommand.Execute(null);

        Policy.Handle<NotNullException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(batchAnalyzeViewModel.AnalysesTable));
        Assert.Equal(4, batchAnalyzeViewModel.AnalysesTable.Count);
        
        Policy.Handle<Exception>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() =>
            {
                foreach (var entry in batchAnalyzeViewModel.AnalysesTable)
                {
                    Assert.True(entry.Success);
                }
            });
    }
    
    [Fact]
    public void TransferResultsSendMessages()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = true,
        };

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

        BatchAnalyzeService batchAnalyzeService = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService, Mock.Of<Configuration>())
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json"),
            UseCurrentFile = true,
            CurrentConfiguration = analysisConfiguration
        };
        
        batchAnalyzeViewModel.LoadBatchFileCommand.Execute(null);
        batchAnalyzeViewModel.RunBatchCommand.Execute(null);
     
        Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(batchAnalyzeViewModel.AnalysesTable.All(analysis => analysis.Success is not null)));

        var analysisReadyMessagesSent = 0;
        WeakReferenceMessenger.Default.Register<BatchAnalyze.BatchAnalyzeAnalysisReadyMessage>(this, (_, _) =>
        {
            analysisReadyMessagesSent++;
        });

        batchAnalyzeViewModel.TransferResultsCommand.Execute(null);

        Assert.Equal(4, analysisReadyMessagesSent);
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
