using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using TestLSAnalyzer.Services;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using LSAnalyzer.Helper;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.ViewModels;

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
        });

        selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);
        await Task.Delay(100);

        Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType);
        Assert.NotEqual(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);
        Assert.Equal(999993, selectAnalysisFileViewModel.SelectedDatasetType.Id);
    }

    [Fact]
    public void TestGuessDatasetTypeAutoEncapsulateRegex()
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
            RepWgts = "repwgt",
        });
        selectAnalysisFileViewModel.SelectedDatasetType = selectAnalysisFileViewModel.DatasetTypes.First();

        selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);

        Policy.Handle<NullException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType));

        selectAnalysisFileViewModel.DatasetTypes.Add(new()
        {
            Id = 999992,
            Name = "Test with PV 10 and NREP 5 (correct)",
            AutoEncapsulateRegex = true,
            Weight = "wgt",
            NMI = 10,
            PVvarsList = new() { new() { Regex = "x[0-9]+", DisplayName = "x", Mandatory = true } },
            RepWgts = "repwgt[0-9]+",
        });

        selectAnalysisFileViewModel.GuessDatasetTypeCommand.Execute(null);

        Policy.Handle<NotNullException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.NotNull(selectAnalysisFileViewModel.SelectedDatasetType));
        Assert.Equal(selectAnalysisFileViewModel.DatasetTypes.Last(), selectAnalysisFileViewModel.SelectedDatasetType);
    }

    [Fact]
    public void TestGuessDatasetTypeSendsFileTypeError()
    {
        var datasetTypesConfiguration = new Mock<Configuration>();
        datasetTypesConfiguration.Setup(conf => conf.GetStoredRecentFiles(It.IsAny<int>())).Returns([]);

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
        
        Policy.Handle<TrueException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageSent));
        Assert.Null(selectAnalysisFileViewModel.SelectedDatasetType);
    }

    [Fact]
    public void TestUseFileForAnalysisSendsMessageOnFailure()
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
        
        Policy.Handle<TrueException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageSent));

        messageSent = false;
        selectAnalysisFileViewModel.FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");

        selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
        
        Policy.Handle<TrueException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageSent));

        messageSent = false;
        selectAnalysisFileViewModel.SelectedDatasetType = new()
        {
            Id = 999991,
            Name = "Test with NMI 10 and NREP 5",
            Weight = "wgtstud",
            NMI = 10,
            MIvar = "mi",
            RepWgts = "repwgt",
        };

        selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

        Policy.Handle<TrueException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(messageSent));
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
    public void TestUseFileForAnalysisWithAutoEncapsulatedRegex()
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
        selectAnalysisFileViewModel.SelectedDatasetType = new()
        {
            AutoEncapsulateRegex = true,
            Weight = "wgt",
            NMI = 10,
            PVvarsList = new() { new() { Regex = "x", DisplayName = "x", Mandatory = true }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = true } },
            RepWgts = "repwgt",
            FayFac = 0.5,
        };

        var failureMessageSent = false;
        WeakReferenceMessenger.Default.Register<FailureAnalysisConfigurationMessage>(this, (r, m) =>
        {
            failureMessageSent = true;
        });
        AnalysisConfiguration? analysisConfiguration = null;
        var successMessageSent = false;
        WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (r, m) =>
        {
            successMessageSent = true;
            analysisConfiguration = m.Value;
        });

        selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);
        
        Policy.Handle<TrueException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(failureMessageSent));
        Assert.Null(analysisConfiguration);
        
        selectAnalysisFileViewModel.SelectedDatasetType.PVvarsList = new() { new() { Regex = "x[0-9]+", DisplayName = "x", Mandatory = false }, new() { Regex = "y[0-9]+", DisplayName = "y", Mandatory = false } };
        selectAnalysisFileViewModel.SelectedDatasetType.RepWgts = "repwgt[0-9]+";

        selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

        Policy.Handle<TrueException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(1))
            .Execute(() => Assert.True(successMessageSent));
        Assert.NotNull(analysisConfiguration);

        var currentVariables = rservice.GetCurrentDatasetVariables(analysisConfiguration);
        Assert.NotNull(currentVariables);
        Assert.Contains("^x[0-9]+$", currentVariables.ConvertAll<string>(var => var.Name));
    }

    [Fact]
    public void TestInitializeFromRecentFile()
    {
        var configurationMock = new Mock<Configuration>();
        configurationMock.Setup(conf => conf.GetStoredRecentFiles(It.IsAny<int>())).Returns([]);

        SelectAnalysisFile selectAnalysisFile = new(configurationMock.Object, Mock.Of<Rservice>(), Mock.Of<IServiceProvider>());

        configurationMock.Verify(conf => conf.GetStoredRecentFiles(It.Is<int>(val => val == 0)), Times.Once);
        
        selectAnalysisFile.DatasetTypes =
            [ new() { Id = 3, Name = "Test", Weight = "wgt", NMI = 10, MIvar = "mi", RepWgts = "repwgt" } ];
        
        var tempFile = Path.GetTempFileName();

        selectAnalysisFile.RecentFilesForAnalyses =
        [
            JsonSerializer.Deserialize<Configuration.RecentFileForAnalysis>($$"""{"FileName":"{{System.Web.HttpUtility.JavaScriptStringEncode(tempFile)}}","UsageAttributes":{"UseCsv2":false},"ConvertCharacters":false,"DatasetTypeId":3,"Weight":"wgt","ModeKeep":false}""")!,
            new() { FileName = "C:\\not_here_16413", DatasetTypeId = 2, Weight = "weight" }
        ];

        selectAnalysisFile.InitializeFromRecentFile(selectAnalysisFile.RecentFilesForAnalyses.First());

        configurationMock.Verify(conf => conf.GetStoredRecentFiles(It.Is<int>(val => val == 0)), Times.Once);
        
        Assert.Equal(tempFile, selectAnalysisFile.FileName);
        Assert.False(selectAnalysisFile.UseCsv2);
        Assert.False(selectAnalysisFile.ReplaceCharacterVectors);
        Assert.Equal(selectAnalysisFile.DatasetTypes.First(), selectAnalysisFile.SelectedDatasetType);
        Assert.Equal("wgt", selectAnalysisFile.SelectedWeightVariable);
        Assert.Equal(SelectAnalysisFile.AnalysisModes.Build, selectAnalysisFile.SelectedAnalysisMode);
        
        selectAnalysisFile.InitializeFromRecentFile(selectAnalysisFile.RecentFilesForAnalyses.Last());

        configurationMock.Verify(conf => conf.GetStoredRecentFiles(It.Is<int>(val => val == 0)), Times.Exactly(2));
        
        Assert.Equal(tempFile, selectAnalysisFile.FileName);
        Assert.Empty(selectAnalysisFile.RecentFilesForAnalyses); // because the mock won't return any recent files when they are fetched again after removing one
    }
}
