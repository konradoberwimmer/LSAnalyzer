﻿using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
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
