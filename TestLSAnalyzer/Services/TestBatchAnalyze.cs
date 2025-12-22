using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services.DataProvider;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using Xunit.Sdk;
using BatchAnalyze = LSAnalyzer.Services.BatchAnalyze;

namespace TestLSAnalyzer.Services
{
    [Collection("Sequential")]
    public class TestBatchAnalyze
    {
        [Fact]
        public void TestRunBatchSendsMessage()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfiguration = new()
            {
                DatasetType = new() { },
                FileName = "dummy.sav",
                ModeKeep = true,
            };

            BatchAnalyze batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

            int messageCounter = 0;
            BatchAnalyzeMessage? lastMessage = null;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messageCounter++;
                lastMessage = m;
            });

            AnalysisPresentation dummyAnalysisPresentation = new();
            
            batchAnalyze.RunBatch(new()
            {
                { 1, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfiguration), ViewSettings = dummyAnalysisPresentation.ViewSettings} }, 
                { 2, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfiguration), ViewSettings = dummyAnalysisPresentation.ViewSettings} }, 
            }, true, analysisConfiguration);

            Policy.Handle<EqualException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(4, messageCounter));
            
            Assert.NotNull(lastMessage);
            Assert.False(lastMessage.Success);
        }

        [Fact]
        public void TestRunBatchWithReloadingFiles()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfigurationInvalid = new()
            {
                DatasetType = new() { },
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

            BatchAnalyze batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });

            AnalysisPresentation dummyAnalysisPresentation = new();

            Dictionary<int, AnalysisWithViewSettings> analyses = new()
            {
                { 1, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "doesntExist", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 2, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 3, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "doesntExist", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 4, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 5, new AnalysisWithViewSettings { Analysis = new AnalysisCorr(analysisConfigurationInvalid) {
                        Vars = new() { new(1, "dummy", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
            };

            batchAnalyze.RunBatch(analyses, false, null);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(10, messages.Count));
            
            Assert.False(messages[1].Success);
            Assert.True(messages[3].Success);
            Assert.False(messages[5].Success);
            Assert.True(messages[7].Success);
            Assert.False(messages[9].Success);
        }

        [Fact]
        public void TestRunBatchOnCurrentFileModeKeep()
        {
            Rservice rservice = new(new());
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

            BatchAnalyze batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });
            
            AnalysisPresentation dummyAnalysisPresentation = new();

            Dictionary<int, AnalysisWithViewSettings> analyses = new()
            {
                { 1, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "doesntExist", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 2, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 3, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "cat", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 4, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
            };

            batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(8, messages.Count));

            Assert.False(messages[1].Success);
            Assert.True(messages[3].Success);
            Assert.True(messages[5].Success);
            Assert.False(messages[7].Success);
        }

        [Fact]
        public void TestRunBatchOnCurrentFileModeBuild()
        {
            Rservice rservice = new(new());
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

            BatchAnalyze batchAnalyze = new(rservice, Mock.Of<Configuration>(), Mock.Of<IServiceProvider>());

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });
            
            AnalysisPresentation dummyAnalysisPresentation = new();

            Dictionary<int, AnalysisWithViewSettings> analyses = new()
            {
                { 1, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "item1", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 2, new AnalysisWithViewSettings { Analysis = new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 3, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "cat", false) },
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
                { 4, new AnalysisWithViewSettings { Analysis = new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    }, ViewSettings = dummyAnalysisPresentation.ViewSettings } },
            };

            batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Multicat);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(8, messages.Count));

            Assert.False(messages[1].Success);
            Assert.False(messages[3].Success);
            Assert.True(messages[5].Success);
            Assert.True(messages[7].Success);
        }

        [Fact]
        public void TestRetrieveDataProvider()
        {
            Rservice rservice = new(new());
            
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
            serviceCollection.AddTransient<Rservice>(_ => rservice);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            BatchAnalyze batchAnalyze = new(rservice, configuration.Object, serviceProvider);
            
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
        public void TestRetrieveFileInformation()
        {
            Rservice rservice = new(new());
            
            var configuration = new Mock<Configuration>();
            
            ServiceCollection serviceCollection = new();
            serviceCollection.AddTransient<Rservice>(_ => rservice);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            BatchAnalyze batchAnalyze = new(rservice, configuration.Object, serviceProvider);
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
}
