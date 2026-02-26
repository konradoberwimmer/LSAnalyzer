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
        rservice.Setup(service => service.GetCurrentDatasetVariables(It.IsAny<AnalysisConfiguration>(), false)).Returns(
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
        VirtualVariables viewModel = new()
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
    }
    
    [Fact]
    public void TestSaveSelectedVirtualVariable()
    {
        var configuration = new Mock<Configuration>();
        
        VirtualVariables viewModel = new(configuration.Object, new RserviceStub())
        {
            SelectedVirtualVariable = null
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
        
        viewModel.SaveSelectedVirtualVariableCommand.Execute(null);
        
        configuration.Verify(conf => conf.StoreVirtualVariable(It.IsAny<VirtualVariable>()), Times.Once);
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
    }
}