using System.Data;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Services.Stubs;
using LSAnalyzer.ViewModels;
using Moq;

namespace TestLSAnalyzer.ViewModels;

public class TestVirtualVariables
{
    [Fact]
    public void TestSettingAnalysisConfigurationLoadsVirtualVariables()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.VirtualVariables).Returns([
            new VirtualVariableCombine { ForFileName = "some_file.csv", ForDatasetTypeId = 12 },
            new VirtualVariableCombine { ForFileName = "other_file.csv", ForDatasetTypeId = null }
        ]);
        
        Configuration configuration = new(String.Empty, null, settingsService.Object, new RegistryServiceStub());

        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), false)).Returns(
        [
            new Variable(1, "x"),
            new Variable(2, "y"),
            new Variable(3, "wgt") { IsSystemVariable = true },
            new Variable(4, "calculates") { IsVirtual = true },
        ]);
        
        VirtualVariables viewModel = new(configuration, rservice.Object);
        
        Assert.Empty(viewModel.CurrentVirtualVariables);

        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType() };
        
        Assert.Single(viewModel.CurrentVirtualVariables);
        
        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType { Id = 12, Name = "myDatasetType" }};
        
        Assert.Equal("other_file.csv", viewModel.CurrentFileName);
        Assert.Equal("myDatasetType", viewModel.CurrentDatasetTypeName);
        Assert.Equal(2, viewModel.CurrentVirtualVariables.Count);
        Assert.Equal(2, viewModel.AvailableVariables.Count);
        Assert.True(viewModel.CurrentVirtualVariables.All(v => !v.IsChanged));
        
        viewModel.AnalysisConfiguration = new AnalysisConfiguration();
        
        Assert.Empty(viewModel.CurrentVirtualVariables);
    }

    [Fact]
    public void TestNewVirtualVariableCommand()
    {
        var rservice = new Mock<IRservice>();
        rservice.Setup(service =>
            service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(),
                false)).Returns([
            new Variable(1, "x"),
            new Variable(2, "mi") { IsSystemVariable = true },
            new Variable(3, "wgt") { IsSystemVariable = true },
        ]);
        
        VirtualVariables viewModel = new(new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryServiceStub()), rservice.Object)
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            AnalysisConfiguration = new AnalysisConfiguration
            {
                DatasetType = new DatasetType()
            },
        };

        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedVirtualVariable);

        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableCombine);
        viewModel.CurrentFileName = "some_file.csv";
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.Equal(typeof(VirtualVariableCombine), viewModel.SelectedVirtualVariable.GetType());
        Assert.Equal("some_file.csv", viewModel.SelectedVirtualVariable.ForFileName);
        Assert.Single(viewModel.CurrentVirtualVariables);
        Assert.True(viewModel.SelectedVirtualVariable.IsChanged);

        viewModel.AnalysisConfiguration = new AnalysisConfiguration
        {
            DatasetType = new DatasetType
            {
                MIvar = "mi",
                Weight = "wgt",
            }
        };
        
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableScale);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.Equal(typeof(VirtualVariableScale), viewModel.SelectedVirtualVariable.GetType());
        var virtualVariableScale = viewModel.SelectedVirtualVariable as VirtualVariableScale;
        Assert.NotNull(virtualVariableScale!.WeightVariable);
        Assert.NotNull(virtualVariableScale!.MiVariable);
    }

    [Fact]
    public void TestHandleAvailableVariables()
    {
        var rservice = new Mock<IRservice>();
        rservice.Setup(service =>
            service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(),
                false)).Returns([
            new Variable(1, "item1"),
            new Variable(1, "item2"),
            new Variable(3, "mi") { IsSystemVariable = true },
            new Variable(4, "wgt") { IsSystemVariable = true },
        ]);
        
        VirtualVariables viewModel = new(new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryServiceStub()), rservice.Object)
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
            AnalysisConfiguration = new AnalysisConfiguration
            {
                DatasetType = new DatasetType()
            },
        };
        
        // expect no error
        viewModel.HandleAvailableVariablesCommand.Execute([]);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        viewModel.HandleAvailableVariablesCommand.Execute([
            new Variable(1, "item1"),
            new Variable(2, "item2"),
        ]);
        
        Assert.Equal(2, (viewModel.SelectedVirtualVariable as VirtualVariableCombine)!.Variables.Count);
        
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableRecode);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        viewModel.HandleAvailableVariablesCommand.Execute([
            new Variable(1, "item1"),
            new Variable(2, "item2"),
        ]);
        
        Assert.Single((viewModel.SelectedVirtualVariable as VirtualVariableRecode)!.Variables);
        Assert.Equal("item1", (viewModel.SelectedVirtualVariable as VirtualVariableRecode)!.Variables.First().Name);
    }
    
    [Fact]
    public void TestSaveSelectedVirtualVariable()
    {
        var rservice = new Mock<IRservice>();
        rservice.Setup(service =>
            service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(),
                false)).Returns([
            new Variable(1, "item1"),
            new Variable(1, "item2"),
            new Variable(3, "mi") { IsSystemVariable = true },
            new Variable(4, "wgt") { IsSystemVariable = true },
        ]);
        
        var configuration = new Mock<Configuration>();
        
        VirtualVariables viewModel = new(configuration.Object, rservice.Object)
        {
            SelectedVirtualVariable = null,
            AvailableVariables = [
                new Variable(1, "existing_variable")
            ],
            AnalysisConfiguration = new AnalysisConfiguration
            {
                DatasetType = new DatasetType
                {
                    PVvarsList = [
                        new PlausibleValueVariable { Regex = "myPV", DisplayName = "myPV" }
                    ], 
                }
            },
        };
        
        // expect no error
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableCombine);
        viewModel.CurrentFileName = "some_file.csv";
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        // expect no error
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.False(viewModel.SelectedVirtualVariable.Validate());
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        
        viewModel.SelectedVirtualVariable.Name = "new_variable";
        (viewModel.SelectedVirtualVariable as VirtualVariableCombine)!.Variables =
        [
            new Variable(1, "item1"),
            new Variable(2, "item2"),
        ];
        Assert.True(viewModel.SelectedVirtualVariable.Validate());
        Assert.False(viewModel.HasChangedVirtualVariables);
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        
        configuration.Verify(conf => conf.StoreVirtualVariable(It.IsAny<VirtualVariable>()), Times.Once);
        Assert.False(viewModel.SelectedVirtualVariable.IsChanged);
        Assert.Single(viewModel.CurrentVirtualVariables);
        Assert.True(viewModel.HasChangedVirtualVariables);
        
        var nameNotAvailableMessageSent = false;
        var nameMatcheRegexMessageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.VariableNameNotAvailableMessage>(this, (_,_) => nameNotAvailableMessageSent = true);
        WeakReferenceMessenger.Default.Register<VirtualVariables.VariableNameMatchesPvRegexMessage>(this, (_,_) => nameMatcheRegexMessageSent = true);
        
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableCombine);
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        viewModel.SelectedVirtualVariable.Name = "new_variable";
        (viewModel.SelectedVirtualVariable as VirtualVariableCombine)!.Variables =
        [
            new Variable(1, "item1"),
            new Variable(2, "item2"),
        ];
        Assert.True(viewModel.SelectedVirtualVariable.Validate());
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);

        Assert.True(nameNotAvailableMessageSent);
        Assert.False(nameMatcheRegexMessageSent);
        Assert.True(viewModel.SelectedVirtualVariable.IsChanged);
        
        nameNotAvailableMessageSent = false;
        
        viewModel.SelectedVirtualVariable.Name = "existing_variable";
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);

        Assert.True(nameNotAvailableMessageSent);
        Assert.False(nameMatcheRegexMessageSent);
        Assert.True(viewModel.SelectedVirtualVariable.IsChanged);
        
        nameNotAvailableMessageSent = false;
        
        viewModel.SelectedVirtualVariable.Name = "zmyPV";
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);

        Assert.False(nameNotAvailableMessageSent);
        Assert.True(nameMatcheRegexMessageSent);
        Assert.True(viewModel.SelectedVirtualVariable.IsChanged);
    }

    [Fact]
    public void TestSaveSelectedVirtualVariableRecode()
    {
        VirtualVariables viewModel = new();

        viewModel.SelectedVirtualVariable = new VirtualVariableRecode();
        viewModel.SelectedVirtualVariable.Name = "new_variable";
        
        viewModel.HandleAvailableVariablesCommand.Execute([new Variable(1, "item1")]);
        viewModel.AddRuleCommand.Execute(null);
        viewModel.RemoveRuleCommand.Execute((viewModel.SelectedVirtualVariable as VirtualVariableRecode)!.Rules.First());
        viewModel.RemoveLastVariableCommand.Execute(null);
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        
        Assert.False(viewModel.SelectedVirtualVariable.IsChanged);
    }

    [Fact]
    public void TestRemoveSelectedVirtualVariable()
    {
        var rservice = new Mock<IRservice>();
        rservice.Setup(service =>
            service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(),
                false)).Returns([
            new Variable(1, "item1"),
            new Variable(1, "item2"),
            new Variable(3, "mi") { IsSystemVariable = true },
            new Variable(4, "wgt") { IsSystemVariable = true },
        ]);
        
        VirtualVariables viewModel = new(new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryServiceStub()), rservice.Object)
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
            AnalysisConfiguration = new AnalysisConfiguration
            {
                DatasetType = new DatasetType()
            },
        };
        
        // expect no error
        viewModel.RemoveSelectedVirtualVariableCommand.Execute([]);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.NotEmpty(viewModel.CurrentVirtualVariables);
        
        viewModel.RemoveSelectedVirtualVariableCommand.Execute([viewModel.SelectedVirtualVariable]);
        
        Assert.Empty(viewModel.CurrentVirtualVariables);
        Assert.Null(viewModel.SelectedVirtualVariable);
        Assert.True(viewModel.HasChangedVirtualVariables);
        
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableCombine);
        viewModel.NewVirtualVariableCommand.Execute(null);
        viewModel.SelectedVirtualVariableType = typeof(VirtualVariableRecode);
        viewModel.NewVirtualVariableCommand.Execute(null);

        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.Equal(2, viewModel.CurrentVirtualVariables.Count);
        
        viewModel.RemoveSelectedVirtualVariableCommand.Execute([..viewModel.CurrentVirtualVariables]);
        
        Assert.Empty(viewModel.CurrentVirtualVariables);
        Assert.Null(viewModel.SelectedVirtualVariable);
        Assert.True(viewModel.HasChangedVirtualVariables);
    }

    [Fact]
    public void TestFetchPreviewData()
    {
        var rservice = new Mock<IRservice>();
        rservice
            .SetupSequence(service => service.CreateVirtualVariable(It.IsAny<VirtualVariable>(), It.IsAny<List<PlausibleValueVariable>>(), It.Is<bool>(b => b == true)))
            .Returns(false).Returns(true).Returns(true);
        rservice
            .SetupSequence(service => service.GetPreviewData(It.IsAny<VirtualVariable>()))
            .Returns((false, null)).Returns((true, new DataTable("preview")));
        
        VirtualVariables viewModel = new(Mock.Of<Configuration>(), rservice.Object)
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
            AnalysisConfiguration = new AnalysisConfiguration
            {
                DatasetType = new DatasetType()
            },
        };
        
        Assert.Equal("Input", viewModel.Preview.Table?.Columns[0].ColumnName);
        Assert.Equal("Output", viewModel.Preview.Table?.Columns[1].ColumnName);

        // expect no error
        viewModel.FetchPreviewDataCommand.Execute(null);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.False(viewModel.SelectedVirtualVariable!.HasErrors);
        viewModel.FetchPreviewDataCommand.Execute(null);
        Assert.True(viewModel.SelectedVirtualVariable!.HasErrors);
        
        viewModel.SelectedVirtualVariable.Name = "new_variable";
        (viewModel.SelectedVirtualVariable as VirtualVariableCombine)!.Variables = [new Variable(1, "x"), new Variable(2, "y")];
        
        var messageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.PreviewImpossibleMessage>(this, (_,_) => messageSent = true);
        
        // 1st run: not possible to calculate
        viewModel.FetchPreviewDataCommand.Execute(null);
        
        Assert.True(messageSent);
        
        messageSent = false;
        
        // 2nd run: not possible to fetch
        viewModel.FetchPreviewDataCommand.Execute(null);
        
        Assert.True(messageSent);
        
        messageSent = false;
        
        // 3rd run: response
        viewModel.FetchPreviewDataCommand.Execute(null);
        
        Assert.False(messageSent);
        
        Assert.Equal("preview", viewModel.Preview.Table?.TableName);
    }
    
    [Fact]
    public void SetIsForDatasetTypeActuallySetsValue()
    {
        VirtualVariables viewModel = new()
        {
            SelectedVirtualVariable = new VirtualVariableCombine(),
            AnalysisConfiguration = new AnalysisConfiguration { DatasetType = new DatasetType { Id = 77 } }
        };
        
        Assert.Null(viewModel.SelectedVirtualVariable.ForDatasetTypeId);
        
        viewModel.SelectedIsForDatasetType = true;
        
        Assert.Equal(77, viewModel.SelectedVirtualVariable.ForDatasetTypeId);
    }

    [Fact]
    public void TestExportVirtualVariables()
    {
        var filename = Path.GetTempFileName();

        VirtualVariables viewModel = new()
        {
            SelectedVirtualVariable = new VirtualVariableCombine
            {
                Type = VirtualVariableCombine.CombinationFunction.Mean,
                Variables = [ new Variable(1, "item1"), new Variable(2, "item2") ],
                RemoveNa = false,
                Name = "combination",
            },
            AnalysisConfiguration = new AnalysisConfiguration { DatasetType = new DatasetType { Id = 77 } }
        };
        viewModel.SelectedIsForDatasetType = true;
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        Assert.False(viewModel.SelectedVirtualVariable.IsChanged);
        
        viewModel.ExportVirtualVariablesCommand.Execute(new VirtualVariables.ExportVirtualVariablesParameters { VirtualVariables = [], FileName = filename });
        Assert.Empty(File.ReadAllLines(filename));
        
        viewModel.ExportVirtualVariablesCommand.Execute(new VirtualVariables.ExportVirtualVariablesParameters { VirtualVariables = [ viewModel.SelectedVirtualVariable ], FileName = filename });
        Assert.NotEmpty(File.ReadAllLines(filename));
    }

    [Fact]
    public void TestImportVirtualVariablesIgnoresWrongOrEmptyFiles()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.VirtualVariables).Returns([
            new VirtualVariableCombine { ForFileName = "some_file.csv", ForDatasetTypeId = 12 },
            new VirtualVariableCombine { ForFileName = "other_file.csv", ForDatasetTypeId = null }
        ]);
        
        Configuration configuration = new(String.Empty, null, settingsService.Object, new RegistryServiceStub());
        
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), false)).Returns(
        [
            new Variable(1, "item1") { Label = "correct label"},
            new Variable(2, "TOTWGT") { IsSystemVariable = true },
        ]);

        VirtualVariables viewModel = new(configuration, rservice.Object);
        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType { Id = 12, Name = "myDatasetType" }};
        
        var wrongFilename = Path.GetTempFileName();
        File.WriteAllText(wrongFilename, "something_not_json");
        
        var invalidFileMessageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.VirtualVariablesFileInvalidMessage>(this, (_, _) => invalidFileMessageSent = true);
        
        viewModel.ImportVirtualVariablesCommand.Execute(wrongFilename);
        Assert.True(invalidFileMessageSent);
        
        var emptyFileName = Path.GetTempFileName();
        File.WriteAllText(emptyFileName, "[]");
        
        invalidFileMessageSent = false;
        
        viewModel.ImportVirtualVariablesCommand.Execute(emptyFileName);
        
        Assert.True(invalidFileMessageSent);
    }

    [Fact]
    public void TestImportVirtualVariablesModifiesRelevantData()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.VirtualVariables).Returns([
            new VirtualVariableCombine { ForFileName = "some_file.csv", ForDatasetTypeId = 12 },
            new VirtualVariableCombine { ForFileName = "other_file.csv", ForDatasetTypeId = null }
        ]);
        
        Configuration configuration = new(String.Empty, null, settingsService.Object, new RegistryServiceStub());
        
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), false)).Returns(
        [
            new Variable(1, "item1") { Label = "correct label"},
            new Variable(2, "TOTWGT") { IsSystemVariable = true },
        ]);

        VirtualVariables viewModel = new(configuration, rservice.Object);
        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType { Id = 12, Name = "myDatasetType" }};
        Assert.Equal(2, viewModel.CurrentVirtualVariables.Count);

        VirtualVariableScale virtualVariableScale = new()
        {
            Id = 2178,
            Name = "myScale",
            InputVariable = new Variable(877, "item1") { Label = "corrupt label" },
            WeightVariable = new Variable(999, "totwgt") { IsSystemVariable = true },
            ForFileName = "old_file.csv",
            ForDatasetTypeId = 99
        };
        
        VirtualVariableScale virtualVariableScaleImpossible = new()
        {
            Id = 2179,
            Name = "impossibleScale",
            InputVariable = new Variable(877, "not_existing_item") { Label = "corrupt label" },
            WeightVariable = new Variable(999, "totwgt") { IsSystemVariable = true },
            ForFileName = "old_file.csv",
            ForDatasetTypeId = 99
        };
        
        List<VirtualVariable> virtualVariables = [virtualVariableScale, virtualVariableScaleImpossible, virtualVariableScale];
        
        var fileName = Path.GetTempFileName();
        viewModel.ExportVirtualVariablesCommand.Execute(new VirtualVariables.ExportVirtualVariablesParameters { VirtualVariables = virtualVariables, FileName = fileName });
        Assert.NotEmpty(File.ReadAllLines(fileName));

        var duplicatedVirtualVariableMessageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.DuplicatedVirtualVariablesAtImportMessage>(this, (_, _) => duplicatedVirtualVariableMessageSent = true);
        
        var ignoredVirtualVariableMessageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.IgnoredVirtualVariablesAtImportMessage>(this, (_, _) => ignoredVirtualVariableMessageSent = true);
        
        viewModel.ImportVirtualVariablesCommand.Execute(fileName);
        Assert.Equal(3, viewModel.CurrentVirtualVariables.Count);
        Assert.True(duplicatedVirtualVariableMessageSent);
        Assert.True(ignoredVirtualVariableMessageSent);
        
        var newVirtualVariable = viewModel.CurrentVirtualVariables.Last() as VirtualVariableScale;
        Assert.NotEqual(2178, newVirtualVariable!.Id);
        Assert.Equal("other_file.csv", newVirtualVariable.ForFileName);
        Assert.Equal(12, newVirtualVariable.ForDatasetTypeId);
        Assert.Equal("correct label", newVirtualVariable.InputVariable!.Label);
        Assert.Equal("TOTWGT", newVirtualVariable.WeightVariable!.Name);
    }
    
    [Fact]
    public void TestImportVirtualVariablesModifiesVariableNamesInExpression()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.VirtualVariables).Returns([]);
        
        Configuration configuration = new(string.Empty, null, settingsService.Object, new RegistryServiceStub());
        
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), It.IsAny<List<VirtualVariable>>(), false)).Returns(
        [
            new Variable(1, "item1"),
            new Variable(2, "item2"),
            new Variable(2, "item3"),
            new Variable(2, "TOTWGT") { IsSystemVariable = true },
        ]);

        VirtualVariables viewModel = new(configuration, rservice.Object);
        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType { Id = 12, Name = "myDatasetType" }};

        VirtualVariableCompute virtualVariableCompute = new()
        {
            Id = 2178,
            Name = "myScale",
            ForFileName = "old_file.csv",
            ForDatasetTypeId = 99,
            Expression = "(ITEM1 + item2 + Item3) / 3.0"
        };
        
        List<VirtualVariable> virtualVariables = [virtualVariableCompute];
        
        var fileName = Path.GetTempFileName();
        viewModel.ExportVirtualVariablesCommand.Execute(new VirtualVariables.ExportVirtualVariablesParameters { VirtualVariables = virtualVariables, FileName = fileName });
        Assert.NotEmpty(File.ReadAllLines(fileName));

        viewModel.ImportVirtualVariablesCommand.Execute(fileName);
        Assert.Single(viewModel.CurrentVirtualVariables);
        
        var newVirtualVariable = viewModel.CurrentVirtualVariables.Last() as VirtualVariableCompute;
        Assert.NotEqual(2178, newVirtualVariable!.Id);
        Assert.Equal("other_file.csv", newVirtualVariable.ForFileName);
        Assert.Equal(12, newVirtualVariable.ForDatasetTypeId);
        Assert.Equal("(item1+item2+item3)/3.0", newVirtualVariable.Expression);
    }
}