using LSAnalyzer.Models;
using LSAnalyzer.Services;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestLSAnalyzer.Services
{
    [Collection("Sequential")]
    public class TestRservice
    {
        [Fact]
        public void TestConnect()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
        }

        [Fact]
        public void TestInstallAndCheckNecessaryRPackages()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InstallNecessaryRPackages());
            Assert.True(rservice.CheckNecessaryRPackages(), "R packages are also necessary for tests");
        }

        [Fact]
        public void ReduceToNecessaryVariables()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));

            Assert.False(rservice.ReduceToNecessaryVariables(new() { "^impossible$" }));
            Assert.True(rservice.ReduceToNecessaryVariables(new() { "^wgt$", "^jkzone$", "^jkrep$" }));
        }

        [Fact]
        public void TestLoadFileIntoGlobalEnvironment()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.False(rservice.LoadFileIntoGlobalEnvironment(Path.GetTempFileName()));
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_dummy.sav")));
        }

        [Fact]
        public void TestCreateReplicateWeights()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgt", "jkzoooone", "jkrep", false));
            Assert.True(rservice.CreateReplicateWeights(2, "wgt", "jkzone", "jkrep", false));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgt", "jkzone", "jkrep", true));
            Assert.True(rservice.CreateReplicateWeights(4, "wgt", "jkzone", "jkrep", true));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgtstud", "jkzone", "jkrep", false));
        }

        [Fact]
        public void TestCreateBIFIEdataObject()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 3, "repwgt", 1));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, null, null, 5, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));
        }

        [Fact]
        public void TestGetCurrentDatasetVariables()
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
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            var variablesList = rservice.GetCurrentDatasetVariables(analysisConfiguration);
            Assert.NotNull(variablesList);
            Assert.True(variablesList.Count == 11);
            Assert.True(variablesList.Where(var => var.Name == "repwgt1").First().IsSystemVariable);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvars = "x;y[0-9]+",
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));
            var variablesListModeBuild = rservice.GetCurrentDatasetVariables(analysisConfigurationModeBuild);

            Assert.NotNull(variablesListModeBuild);
            Assert.True(variablesListModeBuild.Count == 10);
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "x").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "y[0-9]+").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "one").ToList());
        }

        [Fact]
        public void TestCalculateUnivar()
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
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(5, Convert.ToInt32(stats["Ncases"][2]));
            Assert.True(Math.Abs((double)stats["SD"][0] - 44.54742) < 0.0001);
            Assert.True(Math.Abs((double)stats["SD_SE"][0] - 13.24182) < 0.0001);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvars = "x;y[0-9]+",
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisUnivar analysisUnivarModeBuild = new(analysisConfigurationModeBuild)
            {
                Vars = new() { new(1, "x", false), new(1, "y[0-9]+", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            var resultModeBuild = rservice.CalculateUnivar(analysisUnivarModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Single(resultModeBuild);
            var firstResultModeBuild = resultModeBuild.First();
            var statsModeBuild = firstResultModeBuild["stat"].AsDataFrame();
            Assert.Equal(5, Convert.ToInt32(statsModeBuild["Ncases"][2]));
            Assert.True(Math.Abs((double)statsModeBuild["SD"][0] - 44.54742) < 0.0001);
            Assert.True(Math.Abs((double)statsModeBuild["SD_SE"][0] - 13.24182) < 0.0001);

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal((double)statsModeBuild["M_SE"][i], (double)stats["M_SE"][i]);
            }
        }

        [Fact]
        public void TestCalculateUnivarWithOverallValues()
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
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            var result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0]["stat"].AsDataFrame().RowCount);
            Assert.Equal(4, result[1]["stat"].AsDataFrame().RowCount);

            analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false) },
                GroupBy = new() { new(3, "cat", false), new(3, "mi", false) },
                CalculateOverall = true,
            };

            result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            Assert.Equal(1, result[0]["stat"].AsDataFrame().RowCount);
            Assert.Equal(2, result[1]["stat"].AsDataFrame().RowCount);
            Assert.Equal(10, result[2]["stat"].AsDataFrame().RowCount);
            Assert.Equal(20, result[3]["stat"].AsDataFrame().RowCount);

            analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false) },
                CalculateOverall = true,
            };

            result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0]["stat"].AsDataFrame().RowCount);
        }

        [Fact]
        public void TestGetDatasetVariables()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            var filename = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");

            var variables = rservice.GetDatasetVariables(filename);
            Assert.NotNull(variables);
            Assert.Equal(10, variables.Count);
            Assert.Single(variables.Where(var => var.Name == "repwgt3"));
        }

        [Fact]
        public void TestGetValueLabels()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            var filename = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(filename));

            var nonExistentVariableValueLabels = rservice.GetValueLabels("Maulmaus");
            Assert.Null(nonExistentVariableValueLabels);

            var nonExistentValueLabels = rservice.GetValueLabels("mi");
            Assert.Null(nonExistentValueLabels);

            var valueLabels = rservice.GetValueLabels("cat");
            Assert.NotNull(valueLabels);
            Assert.Equal(2, valueLabels.RowCount);
            Assert.Equal("Kategorie B", valueLabels["label"].AsCharacter()[valueLabels["value"].AsInteger().ToList().IndexOf(2)]);
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
