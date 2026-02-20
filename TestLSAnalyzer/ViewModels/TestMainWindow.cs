using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using System.Text.Json;
using LSAnalyzer.Services.Stubs;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestMainWindow
    {
        [Fact]
        public void TestConstructor()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            
            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueueStub());
            
            Assert.False(string.IsNullOrWhiteSpace(mainWindowViewModel.BifieSurveyVersion));
            Assert.True(mainWindowViewModel.ConnectedToR);
        }

        [Fact]
        public void TestHandleSetAnalysisConfigurationMessage()
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

            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueue(rservice));

            Assert.Null(mainWindowViewModel.AnalysisConfiguration);
            Assert.Empty(mainWindowViewModel.CurrentDatasetVariables);
            
            WeakReferenceMessenger.Default.Send(new SetAnalysisConfigurationMessage(analysisConfiguration));
            
            Assert.NotNull(mainWindowViewModel.AnalysisConfiguration);
            Assert.NotEmpty(mainWindowViewModel.CurrentDatasetVariables);
        }

        [Fact]
        public void TestHandleAnalysisQueueCountChangedMessage()
        {
            AnalysisCorr analysisCorr = new(new AnalysisConfiguration());
            
            MainWindow mainWindowViewModel = new(new RserviceStub(), new AnalysisQueueStub())
            {
                Analyses = [
                    new AnalysisPresentation
                    {
                        Analysis = analysisCorr,
                        IsBusy = true
                    }
                ]
            };

            var sentNotifyIsBusyChanged = false;
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "RIsBusy") sentNotifyIsBusyChanged = true;
            };

            WeakReferenceMessenger.Default.Send(new AnalysisQueue.AnalysisQueueCountChangedMessage());

            Assert.True(sentNotifyIsBusyChanged);
        }

        [Fact]
        public void TestHandleBatchAnalyzeAnalysisReadyMessage()
        {
            MainWindow mainWindowViewModel = new(new RserviceStub(), new AnalysisQueueStub());
            
            Assert.Empty(mainWindowViewModel.Analyses);
            
            WeakReferenceMessenger.Default.Send(new BatchAnalyze.BatchAnalyzeAnalysisReadyMessage(new AnalysisPresentation()));
            
            Assert.Single(mainWindowViewModel.Analyses);
            Assert.Equal(mainWindowViewModel, mainWindowViewModel.Analyses.First().MainWindowViewModel);
        }
        
        [Fact]
        public void TestSetAnalysisConfigurationClearsSubsetting()
        {
            AnalysisConfiguration analysisConfigurationA = new()
            {
                FileName = "dummyA.sav",
                DatasetType = new(),
                ModeKeep = true,
            };

            AnalysisConfiguration analysisConfigurationB = new()
            {
                FileName = "dummyB.sav",
                DatasetType = new(),
                ModeKeep = false,
            };

            Rservice rservice = new();
            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueueStub());

            mainWindowViewModel.AnalysisConfiguration = analysisConfigurationA;
            mainWindowViewModel.SubsettingExpression = "x == 2";

            mainWindowViewModel.AnalysisConfiguration = analysisConfigurationB;
            Assert.Null(mainWindowViewModel.SubsettingExpression);
        }

        [Fact]
        public void TestStartAnalysis()
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

            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueue(rservice));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelUnivar = new(analysisUnivar);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelUnivar);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelUnivar);
            
            Policy.Handle<NotEmptyException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(10))
                .Execute(() => Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
                CalculateSeparately = false,
            };
            AnalysisPresentation analysisPresentationViewModelMeanDiff = new(analysisMeanDiff);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelMeanDiff);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelMeanDiff);

            Policy.Handle<NotEmptyException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows));

            analysisConfiguration = new()
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
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "cat") },
                GroupBy = new() { new(1, "instable") },
                CalculateOverall = true,
            };
            AnalysisPresentation analysisPresentationViewModelFreq = new(analysisFreq);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelFreq);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelFreq);

            Policy.Handle<NotEmptyException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows));
        }

        [Fact]
        public void TestStartAnalysisSendsFailureMessage()
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
                Vars = new() { new(1, "z"), new(1, "k") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);

            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueue(rservice));

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<AnalysisQueue.FailureWithAnalysisCalculationMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModel);
            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModel);

            Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.True(messageSent));
            Assert.Empty(mainWindowViewModel.Analyses.First().DataTable.Rows);
        }

        [Fact]
        public void TestRemoveAnalysis()
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

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);

            MainWindow mainWindowViewModel = new();

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModel);
            Assert.NotEmpty(mainWindowViewModel.Analyses);

            while (mainWindowViewModel.Analyses.Count > 0)
            {
                mainWindowViewModel.RemoveAnalysisCommand.Execute(mainWindowViewModel.Analyses.First());
            }
            Assert.Empty(mainWindowViewModel.Analyses);
        }

        [Fact]
        public void TestAnalysisWithDifferentWeights()
        {
            // will also test ViewModels.SelectAnalysisFile
            var fileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiwgt.sav");
            DatasetType datasetType = new()
            {
                Weight = "wgt;wgt100",
                NMI = 10,
                MIvar = "mi",
                FayFac = 1
            };

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueue(rservice));

            var configurationMock = new Mock<Configuration>();
            configurationMock.Setup(conf => conf.GetStoredRecentFiles(It.IsAny<int>())).Returns([]);
            configurationMock.Setup(conf => conf.GetDataProviderConfigurations()).Returns([]);
            
            SelectAnalysisFile selectAnalysisFileViewModel = new(configurationMock.Object, rservice, new ServiceCollection().AddSingleton(rservice).BuildServiceProvider());
            selectAnalysisFileViewModel.FileName = fileName;
            selectAnalysisFileViewModel.SelectedDatasetType = datasetType;
            selectAnalysisFileViewModel.SelectedWeightVariable = selectAnalysisFileViewModel.PossibleWeightVariables.First();
            selectAnalysisFileViewModel.SelectedAnalysisMode = SelectAnalysisFile.AnalysisModes.Build;
            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

            Policy.Handle<NotNullException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(100))
                .Execute(() => Assert.NotNull(mainWindowViewModel.AnalysisConfiguration));
            Assert.Equal("wgt", mainWindowViewModel.AnalysisConfiguration!.DatasetType!.Weight);

            AnalysisCorr analysisCorrWgt = new(mainWindowViewModel.AnalysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(2, "item2") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelWgt = new(analysisCorrWgt);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelWgt);
            Policy.Handle<NotEmptyException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(100))
                .Execute(() => Assert.NotEmpty(analysisPresentationViewModelWgt.DataTable.Rows));

            selectAnalysisFileViewModel.SelectedWeightVariable = selectAnalysisFileViewModel.PossibleWeightVariables.Last();
            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

            Policy.Handle<EqualException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(100))
                .Execute(() => Assert.Equal("wgt100", mainWindowViewModel.AnalysisConfiguration.DatasetType!.Weight));

            AnalysisCorr analysisCorrWgt100 = new(mainWindowViewModel.AnalysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(2, "item2") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelWgt100 = new(analysisCorrWgt100);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelWgt100);
            Policy.Handle<NotEmptyException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(100))
                .Execute(() => Assert.NotEmpty(analysisPresentationViewModelWgt100.DataTable.Rows));

            var columnNweight = analysisPresentationViewModelWgt100.DataTable.Columns.IndexOf("N - weighted");
            var columnEstimate = analysisPresentationViewModelWgt100.DataTable.Columns.IndexOf("correlation");
            for (int rr = 0; rr < analysisPresentationViewModelWgt100.DataTable.Rows.Count; rr++)
            {
                Assert.True(Math.Abs((double)analysisPresentationViewModelWgt100.DataTable.Rows[rr].ItemArray[columnEstimate]! - (double)analysisPresentationViewModelWgt.DataTable.Rows[rr].ItemArray[columnEstimate]!) < 0.000001);
                Assert.True(Math.Abs((double)analysisPresentationViewModelWgt100.DataTable.Rows[rr].ItemArray[columnNweight]! / 100.0 - (double)analysisPresentationViewModelWgt.DataTable.Rows[rr].ItemArray[columnNweight]!) < 0.000001);
            }
        }

        [Fact]
        public void TestSaveAnalysesDefinitionsCommand()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueueStub());
            
            var tmpFile = Path.Combine(Path.GetTempPath(), "TestSaveAnalysesDefintionsCommand.json");
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }

            mainWindowViewModel.SaveAnalysesDefinitionsCommand.Execute(tmpFile);
            Assert.False(File.Exists(tmpFile));

            DatasetType dummyType = new()
            {
                Id = 1,
                Name = "Dummy",
                Description = "Test",
                NMI = 1,
            };

            AnalysisConfiguration dummyAnalysisConfiguration = new()
            {
                DatasetType = dummyType,
                FileName = "dummyFile.sav",
                FileType = "sav",
                ModeKeep = true,
            };

            mainWindowViewModel.Analyses.Add(new(new AnalysisUnivar(dummyAnalysisConfiguration)
            {
                Vars = new() { new(1, "y1"), new(2, "y2") },
                GroupBy = new() { new(3, "x") },
                CalculateOverall = false,
            }));

            mainWindowViewModel.Analyses.Add(new(new AnalysisFreq(dummyAnalysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(2, "item2") },
                GroupBy = new() { new(3, "x") },
                CalculateOverall = false,
                CalculateBivariate = false,
            }));

            mainWindowViewModel.SaveAnalysesDefinitionsCommand.Execute(tmpFile);
            Assert.True(File.Exists(tmpFile));
            
            var analysisSerializationHelpers =
                JsonSerializer.Deserialize<AnalysisWithViewSettings[]>(File.ReadAllText(tmpFile))!;
            
            Assert.Equal(2, analysisSerializationHelpers.Length);
        }

        [Fact]
        public void TestRemoveAllAnalysesCommand()
        {
            Rservice rservice = new();
            MainWindow mainWindowViewModel = new(rservice, new AnalysisQueueStub());
            
            mainWindowViewModel.Analyses.Add(new AnalysisPresentation());
            mainWindowViewModel.Analyses.Add(new AnalysisPresentation());
            
            mainWindowViewModel.RemoveAllAnalysesCommand.Execute(null);
            
            Assert.Empty(mainWindowViewModel.Analyses);
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
