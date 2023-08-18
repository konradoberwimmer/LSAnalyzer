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
    public class TestAnalysisPresentation
    {
        [Fact]
        public void TestSetAnalysisResult()
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
            };

            var result = rservice.CalculateUnivar(analysisUnivar);

            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);
            analysisPresentationViewModel.SetAnalysisResult(result);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
        }

        [Fact]
        public void TestSaveDataTableXlsx()
        {

            AnalysisPresentation analysisPresentationViewModel = new();

            analysisPresentationViewModel.DataTable = new("results")
            {
                Columns = { { "var", typeof(string) }, { "y1", typeof(int) }, { "mean", typeof(double) }, { "mean__se", typeof(double) }, { "sd", typeof(double) }, { "sd__se", typeof(double) } },
                Rows =
                        {
                            { "x1", 1, 0.5, 0.01, 0.1, 0.001 },
                            { "x1", 2, 0.6, 0.006, 0.12, 0.0011 },
                            { "x1", 3, 0.7, 0.012, 0.09, 0.0009 },
                            { "x1", 4, 0.8, 0.011, 0.11, 0.0011 },
                            { "x2", 1, 12.5, 0.12, 1.41, 0.023 },
                            { "x2", 2, 11.3, 0.13, 1.02, 0.064 },
                            { "x2", 3, 9.8, 0.22, 2.01, 0.044 },
                            { "x2", 4, 12.1, 0.21, 2.01, 0.031 },
                            { "x3", 1, -2.28, 0.23, 0.5, 0.012 },
                            { "x3", 2, 3.12, 0.73, 0.3, 0.031 },
                            { "x3", 3, 1.02, 0.32, 0.3, 0.021 },
                            { "x3", 4, -0.45, 0.64, 0.7, 0.011 },
                        }
            };

            var filename = Path.Combine(Path.GetTempPath(), "TestSaveDataTableXlsx.xlsx");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            analysisPresentationViewModel.SaveDataTableXlsxCommand.Execute(filename);

            Assert.True(File.Exists(filename));
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
