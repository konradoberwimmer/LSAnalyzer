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
    [Collection("Sequential")]
    public class TestSystemSettings
    {
        [Fact]
        public void TestSaveSessionLogCommand()
        {
            Logging logger = new();
            Configuration configuration = new("");
            Rservice rservice = new(logger);

            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());

            SystemSettings systemSettingsViewModel = new(rservice, configuration, logger);

            var filename = Path.GetTempFileName();
            systemSettingsViewModel.SaveSessionLogCommand.Execute(filename);

            var savedLog = File.ReadAllText(filename);
            Assert.Contains("lsanalyzer_func_quantile <- function(", savedLog);
        }

        [Fact]
        public void TestLoadDefaultDatasetTypesCommand()
        {
            Logging logger = new();
            Configuration configuration = new(Path.GetTempFileName());
            Rservice rservice = new(logger);

            configuration.StoreDatasetType(new() { Id = 33 });

            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());

            SystemSettings systemSettingsViewModel = new(rservice, configuration, logger);
            systemSettingsViewModel.LoadDefaultDatasetTypesCommand.Execute(null);

            Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count + 1, configuration.GetStoredDatasetTypes()!.Count);
            Assert.NotEmpty(configuration.GetStoredDatasetTypes()!.Where(dst => dst.Id == 33));
        }
    }
}
