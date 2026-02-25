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
        
        VirtualVariables viewModel = new(configuration);
        
        Assert.Empty(viewModel.CurrentVirtualVariables);

        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType() };
        
        Assert.Single(viewModel.CurrentVirtualVariables);
        
        viewModel.AnalysisConfiguration = new AnalysisConfiguration { FileName = @"C:\path\to\other_file.csv", DatasetType = new DatasetType { Id = 12 }};
        
        Assert.Equal(2, viewModel.CurrentVirtualVariables.Count);
        
        viewModel.AnalysisConfiguration = new AnalysisConfiguration();
        
        Assert.Empty(viewModel.CurrentVirtualVariables);
    }

    [Fact]
    public void TestNewVirtualVariableCommand()
    {
        VirtualVariables viewModel = new();
        
        Assert.Null(viewModel.SelectedVirtualVariable);
        
        viewModel.NewVirtualVariableCommand.Execute(null);
        
        Assert.NotNull(viewModel.SelectedVirtualVariable);
        Assert.Equal(typeof(VirtualVariableCombine), viewModel.SelectedVirtualVariable.GetType());
    }
}