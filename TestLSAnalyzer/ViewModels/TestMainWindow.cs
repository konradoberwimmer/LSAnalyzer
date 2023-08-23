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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);

            MainWindow mainWindowViewModel = new(rservice);

            mainWindowViewModel.Analyses.Add(analysisPresentationViewModel);
            Assert.Empty(mainWindowViewModel.Analyses.First().DataTable.Rows);

            mainWindowViewModel.StartAnalysisCommand.Execute(analysisPresentationViewModel);
            await Task.Delay(1000);

            Assert.NotNull(mainWindowViewModel.Analyses.First().DataTable);
            Assert.NotEmpty(mainWindowViewModel.Analyses.First().DataTable.Rows);
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
