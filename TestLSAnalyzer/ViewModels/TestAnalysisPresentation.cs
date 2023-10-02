using ClosedXML.Excel;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
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
        public void TestSetAnalysisResultUnivar()
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

            Rservice rservice = new(new Logging());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            AnalysisUnivar analysisUnivar = new(analysisConfiguration)
            {
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            analysisUnivar.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculateUnivar(analysisUnivar);
            
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat (label)"));
            Assert.Equal(2, analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'").Length);
        }

        [Fact]
        public void TestSetAnalysisResultUnivarWithOverallValues()
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
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            var result = rservice.CalculateUnivar(analysisUnivar);

            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(6, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
        }

        [Fact]
        public void TestSetAnalysisResultUnivarWithTableAverage()
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
                Vars = new() { new(1, "x", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            var result = rservice.CalculateUnivar(analysisUnivar);

            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.HasTableAverage);
            Assert.True(Math.Abs(analysisPresentationViewModel.DataTable.AsEnumerable().Where(row => row.Field<string>("variable") == "- TABLE AVERAGE:").Select(row => row.Field<double>("mean")).First() - 44.8519190) < 0.0001);
            Assert.True(Math.Abs(analysisPresentationViewModel.DataTable.AsEnumerable().Where(row => row.Field<string>("variable") == "- TABLE AVERAGE:").Select(row => row.Field<double>("mean - standard error")).First()- 9.9634143) < 0.0001);

            analysisPresentationViewModel.UseTableAverage = false;
            Assert.Equal(3, analysisPresentationViewModel.DataView.Count);
        }

        [Fact]
        public void TestSetAnalysisResultMeanDiff()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvars = "ASRREA;ASRLIT",
                    Nrep = 150,
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
                Vars = new() { new(1, "ASRREA", false), new(2, "ASRLIT", false) },
                GroupBy = new() { new(3, "ITSEX", false), new(3, "ASBG05C", false) },
                CalculateSeparately = false,
            };

            analysisMeanDiff.ValueLabels.Add("ITSEX", rservice.GetValueLabels("ITSEX")!);
            analysisMeanDiff.ValueLabels.Add("ASBG05C", rservice.GetValueLabels("ASBG05C")!);
            var result = rservice.CalculateMeanDiff(analysisMeanDiff);

            AnalysisPresentation analysisPresentationViewModel = new(analysisMeanDiff);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(12, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("group A - ITSEX"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("group B - ASBG05C (label)"));
            Assert.Equal(8, analysisPresentationViewModel.DataTable.Select("[group A - ITSEX (label)] = 'Girl'").Length);
            Assert.Equal(10, analysisPresentationViewModel.DataTable.Select("[group B - ASBG05C] = 2").Length);

            Assert.NotNull(analysisPresentationViewModel.TableEta);
            Assert.Equal(2, analysisPresentationViewModel.TableEta.Rows.Count);
            Assert.False(analysisPresentationViewModel.TableEta.Columns.Contains("groups by"));
            Assert.True(Math.Abs(analysisPresentationViewModel.TableEta.AsEnumerable().Where(row => row.Field<string>("variable") == "ASRREA").Select(row => row.Field<double>("eta")).First() - 0.1770158) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultMeanDiffSeparately()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav"),
                DatasetType = new()
                {
                    Weight = "TOTWGT",
                    NMI = 5,
                    PVvars = "ASRREA;ASRLIT",
                    Nrep = 150,
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
                Vars = new() { new(1, "ASRREA", false), new(2, "ASRLIT", false) },
                GroupBy = new() { new(3, "ITSEX", false), new(3, "ASBG05C", false) },
                CalculateSeparately = true,
            };

            analysisMeanDiff.ValueLabels.Add("ITSEX", rservice.GetValueLabels("ITSEX")!);
            analysisMeanDiff.ValueLabels.Add("ASBG05C", rservice.GetValueLabels("ASBG05C")!);
            var result = rservice.CalculateMeanDiff(analysisMeanDiff);

            AnalysisPresentation analysisPresentationViewModel = new(analysisMeanDiff);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("groups by"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("group A - value"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("group B - label"));
            Assert.Equal(2, analysisPresentationViewModel.DataTable.Select("[groups by] = 'ITSEX'").Length);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Select("[group B - value] = 2").Length);


            Assert.NotNull(analysisPresentationViewModel.TableEta);
            Assert.Equal(4, analysisPresentationViewModel.TableEta.Rows.Count);
            Assert.True(analysisPresentationViewModel.TableEta.Columns.Contains("groups by"));
            Assert.Equal(2, analysisPresentationViewModel.TableEta.Select("[groups by] = 'ITSEX'").Length);
            Assert.True(Math.Abs(analysisPresentationViewModel.TableEta.AsEnumerable().Where(row => row.Field<string>("variable") == "ASRLIT" && row.Field<string>("groups by") == "ITSEX").Select(row => row.Field<double>("eta - standard error")).First() - 0.01758326) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultFreq()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "instable", false) },
                CalculateOverall = false,
            };

            analysisFreq.ValueLabels.Add("instable", rservice.GetValueLabels("instable")!);
            analysisFreq.BivariateResult = rservice.CalculateBivariate(analysisFreq);
            var result = rservice.CalculateFreq(analysisFreq);

            AnalysisPresentation analysisPresentationViewModel = new(analysisFreq);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("instable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("instable (label)"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("Cat 1"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("Cat 5 - standard error"));
            Assert.Equal(2, analysisPresentationViewModel.DataTable.Select("[instable (label)] = 'Kategorie B'").Length);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("instable = 1")[0]["Cat 1"] - 0.2971394) < 0.0001);

            Assert.NotNull(analysisPresentationViewModel.TableBivariate);
            Assert.Equal(2 * 9, analysisPresentationViewModel.TableBivariate.Rows.Count);
            Assert.Equal(6, analysisPresentationViewModel.TableBivariate.Columns.Count);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.TableBivariate.Select("Y = 'item1' and coefficient = 'w'")[0]["estimate"] - 0.395625) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultFreqWithOverallValues()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisFreq analysisFreq = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(1, "item2", false) },
                GroupBy = new() { new(3, "instable", false) },
                CalculateOverall = true,
            };

            analysisFreq.ValueLabels.Add("instable", rservice.GetValueLabels("instable")!);
            var result = rservice.CalculateFreq(analysisFreq);

            AnalysisPresentation analysisPresentationViewModel = new(analysisFreq);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(6, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("variable = 'item1'")[0]["N - weighted"] - 10.0) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultPercentilesWithoutSE()
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
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Percentiles = new() { 0.25, 0.50, 0.75 },
                CalculateSE = false,
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            analysisPercentiles.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculatePercentiles(analysisPercentiles);

            AnalysisPresentation analysisPresentationViewModel = new(analysisPercentiles);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(6, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.Equal(6, analysisPresentationViewModel.DataTable.Columns.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat (label)"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("Perc 0.25"));
            Assert.Equal(2, analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'").Length);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("cat = 1")[0]["Perc 0.75"] - 74.713136) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultPercentilesWithSE()
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
            Assert.True(rservice.InjectAppFunctions());
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            AnalysisPercentiles analysisPercentiles = new(analysisConfiguration)
            {
                Percentiles = new() { 0.25, 0.50, 0.75 },
                CalculateSE = true,
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = true,
            };

            analysisPercentiles.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculatePercentiles(analysisPercentiles);

            AnalysisPresentation analysisPresentationViewModel = new(analysisPercentiles);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(6, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.Equal(9, analysisPresentationViewModel.DataTable.Columns.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat (label)"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("Perc 0.25"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("Perc 0.25 - standard error"));
            Assert.Equal(2, analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'").Length);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("cat = 1")[0]["Perc 0.75"] - 74.713136) < 0.0001);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("cat = 1")[0]["Perc 0.75 - standard error"] - 51.641681) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultCorr()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisCorr analysisCorr = new(analysisConfiguration)
            {
                Vars = new() { new(1, "item1", false), new(2, "item2", false), new(3, "item3", false), },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = true,
            };

            analysisCorr.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculateCorr(analysisCorr);

            AnalysisPresentation analysisPresentationViewModel = new(analysisCorr);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.NotNull(analysisPresentationViewModel.TableCov);
            Assert.Equal(9, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.Equal(18, analysisPresentationViewModel.TableCov.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable A"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat (label)"));
            Assert.Equal(3, analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'").Length);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.TableCov.Select("[cat (label)] = 'Kategorie B'")[0]["covariance"] - 1.441647597) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultLinregAllIn()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisLinreg analysisLinreg = new(analysisConfiguration)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false), },
                GroupBy = new() { new(4, "cat", false) },
                CalculateOverall = true,
            };

            analysisLinreg.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculateLinreg(analysisLinreg);

            AnalysisPresentation analysisPresentationViewModel = new(analysisLinreg);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(21, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("coefficient"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("cat (label)"));
            Assert.Equal(7, analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'").Length);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("[cat (label)] = 'Kategorie B'")[0]["estimate"] - 2.66666667) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultLinregForward()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisLinreg analysisLinreg = new(analysisConfiguration)
            {
                Dependent = new(1, "item1", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false), },
                CalculateOverall = true,
                Sequence = AnalysisRegression.RegressionSequence.Forward
            };

            analysisLinreg.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            var result = rservice.CalculateLinreg(analysisLinreg);

            AnalysisPresentation analysisPresentationViewModel = new(analysisLinreg);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(7, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.Equal(12, analysisPresentationViewModel.DataTable.Columns.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("coefficient"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("model 2 - FMI"));
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("[coefficient] = 'R^2'")[0]["model 1 - estimate"] - 0.1771340) < 0.0001);
        }

        [Fact]
        public void TestSetAnalysisResultLogistRegAllIn()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_logistic.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 1, null, null));

            AnalysisLogistReg analysisLogistReg = new(analysisConfiguration)
            {
                Dependent = new(1, "event", false),
                Vars = new() { new(2, "item2", false), new(3, "item3", false), },
            };

            var result = rservice.CalculateLogistReg(analysisLogistReg);

            AnalysisPresentation analysisPresentationViewModel = new(analysisLogistReg);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            Assert.NotNull(analysisPresentationViewModel.DataTable);
            Assert.Equal(4, analysisPresentationViewModel.DataTable.Rows.Count);
            Assert.Equal(9, analysisPresentationViewModel.DataTable.Columns.Count);
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("coefficient"));
            Assert.True(analysisPresentationViewModel.DataTable.Columns.Contains("variable"));
            Assert.Equal("", (string)analysisPresentationViewModel.DataTable.Select("coefficient = 'R2'")[0]["variable"]);
            Assert.True(Math.Abs((double)analysisPresentationViewModel.DataTable.Select("coefficient = 'R2'")[0]["estimate"] - 0.244793) < 0.0001);
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
            analysisPresentationViewModel.DataView = new(analysisPresentationViewModel.DataTable);

            var filename = Path.Combine(Path.GetTempPath(), "TestSaveDataTableXlsx.xlsx");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            analysisPresentationViewModel.SaveDataTableXlsxCommand.Execute(filename);

            Assert.True(File.Exists(filename));
        }

        [Fact]
        public void TestSaveFullDataTableXlsx()
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
                Vars = new() { new(1, "x", false), new(1, "y", false) },
                GroupBy = new() { new(3, "cat", false) },
                CalculateOverall = false,
            };

            analysisUnivar.ValueLabels.Add("cat", rservice.GetValueLabels("cat")!);
            DateTime before = DateTime.Now;
            var result = rservice.CalculateUnivar(analysisUnivar);
            DateTime after = DateTime.Now;
            analysisUnivar.ResultAt = after;
            analysisUnivar.ResultDuration = (after - before).TotalSeconds;
            
            AnalysisPresentation analysisPresentationViewModel = new(analysisUnivar);
            analysisPresentationViewModel.SetAnalysisResult(result!);

            analysisPresentationViewModel.ShowPValues = true;
            analysisPresentationViewModel.ShowFMI = true;

            var filename = Path.Combine(Path.GetTempPath(), "TestSaveFullDataTableXlsx.xlsx");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            analysisPresentationViewModel.SaveDataTableXlsxCommand.Execute(filename);

            Assert.True(File.Exists(filename));

            using XLWorkbook wb = new(filename);

            Assert.Equal(13, wb.Worksheets.First().ColumnsUsed().Count());
            Assert.Equal(2, wb.Worksheets.Count);
            Assert.Equal("Univariate", (string)wb.Worksheet("Meta").Cell("B1").Value);
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
