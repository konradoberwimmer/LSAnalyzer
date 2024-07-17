using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels.DataProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.DataProvider;

public class TestDataverse
{
    [Fact]
    public async Task TestTestFileAccess()
    {
        DataverseConfiguration configuration = new();
        configuration.Name = "test";
        configuration.Url = "http://test.at";
        configuration.ApiToken = "token";

        var rserviceMock = new Mock<Rservice>();
        rserviceMock.SetupSequence(rservice => rservice.CheckNecessaryRPackages(It.IsAny<string>()))
            .Returns(false)
            .Returns(true)
            .Returns(true);
        rserviceMock.SetupSequence(rservice => rservice.Execute(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false)
            .Returns(true).Returns(true).Returns(true);

        var serviceProvider = new ServiceCollection().AddSingleton(rserviceMock.Object).BuildServiceProvider();

        var viewModel = configuration.GetViewModel(serviceProvider) as Dataverse;

        viewModel!.TestFileAccessCommand.Execute(null);
        Assert.False(viewModel.TestResults.IsSuccess);
        Assert.Empty(viewModel.TestResults.Message);

        viewModel.File = "test.tab";
        viewModel.Dataset = "doi:99.99999/ABCDEFGHI";
        viewModel.SelectedFileFormat = new("tsv", "Archive (TSV)");

        viewModel!.TestFileAccessCommand.Execute(null);

        await Task.Delay(500);

        Assert.False(viewModel.TestResults.IsSuccess);
        Assert.Equal("Missing R package 'dataverse'", viewModel.TestResults.Message);

        viewModel!.TestFileAccessCommand.Execute(null);

        await Task.Delay(500);

        Assert.False(viewModel.TestResults.IsSuccess);
        Assert.Equal("File access not working", viewModel.TestResults.Message);

        viewModel!.TestFileAccessCommand.Execute(null);

        await Task.Delay(500);

        Assert.True(viewModel.TestResults.IsSuccess);
        Assert.Equal("File access works", viewModel.TestResults.Message);
    }

    [Fact]
    public void TestLoadDataTemporarilyAndGetVariables()
    {
        DataverseConfiguration configuration = new();
        configuration.Name = "test";
        configuration.Url = "http://test.at";
        configuration.ApiToken = "token";

        var rserviceMock = new Mock<Rservice>();
        rserviceMock.SetupSequence(rservice => rservice.Execute(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true).Returns(false)
            .Returns(true).Returns(true).Returns(true).Returns(true)
            .Returns(true).Returns(true).Returns(true).Returns(true);
        rserviceMock.SetupSequence(rservice => rservice.Fetch(It.IsAny<string>()))
            .Returns((CharacterVector?)null);

        var serviceProvider = new ServiceCollection().AddSingleton(rserviceMock.Object).BuildServiceProvider();

        var viewModel = configuration.GetViewModel(serviceProvider) as Dataverse;
        viewModel!.File = "test.tab";
        viewModel.Dataset = "doi:99.99999/ABCDEFGHI";
        viewModel.SelectedFileFormat = new("tsv", "Archive (TSV)");

        // mocking failure from Rservice with loading data
        var result = viewModel!.LoadDataTemporarilyAndGetVariables();
        Assert.Empty(result);

        // mocking failure from Rservice with reading column names
        result = viewModel!.LoadDataTemporarilyAndGetVariables();
        Assert.Empty(result);

        // happy case not mockable :-(
    }

    [Fact]
    public void TestLoadDataForUsage()
    {
        DataverseConfiguration configuration = new();
        configuration.Name = "test";
        configuration.Url = "http://test.at";
        configuration.ApiToken = "token";

        var rserviceMock = new Mock<Rservice>();
        rserviceMock.SetupSequence(rservice => rservice.Execute(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true).Returns(false)
            .Returns(true).Returns(true).Returns(true).Returns(true).Returns(true);

        var serviceProvider = new ServiceCollection().AddSingleton(rserviceMock.Object).BuildServiceProvider();

        var viewModel = configuration.GetViewModel(serviceProvider) as Dataverse;
        viewModel!.File = "test.tab";
        viewModel.Dataset = "doi:99.99999/ABCDEFGHI";
        viewModel.SelectedFileFormat = new("tsv", "Archive (TSV)");

        // mocking failure from Rservice
        var result = viewModel!.LoadDataForUsage();
        Assert.False(result);

        // mocking success from Rservice
        result = viewModel!.LoadDataForUsage();
        Assert.True(result);
    }

    private static string GetTestApiToken()
    {
        ConfigurationBuilder builder = new();
        builder.AddUserSecrets<TestDataverse>();

        var configuration = builder.Build();
        return (string)configuration["dataverseTestKey"]!;
    }
}
