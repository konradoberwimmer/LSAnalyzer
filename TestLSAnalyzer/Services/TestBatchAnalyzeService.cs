using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System.Reflection;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services.DataProvider;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.Services;

[Collection("Sequential")]
public class TestBatchAnalyzeService
{
    [Fact]
    public void TestRunBatchSendsMessages()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = new DatasetType(),
            FileName = "dummy.sav",
            ModeKeep = true,
        };

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        var messageCounter = 0;
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeProgression>(this, (_, _) => messageCounter++);

        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true,
                Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfiguration),
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 2, Selected = true,
                Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfiguration),
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];
        
        batchAnalyze.RunBatch(analyses, true, analysisConfiguration);

        Policy.Handle<EqualException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.Equal(3, messageCounter));
        
        Assert.True(analyses.All(analysis => analysis.Success is not null));
        Assert.False(analyses.Last().Success);
    }

    [Fact]
    public void TestRunBatchWithReloadingFiles()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationInvalid = new()
        {
            DatasetType = new DatasetType(),
            FileName = "dummy.sav",
        };
        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10, MIvar = "mi",
                RepWgts = "repwgt", FayFac = 0.5,
            },
            ModeKeep = true,
        };
        AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10, MIvar = "mi",
            },
            ModeKeep = false,
        };

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "doesntExist", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 2, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "x", false)],
                        GroupBy = [new Variable(2, "cat", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = [new Variable(1, "doesntExist", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 4, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = [new Variable(1, "item1", false), new(2, "item2", false)],
                        GroupBy = [new Variable(2, "cat", false)],
                        CalculateBivariate = true,
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 5, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisCorr(analysisConfigurationInvalid)
                    {
                        Vars = [new Variable(1, "dummy", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null);

        Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.False(analyses[0].Success);
        Assert.Contains("not calculate", analyses[0].Message);
        Assert.True(analyses[1].Success);
        Assert.False(analyses[2].Success);
        Assert.Contains("not build", analyses[2].Message);
        Assert.True(analyses[3].Success);
        Assert.False(analyses[4].Success);
        Assert.Contains("not load", analyses[4].Message);
    }

    [Fact]
    public void TestRunBatchIgnoresUnselected()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10, MIvar = "mi",
                RepWgts = "repwgt", FayFac = 0.5,
            },
            ModeKeep = true,
        };
        AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10, MIvar = "mi",
            },
            ModeKeep = false,
        };

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "doesntExist", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 2, Selected = false, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "x", false)],
                        GroupBy = [new Variable(2, "cat", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = [new Variable(1, "doesntExist", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null || analysis.WasIgnored)));
        
        Assert.False(analyses[0].Success);
        Assert.Null(analyses[1].Success);
        Assert.True(analyses[1].WasIgnored);
        Assert.False(analyses[2].Success);
    }
    
    [Fact]
    public void TestRunBatchOnCurrentFileModeKeep()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                RepWgts = "repwgt",
                FayFac = 0.5,
            },
            ModeKeep = true,
        };
        AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = false,
        };

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Rep5.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5));

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        
        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses = 
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "doesntExist", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 2, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = new() { new(1, "cat", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 4, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5);

        Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));

        Assert.False(analyses[0].Success);
        Assert.Contains("not calculate", analyses[0].Message);
        Assert.True(analyses[1].Success);
        Assert.True(analyses[2].Success);
        Assert.False(analyses[3].Success);
        Assert.Contains("not calculate", analyses[3].Message);
    }

    [Fact]
    public void TestRunBatchOnCurrentFileModeBuild()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                RepWgts = "repwgt",
                FayFac = 0.5,
            },
            ModeKeep = true,
        };
        AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
            },
            ModeKeep = false,
        };

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Multicat.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Multicat));

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        
        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "item1", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 2, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = new() { new(1, "cat", false) },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
            new()
            {
                Id = 4, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Multicat);

        Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));

        Assert.False(analyses[0].Success);
        Assert.Contains("not build", analyses[0].Message);
        Assert.False(analyses[1].Success);
        Assert.Contains("not build", analyses[1].Message);
        Assert.True(analyses[2].Success);
        Assert.True(analyses[3].Success);
    }
    
    [Fact]
    public void TestAbortBatch()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                RepWgts = "repwgt",
                FayFac = 0.5,
            },
            ModeKeep = true,
        };

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "item1", false) },
                    },
                    ViewSettings = []
                }
            },
            new()
            {
                Id = 2, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    },
                    ViewSettings = []
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null);
        
        Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.Equal("Working ...", analyses.First().Message));
        
        batchAnalyze.AbortBatch();

        Policy.Handle<ContainsException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.Contains(analyses, analysis => analysis is { Success: false, Message: "Aborted!" }));
    }

    [Fact]
    public void TestRetrieveDataProvider()
    {
        Rservice rservice = new();
        
        var configuration = new Mock<Configuration>();
        configuration
            .SetupSequence(c => c.GetMatchingDataProviderConfiguration(It.IsAny<IDataProviderConfiguration>()))
            .Returns((IDataProviderConfiguration?)null)
            .Returns(new DataverseConfiguration
            {
                Id = 1,
                Name = "myProvider",
                Url = "https://test.service.at",
                ApiToken = "very secret",
            });
        
        ServiceCollection serviceCollection = new();
        serviceCollection.AddTransient<IRservice>(_ => rservice);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        BatchAnalyzeService batchAnalyze = new(rservice, configuration.Object, serviceProvider);
        
        Assert.False(batchAnalyze.RetrieveDataProvider(string.Empty).success);
        Assert.False(batchAnalyze.RetrieveDataProvider("""{"File":{"Filename":"myFile.txt","DOI":"myDoi"}}""").success);

        var resultUnknownProvider = batchAnalyze.RetrieveDataProvider(
            """{"Provider":{"$type":"unknown","Id":33,"Name":"myProvider","Url":"https://test.service.at"},"File":{"Filename":"myFile.txt","DOI":"myDoi"}}"""); 
        Assert.False(resultUnknownProvider.success);
        Assert.Equal("Could not read data provider configuration!", resultUnknownProvider.errorMessage);
        Assert.Null(resultUnknownProvider.dataProvider);
        
        var resultUnknownConfiguration = batchAnalyze.RetrieveDataProvider(
            """{"Provider":{"$type":"dataverse","Id":33,"Name":"myProvider","Url":"https://test.service.at"},"File":{"Filename":"myFile.txt","DOI":"myDoi"}}"""); 
        Assert.False(resultUnknownConfiguration.success);
        Assert.Equal("Data provider configuration is no longer available!", resultUnknownConfiguration.errorMessage);
        Assert.Null(resultUnknownConfiguration.dataProvider);

        var resultSuccess = batchAnalyze.RetrieveDataProvider(
            """{"Provider":{"$type":"dataverse","Id":33,"Name":"myProvider","Url":"https://test.service.at"},"File":{"Filename":"myFile.txt","DOI":"myDoi"}}"""); 
        Assert.True(resultSuccess.success);
        Assert.Null(resultSuccess.errorMessage);
        Assert.NotNull(resultSuccess.dataProvider);
    }
    
    [Fact]
    public void TestRunBatchWorksRepeatedly()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

        AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
        {
            FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
            DatasetType = new()
            {
                Weight = "wgt",
                NMI = 10, MIvar = "mi",
                RepWgts = "repwgt", FayFac = 0.5,
            },
            ModeKeep = true,
        };

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "x", false)],
                        GroupBy = [new Variable(2, "cat", false)],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);

        analyses[0].Success = null;
        
        batchAnalyze.RunBatch(analyses, false, null);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);
    }

    [Fact]
    public void TestRetrieveFileInformation()
    {
        Rservice rservice = new();
        
        var configuration = new Mock<Configuration>();
        
        ServiceCollection serviceCollection = new();
        serviceCollection.AddTransient<Rservice>(_ => rservice);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        BatchAnalyzeService batchAnalyze = new(rservice, configuration.Object, serviceProvider);
        Dataverse dataverse = new(rservice);
        
        Assert.False(batchAnalyze.RetrieveFileInformation(dataverse, string.Empty).success);
        Assert.False(batchAnalyze.RetrieveFileInformation(dataverse, """{"Provider":{"$type":"dataverse","Id":33,"Name":"myProvider","Url":"https://test.service.at"}}""").success);
        
        var resultMissingFileInformation = batchAnalyze.RetrieveFileInformation(dataverse, 
            """{"Provider":{"$type":"dataverse","Id":33,"Name":"myProvider","Url":"https://test.service.at"},"File":{"Filename":"myFile.txt","DOI":"myDoi"}}"""); 
        Assert.False(resultMissingFileInformation.success);
        Assert.Equal("Could not read file information!", resultMissingFileInformation.errorMessage);
        Assert.Null(resultMissingFileInformation.fileInformation);

        var resultSuccess = batchAnalyze.RetrieveFileInformation(dataverse, 
            """{"Provider":{"$type":"dataverse","Id":33,"Name":"myProvider","Url":"https://test.service.at"},"File":{"File":"myFile.txt","Dataset":"myDoi","SelectedFileFormat":"spss"}}"""); 
        Assert.True(resultSuccess.success);
        Assert.Null(resultSuccess.errorMessage);
        Assert.NotNull(resultSuccess.fileInformation);
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
