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
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Services.Stubs;

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
        public void TestExecute()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.Execute("x <- 2 * 2"));
            Assert.False(rservice.Execute("x <- abcdefg[2, 'hij']"));
        }

        [Fact]
        public void TestFetch()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.Execute("x <- 2 * 2"));

            Assert.NotNull(rservice.Fetch("x"));
            Assert.Null(rservice.Fetch("notexistenttestvariable"));
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
        public void TestInstallAndCheckOptionalRPackages()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.False(rservice.CheckNecessaryRPackages("sinnlos123"));
            Assert.True(rservice.InstallNecessaryRPackages("jsonlite"));
            Assert.True(rservice.CheckNecessaryRPackages("jsonlite"));
        }

        [Fact]
        public void TestInjectAppFunctions()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.InjectAppFunctions());
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
        public void TestReplaceCharacterVariables()
        {
            Rservice rservice = new();
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
            Rservice rservice = new();
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
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));

            Assert.False(rservice.ApplySubsetting("xyz == 2"));
            Assert.True(rservice.ApplySubsetting("mi == 2"));
        }

        [Fact]
        public void TestReduceToNecessaryVariables()
        {
            Rservice rservice = new();
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
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfiguration) { Vars = new() { new(1, "x") } }, new() { "y" }, "cat == 2"));
        }

        [Fact]
        public void TestCreateReplicateWeights()
        {
            Rservice rservice = new();
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
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.True(rservice.ApplySubsetting("subset == 1"));

            Assert.True(rservice.CreateReplicateWeights("wgt", "jkzone", "jkrep", false));
        }

        [Fact]
        public void TestCreateBIFIEdataObject()
        {
            Rservice rservice = new();
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 0.5));

            var variablesList = rservice.GetCurrentDatasetVariables(analysisConfiguration, []);
            Assert.NotNull(variablesList);
            Assert.True(variablesList.Count == 11);
            Assert.True(variablesList.Where(var => var.Name == "repwgt1").First().IsSystemVariable);
            Assert.Equal("my categories", variablesList.Where(var => var.Name == "cat").First().Label);
            Assert.True(variablesList.All(variable => !variable.FromPlausibleValues));

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
            var variablesListModeBuild = rservice.GetCurrentDatasetVariables(analysisConfigurationModeBuild, []);

            Assert.NotNull(variablesListModeBuild);
            Assert.True(variablesListModeBuild.Count == 20);
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "x").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "y").ToList());
            Assert.Single(variablesListModeBuild.Where(var => var.Name == "one").ToList());
            Assert.Equal("PV Mathematics 1", variablesListModeBuild.Where(var => var.Name == "y").First().Label);
            Assert.Contains(variablesListModeBuild, variable => variable.FromPlausibleValues);
            Assert.True(variablesListModeBuild.Where(var => var.Name == "y").First().FromPlausibleValues);

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
            var variablesListKeepPV = rservice.GetCurrentDatasetVariables(analysisConfigurationKeepPV, []);

            Assert.NotNull(variablesListKeepPV);
            Assert.True(variablesListKeepPV.Count == 11);
            Assert.Single(variablesListKeepPV.Where(var => var.Name == "y").ToList());
            Assert.Equal("PV Mathematics 1", variablesListKeepPV.Where(var => var.Name == "y").First().Label);
            Assert.Contains(variablesListKeepPV, variable => variable.FromPlausibleValues);
            Assert.True(variablesListKeepPV.Where(var => var.Name == "y").First().FromPlausibleValues);

            var rawVariablesListKeepPV = rservice.GetCurrentDatasetVariables(analysisConfigurationKeepPV, [], true);

            Assert.NotNull(rawVariablesListKeepPV);
            Assert.True(rawVariablesListKeepPV.Count == 37);
            Assert.Empty(rawVariablesListKeepPV.Where(var => var.Name == "y").ToList());
            Assert.Single(rawVariablesListKeepPV.Where(var => var.Name == "y1").ToList());
            Assert.True(rawVariablesListKeepPV.All(variable => !variable.FromPlausibleValues));
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            analysisConfiguration.FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration, []));

            analysisConfiguration.ModeKeep = false;
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration, []));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration, [], "xyz == 1"));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration, [], "cat == 1"));

            analysisConfiguration.DatasetType.JKzone = "mi";
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration, []));

            analysisConfiguration.DatasetType.JKzone = null;
            analysisConfiguration.DatasetType.RepWgts = "Bloedsinn";
            Assert.False(rservice.TestAnalysisConfiguration(analysisConfiguration, []));
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
                Vars = new() { new(1, "x") },
                GroupBy = new() { new(2, "cat") },
            };
            AnalysisLinreg analysisLinregCorrect = new(analysisConfiguration)
            {
                Dependent = new Variable(1, "x"),
                Vars = [new Variable(1, "y")],
                GroupBy = [new Variable(2, "cat")]
            };
            AnalysisLogistReg analysisLogistregIncorrect = new(analysisConfiguration)
            {
                Vars = [new Variable(1, "y")],
                GroupBy = [new Variable(2, "cat")]
            };

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            Assert.True(rservice.PrepareForAnalysis(analysisUnivar));
            Assert.True(rservice.PrepareForAnalysis(analysisLinregCorrect));
            Assert.False(rservice.PrepareForAnalysis(analysisLogistregIncorrect));
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
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
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
                CalculateOverall = true,
            };

            var result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0]["stat"].AsDataFrame().RowCount);
            Assert.Equal(4, result[1]["stat"].AsDataFrame().RowCount);

            analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x") },
                GroupBy = new() { new(3, "cat"), new(3, "mi") },
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
                Vars = new() { new(1, "x") },
                GroupBy = new() { new(3, "cat"), new(3, "mi") },
                CalculateOverall = true,
                CalculateCrosswise = false,
            };

            result = rservice.CalculateUnivar(analysisUnivar);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0]["stat"].AsDataFrame().RowCount);
            Assert.Equal(20, result[1]["stat"].AsDataFrame().RowCount);

            analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA") },
                GroupBy = new() { },
                CalculateSeparately = false,
            };

            Assert.Null(rservice.CalculateMeanDiff(analysisMeanDiff));

            analysisMeanDiff.GroupBy = new() { new(2, "ITSEX"), new(3, "ASBG05C") };

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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA") },
                GroupBy = new() { new(2, "ITSEX"), new(3, "ASBG05C") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(1, "item2") },
                GroupBy = new() { new(3, "cat") },
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
                Vars = new() { new(1, "item1"), new(1, "item2") },
                GroupBy = new() { new(3, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(1, "item2") },
                GroupBy = new() { new(3, "cat"), new(4, "instable") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(1, "item2") },
                GroupBy = new() { new(3, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
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
        public void TestCalculatePercentialesUnorderedWithoutSE()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Vars = [ new(1, "x") ],
                Percentiles = new() { 0.25, 0.50, 0.75, 0.05, 0.95 },
                CalculateSE = false,
            };

            var result = rservice.CalculatePercentiles(analysisPercentiles);
            Assert.NotNull(result);
            Assert.Single(result);
            var firstResult = result.First();
            var stats = firstResult["stat"].AsDataFrame();

            for (int i = 1; i < stats.RowCount; i++)
            {
                Assert.True((stats[i, "quant"] as double?) >= (stats[i - 1, "quant"] as double?));
            }
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x"), new(1, "y") },
                GroupBy = new() { new(3, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisCorr analysisCorr = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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
                Vars = new() { new(1, "item1"), new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            analysisCorrModeBuild.CalculateCrosswise = false;
            
            resultModeBuild = rservice.CalculateCorr(analysisCorrModeBuild);
            Assert.NotNull(resultModeBuild);
            Assert.Equal(2, resultModeBuild.Count);
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinreg = new(analysisConfiguration)
            {
                Dependent = new(1, "item1"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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
                Dependent = new(1, "item1"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinregForward = new(analysisConfiguration)
            {
                Dependent = new(1, "item1"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLinreg analysisLinregBackward = new(analysisConfiguration)
            {
                Dependent = new(1, "item1"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLogistReg analysisLogistReg = new(analysisConfiguration)
            {
                Dependent = new(1, "event"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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
                Dependent = new(1, "event"),
                Vars = new() { new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisLogistReg analysisLogistRegForward = new(analysisConfiguration)
            {
                Dependent = new(1, "event"),
                Vars = new() { new(2, "item1"), new(3, "item2"), new(4, "item3") },
                GroupBy = new() { new(5, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, null, null));

            AnalysisCorr analysisCorr = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1"), new(2, "item2"), new(3, "item3") },
                GroupBy = new() { new(4, "cat") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            AnalysisMeanDiff analysisMeanDiff = new(analysisConfiguration)
            {
                Vars = new() { new(1, "ASRREA") },
                GroupBy = new() { new(2, "ITSEX") },
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

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            var fileFullPVs = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");
            var fileMissingPVs = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5_missing_pvs.sav");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileFullPVs));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationAll)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = true } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.Contains("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileFullPVs));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "yoptional", Mandatory = false } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.Contains("yoptional", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationSome)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileMissingPVs));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = true } }, "repwgt", 0.5));
            Assert.False(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationAll)));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileMissingPVs));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.True(rservice.ReduceToNecessaryVariables(new AnalysisUnivar(analysisConfigurationSome)));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, new Collection<PlausibleValueVariable>() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y", DisplayName = "y", Mandatory = false } }, "repwgt", 0.5));
            Assert.DoesNotContain("y", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));

            analysisConfigurationSome.ModeKeep = false;
            analysisConfigurationSome.DatasetType.PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = false }, new() { Regex = "y", DisplayName = "y", Mandatory = false } };
            Assert.DoesNotContain("(y)", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
            Assert.Contains("x", rservice.GetCurrentDatasetVariables(analysisConfigurationAll, [])!.Select(var => var.Name));
        }
        
        [Fact(Skip = "Interferes with other tests when not run on its own")]
        public void TestDispose()
        {
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.IsConnected);
            
            rservice.Dispose();
            
            Assert.False(rservice.IsConnected);
        }

        [Fact]
        public void TestCreateVirtualVariableCombine()
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
            
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            VirtualVariableCombine virtualVariable = new() { Name = "ASBR01_mean_rmNA" };
            
            // not possible without at least one input variable
            Assert.False(rservice.CreateVirtualVariable(virtualVariable));
            
            // not possible to overwrite an existing variable
            virtualVariable.Variables = [
                new Variable(1, "ASBG05A"),
                new Variable(2, "ASBG05B"),
                new Variable(3, "ASBG05C"),
            ];
            virtualVariable.Name = "ASBG01";
            
            Assert.False(rservice.CreateVirtualVariable(virtualVariable));
            
            // possible with mean (default) and removeNa (default)
            virtualVariable.Name = "ASBR01_mean_rmNA";
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_mean_rmNA' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_mean_rmNA))"));
            Assert.Equal(14, rservice.Fetch("missingValues").AsInteger().First());
            
            // possible with mean (default) without removeNa
            virtualVariable.RemoveNa = false;
            virtualVariable.Name = "ASBR01_mean";
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_mean' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_mean))"));
            Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
            // possible with label
            virtualVariable.Label = "label for new variable";
            virtualVariable.Name = "ASBR01_label";
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_label' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("hasLabel <- 'label for new variable' == attributes(lsanalyzer_dat_raw_stored)$variable.labels['ASBR01_label']"));
            Assert.True(rservice.Fetch("hasLabel").AsLogical().First());
            
            // possible with sum without removeNa
            virtualVariable.Type = VirtualVariableCombine.CombinationFunction.Sum;
            virtualVariable.Name = "ASBR01_sum";
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_sum' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_sum))"));
            Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
            // possible with factor scores without removeNa
            virtualVariable.Type = VirtualVariableCombine.CombinationFunction.FactorScores;
            virtualVariable.Name = "ASBR01_factor";
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_factor' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_factor))"));
            Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
            // as preview (preview is possible even though virtual variable now exists in raw data)
            Assert.True(rservice.CreateVirtualVariable(virtualVariable, null, true));
            var previewDataSet = rservice.Fetch("lsanalyzer_dat_raw_preview").AsDataFrame();
            Assert.NotNull(previewDataSet);
            Assert.Equal(4, previewDataSet.ColumnCount);
            Assert.Equal(65, previewDataSet["ASBR01_factor"].Count(value => (double)value is double.NaN));
        }

        [Fact]
        public void TestCreateVirtualVariableCombineFromPVs()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = [
                        new PlausibleValueVariable { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true }
                    ],
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };
            
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            VirtualVariableCombine virtualVariable = new()
            {
                Name = "mean_of_subdimensions",
                Variables = [
                    new Variable(1, "ASRLIT") { FromPlausibleValues = true },
                    new Variable(2, "ASRINF") { FromPlausibleValues = true },
                ]
            };
            
            // not possible without passing pv list
            Assert.False(rservice.CreateVirtualVariable(virtualVariable));
            
            // not possible when not actually pvs
            virtualVariable.Variables = [
                new Variable(1, "ASBG05A") { FromPlausibleValues = true },
                new Variable(2, "ASBG05B") { FromPlausibleValues = true },
                new Variable(3, "ASBG05C") { FromPlausibleValues = true },
            ];
            
            Assert.False(rservice.CreateVirtualVariable(virtualVariable, new List<PlausibleValueVariable>(analysisConfiguration.DatasetType.PVvarsList)));
            
            // not possible when pv vars are inconsistent
            virtualVariable.Variables = [
                new Variable(1, "ASRLIT") { FromPlausibleValues = true },
                new Variable(2, "ASRINF") { FromPlausibleValues = true },
            ];
            
            Assert.False(rservice.CreateVirtualVariable(virtualVariable, [
                new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
                new PlausibleValueVariable { Regex = "ASRINF01", DisplayName = "ASRINF", Mandatory = true }
            ]));
            
            // possible
            Assert.True(rservice.CreateVirtualVariable(virtualVariable, new List<PlausibleValueVariable>(analysisConfiguration.DatasetType.PVvarsList)));
            Assert.True(rservice.Execute("newVariables <- grep('mean_of_subdimensions', colnames(lsanalyzer_dat_raw_stored), value = TRUE)"));
            var newVariables = rservice.Fetch("newVariables").AsCharacter().ToList();
            Assert.Equal(5, newVariables.Count);
            Assert.Contains("mean_of_subdimensions_3", newVariables);
            
            // equals single variable transformation
            virtualVariable.Name = "verify";
            virtualVariable.Variables = [
                new Variable(1, "ASRLIT03"),
                new Variable(2, "ASRINF03"),
            ];
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable));
            Assert.True(rservice.Execute("areEqual <- (TRUE == all.equal(lsanalyzer_dat_raw_stored$mean_of_subdimensions_3, lsanalyzer_dat_raw_stored$verify, check.attributes = FALSE))"));
            Assert.True(rservice.Fetch("areEqual").AsLogical().First());
            
            // as preview
            virtualVariable.Name = "preview";
            virtualVariable.Variables = [
                new Variable(1, "ASRLIT") { FromPlausibleValues = true },
                new Variable(2, "ASRINF") { FromPlausibleValues = true },
            ];
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable, [..analysisConfiguration.DatasetType.PVvarsList], true));
            var previewDataSet = rservice.Fetch("lsanalyzer_dat_raw_preview").AsDataFrame();
            Assert.NotNull(previewDataSet);
            Assert.Equal((IEnumerable<string>?)["ASRLIT05", "ASRINF05", "preview_5"], previewDataSet.ColumnNames);
        }

        [Fact]
        public void TestGetPreviewData()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = [
                        new PlausibleValueVariable { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true }
                    ],
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };
            
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            
            var (successNoPreviewData, previewDataNone) = rservice.GetPreviewData();
            Assert.False(successNoPreviewData);
            Assert.Null(previewDataNone);
            
            VirtualVariableCombine virtualVariable = new()
            {
                Variables =
                [
                    new Variable(1, "ASBG05A"),
                    new Variable(2, "ASBG05B"),
                    new Variable(3, "ASBG05C"),
                ],
                RemoveNa = true,
                Name = "ASBG05sum",
            };
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariable, null, true));

            var (success, previewData) = rservice.GetPreviewData();
            Assert.True(success);
            Assert.NotNull(previewData);
            Assert.True(previewData.Rows.Count < 50);
            
            VirtualVariableCombine virtualVariableContinuous = new()
            {
                Variables =
                [
                    new Variable(1, "ASRLIT01"),
                    new Variable(2, "ASRLIT02"),
                    new Variable(3, "ASRLIT03"),
                    new Variable(4, "ASRLIT04"),
                    new Variable(5, "ASRLIT05"),
                ],
                RemoveNa = false,
                Name = "ASRLITnaive",
            };
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariableContinuous, null, true));

            var (successContinuous, previewDataContinuous) = rservice.GetPreviewData();
            Assert.True(successContinuous);
            Assert.NotNull(previewDataContinuous);
            Assert.True(previewDataContinuous.Rows.Count == 50);
        }

        [Fact]
        public void TestTestAnalysisConfigurationHandlesVirtualVariables()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = [
                        new PlausibleValueVariable { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true }
                    ],
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = false,
            };
            
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            var sentMessage = false;
            List<string> failedVirtualVariables = [];
            WeakReferenceMessenger.Default.Register<Rservice.VirtualVariableErrorMessage>(this, (_,m) =>
            {
                sentMessage = true;
                failedVirtualVariables = m.FailedVirtualVariables.Select(x => x.Name).ToList();
            });

            var result = rservice.TestAnalysisConfiguration(
                analysisConfiguration,
                [
                    new VirtualVariableCombine { Name = "ITSEXclone", Label = "cloneITSEX", Type = VirtualVariableCombine.CombinationFunction.Sum, RemoveNa = false, Variables = [ new Variable(1, "ITSEX") ]},
                    new VirtualVariableCombine { Name = "impossible", Variables = [ new Variable(13, "not_there"), new Variable(14, "not_here") ]},
                    new VirtualVariableCombine { Name = "impossible2", Variables = [ new Variable(13, "not_there2"), new Variable(14, "not_here2") ]},
                ],
                "ITSEXclone == 1"
            );
            
            Assert.True(result);
            Assert.True(sentMessage);
            Assert.Equal([ "impossible", "impossible2" ], failedVirtualVariables);
            
            sentMessage = false;
            
            var result2 = rservice.TestAnalysisConfiguration(
                analysisConfiguration,
                [
                    new VirtualVariableCombine { Name = "ITSEXcopy", Label = "copyITSEX", Type = VirtualVariableCombine.CombinationFunction.Sum, RemoveNa = false, Variables = [ new Variable(1, "ITSEX") ]},
                ]
            );
            
            Assert.True(result2);
            Assert.False(sentMessage);
            Assert.True(rservice.Execute("hasCorrectVariables <- 'ITSEXcopy' %in% colnames(lsanalyzer_dat_raw_stored) && !('ITSEXclone' %in% colnames(lsanalyzer_dat_raw_stored))"));
            Assert.True(rservice.Fetch("hasCorrectVariables").AsLogical().First());
            Assert.True(rservice.Execute("hasLabels <- 'copyITSEX' %in% attributes(lsanalyzer_dat_raw_stored)$variable.labels && 'cloneITSEX' %in% attributes(lsanalyzer_dat_raw_stored)$variable.labels"));
            Assert.True(rservice.Fetch("hasLabels").AsLogical().First());
        }

        [Fact]
        public void TestGetCurrentDatasetVariablesMarksVirtualVariables()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvarsList = [
                        new PlausibleValueVariable { Regex = "ASRREA", DisplayName = "ASRREA", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
                        new PlausibleValueVariable { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true }
                    ],
                    FayFac = 0.5,
                    JKzone = "JKZONE",
                    JKrep = "JKREP",
                    JKreverse = true,
                },
                ModeKeep = true,
            };
            
            Rservice rservice = new();
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            List<VirtualVariable> virtualVariables =
            [
                new VirtualVariableCombine
                {
                    Name = "ITSEXclone", Label = "cloneITSEX", Type = VirtualVariableCombine.CombinationFunction.Sum,
                    RemoveNa = false, Variables = [new Variable(1, "ITSEX")]
                },
                new VirtualVariableCombine
                {
                    Name = "ASBG05_combined",
                    Variables =
                    [
                        new Variable(1, "ASBG05A"), new Variable(2, "ASBG05B"), new Variable(3, "ASBG05C"),
                        new Variable(4, "ASBG05D")
                    ]
                },
            ];
            
            rservice.TestAnalysisConfiguration(analysisConfiguration, virtualVariables);

            var result = rservice.GetCurrentDatasetVariables(analysisConfiguration, virtualVariables, false);

            Assert.NotNull(result);
            Assert.Equal([ "ITSEXclone", "ASBG05_combined" ], result.Where(v => v.IsVirtual).Select(v => v.Name).ToList());
        }

        [Fact]
        public void TestVirtualVariablesFromPVsAreHandledCorrectly()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                }
            };
            
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));

            List<VirtualVariable> virtualVariables =
            [
                new VirtualVariableCombine
                    { Name = "combined", Label = "xy", Variables = [ new Variable(1, "x") { FromPlausibleValues = true }, new Variable(2, "y") { FromPlausibleValues = true } ] },
            ];
            
            Assert.True(rservice.CreateVirtualVariable(virtualVariables[0], analysisConfiguration.DatasetType.PVvarsList.ToList()));
            
            // concerning GetCurrentDatasetVariables and PrepareForAnalysis, virtual variables from PV vars are only interesting with ModeBuild, as they will already exist in the BIFIEdata object otherwise anyway 
            analysisConfiguration.ModeKeep = false;
            
            Assert.Contains("combined", rservice.GetCurrentDatasetVariables(analysisConfiguration, virtualVariables)?.Select(v => v.Name).ToList() ?? [ "cannot get list" ]);

            var analysisCorr = new AnalysisCorr(analysisConfiguration)
            {
                Vars = [ new Variable(1, "x"), new Variable(2, "y"), new Variable(3, "combined") ],
                VirtualVariables = virtualVariables,
            };
            Assert.True(rservice.PrepareForAnalysis(analysisCorr));
            
            var result = rservice.CalculateCorr(analysisCorr);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // TestAnalysisConfiguration is relevant in ModeKeep
            analysisConfiguration.ModeKeep = true;
            
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration, virtualVariables));
            
            Assert.Contains("combined", rservice.GetCurrentDatasetVariables(analysisConfiguration, virtualVariables)?.Select(v => v.Name).ToList() ?? [ "cannot get list" ]);
            
            var result2 = rservice.CalculateCorr(analysisCorr);
            Assert.NotNull(result2);
            Assert.NotEmpty(result2);
        }

        [Fact]
        public void TestCreateVirtualVariableScaleLinear()
        {
            Logging logger = new();
            Rservice rservice = new(logger)
            {
                RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, String.Empty),
            };
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            
            // no mi/pv
            VirtualVariableScale scaleNoMiPv = new()
            {
                Name = "scaleNoMiPv",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                Mean = 50,
                Sd = 10,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleNoMiPv));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleNoMiPv' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 1, null, null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleNoMiPv')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 50.0) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleNoMiPv')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 10.0) < 1e-10);
            
            // missings
            Assert.True(rservice.Execute("lsanalyzer_dat_raw_stored[17, 'x'] <- NA"));
            Assert.True(rservice.Execute("lsanalyzer_dat_raw_stored[22, 'x'] <- NA"));
            Assert.True(rservice.Execute("lsanalyzer_dat_raw_stored[48, 'x'] <- NA"));
            Assert.True(rservice.Execute("lsanalyzer_dat_raw_stored[99, 'x'] <- NA"));
            
            VirtualVariableScale scaleWithMissings = new()
            {
                Name = "scaleWithMissings",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                Mean = 500,
                Sd = 100,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleWithMissings));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleWithMissings' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 1, null, null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleWithMissings')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 500.0) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleWithMissings')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 100.0) < 1e-10);
            
            // mi
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            
            VirtualVariableScale scaleMi = new()
            {
                Name = "scaleMi",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                MiVariable = new Variable(3, "mi"),
                Mean = 0,
                Sd = 1,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleMi));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleMi' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleMi')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 0.0) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleMi')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 1.0) < 1e-10);
            
            // pv
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav")));
            
            VirtualVariableScale scalePv = new()
            {
                Name = "scalePv",
                Type = VirtualVariableScale.ScaleType.Linear,
                InputVariable = new(1, "x") { FromPlausibleValues = true },
                WeightVariable = new Variable(2, "wgt"),
                Mean = 127.3,
                Sd = 12.5,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scalePv, [ new PlausibleValueVariable { DisplayName = "x", Regex = "x", Mandatory = true }]));
            Assert.True(rservice.Execute("hasNewVariable <- 'scalePv_7' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, [ new PlausibleValueVariable { DisplayName = "scalePv", Regex = "scalePv", Mandatory = true }], "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scalePv')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 127.3) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scalePv')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 12.5) < 1e-10);
        }
        
        [Fact]
        public void TestCreateVirtualVariableScaleLogarithmic()
        {
            Logging logger = new();
            Rservice rservice = new(logger)
            {
                RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, String.Empty),
            };
            
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            
            // no centering
            VirtualVariableScale scaleNoCentering = new()
            {
                Name = "scaleNoCentering",
                Type = VirtualVariableScale.ScaleType.Logarithmic,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                LogBase = 2.0,
                Center = false,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleNoCentering));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleNoCentering' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 1, null, null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleNoCentering')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 4.94488266445825) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleNoCentering')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 1.44238700614497) < 1e-10);
            
            // centering without mi
            VirtualVariableScale scaleCenteringWithoutMi = new()
            {
                Name = "scaleCenteringWithoutMi",
                Type = VirtualVariableScale.ScaleType.Logarithmic,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                LogBase = 10.0,
                Center = true,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleCenteringWithoutMi));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleCenteringWithoutMi' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 1, null, null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleCenteringWithoutMi')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - -0.161729068234767) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleCenteringWithoutMi')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 0.434201754205607) < 1e-10);
            
            // centering with mi
            VirtualVariableScale scaleCenteringWithMi = new()
            {
                Name = "scaleCenteringWithMi",
                Type = VirtualVariableScale.ScaleType.Logarithmic,
                InputVariable = new(1, "x"),
                WeightVariable = new Variable(2, "wgt"),
                MiVariable = new Variable(3, "mi"),
                LogBase = 2.0,
                Center = true,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scaleCenteringWithMi));
            Assert.True(rservice.Execute("hasNewVariable <- 'scaleCenteringWithMi' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleCenteringWithMi')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - -0.53724825525693) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scaleCenteringWithMi')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 1.51278563166873) < 1e-10);
            
            // on pvs
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_pv10_nrep5.sav")));
            
            VirtualVariableScale scalePv = new()
            {
                Name = "scalePv",
                Type = VirtualVariableScale.ScaleType.Logarithmic,
                InputVariable = new(1, "x") { FromPlausibleValues = true },
                WeightVariable = new Variable(2, "wgt"),
                LogBase = 2.0,
                Center = false,
            };
            
            Assert.True(rservice.CreateVirtualVariable(scalePv, [ new PlausibleValueVariable { DisplayName = "x", Regex = "x", Mandatory = true} ]));
            Assert.True(rservice.Execute("hasNewVariable <- 'scalePv_3' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
            Assert.True(rservice.Execute("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored"));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, null, [ new PlausibleValueVariable { DisplayName = "scalePv", Regex = "scalePv", Mandatory = true} ], "repwgt", 1.0));
            Assert.True(rservice.Execute("newMean <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scalePv')$stat_M$M"));
            Assert.True(Math.Abs(rservice.Fetch("newMean").AsNumeric().First() - 4.94488266445825) < 1e-10);
            Assert.True(rservice.Execute("newSd <- BIFIEsurvey::BIFIE.univar(lsanalyzer_dat_BO, vars = 'scalePv')$stat_SD$SD"));
            Assert.True(Math.Abs(rservice.Fetch("newSd").AsNumeric().First() - 1.51278563166873) < 1e-10);
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
