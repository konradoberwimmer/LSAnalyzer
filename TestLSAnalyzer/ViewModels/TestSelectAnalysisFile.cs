using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using TestLSAnalyzer.Services;
using GalaSoft.MvvmLight.Threading;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestSelectAnalysisFile
    {
        [Fact]
        public async Task TestGuessDatasetType()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice);
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<MultiplePossibleDatasetTypesMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.False(messageSent);

            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                Nrep = 5,
                RepWgts = "repwgt",
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.Equal("Test with NMI 10 and NREP 5", selectAnalysisFileViewModel.SelectedDatasetType.Name);
            Assert.False(messageSent);

            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5 (duplicate)",
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                Nrep = 5,
                RepWgts = "repwgt",
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.True(messageSent);

            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");
            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999991,
                Name = "Test with PV 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                PVvars = "x;y[0-9]+",
                Nrep = 5,
                RepWgts = "repwgt",
            });
            selectAnalysisFileViewModel.SelectedAnalysisMode = SelectAnalysisFile.AnalysisModes.Build;

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.Equal(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);
        }

        [Fact]
        public async Task TestUseFileForAnalysisSendsMessageOnFailure()
        {
            DispatcherHelper.Initialize();

            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice);
            selectAnalysisFileViewModel.FileName = "C:\\dummy.sav";
            selectAnalysisFileViewModel.SelectedDatasetType = selectAnalysisFileViewModel.DatasetTypes.First();

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<FailureAnalysisConfigurationMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(500);

            Assert.True(messageSent);

            messageSent = false;
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(500);

            Assert.True(messageSent);

            messageSent = false;
            selectAnalysisFileViewModel.SelectedDatasetType = new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5",
                Weight = "wgtstud",
                NMI = 10,
                MIvar = "mi",
                Nrep = 3,
                RepWgts = "repwgt",
            };

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(500);

            Assert.True(messageSent);
        }

        [Fact]
        public async Task TestUseFileForAnalysisSendsMessageOnSuccess()
        {
            DispatcherHelper.Initialize();

            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice);
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            selectAnalysisFileViewModel.SelectedDatasetType = new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                Nrep = 5,
                RepWgts = "repwgt",
            };

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(100);

            Assert.True(messageSent);
        }
    }
}
