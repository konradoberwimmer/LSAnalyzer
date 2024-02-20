using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels;

public class TestDataProviders
{
    [Fact]
    public void TestInitializeFaultyConfig()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.GetTempFileName(), true);
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);

        Assert.Empty(dataProviders.Configurations);
        Assert.NotEmpty(dataProviders.Types);
    }

    [Fact]
    public void TestInitialize()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"));
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);

        Assert.Single(dataProviders.Configurations);
        Assert.Equal("Test Dataverse", dataProviders.Configurations.First().Name);
        Assert.Null(dataProviders.SelectedConfiguration);
    }

    [Fact]
    public void TestNewDataProviderFaultyCall()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"));
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);
        dataProviders.NewDataProviderCommand.Execute(typeof(string));

        Assert.Single(dataProviders.Configurations);
        Assert.Null(dataProviders.SelectedConfiguration);
    }

    [Fact]
    public void TestNewDataProvider()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"));
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);
        dataProviders.NewDataProviderCommand.Execute(dataProviders.Types.Where(t => t.Name == "DataverseConfiguration").First());

        Assert.Equal(2, dataProviders.Configurations.Count);
        Assert.NotNull(dataProviders.SelectedConfiguration);
        Assert.Equal("New dataverse provider", dataProviders.SelectedConfiguration.Name);
    }

    [Fact]
    public void TestSaveDataProvider()
    {
        var tmpConfigFile = Path.GetTempFileName();
        File.Copy(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"), tmpConfigFile, true);

        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(tmpConfigFile);
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);

        dataProviders.SaveDataProviderCommand.Execute(null);
        Assert.Single(JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders);

        dataProviders.NewDataProviderCommand.Execute(dataProviders.Types.Where(t => t.Name == "DataverseConfiguration").First());
        Assert.True(dataProviders.SelectedConfiguration!.IsChanged);

        dataProviders.SaveDataProviderCommand.Execute(null);
        Assert.Single(JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders);

        var selectedConfiguration = dataProviders.SelectedConfiguration as DataverseConfiguration;
        selectedConfiguration!.Name = "New dataverse";
        selectedConfiguration.Url = "https://new-dataverse.com/";
        selectedConfiguration.ApiToken = "very-very-secret";

        dataProviders.SaveDataProviderCommand.Execute(null);
        Assert.Equal(1, dataProviders.SelectedConfiguration.Id);
        Assert.False(dataProviders.SelectedConfiguration.IsChanged);
        Assert.Equal(2, JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders.Count);

        selectedConfiguration.ApiToken = "very-very-very-secret";
        Assert.True(dataProviders.SelectedConfiguration.IsChanged);
        dataProviders.SaveDataProviderCommand.Execute(null);
        Assert.Equal(1, dataProviders.SelectedConfiguration.Id);
        Assert.Equal(2, JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders.Count);
        var lastSavedDataProvider = JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders.Last() as DataverseConfiguration;
        Assert.Equal("very-very-very-secret", lastSavedDataProvider!.ApiToken);

        dataProviders.NewDataProviderCommand.Execute(dataProviders.Types.Where(t => t.Name == "DataverseConfiguration").First()); selectedConfiguration = dataProviders.SelectedConfiguration as DataverseConfiguration;
        selectedConfiguration = dataProviders.SelectedConfiguration as DataverseConfiguration; 
        selectedConfiguration!.Name = "Super dataverse";
        selectedConfiguration.Url = "https://dataverse.super.at/";
        selectedConfiguration.ApiToken = "very-very-secret";

        dataProviders.SaveDataProviderCommand.Execute(null);
        Assert.Equal(3, dataProviders.SelectedConfiguration.Id);
    }

    [Fact]
    public void TestDeleteDataProvider()
    {
        var tmpConfigFile = Path.GetTempFileName();
        File.Copy(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"), tmpConfigFile, true);

        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(tmpConfigFile);
        Configuration configuration = new(string.Empty, configurationBuilder);

        DataProviders dataProviders = new(configuration, new Mock<IServiceProvider>().Object);

        dataProviders.DeleteDataProviderCommand.Execute(null);
        Assert.Single(JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders);

        dataProviders.SelectedConfiguration = dataProviders.Configurations.First();
        dataProviders.DeleteDataProviderCommand.Execute(null);
        Assert.Empty(JsonSerializer.Deserialize<DataProviderConfigurationsList>(File.ReadAllText(tmpConfigFile))!.DataProviders);
    }

    [Fact]
    public void TestTestDataProviderMissingPackage()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"));
        Configuration configuration = new(string.Empty, configurationBuilder);

        var RServiceMock = new Mock<Rservice>();
        RServiceMock.Setup(rservice => rservice.CheckNecessaryRPackages(It.IsAny<string>())).Returns(false);

        ServiceCollection services = new();
        services.AddSingleton(RServiceMock.Object);

        DataProviders dataProviders = new(configuration, services.BuildServiceProvider());

        bool messageSent = false;
        WeakReferenceMessenger.Default.Register<MissingRPackageMessage>(this, (r, m) =>
        {
            messageSent = true;
        });

        dataProviders.TestDataProviderCommand.Execute(null);
        Assert.Equal(string.Empty, dataProviders.TestResults.Message);
        Assert.False(messageSent);

        dataProviders.SelectedConfiguration = dataProviders.Configurations.First();
        
        dataProviders.TestDataProviderCommand.Execute(null);
        Assert.True(messageSent);
    }

    [Fact]
    public void TestTestDataProvider()
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddJsonFile(Path.Combine(AssemblyDirectory, "_testData", "dataProviders.json"));
        Configuration configuration = new(string.Empty, configurationBuilder);

        var RServiceMock = new Mock<Rservice>();
        RServiceMock.Setup(rservice => rservice.CheckNecessaryRPackages(It.IsAny<string>())).Returns(true);
        RServiceMock.Setup(rservice => rservice.Execute(It.IsAny<string>())).Returns(true);

        ServiceCollection services = new();
        services.AddSingleton(RServiceMock.Object);

        DataProviders dataProviders = new(configuration, services.BuildServiceProvider());

        Assert.Equal(string.Empty, dataProviders.TestResults.Message);

        dataProviders.SelectedConfiguration = dataProviders.Configurations.First();

        dataProviders.TestDataProviderCommand.Execute(null);
        Assert.True(dataProviders.TestResults.IsSuccess);
    }

    public static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path)!;
        }
    }
}
