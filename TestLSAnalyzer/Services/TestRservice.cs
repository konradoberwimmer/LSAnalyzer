using LSAnalyzer.Models;
using LSAnalyzer.Services;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
        }

        [Fact]
        public void TestExecute()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.Execute("x <- 2 * 2"));
            Assert.False(rservice.Execute("x <- abcdefg[2, 'hij']"));
        }

        [Fact]
        public void TestFetch()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.Execute("x <- 2 * 2"));

            Assert.NotNull(rservice.Fetch("x"));
            Assert.Null(rservice.Fetch("notexistenttestvariable"));
        }

        [Fact]
        public void TestInstallAndCheckNecessaryRPackages()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InstallNecessaryRPackages());
            Assert.True(rservice.CheckNecessaryRPackages(), "R packages are also necessary for tests");
        }

        [Fact]
        public void TestInstallAndCheckOptionalRPackages()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.False(rservice.CheckNecessaryRPackages("sinnlos123"));
            Assert.True(rservice.InstallNecessaryRPackages("jsonlite"));
            Assert.True(rservice.CheckNecessaryRPackages("jsonlite"));
        }

        [Fact]
        public void TestInjectAppFunctions()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.InjectAppFunctions());
        }

        [Fact]
        public void TestLoadFileIntoGlobalEnvironment()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.False(rservice.LoadFileIntoGlobalEnvironment(Path.GetTempFileName()));
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_dummy.sav")));
        }

        [Fact]
        public void TestReplaceCharacterVariables()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.False(rservice.ReplaceCharacterVariables());

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_characters.sav")));

            var engine = REngine.GetInstance();

            Assert.False(engine.Evaluate("is.numeric(lsanalyzer_dat_raw_stored$text)").AsLogical().First());
            Assert.True(rservice.ReplaceCharacterVariables());
            Assert.True(engine.Evaluate("is.numeric(lsanalyzer_dat_raw_stored$text)").AsLogical().First());
        }

        [Fact]
        public void TestTestSubsetting()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));

            var subsetInformationInvalid = rservice.TestSubsetting("xyz == 2");
            Assert.False(subsetInformationInvalid.ValidSubset);
            Assert.False(subsetInformationInvalid.MIvariance);
            Assert.Contains("Invalid", subsetInformationInvalid.Stringify);

            var subsetInformationWithoutMI = rservice.TestSubsetting("cat == 2 & mi >= 5");
            Assert.True(subsetInformationWithoutMI.ValidSubset);
            Assert.Equal(100, subsetInformationWithoutMI.NCases);
            Assert.Equal(30, subsetInformationWithoutMI.NSubset);

            var subsetInformationWithMI = rservice.TestSubsetting("cat == 1", "mi");
            Assert.True(subsetInformationWithMI.ValidSubset);
            Assert.Equal(10, subsetInformationWithMI.NCases);
            Assert.Equal(5, subsetInformationWithMI.NSubset);
            Assert.DoesNotContain("Invalid", subsetInformationWithMI.Stringify);

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_imputed_subset.sav")));

            var subsetInformationWithoutMIvariance = rservice.TestSubsetting("cat == 1", "mi");
            Assert.True(subsetInformationWithoutMIvariance.ValidSubset);
            Assert.Equal(10, subsetInformationWithoutMIvariance.NCases);
            Assert.Equal(5, subsetInformationWithoutMIvariance.NSubset);
            Assert.DoesNotContain("Invalid", subsetInformationWithoutMIvariance.Stringify);

            var subsetInformationWithMIvariance = rservice.TestSubsetting("instable == 1", "mi");
            Assert.False(subsetInformationWithMIvariance.ValidSubset);
            Assert.True(subsetInformationWithMIvariance.MIvariance);
            Assert.Contains("variance", subsetInformationWithMIvariance.Stringify);

            var subsetInformationWithEmptySubset = rservice.TestSubsetting("cat == 3");
            Assert.False(subsetInformationWithEmptySubset.ValidSubset);
            Assert.True(subsetInformationWithEmptySubset.EmptySubset);
            Assert.Contains("Empty subset.", subsetInformationWithEmptySubset.Stringify);
        }

        [Fact]
        public void TestApplySubsetting()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));

            Assert.False(rservice.ApplySubsetting("xyz == 2"));
            Assert.True(rservice.ApplySubsetting("mi == 2"));
        }

        [Fact]
        public void TestReduceToNecessaryVariables()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));

            Assert.False(rservice.ReduceToNecessaryVariables(new List<string>() { "^impossible$" }));
            Assert.True(rservice.ReduceToNecessaryVariables(new List<string>() { "^wgt$", "^jkzone$", "^jkrep$" }));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));

            Assert.True(rservice.ReduceToNecessaryVariables(new List<string>() { "^wgt$", "x" }, "cat == 1"));

            AnalysisConfiguration analysisConfiguration = new()
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
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfiguration) { Vars = new() { new(1, "x", false) } }, new() { "y" }, "cat == 2"));
        }

        [Fact]
        public void TestCreateReplicateWeights()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights("wgt", "jkzoooone", "jkrep", false));
            Assert.True(rservice.CreateReplicateWeights("wgt", "jkzone", "jkrep", false));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.True(rservice.CreateReplicateWeights("wgt", "jkzone", "jkrep", true));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights("wgtstud", "jkzone", "jkrep", false));
        }

        [Fact]
        public void TestCreateReplicateWeightsAfterSubsetting()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.True(rservice.ApplySubsetting("subset == 1"));

            Assert.True(rservice.CreateReplicateWeights("wgt", "jkzone", "jkrep", false));
        }

        [Fact]
        public void TestCreateBIFIEdataObject()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, null, null, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 0.5));
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
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 0.5));

            var variablesList = rservice.GetCurrentDatasetVariables(analysisConfiguration);
            Assert.NotNull(variablesList);
            Assert.True(variablesList.Count == 11);
            Assert.True(variablesList.Where(var => var.Name == "repwgt1").First().IsSystemVariable);
            Assert.Equal("my categories", variablesList.Where(var => var.Name == "cat").First().Label);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));
            var variablesListModeBuild = rservice.GetCurrentDatasetVariables(analysisConfigurationModeBuild);

            Assert.NotNull(variablesListModeBuild);
            Assert.True(variablesListModeBuild.Count == 20);
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "x").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "y").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "one").ToList());
            Assert.Equal("PV Mathematics 1", variablesListModeBuild.Where(var => var.Name == "y").First().Label);

            AnalysisConfiguration analysisConfigurationKeepPV = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationKeepPV.FileName));
            var variablesListKeepPV = rservice.GetCurrentDatasetVariables(analysisConfigurationKeepPV);

            Assert.NotNull(variablesListKeepPV);
            Assert.True(variablesListKeepPV.Count == 11);
            Assert.Single(variablesListKeepPV.Where(var => var.Name == "y").ToList());
            Assert.Equal("PV Mathematics 1", variablesListKeepPV.Where(var => var.Name == "y").First().Label);

            var rawVariablesListKeepPV = rservice.GetCurrentDatasetVariables(analysisConfigurationKeepPV, true);

            Assert.NotNull(rawVariablesListKeepPV);
            Assert.True(rawVariablesListKeepPV.Count == 37);
            Assert.Empty(rawVariablesListKeepPV.Where(var => var.Name == "y").ToList());
            Assert.Single(rawVariablesListKeepPV.Where(var => var.Name == "y1").ToList());
        }

        [Fact]
        public void TestTestAnalysisConfiguration()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            analysisConfiguration.ModeKeep = false;
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration, "xyz == 1"));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration, "cat == 1"));

            analysisConfiguration.DatasetType.JKzone = "mi";
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration));

            analysisConfiguration.DatasetType.JKzone = null;
            analysisConfiguration.DatasetType.RepWgts = "Bloedsinn";
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration));
        }

        [Fact]
        public void TestPrepareForAnalysis()
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
                ModeKeep = false,
            };

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false) },
                GroupBy = new() { new(2, "cat", false) },
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            Assert.True(rservice.PrepareForAnalysis(analysisUnivar));
            Assert.False(rservice.PrepareForAnalysis(analysisUnivar, new() { "xyz" }));


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
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

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
            Assert.Equal(1, Convert.ToInt32(stats["lsanalyzer_rank"][1]));
            Assert.True(Math.Abs((double)stats["SD"][0] - 44.54742) < 0.0001);
            Assert.True(Math.Abs((double)stats["SD_SE"][0] - 13.24182) < 0.0001);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisUnivar analysisUnivarModeBuild = new(analysisConfigurationModeBuild)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
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
                    RepWgts = "repwgt",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

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
        public void TestCalculateMeanDiffAllGroups()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } },
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA", false) },
                GroupBy = new() { },
                CalculateSeparately = false,
            };

            Assert.Null(rservice.CalculateMeanDiff(analysisMeanDiff));

            analysisMeanDiff.GroupBy = new() { new(2, "ITSEX", false), new(3, "ASBG05C", false) };

            var result = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(result);
            Assert.Single(result);

            var firstResult = result.First();
            var statM = firstResult["stat_M"].AsDataFrame();
            Assert.True(Math.Abs((double)statM["M"][0] - 549.66678562657671) < 0.0001);
            Assert.True(Math.Abs((double)statM["M_SE"][0] - 2.4462113724162045) < 0.0001);
            var statEta = firstResult["stat.eta"].AsDataFrame();
            Assert.True(Math.Abs((double)statEta["eta"][0] - 0.1770158) < 0.0001);
            Assert.True(Math.Abs((double)statEta["eta_SE"][0] - 0.0211625) < 0.0001);
            var statD= firstResult["stat.dstat"].AsDataFrame();
            Assert.Equal(6, statD.RowCount);
        }

        [Fact]
        public void TestCalculateMeanDiffSeparate()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } },
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA", false) },
                GroupBy = new() { new(2, "ITSEX", false), new(3, "ASBG05C", false) },
                CalculateSeparately = true,
            };

            var result = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            var firstResult = result[0];
            var statEta = firstResult["stat.eta"].AsDataFrame();
            Assert.True(Math.Abs((double)statEta["eta"][0] - 0.04523357) < 0.0001);
            Assert.True(Math.Abs((double)statEta["eta_SE"][0] - 0.0185994) < 0.0001);
            var statD = firstResult["stat.dstat"].AsDataFrame();
            Assert.Equal(1, statD.RowCount);

            var secondResult = result[1];
            Assert.Contains("stat_M", secondResult.AsList().Names);
        }

        [Fact]
        public void TestCalculateFreq()
        {
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateFreq(analysisFreq);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(1.5, Convert.ToDouble(stats["Ncases"][2]));
            Assert.Equal(2, Convert.ToInt32(stats["lsanalyzer_rank"][0]));
            Assert.True(Math.Abs((double)stats["perc"][0] - 0.2941176) < 0.0001);
            Assert.True(Math.Abs((double)stats["Nweight"][0] - 1.250) < 0.0001);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
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

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisFreq analysisFreqModeBuild = new(analysisConfigurationModeBuild)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            var resultModeBuild = rservice.CalculateFreq(analysisFreqModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Single(resultModeBuild);
            var firstResultModeBuild = resultModeBuild.First();
            var statsModeBuild = firstResultModeBuild["stat"].AsDataFrame();

            for (int i = 0; i < statsModeBuild.RowCount; i++)
            {
                Assert.Equal((double)statsModeBuild["perc_SE"][i], (double)stats["perc_SE"][i]);
            }
        }

        [Fact]
        public void TestCalculateBivariate()
        {
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "cat", false), new(4, "instable", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateBivariate(analysisFreq);
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            var firstResult = result.First();
            var stats = firstResult["stat.es"].AsDataFrame();
            Assert.Equal("w", stats["parm"][0]);
            Assert.True(Math.Abs((double)stats["est"][0] - 0.262162) < 0.0001);
            var dfNames = firstResult["stat.probs"].AsDataFrame();
            Assert.Equal("cat", dfNames["var1"][0]);
            Assert.Equal("item1", dfNames["var2"][0]);
        }

        [Fact]
        public void TestCalculateFreqWithOverallValues()
        {
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            var result = rservice.CalculateFreq(analysisFreq);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(9, result.First()["stat"].AsDataFrame().RowCount);
            var firstResult = result.First();
            var firstResultStats = firstResult["stat"].AsDataFrame();
            Assert.DoesNotContain("lsanalyzer_rank", firstResultStats.ColumnNames);
            Assert.Equal(18, result.Last()["stat"].AsDataFrame().RowCount);
        }

        [Fact]
        public void TestCalculatePercentialesWithoutSE()
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                Percentiles = new() { 0.25, 0.50, 0.75 },
                CalculateOverall = false,
                CalculateSE = false,
            };

            var result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(6, stats["groupval"].AsNumeric().Where(val => val == 2).ToList().Count);
            Assert.Equal(4, stats["yval"].AsNumeric().Where(val => val == 0.5).ToList().Count);
            Assert.True(Math.Abs((double)stats["quant"][0] - 5.836678) < 0.0001);

            analysisPercentiles.UseInterpolation = false;
            analysisPercentiles.CalculateOverall = true;

            result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            var lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.True(Math.Abs((double)stats["quant"][0] - 4.420020) < 0.0001);
        }

        [Fact]
        public void TestCalculatePercentialesWithSE()
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

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                Percentiles = new() { 0.25, 0.50, 0.75 },
                CalculateOverall = false,
                CalculateSE = true,
            };

            var result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(6, stats["groupval"].AsNumeric().Where(val => val == 2).ToList().Count);
            Assert.Equal(4, stats["yval"].AsNumeric().Where(val => val == 0.5).ToList().Count);
            Assert.True(Math.Abs((double)stats["quant"][0] - 5.836678) < 0.0001);
            Assert.True(Math.Abs((double)stats["SE"][1] - 43.010309) < 0.0001);

            analysisPercentiles.UseInterpolation = false;
            analysisPercentiles.CalculateOverall = true;

            result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            var lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.True(Math.Abs((double)stats["quant"][0] - 4.420020) < 0.0001);
            Assert.True(Math.Abs((double)stats["SE"][1] - 19.07956) < 0.0001);

            analysisPercentiles.MimicIdbAnalyzer = true;

            result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.True(Math.Abs((double)stats["quant"][0] - 7.82000) < 0.0001);
            Assert.True(Math.Abs((double)stats["SE"][1] - 52.179526) < 0.0001);
        }

        [Fact]
        public void TestCalculateCorr()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisCorr analysisCorr = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateCorr(analysisCorr);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var statsCor = firstResult["stat.cor"].AsDataFrame();
            var statsCov = firstResult["stat.cov"].AsDataFrame();
            Assert.Equal(5, Convert.ToInt32(statsCor["Ncases"][2]));
            Assert.True(Math.Abs((double)statsCor["cor"][0] - 0.032219071) < 0.0001);
            Assert.True(Math.Abs((double)statsCor["cor_SE"][0] - 0.2347620) < 0.0001);
            Assert.True(Math.Abs((double)statsCov["cov"][0] - 1.583710407) < 0.0001);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisCorr analysisCorrModeBuild = new(analysisConfigurationModeBuild)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = true,
            };

            var resultModeBuild = rservice.CalculateCorr(analysisCorrModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Equal(2, resultModeBuild.Count);
            var lastResultModeBuild = resultModeBuild.Last();
            var statsCorModeBuild = lastResultModeBuild["stat.cor"].AsDataFrame();

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal((double)statsCorModeBuild["cor_SE"][i], (double)statsCor["cor_SE"][i]);
            }
        }

        [Fact]
        public void TestCalculateLinregAllIn()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinreg = new(analysisConfiguration)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateLinreg(analysisLinreg);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(16, stats.RowCount);
            Assert.Equal(5, Convert.ToInt32(stats["Ncases"][2]));
            Assert.True(Math.Abs((double)stats["est"][0] - 2.70788043) < 0.0001);
            Assert.True(Math.Abs((double)stats["SE"][1] - 0.01802513) < 0.0001);

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisLinreg analysisLinregModeBuild = new(analysisConfigurationModeBuild)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = true,
            };

            var resultModeBuild = rservice.CalculateLinreg(analysisLinregModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Equal(2, resultModeBuild.Count);
            var lastResultModeBuild = resultModeBuild.Last();
            var statsModeBuild = lastResultModeBuild["stat"].AsDataFrame();

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal((double)statsModeBuild["SE"][i], (double)stats["SE"][i]);
            }
        }

        [Fact]
        public void TestCalculateLinregForward()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinregForward = new(analysisConfiguration)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
                Sequence = AnalysisRegression.RegressionSequence.Forward,
            };

            var result = rservice.CalculateLinreg(analysisLinregForward);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(6, stats.RowCount);
            Assert.Empty(stats["var"].Where(var => (string)var == "item2"));
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item3").Count());
            Assert.True(Math.Abs((double)stats["est"][3] - 0.1771340) < 0.0001);
            var lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.Equal(8, stats.RowCount);
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item2").Count());
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item3").Count());
            Assert.True(Math.Abs((double)stats["est"][4] - 0.28933320) < 0.0001);
        }

        [Fact]
        public void TestCalculateLinregBackward()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinregBackward = new(analysisConfiguration)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
                Sequence = AnalysisRegression.RegressionSequence.Backward,
            };

            var result = rservice.CalculateLinreg(analysisLinregBackward);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(8, stats.RowCount);
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item2").Count());
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item3").Count());
            Assert.True(Math.Abs((double)stats["est"][4] - 0.28933320) < 0.0001);
            var lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.Equal(6, stats.RowCount);
            Assert.Empty(stats["var"].Where(var => (string)var == "item3"));
            Assert.Equal(2, stats["var"].Where(var => (string)var == "item2").Count());
            Assert.True(Math.Abs((double)stats["est"][3] - 0.006023262) < 0.0001);
        }

        [Fact]
        public void TestCalculateLogistregAllIn()
        {
            // TODO: test should also assert correct values but at BIFIEsurvey version 3.4-15, logistic regression with grouping variable is broken
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_logistic.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLogistReg analysisLogistReg = new(analysisConfiguration)
            {
                Dependent = new(1, "event", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateLogistReg(analysisLogistReg);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(8, stats.RowCount);
            Assert.Equal(5, Convert.ToInt32(stats["Ncases"][2]));

            AnalysisConfiguration analysisConfigurationModeBuild = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_logistic.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationModeBuild.FileName));

            AnalysisLogistReg analysisLogistRegModeBuild = new(analysisConfigurationModeBuild)
            {
                Dependent = new(1, "event", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = true,
            };

            var resultModeBuild = rservice.CalculateLogistReg(analysisLogistRegModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Equal(2, resultModeBuild.Count);
            var lastResultModeBuild = resultModeBuild.Last();
            var statsModeBuild = lastResultModeBuild["stat"].AsDataFrame();

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal((double)statsModeBuild["SE"][i], (double)stats["SE"][i]);
            }
        }

        [Fact]
        public void TestCalculateLogistRegForward()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_logistic.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLogistReg analysisLogistRegForward = new(analysisConfiguration)
            {
                Dependent = new(1, "event", false),
                Vars = new() { new(2, "item1", false), new(3, "item2", false), new(4, "item3", false) },
                GroupBy = new() { new(5, "cat", false) },
                CalculateOverall = false,
                Sequence = AnalysisRegression.RegressionSequence.Forward,
            };

            var result = rservice.CalculateLogistReg(analysisLogistRegForward);
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();
            Assert.Equal(3, stats.RowCount);
            Assert.Single(stats["var"].Where(var => (string)var == "item1"));
            Assert.Empty(stats["var"].Where(var => (string)var == "item2"));
            Assert.Empty(stats["var"].Where(var => (string)var == "item3"));
            Assert.True(Math.Abs((double)stats["est"][2] - 0.2721460) < 0.0001);
            var lastResult = result.Last();
            stats = lastResult["stat"].AsDataFrame();
            Assert.Equal(5, stats.RowCount);
            Assert.Single(stats["var"].Where(var => (string)var == "item1"));
            Assert.Single(stats["var"].Where(var => (string)var == "item2"));
            Assert.Single(stats["var"].Where(var => (string)var == "item3"));
            Assert.True(Math.Abs((double)stats["est"][4] - 0.8968274) < 0.0001);
        }

        [Fact]
        public void TestGetDatasetVariables()
        {
            Rservice rservice = new(new());
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
            Rservice rservice = new(new());
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

            var filenamePvs = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(filenamePvs));

            var pvValueLabels = rservice.GetValueLabels("ibm");
            Assert.NotNull(pvValueLabels);
            Assert.Equal(3, pvValueLabels.RowCount);
            Assert.Equal("surpassed", pvValueLabels["label"].AsCharacter()[pvValueLabels["value"].AsInteger().ToList().IndexOf(3)]);
        }

        [Fact]
        public void TestIdVariableRepairsMIdamage()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisCorr analysisCorr = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false), new(3, "item3", false) },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = false,
            };

            var result = rservice.CalculateCorr(analysisCorr);

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem_resorted.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            var resultCorrupt = rservice.CalculateCorr(analysisCorr);
            Assert.NotEqual((double)result![0]["stat.cor"].AsDataFrame()["cor_SE"][1], (double)resultCorrupt![0]["stat.cor"].AsDataFrame()["cor_SE"][1]);

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName, null));
            Assert.True(rservice.SortRawDataStored("id"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            var resultCorrect = rservice.CalculateCorr(analysisCorr);
            Assert.Equal((double)result![0]["stat.cor"].AsDataFrame()["cor_SE"][1], (double)resultCorrect![0]["stat.cor"].AsDataFrame()["cor_SE"][1]);
        }

        [Fact]
        public void TestAnalysisResultIsSameForMultipleDataFormats()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = new() { new() { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true } },
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA", false) },
                GroupBy = new() { new(2, "ITSEX", false) },
                CalculateSeparately = true,
            };

            var resultSav = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(resultSav);
            Assert.Single(resultSav);
            var dataFrameSav = resultSav.Last()["stat.dstat"].AsDataFrame();

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.rds");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            var resultRds = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(resultRds);
            Assert.Single(resultRds);
            var dataFrameRds = resultRds.Last()["stat.dstat"].AsDataFrame();

            var valuesRds = dataFrameRds["d"].AsNumeric();
            for (int vv = 0; vv < valuesRds.Length; vv++)
            {
                Assert.Equal(valuesRds[vv], dataFrameSav["d"].AsNumeric()[vv]);
            }

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.csv");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName, "csv2"));

            var resultCsv2 = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(resultCsv2);
            Assert.Single(resultCsv2);
            var dataFrameCsv2 = resultCsv2.Last()["stat.dstat"].AsDataFrame();

            var valuesCsv2 = dataFrameCsv2["d"].AsNumeric();
            for (int vv = 0; vv < valuesCsv2.Length; vv++)
            {
                Assert.Equal(valuesCsv2[vv], dataFrameSav["d"].AsNumeric()[vv]);
            }

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.xlsx");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            var resultXlsx = rservice.CalculateMeanDiff(analysisMeanDiff);
            Assert.NotNull(resultXlsx);
            Assert.Single(resultXlsx);
            var dataFrameXlsx = resultXlsx.Last()["stat.dstat"].AsDataFrame();

            var valuesXlsx = dataFrameXlsx["d"].AsNumeric();
            for (int vv = 0; vv < valuesXlsx.Length; vv++)
            {
                Assert.Equal(valuesXlsx[vv], dataFrameSav["d"].AsNumeric()[vv]);
            }
        }

        [Fact]
        public void TestOptionalPVs()
        {
            AnalysisConfiguration analysisConfigurationAll = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = true } },
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };
            AnalysisConfiguration analysisConfigurationSome = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "yoptional", Mandatory = false } },
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            var fileFullPVs = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");
            var fileMissingPVs = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5_missing_pvs.sav");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileFullPVs));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationAll)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = true } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.Contains("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileFullPVs));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "yoptional", Mandatory = false } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.Contains("yoptional", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationSome)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileMissingPVs));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = true } }, "repwgt", 0.5));
            Assert.False(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationAll)));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileMissingPVs));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationSome)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));

            analysisConfigurationSome.ModeKeep = false;
            analysisConfigurationSome.DatasetType.PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = false }, new() { Regex = "y", DisplayName = "y", Mandatory = false } };
            Assert.DoesNotContain("(y)", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll)!.Select(var => var.Name));
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
