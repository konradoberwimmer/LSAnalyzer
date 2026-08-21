using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Services.Stubs;
using RDotNet;

namespace TestLSAnalyzer.Services;

[Collection("Sequential")]
public class TestRserviceVirtualVariables
{
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
        Assert.False(rservice.CreateVirtualVariable(virtualVariable, []));
            
        // not possible to overwrite an existing variable
        virtualVariable.Variables = [
            new Variable(1, "ASBG05A"),
            new Variable(2, "ASBG05B"),
            new Variable(3, "ASBG05C"),
        ];
        virtualVariable.Name = "ASBG01";
            
        Assert.False(rservice.CreateVirtualVariable(virtualVariable, []));
            
        // possible with mean (default) and removeNa (default)
        virtualVariable.Name = "ASBR01_mean_rmNA";
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_mean_rmNA' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_mean_rmNA))"));
        Assert.Equal(14, rservice.Fetch("missingValues").AsInteger().First());
            
        // possible with mean (default) without removeNa
        virtualVariable.RemoveNa = false;
        virtualVariable.Name = "ASBR01_mean";
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_mean' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_mean))"));
        Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
        // possible with label
        virtualVariable.Label = "label for new variable";
        virtualVariable.Name = "ASBR01_label";
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_label' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("hasLabel <- 'label for new variable' == attributes(lsanalyzer_dat_raw_stored)$variable.labels['ASBR01_label']"));
        Assert.True(rservice.Fetch("hasLabel").AsLogical().First());
            
        // possible with sum without removeNa
        virtualVariable.Type = VirtualVariableCombine.CombinationFunction.Sum;
        virtualVariable.Name = "ASBR01_sum";
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_sum' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_sum))"));
        Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
        // possible with factor scores without removeNa
        virtualVariable.Type = VirtualVariableCombine.CombinationFunction.FactorScores;
        virtualVariable.Name = "ASBR01_factor";
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'ASBR01_factor' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("missingValues <- sum(is.na(lsanalyzer_dat_raw_stored$ASBR01_factor))"));
        Assert.Equal(65, rservice.Fetch("missingValues").AsInteger().First());
            
        // as preview (preview is possible even though virtual variable now exists in raw data)
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, [], true));
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
        Assert.False(rservice.CreateVirtualVariable(virtualVariable, []));
            
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
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, [], true));

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
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableContinuous, [], true));

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
            
        Assert.True(rservice.CreateVirtualVariable(scaleNoMiPv, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(scaleWithMissings, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(scaleMi, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(scaleNoCentering, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(scaleCenteringWithoutMi, []));
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
            
        Assert.True(rservice.CreateVirtualVariable(scaleCenteringWithMi, []));
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

    [Fact]
    public void TestCreateVirtualVariableRecodeNoPv()
    {
        Logging logger = new();
        Rservice rservice = new(logger)
        {
            RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, String.Empty),
        };
            
        Assert.True(rservice.Connect(), "R must also be available for tests");
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multiitem.sav")));
            
        // recode to else only
        VirtualVariableRecode virtualVariableRecodeElseOnly = new()
        {
            Name = "elseNA",
            Label = "elseNA - Label",
            Else = VirtualVariableRecode.ElseAction.Missing,
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeElseOnly, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'elseNA' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("hasLabel <- 'elseNA - Label' == attributes(lsanalyzer_dat_raw_stored)$variable.labels['elseNA']"));
        Assert.True(rservice.Fetch("hasLabel").AsLogical().First());
        Assert.True(rservice.Execute("allNA <- all(is.na(lsanalyzer_dat_raw_stored$elseNA))"));
        Assert.True(rservice.Fetch("allNA").AsLogical().First());
            
        // recode to copy only
        VirtualVariableRecode virtualVariableRecodeCopyOnly = new()
        {
            Name = "elseCopy",
            Variables = [
                new Variable(1, "item1"),
            ],
            Else = VirtualVariableRecode.ElseAction.Copy,
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeCopyOnly, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'elseCopy' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_stored$elseCopy))"));
        Assert.False(rservice.Fetch("anyNA").AsLogical().First());
            
        // recode from single variable
        VirtualVariableRecode virtualVariableRecodeSingleVariable = new()
        {
            Name = "combine2",
            Variables = [
                new Variable(1, "item1"),
            ],
            Rules = [
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 1 } ], ResultNa = false, ResultValue = 1, Label = "L1"},
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 2 } ], ResultNa = false, ResultValue = 1 },
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 3 } ], ResultNa = false, ResultValue = 1 },
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 3, MaxValue = 4 } ], ResultNa = false, ResultValue = 2, Label = "L1" },
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Missing } ], ResultNa = false, ResultValue = 3 },
            ],
            Else = VirtualVariableRecode.ElseAction.Set,
            ElseValue = 9.0,
            ElseLabel = "N/A",
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeSingleVariable, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'combine2' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("hasRecodeLabel <- 1 == attributes(lsanalyzer_dat_raw_stored$combine2)$value.labels['L1']"));
        Assert.True(rservice.Fetch("hasRecodeLabel").AsLogical().First());
        Assert.True(rservice.Execute("hasElseLabel <- 9 == attributes(lsanalyzer_dat_raw_stored$combine2)$value.labels['N/A']"));
        Assert.True(rservice.Fetch("hasElseLabel").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_stored$combine2))"));
        Assert.False(rservice.Fetch("anyNA").AsLogical().First());
        Assert.True(rservice.Execute("tab <- table(lsanalyzer_dat_raw_stored$combine2)"));
        Assert.Equal([50, 50], rservice.Fetch("tab").AsNumeric().ToArray());
            
        // recode from two variables
        VirtualVariableRecode virtualVariableRecodeMultipleVariables = new()
        {
            Name = "combineMultiple",
            Variables = [
                new Variable(1, "item1"),
                new Variable(2, "item2"),
            ],
            Rules = [
                new VirtualVariableRecode.Rule
                {
                    Criteria = [ 
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtMost, MaxValue = 1 },
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 1 },
                    ], 
                    ResultNa = false, 
                    ResultValue = -1
                },
                new VirtualVariableRecode.Rule
                {
                    Criteria = [ 
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 2 },
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 2 },
                    ], 
                    ResultNa = true, 
                    ResultValue = 17
                },
                new VirtualVariableRecode.Rule
                {
                    Criteria = [ 
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 4 },
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Between, Value = 4, MaxValue = 5},
                    ], 
                    ResultNa = false, 
                    ResultValue = 1
                },
            ],
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeMultipleVariables, []));
        Assert.True(rservice.Execute("hasNewVariable <- 'combineMultiple' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_stored$combineMultiple))"));
        Assert.True(rservice.Fetch("anyNA").AsLogical().First());
        Assert.True(rservice.Execute("tab <- table(lsanalyzer_dat_raw_stored$combineMultiple, useNA = 'ifany')"));
        Assert.Equal([5, 10, 85], rservice.Fetch("tab").AsNumeric().ToArray());
    }

    [Fact]
    public void TestCreateVirtualVariableRecodeOnPvs()
    {
        Logging logger = new();
        Rservice rservice = new(logger)
        {
            RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, string.Empty),
        };
            
        Assert.True(rservice.Connect(), "R must also be available for tests");
        Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav")));
            
        // just from a PV
        VirtualVariableRecode virtualVariableRecodeOnePv = new()
        {
            Name = "topPerformer",
            Variables = [
                new Variable(1, "ASRREA") { FromPlausibleValues = true },
            ],
            Rules = [
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 0, MaxValue = 600 } ], ResultNa = false, ResultValue = 0 },
                new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 600, MaxValue = 1000 } ], ResultNa = false, ResultValue = 1 },
            ],
        };
            
        Assert.False(rservice.CreateVirtualVariable(virtualVariableRecodeOnePv, []));
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeOnePv, [ new PlausibleValueVariable { DisplayName = "ASRREA", Regex = "ASRREA", Mandatory = true }], true));
        Assert.True(rservice.Execute("hasNewVariable <- 'topPerformer_5' %in% colnames(lsanalyzer_dat_raw_preview)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_preview$topPerformer_5))"));
        Assert.False(rservice.Fetch("anyNA").AsLogical().First());
        Assert.True(rservice.Execute("tabLength <- length(table(lsanalyzer_dat_raw_preview$topPerformer_5))"));
        Assert.Equal(2, rservice.Fetch("tabLength").AsNumeric().First());
            
        // from two PVs
        VirtualVariableRecode virtualVariableRecodeTwoPvs = new()
        {
            Name = "constantUnderPerformer",
            Variables = [
                new Variable(1, "ASRREA") { FromPlausibleValues = true },
                new Variable(2, "ASRINF") { FromPlausibleValues = true },
            ],
            Rules = [
                new VirtualVariableRecode.Rule
                {
                    Criteria = [ 
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 0, MaxValue = 400 },
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Between, Value = 0, MaxValue = 400 },
                    ], 
                    ResultNa = false, 
                    ResultValue = 1
                },
                new VirtualVariableRecode.Rule
                {
                    Criteria = [
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 400, MaxValue = 1000 }, 
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.AtLeast, Value = 400 }, 
                    ], 
                    ResultNa = false, 
                    ResultValue = 0
                },
            ],
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeTwoPvs, [ new PlausibleValueVariable { DisplayName = "ASRREA", Regex = "ASRREA", Mandatory = true }, new PlausibleValueVariable { DisplayName = "ASRINF", Regex = "ASRINF", Mandatory = true } ]));
        Assert.True(rservice.Execute("hasNewVariable <- 'constantUnderPerformer_2' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_stored$constantUnderPerformer_2))"));
        Assert.True(rservice.Fetch("anyNA").AsLogical().First());
        Assert.True(rservice.Execute("tabLength <- length(table(lsanalyzer_dat_raw_stored$constantUnderPerformer_2))"));
        Assert.Equal(2, rservice.Fetch("tabLength").AsNumeric().First());
            
        // from a PV and one other
        VirtualVariableRecode virtualVariableRecodeMix = new()
        {
            Name = "overperformerMales",
            Variables = [
                new Variable(1, "ASRREA") { FromPlausibleValues = true },
                new Variable(2, "ITSEX"),
            ],
            Rules = [
                new VirtualVariableRecode.Rule
                {
                    Criteria = [ 
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 0, MaxValue = 1000 },
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Missing },
                    ], 
                    ResultNa = false, 
                    ResultValue = 0
                },
                new VirtualVariableRecode.Rule
                {
                    Criteria = [
                        new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.Between, Value = 600, MaxValue = 1000 }, 
                        new VirtualVariableRecode.Term { VariableIndex = 1, Type = VirtualVariableRecode.Term.TermType.Exactly, Value = 1 }, 
                    ], 
                    ResultNa = false, 
                    ResultValue = 1
                },
            ],
            Else = VirtualVariableRecode.ElseAction.Set,
            ElseValue = 0,
        };
            
        Assert.True(rservice.CreateVirtualVariable(virtualVariableRecodeMix, [ new PlausibleValueVariable { DisplayName = "ASRREA", Regex = "ASRREA", Mandatory = true } ]));
        Assert.True(rservice.Execute("hasNewVariable <- 'overperformerMales_1' %in% colnames(lsanalyzer_dat_raw_stored)"));
        Assert.True(rservice.Fetch("hasNewVariable").AsLogical().First());
        Assert.True(rservice.Execute("anyNA <- any(is.na(lsanalyzer_dat_raw_stored$overperformerMales_1))"));
        Assert.False(rservice.Fetch("anyNA").AsLogical().First());
        Assert.True(rservice.Execute("tabLength <- length(table(lsanalyzer_dat_raw_stored$overperformerMales_1))"));
        Assert.Equal(2, rservice.Fetch("tabLength").AsNumeric().First());
    }
    
    public static IEnumerable<object[]> TestCreateVirtualVariableComputeNoPvData => [
        [ "", false, 0.0 ],
        [ "ITSEX -", false, 0.0 ],
        [ "ITSEX", true, 1.516055 ],
        [ "13.7", true, 13.7 ],
        [ "-0.005", true, -0.005 ],
        [ "-ITSEX", true, -1.516055 ],
        [ "2 - ITSEX", true, 0.483945 ],
        [ "200 - ITSEX * 100", true, 48.3945 ],
        [ "(2 - ITSEX) * 100", true, 48.3945 ],
        [ "(ASBG05A + ASBG05B + ASBG05C) / 3.0", true, 1.10811 ],
        [ "(-4.3 + 13.3) / (2.0 - -1.0)", true, 3],
        [ "(-4.3+13.3)/(2.0--1.0)", true, 3],
    ];
    
    [Theory, MemberData(nameof(TestCreateVirtualVariableComputeNoPvData))]
    public void TestCreateVirtualVariableComputeNoPv(string text, bool computed, double mean)
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

        VirtualVariableCompute virtualVariable = new() { Name = "myComputation", Expression = text };

        Assert.Equal(computed, rservice.CreateVirtualVariable(virtualVariable, []));

        if (computed)
        {
            Assert.True(rservice.Execute("hasComputedVariable <- 'myComputation' %in% colnames(lsanalyzer_dat_raw_stored)"));
            Assert.True(rservice.Fetch("hasComputedVariable").AsLogical().First());
            Assert.True(rservice.Execute("computedMean <- mean(lsanalyzer_dat_raw_stored$myComputation, na.rm = TRUE)"));
            var computedMean = rservice.Fetch("computedMean").AsNumeric().First();
            Assert.True(Math.Abs(computedMean - mean) < 0.00001, $"Mean was not {mean}, but {computedMean}");
        }
    }

    [Fact]
    public void TestReplaceVariableNamesListener()
    {
        VirtualVariableComputeLexer lexer = new(new AntlrInputStream("(item11 + item1) / 2.0 + (item12 + item1) / 2.0"));
        CommonTokenStream tokens = new(lexer);
        VirtualVariableComputeParser parser = new(tokens);
        Rservice.ReplaceVariableNamesListener listener = new(tokens) { VariableName = "item1", Replacement = "item1_01" };
        ParseTreeWalker.Default.Walk(listener, parser.expression());
        
        Assert.Equal("(item11+item1_01)/2.0+(item12+item1_01)/2.0", listener.GetReplacedExpression());
    }
    
    [Fact]
    public void TestCreateVirtualVariableComputePv()
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
                    new PlausibleValueVariable { Regex = "ASRINF", DisplayName = "ASRINF", Mandatory = true },
                    new PlausibleValueVariable { Regex = "ASRLIT", DisplayName = "ASRLIT", Mandatory = true },
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

        VirtualVariableCompute virtualVariable = new() { Name = "comb", Expression = "(ASRLIT + ASRINF) / 2.0", PossiblePlausibleValueVariables = [..analysisConfiguration.DatasetType.PVvarsList] };
        Assert.True(virtualVariable.FromPlausibleValues);
        
        Assert.True(rservice.CreateVirtualVariable(virtualVariable, [..analysisConfiguration.DatasetType.PVvarsList]));

        Assert.True(rservice.Execute("hasComputedVariables <- all(c('comb_1', 'comb_2', 'comb_3', 'comb_4', 'comb_5') %in% colnames(lsanalyzer_dat_raw_stored))"));
        Assert.True(rservice.Fetch("hasComputedVariables").AsLogical().First());
        Assert.True(rservice.Execute("computedMean1 <- mean(lsanalyzer_dat_raw_stored$comb_1)"));
        var computedMean1 = rservice.Fetch("computedMean1").AsNumeric().First();
        Assert.True(Math.Abs(computedMean1 - 540.4559) < 0.0001);
        Assert.True(rservice.Execute("computedMean2 <- mean(lsanalyzer_dat_raw_stored$comb_2)"));
        var computedMean2 = rservice.Fetch("computedMean2").AsNumeric().First();
        Assert.True(Math.Abs(computedMean2 - 540.0522) < 0.0001);
        Assert.True(rservice.Execute("computedMean3 <- mean(lsanalyzer_dat_raw_stored$comb_3)"));
        var computedMean3 = rservice.Fetch("computedMean3").AsNumeric().First();
        Assert.True(Math.Abs(computedMean3 - 539.9573) < 0.0001);
        Assert.True(rservice.Execute("computedMean4 <- mean(lsanalyzer_dat_raw_stored$comb_4)"));
        var computedMean4 = rservice.Fetch("computedMean4").AsNumeric().First();
        Assert.True(Math.Abs(computedMean4 - 539.8065) < 0.0001);
        Assert.True(rservice.Execute("computedMean5 <- mean(lsanalyzer_dat_raw_stored$comb_5)"));
        var computedMean5 = rservice.Fetch("computedMean5").AsNumeric().First();
        Assert.True(Math.Abs(computedMean5 - 539.4367) < 0.0001);
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