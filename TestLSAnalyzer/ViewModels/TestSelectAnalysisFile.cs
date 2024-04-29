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
using Moq;
using Microsoft.Extensions.DependencyInjection;
using LSAnalyzer.Helper;

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
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice, new ServiceCollection().BuildServiceProvider());
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
                Id = 999992,
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
                Id = 999993,
                Name = "Test with PV 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                Nrep = 5,
                RepWgts = "repwgt",
            });
            selectAnalysisFileViewModel.SelectedAnalysisMode = SelectAnalysisFile.AnalysisModes.Build;

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.Equal(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);

            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999994,
                Name = "Test with PV 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                Nrep = 1,
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(100);

            Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.NotEqual(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.Equal(999993, selectAnalysisFileViewModel.SelectedDatasetType.Id);
        }

        [Fact]
        public async Task TestGuessDatasetTypeAutoEncapsulateRegex()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice, new ServiceCollection().BuildServiceProvider());
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");

            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999991,
                Name = "Test with PV 10 and NREP 5 (incorrect)",
                AutoEncapsulateRegex = true,
                Weight = "wgt",
                NMI = 10,
                PVvarsList = new() { new() { Regex = "x[0-9]+", DisplayName = "x", Mandatory = true } },
                Nrep = 5,
                RepWgts = "repwgt",
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(1000);

            Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType);

            selectAnalysisFileViewModel.DatasetTypes.Add(new()
            {
                Id = 999992,
                Name = "Test with PV 10 and NREP 5 (correct)",
                AutoEncapsulateRegex = true,
                Weight = "wgt",
                NMI = 10,
                PVvarsList = new() { new() { Regex = "x[0-9]+", DisplayName = "x", Mandatory = true } },
                Nrep = 5,
                RepWgts = "repwgt[0-9]+",
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(1000);

            Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.Equal(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);
        }

        [Fact]
        public async Task TestGuessDatasetTypeSendsFileTypeError()
        {
            var datasetTypesConfiguration = new Mock<Configuration>();
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration.Object, rservice, new ServiceCollection().BuildServiceProvider());
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_asgautr4.csv");
            selectAnalysisFileViewModel.UseCsv2 = false;

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<FailureAnalysisFileMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
            await Task.Delay(500);

            Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType);
            Assert.True(messageSent);
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
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice, new ServiceCollection().BuildServiceProvider());
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
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice, new ServiceCollection().BuildServiceProvider());
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

        [Fact]
        public async Task TestUseFileForAnalysisWithAutoEncapsulatedRegex()
        {
            DispatcherHelper.Initialize();

            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration, rservice, new ServiceCollection().BuildServiceProvider());
            selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_pv10_nrep5.sav");
            selectAnalysisFileViewModel.SelectedDatasetType = new()
            {
                AutoEncapsulateRegex = true,
                Weight = "wgt",
                NMI = 10,
                PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
                Nrep = 5,
                RepWgts = "repwgt",
                FayFac = 0.5,
            };

            AnalysisConfiguration? analysisConfiguration = null;
            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (r, m) =>
            {
                messageSent = true;
                analysisConfiguration = m.Value;
            });

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(5000);

            Assert.False(messageSent);

            selectAnalysisFileViewModel.SelectedDatasetType.PVvarsList = new() { new() { Regex = "x[0-9]+", DisplayName = "x", Mandatory = false }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = false } };
            selectAnalysisFileViewModel.SelectedDatasetType.RepWgts = "repwgt[0-9]+";

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
            await Task.Delay(5000);

            Assert.True(messageSent);
            Assert.NotNull(analysisConfiguration);

            var currentVariables = rservice.GetCurrentDatasetVariables(analysisConfiguration);
            Assert.NotNull(currentVariables);
            Assert.Contains("^x[0-9]+$", currentVariables.ConvertAll<string>(var => var.Name));
        }
    }
}
