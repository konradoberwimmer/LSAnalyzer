using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.Services.DataProvider;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Services.DataProvider
{
    [Collection("Sequential")]
    public class TestDataverse
    {
        [Fact]
        public void TestTestProvider()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages("dataverse"), "Package dataverse must be installed for this test");

            Dataverse dataverseService = new(rservice)
            {
                Configuration = new Mock<IDataProviderConfiguration>().Object
            };  

            var result = dataverseService.TestProvider();
            
            Assert.False(result.IsSuccess);
            Assert.Contains("Mismatch", result.Message);

            DataverseConfiguration dataverseConfiguration = new()
            {
                Id = 1,
                Name = "test (thx to AUSSDA)",
                Url = "https://dat.ausda.at/",
                ApiToken = "not a valid api token",
            };

            dataverseService.Configuration = dataverseConfiguration;

            result = dataverseService.TestProvider();

            Assert.False(result.IsSuccess);
            Assert.Contains("URL wrong?", result.Message);

            dataverseConfiguration.Url = "https://data.aussda.at/";
            
            result = dataverseService.TestProvider();

            Assert.False(result.IsSuccess);
            Assert.Contains("API token wrong?", result.Message);
            
            dataverseConfiguration.ApiToken = GetTestApiToken();

            result = dataverseService.TestProvider();

            Assert.True(result.IsSuccess, "dataverse access (https://data.aussda.at/) not working - be sure to set up an API key in your user secrets");
            Assert.Contains("works", result.Message);
        }

        [Fact]
        public void TestTestFileAccess()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages("dataverse"), "Package dataverse must be installed for this test");

            Dataverse dataverseService = new(rservice)
            {
                Configuration = new Mock<IDataProviderConfiguration>().Object
            };

            var result = dataverseService.TestFileAccess(new { });

            Assert.False(result.IsSuccess);
            Assert.Contains("Mismatch", result.Message);

            DataverseConfiguration dataverseConfiguration = new()
            {
                Id = 1,
                Name = "test (thx to AUSSDA)",
                Url = "https://data.aussda.at/",
                ApiToken = "not a valid api token",
            };

            dataverseService.Configuration = dataverseConfiguration;

            result = dataverseService.TestFileAccess(new { });

            Assert.False(result.IsSuccess);
            Assert.Contains("Missing", result.Message);

            dynamic fileAccessValues = new ExpandoObject();
            fileAccessValues.File = "abc.tab";
            fileAccessValues.Dataset = "doi:123";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.TestFileAccess(fileAccessValues);

            Assert.False(result.IsSuccess);
            Assert.Contains("not working", result.Message);

            dataverseConfiguration.ApiToken = GetTestApiToken();

            fileAccessValues.File = "10715_vi_de_v1_0.tab";
            fileAccessValues.Dataset = "doi:10.11587/5ZCVJY";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.TestFileAccess(fileAccessValues);

            Assert.True(result.IsSuccess, "dataverse access (https://data.aussda.at/) not working - be sure to set up an API key in your user secrets");
            Assert.Contains("works", result.Message);
        }

        [Fact]
        public void TestGetDatasetVariables()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages("dataverse"), "Package dataverse must be installed for this test");

            Dataverse dataverseService = new(rservice)
            {
                Configuration = new Mock<IDataProviderConfiguration>().Object
            };

            var result = dataverseService.GetDatasetVariables(new { });

            Assert.Empty(result);

            DataverseConfiguration dataverseConfiguration = new()
            {
                Id = 1,
                Name = "test (thx to AUSSDA)",
                Url = "https://data.aussda.at/",
                ApiToken = "not a valid api token",
            };

            dataverseService.Configuration = dataverseConfiguration;

            result = dataverseService.GetDatasetVariables(new { });

            Assert.Empty(result);

            dynamic fileAccessValues = new ExpandoObject();
            fileAccessValues.File = "abc.tab";
            fileAccessValues.Dataset = "doi:123";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.GetDatasetVariables(fileAccessValues);

            Assert.Empty(result);

            dataverseConfiguration.ApiToken = GetTestApiToken();

            fileAccessValues.File = "10715_vi_de_v1_0.tab";
            fileAccessValues.Dataset = "doi:10.11587/5ZCVJY";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.GetDatasetVariables(fileAccessValues);

            Assert.NotEmpty(result);
            Assert.NotEmpty(result.Where(var => var.Name == "Label"));
        }

        [Fact]
        public void TestLoadFileIntoGlobalEnvironment()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages("dataverse"), "Package dataverse must be installed for this test");

            Dataverse dataverseService = new(rservice)
            {
                Configuration = new Mock<IDataProviderConfiguration>().Object
            };

            var result = dataverseService.LoadFileIntoGlobalEnvironment(new { });

            Assert.False(result);

            DataverseConfiguration dataverseConfiguration = new()
            {
                Id = 1,
                Name = "test (thx to AUSSDA)",
                Url = "https://data.aussda.at/",
                ApiToken = "not a valid api token",
            };

            dataverseService.Configuration = dataverseConfiguration;

            result = dataverseService.LoadFileIntoGlobalEnvironment(new { });

            Assert.False(result);

            dynamic fileAccessValues = new ExpandoObject();
            fileAccessValues.File = "abc.tab";
            fileAccessValues.Dataset = "doi:123";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.LoadFileIntoGlobalEnvironment(fileAccessValues);

            Assert.False(result);

            dataverseConfiguration.ApiToken = GetTestApiToken();

            fileAccessValues.File = "10715_vi_de_v1_0.tab";
            fileAccessValues.Dataset = "doi:10.11587/5ZCVJY";
            fileAccessValues.FileFormat = "tsv";
            result = dataverseService.LoadFileIntoGlobalEnvironment(fileAccessValues);

            Assert.True(result, "dataverse access (https://data.aussda.at/) not working - be sure to set up an API key in your user secrets");
        }

        internal static string GetTestApiToken()
        {
            ConfigurationBuilder builder = new();
            builder.AddUserSecrets<TestDataverse>();

            var configuration = builder.Build();
            return (string)configuration["testDataverseKey"]!;
        }
    }
}
