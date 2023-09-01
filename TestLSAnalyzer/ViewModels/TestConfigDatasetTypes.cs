using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels
{
    public class TestConfigDatasetTypes
    {
        [Fact]
        public void TestConstructorAndSwitchingBetweenDatasetTypes()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            
            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);

            Assert.NotEmpty(datasetTypesViewModel.DatasetTypes);
            Assert.Empty(datasetTypesViewModel.UnsavedDatasetTypeNames);

            datasetTypesViewModel.SelectedDatasetType = datasetTypesViewModel.DatasetTypes[0];
        }

        [Fact]
        public void TestNewDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            int maxId = 0;
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
                if (datasetType.Id > maxId)
                {
                    maxId = datasetType.Id;
                }
            }

            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);

            datasetTypesViewModel.NewDatasetTypeCommand.Execute(null);
            Assert.NotNull(datasetTypesViewModel.SelectedDatasetType);
            Assert.Equal("New dataset type", datasetTypesViewModel.SelectedDatasetType.Name);
            Assert.Equal(maxId + 1, datasetTypesViewModel.SelectedDatasetType.Id);
            Assert.DoesNotContain("New dataset type", datasetTypesViewModel.UnsavedDatasetTypeNames);

            datasetTypesViewModel.SelectedDatasetType.Weight = "wgt";
            Assert.Contains("New dataset type", datasetTypesViewModel.UnsavedDatasetTypeNames);
        }

        [Fact]
        public void TestSaveSelectedDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }

            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);

            datasetTypesViewModel.SelectedDatasetType = datasetTypesViewModel.DatasetTypes.First();
            var id = datasetTypesViewModel.SelectedDatasetType.Id;
            var savedRepWgts = datasetTypesViewModel.SelectedDatasetType.RepWgts;
            datasetTypesViewModel.SelectedDatasetType.RepWgts = "*rather-stupid*";

            datasetTypesViewModel.SaveSelectedDatasetTypeCommand.Execute(null);
            Assert.NotEmpty(datasetTypesViewModel.SelectedDatasetType.Errors);

            datasetTypesViewModel.SelectedDatasetType.RepWgts = savedRepWgts;
            datasetTypesViewModel.SaveSelectedDatasetTypeCommand.Execute(null);
            Assert.Empty(datasetTypesViewModel.SelectedDatasetType.Errors);
            Assert.Empty(datasetTypesViewModel.UnsavedDatasetTypeNames);

            datasetTypesViewModel.SelectedDatasetType.NMI = 777;
            datasetTypesViewModel.SaveSelectedDatasetTypeCommand.Execute(null);

            var storedDatasetTypes = datasetTypesConfiguration.GetStoredDatasetTypes();
            Assert.Equal(777, storedDatasetTypes!.Where(dst => dst.Id == id).First().NMI);
        }

        [Fact]
        public void TestRemoveDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }

            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);

            datasetTypesViewModel.SelectedDatasetType = datasetTypesViewModel.DatasetTypes.First();
            var id = datasetTypesViewModel.SelectedDatasetType.Id;

            datasetTypesViewModel.RemoveDatasetTypeCommand.Execute(null);
            Assert.Null(datasetTypesViewModel.SelectedDatasetType);
            Assert.Empty(datasetTypesViewModel.UnsavedDatasetTypeNames);

            var storedDatasetTypes = datasetTypesConfiguration.GetStoredDatasetTypes();
            Assert.Empty(storedDatasetTypes!.Where(dst => dst.Id == id));
        }

        [Fact]
        public void TestImportDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var aDatasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(aDatasetType);
            }

            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);
            var numberOfDatasetTypes = datasetTypesViewModel.DatasetTypes.Count;

            string failureMessageSent = string.Empty;
            WeakReferenceMessenger.Default.Register<FailureImportDatasetTypeMessage>(this, (r, m) =>
            {
                failureMessageSent = m.Value;
            });

            datasetTypesViewModel.ImportDatasetTypeCommand.Execute(null);
            Assert.True(failureMessageSent == string.Empty);

            var corruptFileName = Path.GetTempFileName();
            datasetTypesViewModel.ImportDatasetTypeCommand.Execute(corruptFileName);
            Assert.True(failureMessageSent == "invalid file");

            failureMessageSent = string.Empty;
            var datasetType = datasetTypesViewModel.DatasetTypes.First();
            datasetType.Weight = "";
            File.WriteAllText(corruptFileName, JsonSerializer.Serialize(datasetType));
            datasetTypesViewModel.ImportDatasetTypeCommand.Execute(corruptFileName);
            Assert.True(failureMessageSent == "invalid dataset type");

            failureMessageSent = string.Empty;
            var goodFileName = Path.GetTempFileName();
            datasetType.Weight = "validWeight";
            File.WriteAllText(goodFileName, JsonSerializer.Serialize(datasetType));
            datasetTypesViewModel.ImportDatasetTypeCommand.Execute(goodFileName);
            Assert.True(failureMessageSent == string.Empty);
            Assert.True(datasetTypesViewModel.DatasetTypes.Count == numberOfDatasetTypes + 1);
        }

        [Fact]
        public void TestExportDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }

            ConfigDatasetTypes datasetTypesViewModel = new(datasetTypesConfiguration);

            var filename = Path.GetTempFileName();

            datasetTypesViewModel.ExportDatasetTypeCommand.Execute(filename);
            Assert.Empty(File.ReadAllLines(filename));

            datasetTypesViewModel.SelectedDatasetType = datasetTypesViewModel.DatasetTypes.First();
            datasetTypesViewModel.ExportDatasetTypeCommand.Execute(null);
            Assert.Empty(File.ReadAllLines(filename));

            datasetTypesViewModel.SelectedDatasetType.Weight = string.Empty;
            datasetTypesViewModel.ExportDatasetTypeCommand.Execute(filename);
            Assert.Empty(File.ReadAllLines(filename));

            datasetTypesViewModel.SelectedDatasetType.Weight = "validWeight";
            datasetTypesViewModel.ExportDatasetTypeCommand.Execute(filename);
            Assert.NotEmpty(File.ReadAllLines(filename));
        }
    }
}
