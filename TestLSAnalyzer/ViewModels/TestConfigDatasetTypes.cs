using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Assert.NotEmpty(datasetTypesViewModel.UnsavedDatasetTypeNames);
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
            Assert.NotEmpty(datasetTypesViewModel.UnsavedDatasetTypeNames);

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
    }
}
