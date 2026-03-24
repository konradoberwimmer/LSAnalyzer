using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System.Reflection;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services.DataProvider;
using LSAnalyzer.Services.Stubs;
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
        
        batchAnalyze.RunBatch(analyses, true, analysisConfiguration, null, []);

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
                        Vars = [new Variable(1, "doesntExist")],
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
                        Vars = [new Variable(1, "x")],
                        GroupBy = [new Variable(2, "cat")],
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
                        Vars = [new Variable(1, "doesntExist")],
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
                        Vars = [new Variable(1, "item1"), new(2, "item2")],
                        GroupBy = [new Variable(2, "cat")],
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
                        Vars = [new Variable(1, "dummy")],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null, null, []);

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
                        Vars = [new Variable(1, "doesntExist")],
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
                        Vars = [new Variable(1, "x")],
                        GroupBy = [new Variable(2, "cat")],
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
                        Vars = [new Variable(1, "doesntExist")],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null, null, []);

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
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5, []));

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
                        Vars = new() { new(1, "doesntExist") },
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
                        Vars = new() { new(1, "x") },
                        GroupBy = new() { new(2, "cat") },
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
                        Vars = new() { new(1, "cat") },
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
                        Vars = new() { new(1, "item1"), new(2, "item2") },
                        GroupBy = new() { new(2, "cat") },
                        CalculateBivariate = true,
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5, null, []);

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
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Multicat, []));

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
                        Vars = new() { new(1, "item1") },
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
                        Vars = new() { new(1, "x") },
                        GroupBy = new() { new(2, "cat") },
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
                        Vars = new() { new(1, "cat") },
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
                        Vars = new() { new(1, "item1"), new(2, "item2") },
                        GroupBy = new() { new(2, "cat") },
                        CalculateBivariate = true,
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Multicat, null, []);

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
    public void TestRunBatchPreparesAnalysisPresentation()
    {
        Rservice rservice = new();
        Assert.True(rservice.Connect(), "R must also be available for tests");

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
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Multicat, []));

        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());
        
        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat)
                    {
                        Vars = new() { new(1, "cat") },
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Multicat, null, []);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));

        Assert.True(analyses[0].Success);
        Assert.NotNull(analyses[0].PreparedPresentation);
        Assert.NotEmpty(analyses[0].PreparedPresentation!.Analysis.Result);
        Assert.NotEmpty(analyses[0].PreparedPresentation!.DataTable.Rows);
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

        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Rep5.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5, []));
        
        BatchAnalyzeService batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "item1") },
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
                        Vars = new() { new(1, "x") },
                        GroupBy = new() { new(2, "cat") },
                    },
                    ViewSettings = []
                }
            },
            new()
            {
                Id = 3, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "x") },
                        GroupBy = new() { new(2, "cat") },
                    },
                    ViewSettings = []
                }
            },
            new()
            {
                Id = 4, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = new() { new(1, "x") },
                        GroupBy = new() { new(2, "cat") },
                    },
                    ViewSettings = []
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null, null, []);
        
        Policy.Handle<ContainsException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.Contains(analyses, analysis => analysis.Message == "Working ..."));
        
        batchAnalyze.AbortBatch();

        Policy.Handle<ContainsException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(10))
            .Execute(() => Assert.Contains(analyses, analysis => analysis is { Success: false, Message: "Aborted!" }));

        Assert.Contains(analyses, analysis => analysis is { Success: null, WasIgnored: true });
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
                        Vars = [new Variable(1, "x")],
                        GroupBy = [new Variable(2, "cat")],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null, null, []);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);

        analyses[0].Success = null;
        
        batchAnalyze.RunBatch(analyses, false, null, null, []);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);
    }
    
    [Fact]
    public void TestRunBatchDoesNotReapplySameSubsettingOnCurrentFile()
    {
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.CalculateUnivar(It.IsAny<AnalysisUnivar>())).Returns([]);
        rservice.Setup(service =>
            service.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), It.IsAny<string?>())).Returns(true);
        
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

        BatchAnalyzeService batchAnalyze = new(rservice.Object, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

        AnalysisPresentation dummyAnalysisPresentation = new();

        List<BatchAnalyze.BatchEntry> analyses =
        [
            new()
            {
                Id = 1, Selected = true, Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5)
                    {
                        Vars = [new Variable(1, "x")],
                        GroupBy = [new Variable(2, "cat")],
                        SubsettingExpression = "cat == 1",
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5, "cat == 1", []);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));

        rservice.Verify(service => service.CalculateUnivar(It.IsAny<AnalysisUnivar>()), Times.Once);
        rservice.Verify(service => service.TestAnalysisConfiguration(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), It.IsAny<string?>()), Times.Never);
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

    [Fact]
    public void TestRunBatchReappliesVirtualVariablesOnReload()
    {
        Logging logging = new();
        Rservice rservice = new(logging)
        {
            RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, string.Empty)
        };
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
                        Vars = [new Variable(1, "x"), new Variable(3, "zx")],
                        GroupBy = [new Variable(2, "cat")],
                        VirtualVariables = [
                            new VirtualVariableScale
                            {
                                Name = "zx",
                                ForFileName = "test_nmi10_nrep5.sav",
                                Type = VirtualVariableScale.ScaleType.Linear,
                                InputVariable = new Variable(1, "x"),
                                WeightVariable = new Variable(4, "wgt"),
                                MiVariable = new Variable(5, "mi"),
                            }
                        ],
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
                        Vars = [new Variable(1, "x")],
                        GroupBy = [new Variable(2, "cat")],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, false, null, null, []);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);
        Assert.Equal(0, double.Parse(analyses[0].PreparedPresentation!.DataTable.Select("variable = 'zx'")[0]["mean"].ToString()!), 10);
        Assert.Single(logging.LogEntries.Where(entry => entry.Rcode.Contains("read.spss")));
    }

    [Fact]
    public void TestRunBatchUpdatesVirtualVariableDefinitionsFromCurrentFile()
    {
        Logging logging = new();
        Rservice rservice = new(logging)
        {
            RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, string.Empty)
        };
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
        
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Rep5.FileName));
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5, [
            new VirtualVariableScale
            {
                Name = "zx",
                ForFileName = "test_nmi10_nrep5.sav",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new Variable(1, "x"),
                WeightVariable = new Variable(4, "wgt"),
                MiVariable = new Variable(5, "mi"),
            }
        ]));

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
                        Vars = [new Variable(1, "x"), new Variable(3, "zx")],
                        GroupBy = [new Variable(2, "cat")],
                        VirtualVariables = [
                            new VirtualVariableScale
                            {
                                Name = "zx",
                                ForFileName = "test_nmi10_nrep5.sav",
                                Type = VirtualVariableScale.ScaleType.Linear,
                                InputVariable = new Variable(1, "x"),
                                WeightVariable = new Variable(4, "wgt"),
                                MiVariable = new Variable(5, "mi"),
                            }
                        ],
                    },
                    ViewSettings = dummyAnalysisPresentation.ViewSettings
                }
            },
        ];

        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5, string.Empty, [
            new VirtualVariableScale
            {
                Name = "zx",
                ForFileName = "test_nmi10_nrep5.sav",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new Variable(1, "x"),
                WeightVariable = new Variable(4, "wgt"),
                MiVariable = new Variable(5, "mi"),
            }
        ]);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);
        Assert.Equal(0, double.Parse(analyses[0].PreparedPresentation!.DataTable.Select("variable = 'zx'")[0]["mean"].ToString()!), 10);
        
        Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5, [
            new VirtualVariableScale
            {
                Name = "zx",
                ForFileName = "test_nmi10_nrep5.sav",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new Variable(1, "x"),
                WeightVariable = new Variable(4, "wgt"),
                MiVariable = new Variable(5, "mi"),
                Mean = 500
            }
        ]));
        
        batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5, string.Empty, [
            new VirtualVariableScale
            {
                Name = "zx",
                ForFileName = "test_nmi10_nrep5.sav",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new Variable(1, "x"),
                WeightVariable = new Variable(4, "wgt"),
                MiVariable = new Variable(5, "mi"),
                Mean = 500
            }
        ]);

        Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(100))
            .Execute(() => Assert.True(analyses.All(analysis => analysis.Success is not null)));
        
        Assert.True(analyses[0].Success);
        Assert.Equal(500, double.Parse(analyses[0].PreparedPresentation!.DataTable.Select("variable = 'zx'")[0]["mean"].ToString()!), 10);
        Assert.Equal(500, (analyses[0].Analysis.Analysis.VirtualVariables.First() as VirtualVariableScale)!.Mean);
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
