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
        VirtualVariables viewModel = new()
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
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
        var configuration = new Mock<Configuration>();
        
        VirtualVariables viewModel = new(configuration.Object, new RserviceStub())
        {
            SelectedVirtualVariable = null,
            AvailableVariables = [
                new Variable(1, "existing_variable")
            ],
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
        
        var messageSent = false;
        WeakReferenceMessenger.Default.Register<VirtualVariables.VariableNameNotAvailableMessage>(this, (_,_) => messageSent = true);
        
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

        Assert.True(messageSent);
        Assert.True(viewModel.SelectedVirtualVariable.IsChanged);
        
        messageSent = false;
        
        viewModel.SelectedVirtualVariable.Name = "existing_variable";
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);

        Assert.True(messageSent);
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
        VirtualVariables viewModel = new()
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
        };
        
        // expect no error
        viewModel.RemoveSelectedVirtualVariableCommand.Execute(null);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.NotEmpty(viewModel.CurrentVirtualVariables);
        
        viewModel.RemoveSelectedVirtualVariableCommand.Execute(null);
        
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
            .SetupSequence(service => service.GetPreviewData())
            .Returns((false, null)).Returns((true, new DataTable("preview")));
        
        VirtualVariables viewModel = new(Mock.Of<Configuration>(), rservice.Object)
        {
            SelectedVirtualVariable = null,
            CurrentVirtualVariables = [],
            SelectedVirtualVariableType = typeof(VirtualVariableCombine),
        };
        
        Assert.Equal("Input", viewModel.Preview.Table?.Columns[0].ColumnName);
        Assert.Equal("Output", viewModel.Preview.Table?.Columns[1].ColumnName);

        // expect no error
        viewModel.FetchPreviewDataCommand.Execute(null);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
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
}