using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels.DataProvider;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        rserviceMock.SetupSequence(rservice => rservice.CheckNecessaryRPackages(It.IsAny<string>())).Returns(false).Returns(true).Returns(true);
        rserviceMock.SetupSequence(rservice => rservice.Execute(It.IsAny<string>())).Returns(false).Returns(true);

        var serviceProvider = new ServiceCollection().AddSingleton(rserviceMock.Object).BuildServiceProvider();

        var viewModel = configuration.GetViewModel(serviceProvider) as Dataverse;

        viewModel!.TestFileAccessCommand.Execute(null);
        Assert.False(viewModel.TestResults.IsSuccess);
        Assert.Empty(viewModel.TestResults.Message);

        viewModel.File = "test.tab";
        viewModel.Dataset = "doi:99.99999/ABCDEFGHI";

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
}
