using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestMainWindow
    {
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

            Rservice rservice = new(new());
            MainWindow mainWindowViewModel = new(rservice);

            mainWindowViewModel.AnalysisConfiguration = analysisConfigurationA;
            mainWindowViewModel.SubsettingExpression = "x == 2";

            mainWindowViewModel.AnalysisConfiguration = analysisConfigurationB;
            Assert.Null(mainWindowViewModel.SubsettingExpression);
        }

        [Fact]
        public async Task TestStartAnalysis()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            MainWindow mainWindowViewModel = new(rservice);

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelUnivar = new(analysisUnivar);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelUnivar);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelUnivar);
            await Task.Delay(500);

            Assert.NotNull(mainWindowViewModel.Analyses.Last().DataTable);
            Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateSeparately = false,
            };
            AnalysisPresentation analysisPresentationViewModelMeanDiff = new(analysisMeanDiff);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelMeanDiff);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelMeanDiff);
            await Task.Delay(500);

            Assert.NotNull(mainWindowViewModel.Analyses.Last().DataTable);
            Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "cat", false) },
                GroupBy = new() { new(1, "instable", false) },
                CalculateOverall = true,
            };
            AnalysisPresentation analysisPresentationViewModelFreq = new(analysisFreq);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModelFreq);
            Assert.Empty(mainWindowViewModel.Analyses.Last().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelFreq);
            await Task.Delay(500);

            Assert.NotNull(mainWindowViewModel.Analyses.Last().DataTable);
            Assert.NotEmpty(mainWindowViewModel.Analyses.Last().DataTable.Rows);
        }

        [Fact]
        public async Task TestStartAnalysisSendsFailureMessage()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "z", false), new(1, "k", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);

            MainWindow mainWindowViewModel = new(rservice);

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<FailureWithAnalysisCalculationMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModel);
            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModel);
            await Task.Delay(1000);

            Assert.Empty(mainWindowViewModel.Analyses.First().DataTable.Rows);
            Assert.True(messageSent);
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
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
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
        public async Task TestAnalysisWithDifferentWeights()
        {
            // will also test ViewModels.SelectAnalysisFile
            var fileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiwgt.sav");
            DatasetType datasetType = new()
            {
                Weight = "wgt;wgt100",
                NMI = 10,
                MIvar = "mi",
                Nrep = 1,
                FayFac = 1
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            MainWindow mainWindowViewModel = new(rservice);

            SelectAnalysisFile selectAnalysisFileViewModel = new(new Mock<Configuration>().Object, rservice, new ServiceCollection().AddSingleton(rservice).BuildServiceProvider());
            selectAnalysisFileViewModel.FileName = fileName;
            selectAnalysisFileViewModel.SelectedDatasetType = datasetType;
            selectAnalysisFileViewModel.SelectedWeightVariable = selectAnalysisFileViewModel.PossibleWeightVariables.First();
            selectAnalysisFileViewModel.SelectedAnalysisMode = SelectAnalysisFile.AnalysisModes.Build;
            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

            await Task.Delay(5000);
            Assert.NotNull(mainWindowViewModel.AnalysisConfiguration);
            Assert.Equal("wgt", mainWindowViewModel.AnalysisConfiguration.DatasetType!.Weight);

            AnalysisCorr analysisCorrWgt = new(mainWindowViewModel.AnalysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelWgt = new(analysisCorrWgt);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelWgt);
            await Task.Delay(500);
            Assert.NotEmpty(analysisPresentationViewModelWgt.DataTable.Rows);

            selectAnalysisFileViewModel.SelectedWeightVariable = selectAnalysisFileViewModel.PossibleWeightVariables.Last();
            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

            await Task.Delay(5000);
            Assert.Equal("wgt100", mainWindowViewModel.AnalysisConfiguration.DatasetType!.Weight);

            AnalysisCorr analysisCorrWgt100 = new(mainWindowViewModel.AnalysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModelWgt100 = new(analysisCorrWgt100);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModelWgt100);
            await Task.Delay(500);
            Assert.NotEmpty(analysisPresentationViewModelWgt100.DataTable.Rows);

            var columnNweight = analysisPresentationViewModelWgt100.DataTable.Columns.IndexOf("N - weighted");
            var columnEstimate = analysisPresentationViewModelWgt100.DataTable.Columns.IndexOf("correlation");
            for (int rr = 0; rr < analysisPresentationViewModelWgt100.DataTable.Rows.Count; rr++)
            {
                Assert.True(Math.Abs((double)analysisPresentationViewModelWgt100.DataTable.Rows[rr].ItemArray[columnEstimate]! - (double)analysisPresentationViewModelWgt.DataTable.Rows[rr].ItemArray[columnEstimate]!) < 0.000001);
                Assert.True(Math.Abs((double)analysisPresentationViewModelWgt100.DataTable.Rows[rr].ItemArray[columnNweight]! / 100.0 - (double)analysisPresentationViewModelWgt.DataTable.Rows[rr].ItemArray[columnNweight]!) < 0.000001);
            }
        }

        [Fact]
        public void TestSaveAnalysesDefintionsCommand()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            MainWindow mainWindowViewModel = new(rservice);
            
            var tmpFile = Path.Combine(Path.GetTempPath(), "TestSaveAnalysesDefintionsCommand.json");
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }

            mainWindowViewModel.SaveAnalysesDefintionsCommand.Execute(tmpFile);
            Assert.False(File.Exists(tmpFile));

            DatasetType dummyType = new()
            {
                Id = 1,
                Name = "Dummy",
                Description = "Test",
                NMI = 1,
                Nrep = 1,
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
                Vars = new() { new(1, "y1", false), new(2, "y2", false) },
                GroupBy = new() { new(3, "x", false) },
                CalculateOverall = false,
            }));

            mainWindowViewModel.Analyses.Add(new(new AnalysisFreq(dummyAnalysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                GroupBy = new() { new(3, "x", false) },
                CalculateOverall = false,
                CalculateBivariate = false,
            }));

            mainWindowViewModel.SaveAnalysesDefintionsCommand.Execute(tmpFile);
            Assert.True(File.Exists(tmpFile));
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
