using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Services.Stubs;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestSystemSettings
    {
        [Fact]
        public void TestSaveSessionRcodeCommand()
        {
            Logging logger = new();
            Configuration configuration = new("", null, new SettingsServiceStub(), new RegistryService());
            Rservice rservice = new(logger);
            rservice.RLocation = configuration.GetRLocation() ?? (string.Empty, string.Empty);

            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());

            SystemSettings systemSettingsViewModel = new(rservice, configuration, logger);

            var filename = Path.GetTempFileName();
            systemSettingsViewModel.SaveSessionRcodeCommand.Execute(filename);

            var savedLog = File.ReadAllText(filename);
            Assert.Contains("lsanalyzer_func_quantile <- function(", savedLog);
            Assert.DoesNotContain("- lsanalyzer_func_quantile <- function(", savedLog);
        }

        [Fact]
        public void TestSaveSessionLogCommand()
        {
            Logging logger = new();
            Configuration configuration = new("", null, new SettingsServiceStub(), new RegistryService());
            Rservice rservice = new(logger);
            rservice.RLocation = configuration.GetRLocation() ?? (string.Empty, string.Empty);

            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());

            SystemSettings systemSettingsViewModel = new(rservice, configuration, logger);

            var filename = Path.GetTempFileName();
            systemSettingsViewModel.SaveSessionLogCommand.Execute(filename);

            var savedLog = File.ReadAllText(filename);
            Assert.Contains("- lsanalyzer_func_quantile <- function(", savedLog);
        }

        [Fact]
        public void TestLoadDefaultDatasetTypesCommand()
        {
            Logging logger = new();
            Configuration configuration = new(Path.GetTempFileName(), null, new SettingsServiceStub(), new RegistryService());
            Rservice rservice = new(logger);
            rservice.RLocation = configuration.GetRLocation() ?? (string.Empty, string.Empty);

            configuration.StoreDatasetType(new() { Id = 33 });

            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InjectAppFunctions());

            SystemSettings systemSettingsViewModel = new(rservice, configuration, logger);
            systemSettingsViewModel.LoadDefaultDatasetTypesCommand.Execute(null);

            Assert.Equal(DatasetType.CreateDefaultDatasetTypes().Count + 1, configuration.GetStoredDatasetTypes()!.Count);
            Assert.NotEmpty(configuration.GetStoredDatasetTypes()!.Where(dst => dst.Id == 33));
        }
        
        [Fact]
        public void TestSaveSettingsCommand()
        {
            SystemSettings systemSettingsViewModel = new();
            int oldValue = systemSettingsViewModel.NumberRecentSubsettingExpressions;            
            
            systemSettingsViewModel.NumberRecentSubsettingExpressions = -1;
            systemSettingsViewModel.SaveSettingsCommand.Execute(null);
            systemSettingsViewModel = new();
            
            Assert.Equal(oldValue, systemSettingsViewModel.NumberRecentSubsettingExpressions);
            
            systemSettingsViewModel.NumberRecentSubsettingExpressions = 127;
            systemSettingsViewModel.SaveSettingsCommand.Execute(null);
            systemSettingsViewModel = new();
            
            Assert.Equal(127, systemSettingsViewModel.NumberRecentSubsettingExpressions);
            
            systemSettingsViewModel.NumberRecentSubsettingExpressions = oldValue;
            systemSettingsViewModel.SaveSettingsCommand.Execute(null);
        }

        [Fact]
        public void TestSetAlternativeRLocation()
        {
            var impossibleRLocationMessage = false;
            WeakReferenceMessenger.Default.Register<ImpossibleRLocationMessage>(this, (_, _) => impossibleRLocationMessage = true);
            var requestRestartMessage = false;
            WeakReferenceMessenger.Default.Register<RequestRestartMessage>(this, (_, _) => requestRestartMessage = true);

            SystemSettings systemSettingsViewModel = new();
            
            Assert.True(string.IsNullOrWhiteSpace(systemSettingsViewModel.AlternativeRLocation));

            systemSettingsViewModel.SetAlternativeRLocationCommand.Execute("""C:\somewhere_really_wrong""");
            
            Assert.True(impossibleRLocationMessage);
            Assert.True(string.IsNullOrWhiteSpace(systemSettingsViewModel.AlternativeRLocation));

            impossibleRLocationMessage = false;
            var stillImpossibleRLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stillImpossibleRLocation);
            
            systemSettingsViewModel.SetAlternativeRLocationCommand.Execute(stillImpossibleRLocation);
            
            Assert.True(impossibleRLocationMessage);
            Assert.True(string.IsNullOrWhiteSpace(systemSettingsViewModel.AlternativeRLocation));
            
            impossibleRLocationMessage = false;
            var possibleRLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(possibleRLocation, "bin", "x64"));
            var fileStream = File.Create(Path.Combine(possibleRLocation, "bin", "x64", "R.dll"));
            fileStream.Close();
            
            systemSettingsViewModel.SetAlternativeRLocationCommand.Execute(possibleRLocation);
            
            Assert.False(impossibleRLocationMessage);
            Assert.Equal(possibleRLocation, systemSettingsViewModel.AlternativeRLocation);
            Assert.True(requestRestartMessage);
        }
        
        [Fact]
        public void TestClearAlternativeRLocation()
        {
            var requestRestartMessage = false;
            WeakReferenceMessenger.Default.Register<RequestRestartMessage>(this, (_, _) => requestRestartMessage = true);

            SystemSettings systemSettingsViewModel = new();
            systemSettingsViewModel.AlternativeRLocation = """C:\somewhere_good""";
            
            systemSettingsViewModel.ClearAlternativeRLocationCommand.Execute(null);
            
            Assert.True(string.IsNullOrWhiteSpace(systemSettingsViewModel.AlternativeRLocation));
            Assert.True(requestRestartMessage);
        }
    }
}
