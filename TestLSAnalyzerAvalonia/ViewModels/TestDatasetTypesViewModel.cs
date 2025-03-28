using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using Moq;

namespace TestLSAnalyzerAvalonia.ViewModels;

public class TestDatasetTypesViewModel
{
    [Fact]
    public void TestConstructor()
    {
        // failed configuration
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.DatasetTypesConfigFilePath).Returns("/");
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns((List<DatasetType>?)null);
        appConfiguration.Setup(x => x.RestoreDefaultDatasetTypesStorage()).Returns(false);
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches(new Regex("at /!"), viewModel.Message);
        
        // restore default configuration
        appConfiguration.SetupSequence(x => x.GetStoredDatasetTypes()).Returns((List<DatasetType>?)null).Returns(DatasetType.CreateDefaultDatasetTypes());
        appConfiguration.Setup(x => x.RestoreDefaultDatasetTypesStorage()).Returns(true);
        
        viewModel = new(appConfiguration.Object);
        
        Assert.False(viewModel.ShowMessage);
        Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count, viewModel.DatasetTypes.Count);
        
        // existing configuration
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns([]);
        
        viewModel = new(appConfiguration.Object);
        
        Assert.False(viewModel.ShowMessage);
        Assert.Empty(viewModel.DatasetTypes);
    }

    [Fact]
    public void TestUnsavedDatasetTypeNames()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);

        Assert.Empty(viewModel.UnsavedDatasetTypeNames);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        Assert.Single(viewModel.UnsavedDatasetTypeNames);
        Assert.Contains("New dataset type", viewModel.UnsavedDatasetTypeNames);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First();
        viewModel.SelectedDatasetType.Name = "changed";
        
        Assert.Equal(2, viewModel.UnsavedDatasetTypeNames.Count);
    }

    [Fact]
    public void TestNewDatasetTypeCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count + 1, viewModel.DatasetTypes.Count);
        Assert.NotNull(viewModel.SelectedDatasetType);
        Assert.Equal(1, viewModel.SelectedDatasetType.Id);
        Assert.True(viewModel.SelectedDatasetType.IsChanged);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        Assert.Equal(2, viewModel.SelectedDatasetType.Id);
    }

    [Fact]
    public void TestSaveDatasetTypeCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        viewModel.SaveSelectedDatasetTypeCommand.Execute(null);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        viewModel.SaveSelectedDatasetTypeCommand.Execute(null);
        
        Assert.True(viewModel.SelectedDatasetType!.IsChanged);
        
        viewModel.SelectedDatasetType.Weight = "weight";
        viewModel.SelectedDatasetType.NMI = 1;
        
        viewModel.SaveSelectedDatasetTypeCommand.Execute(null);
        
        Assert.False(viewModel.SelectedDatasetType.IsChanged);
    }
    
    [Fact]
    public void TestRemoveDatasetTypeCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        viewModel.RemoveDatasetTypeCommand.Execute(null);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        Assert.Contains(viewModel.DatasetTypes, dst => dst.Id == 1);
        
        viewModel.SelectedDatasetType!.Weight = "weight";
        viewModel.SelectedDatasetType.NMI = 1;
        viewModel.SaveSelectedDatasetTypeCommand.Execute(null);
        
        Assert.False(viewModel.SelectedDatasetType.IsChanged);
        
        viewModel.RemoveDatasetTypeCommand.Execute(null);
        
        Assert.Null(viewModel.SelectedDatasetType);
        Assert.DoesNotContain(viewModel.DatasetTypes, dst => dst.Id == 1);
        Assert.True(viewModel.ShowMessage);
    }

    [Fact]
    public void TestAddPlausibleValuesVariableCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        var exception = Record.Exception(() => { viewModel.AddPlausibleValueVariableCommand.Execute(null); });
        Assert.Null(exception);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First();
        
        var numberOfPvVars = viewModel.SelectedDatasetType.PVvarsList.Count;
        
        viewModel.AddPlausibleValueVariableCommand.Execute(null);
        
        Assert.Equal(numberOfPvVars + 1, viewModel.SelectedDatasetType.PVvarsList.Count);
    }
    
    [Fact]
    public void TestRemovePlausibleValuesVariableCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        var exception = Record.Exception(() => { viewModel.RemovePlausibleValueVariablesCommand.Execute(new PlausibleValueVariable()); });
        Assert.Null(exception);
        
        viewModel.SelectedDatasetType = viewModel.DatasetTypes.First(dst => dst.PVvarsList.Count > 0);
        
        var numberOfPvVars = viewModel.SelectedDatasetType.PVvarsList.Count;
        
        viewModel.RemovePlausibleValueVariablesCommand.Execute(viewModel.SelectedDatasetType.PVvarsList.First());
        
        Assert.Equal(numberOfPvVars - 1, viewModel.SelectedDatasetType.PVvarsList.Count);
    }

    [Fact]
    public void TestExportDatasetTypeCommand()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        var exception = Record.Exception(() => { viewModel.ExportDatasetTypeCommand.Execute(string.Empty); });
        Assert.Null(exception);
        
        viewModel.NewDatasetTypeCommand.Execute(null);
        
        exception = Record.Exception(() => { viewModel.ExportDatasetTypeCommand.Execute(string.Empty); });
        Assert.Null(exception);
        
        viewModel.SelectedDatasetType!.Weight = "weight";
        viewModel.SelectedDatasetType.NMI = 1;

        viewModel.ExportDatasetTypeCommand.Execute(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "test_export_dataset_type.json"));
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Exported New dataset type to", viewModel.Message);
        
        appConfiguration.VerifySet(x => x.LastOutFileLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Times.Once);
    }

    [Fact]
    public void TestImportDatasetTypeCommandInvalidCases()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        viewModel.ImportDatasetTypeCommand.Execute(string.Empty);
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Invalid file", viewModel.Message);
        viewModel.ShowMessage = false;
        
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_null.json"), "null");
        
        viewModel.ImportDatasetTypeCommand.Execute(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_null.json"));
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Invalid file", viewModel.Message);
        viewModel.ShowMessage = false;
        
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_wrong.json"), "{}");
        
        viewModel.ImportDatasetTypeCommand.Execute(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_wrong.json"));
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Invalid dataset type", viewModel.Message);
        viewModel.ShowMessage = false;

        DatasetType newDatasetType = new();
        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_invalid.json"), JsonSerializer.Serialize(newDatasetType, jsonSerializerOptions));
        
        viewModel.ImportDatasetTypeCommand.Execute(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type_invalid.json"));
        
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Invalid dataset type", viewModel.Message);
    }
    
    [Fact]
    public void TestImportDatasetTypeCommandHappyCase()
    {
        var appConfiguration = new Mock<IAppConfiguration>();
        appConfiguration.Setup(x => x.GetStoredDatasetTypes()).Returns(DatasetType.CreateDefaultDatasetTypes());
        
        DatasetTypesViewModel viewModel = new(appConfiguration.Object);
        
        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type.json"), JsonSerializer.Serialize(viewModel.DatasetTypes.First(), jsonSerializerOptions));
        
        viewModel.ImportDatasetTypeCommand.Execute(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type.json"));
        
        appConfiguration.Verify(x => x.StoreDatasetType(It.IsAny<DatasetType>()), Times.Once);
        Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count + 1, viewModel.DatasetTypes.Count);
        Assert.Equal(1, viewModel.SelectedDatasetType!.Id);
        Assert.False(viewModel.SelectedDatasetType.IsChanged);
        appConfiguration.VerifySet(x => x.LastInFileLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Times.Once);
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Imported", viewModel.Message);
        viewModel.ShowMessage = false;
        
        viewModel.ImportDatasetTypeCommand.Execute(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test_import_dataset_type.json"));

        Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count + 2, viewModel.DatasetTypes.Count);
        Assert.Equal(2, viewModel.SelectedDatasetType!.Id);
        Assert.True(viewModel.ShowMessage);
        Assert.Matches("Imported", viewModel.Message);
    }
}