using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Services.Stubs;
using Moq;
using RDotNet;

namespace TestLSAnalyzer.ViewModels;

public class TestSystemSettings
{
    [Fact]
    public void TestSaveSessionRcodeCommand()
    {
        Logging logger = new();
        FakeRservice rservice = new(logger);

        rservice.Connect();
        rservice.InjectAppFunctions();

        SystemSettings systemSettingsViewModel = new(rservice, new Mock<Configuration>().Object, logger, new DatasetTypeRepositoryStub());

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
        FakeRservice rservice = new(logger);

        rservice.Connect();
        rservice.InjectAppFunctions();

        SystemSettings systemSettingsViewModel = new(rservice, new Mock<Configuration>().Object, logger, new DatasetTypeRepositoryStub());

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

        configuration.StoreDatasetType(new() { Id = 33 });

        SystemSettings systemSettingsViewModel = new(new Mock<IRservice>().Object, configuration, logger, new DatasetTypeRepositoryStub());
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
    public void TestSaveSettingsCommandInvokesConfiguration()
    {
        var configuration = new Mock<Configuration>();
        
        SystemSettings systemSettingsViewModel = new(new RserviceStub(), configuration.Object, new LoggingStub(), new DatasetTypeRepositoryStub());

        systemSettingsViewModel.SaveSettingsCommand.Execute(null);
        
        configuration.Verify(conf => conf.TrimRecentFiles(It.IsAny<int>()), Times.Once);
        configuration.Verify(conf => conf.TrimRecentBatchAnalyzeFiles(It.IsAny<int>()), Times.Once);
        configuration.Verify(conf => conf.TrimRecentSubsettingExpressions(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void TestSetAlternativeRLocation()
    {
        var impossibleRLocationMessage = false;
        WeakReferenceMessenger.Default.Register<SystemSettings.ImpossibleRLocationMessage>(this, (_, _) => impossibleRLocationMessage = true);
        var requestRestartMessage = false;
        WeakReferenceMessenger.Default.Register<SystemSettings.RequestRestartMessage>(this, (_, _) => requestRestartMessage = true);

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
        WeakReferenceMessenger.Default.Register<SystemSettings.RequestRestartMessage>(this, (_, _) => requestRestartMessage = true);

        SystemSettings systemSettingsViewModel = new();
        systemSettingsViewModel.AlternativeRLocation = """C:\somewhere_good""";
        
        systemSettingsViewModel.ClearAlternativeRLocationCommand.Execute(null);
        
        Assert.True(string.IsNullOrWhiteSpace(systemSettingsViewModel.AlternativeRLocation));
        Assert.True(requestRestartMessage);
    }

    [Fact]
    public void TestFetchDatasetTypesInvalidRepository()
    {
        var configuration = new Mock<Configuration>();

        const string wrongUrl = "https://www.nothere.com/index.json";
        
        var datasetTypeRepository = new Mock<IDatasetTypeRepository>();
        datasetTypeRepository
            .Setup(service => service.FetchDatasetTypeCollections(It.Is<string>(url => url == wrongUrl)))
            .Returns((IDatasetTypeRepository.FetchResult.NotFound, []));
        
        SystemSettings systemSettingsViewModel = new(new RserviceStub(), configuration.Object, new LoggingStub(), datasetTypeRepository.Object)
        {
            RepositoryUrl = wrongUrl,
            CollectionName = "default"
        };

        var messageSent = false;
        WeakReferenceMessenger.Default.Register<SystemSettings.DatasetTypeRepositoryUrlInvalidMessage>(this, (_, _) => messageSent = true);
        
        systemSettingsViewModel.FetchDatasetTypesCommand.Execute(null);
        
        Assert.True(messageSent);
    }
    
    [Fact]
    public void TestFetchDatasetTypesInvalidCollection()
    {
        var configuration = new Mock<Configuration>();

        const string correctUrl = "https://www.my_lsanalyzer_repo.com/index.json";
        
        var datasetTypeRepository = new Mock<IDatasetTypeRepository>();
        datasetTypeRepository
            .Setup(service => service.FetchDatasetTypeCollections(It.Is<string>(url => url == correctUrl)))
            .Returns((IDatasetTypeRepository.FetchResult.Success, [
                new DatasetTypeCollection
                {
                    Name = "default",
                }
            ]));
        
        SystemSettings systemSettingsViewModel = new(new RserviceStub(), configuration.Object, new LoggingStub(), datasetTypeRepository.Object)
        {
            RepositoryUrl = correctUrl,
            CollectionName = "indisputable"
        };

        var messageSent = false;
        WeakReferenceMessenger.Default.Register<SystemSettings.CollectionNotInDatasetTypeRepositoryMessage>(this, (_, _) => messageSent = true);
        
        systemSettingsViewModel.FetchDatasetTypesCommand.Execute(null);
        
        Assert.True(messageSent);
    }
    
    [Fact]
    public void TestFetchDatasetTypesInvalidFile()
    {
        var configuration = new Mock<Configuration>();

        const string correctUrl = "https://www.my_lsanalyzer_repo.com/index.json";
        const string correctFile = "12_exists.json";
        const string wrongFile = "24_does_not_exist.json";
        
        var datasetTypeRepository = new Mock<IDatasetTypeRepository>();
        datasetTypeRepository
            .Setup(service => service.FetchDatasetTypeCollections(It.Is<string>(url => url == correctUrl)))
            .Returns((IDatasetTypeRepository.FetchResult.Success, [
                new DatasetTypeCollection
                {
                    Name = "default",
                    Entries = [
                        new DatasetTypeCollection.Entry
                        {
                            DatasetTypeId = 12,
                            FileName = correctFile,
                        },
                        new DatasetTypeCollection.Entry
                        {
                            DatasetTypeId = 24,
                            FileName = wrongFile,
                        }
                    ]
                }
            ]));
        datasetTypeRepository
            .Setup(service => service.FetchDatasetType(It.Is<string>(baseUrl => correctUrl.StartsWith(baseUrl)),It.Is<string>(fileName => fileName == correctFile)))
            .Returns((IDatasetTypeRepository.FetchResult.Success, new DatasetType()));
        datasetTypeRepository
            .Setup(service => service.FetchDatasetType(It.Is<string>(baseUrl => correctUrl.StartsWith(baseUrl)),It.Is<string>(fileName => fileName == wrongFile)))
            .Returns((IDatasetTypeRepository.FetchResult.Malformed, null));
        
        SystemSettings systemSettingsViewModel = new(new RserviceStub(), configuration.Object, new LoggingStub(), datasetTypeRepository.Object)
        {
            RepositoryUrl = correctUrl,
            CollectionName = "default"
        };

        var messageSent = false;
        WeakReferenceMessenger.Default.Register<SystemSettings.DatasetTypeUrlInvalidMessage>(this, (_, _) => messageSent = true);
        
        systemSettingsViewModel.FetchDatasetTypesCommand.Execute(null);
        
        Assert.True(messageSent);
        
        // first file should have worked
        configuration.Verify(conf => conf.StoreDatasetType(It.IsAny<DatasetType>()), Times.Once);
    }
        
    [Fact]
    public void TestFetchDatasetTypesSuccessful()
    {
        var configuration = new Mock<Configuration>();

        const string correctUrl = "https://www.my_lsanalyzer_repo.com/index.json";
        
        var datasetTypeRepository = new Mock<IDatasetTypeRepository>();
        datasetTypeRepository
            .Setup(service => service.FetchDatasetTypeCollections(It.Is<string>(url => url == correctUrl)))
            .Returns((IDatasetTypeRepository.FetchResult.Success, [
                new DatasetTypeCollection
                {
                    Name = "default",
                    Entries = [
                        new DatasetTypeCollection.Entry
                        {
                            DatasetTypeId = 101,
                            FileName = "101_correct1.json",
                        },
                        new DatasetTypeCollection.Entry
                        {
                            DatasetTypeId = 102,
                            FileName = "102_correct2.json",
                        }
                    ]
                },
                new DatasetTypeCollection
                {
                    Name = "historic",
                    Entries = [
                        new DatasetTypeCollection.Entry
                        {
                            DatasetTypeId = 22,
                            FileName = "22_historic.json",
                        },
                    ]
                }
            ]));
        datasetTypeRepository
            .Setup(service => service.FetchDatasetType(It.Is<string>(baseUrl => correctUrl.StartsWith(baseUrl)),It.IsAny<string>()))
            .Returns((IDatasetTypeRepository.FetchResult.Success, new DatasetType()));
        
        SystemSettings systemSettingsViewModel = new(new RserviceStub(), configuration.Object, new LoggingStub(), datasetTypeRepository.Object)
        {
            RepositoryUrl = correctUrl,
            CollectionName = "default"
        };

        var messageSent = false;
        var count = 0;
        WeakReferenceMessenger.Default.Register<SystemSettings.FetchDatasetTypeCollectionSuccessfulMessage>(this, (_, m) =>
        {
            count = m.Count;
            messageSent = true;
        });
        
        systemSettingsViewModel.FetchDatasetTypesCommand.Execute(null);
        
        Assert.True(messageSent);
        Assert.Equal(2, count);

        configuration.Verify(conf => conf.StoreDatasetType(It.IsAny<DatasetType>()), Times.Exactly(2));
    }

    internal class FakeRservice : IRservice
    {
        private ILogging _logger;

        internal FakeRservice(ILogging logger)
        {
            _logger = logger;
        }
        
        public (string rHome, string rPath) RLocation { get; set; }
        public bool Connect()
        {
            _logger.AddEntry(new LogEntry(DateTime.Now, "options(BIFIEsurvey.quiet = TRUE)"));
            return true;
        }

        public bool IsConnected { get; }
        public string? GetRVersion()
        {
            throw new NotImplementedException();
        }

        public bool CheckNecessaryRPackages(string? packageName = null)
        {
            throw new NotImplementedException();
        }

        public bool InstallNecessaryRPackages(string? packageName = null)
        {
            throw new NotImplementedException();
        }

        public bool NecessaryPackagesConfirmed => true;
        
        public string? GetBifieSurveyVersion()
        {
            throw new NotImplementedException();
        }

        public IRservice.UpdateResult UpdateBifieSurvey()
        {
            throw new NotImplementedException();
        }

        public bool InjectAppFunctions()
        {
            _logger.AddEntry(new LogEntry(DateTime.Now, "lsanalyzer_func_quantile <- function(X, w) { }"));
            return true;
        }

        public bool LoadFileIntoGlobalEnvironment(string fileName, string? fileType = null)
        {
            throw new NotImplementedException();
        }

        public bool SortRawDataStored(string sortBy)
        {
            throw new NotImplementedException();
        }

        public bool ReplaceCharacterVariables()
        {
            throw new NotImplementedException();
        }

        public SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null)
        {
            throw new NotImplementedException();
        }

        public bool ApplySubsetting(string subsettingExpression)
        {
            throw new NotImplementedException();
        }

        public bool ReduceToNecessaryVariables(List<string> regexNecessaryVariables, string? subsettingExpression = null)
        {
            throw new NotImplementedException();
        }

        public bool ReduceToNecessaryVariables(Analysis analysis, List<string>? additionalVariables = null,
            string? subsettingExpression = null)
        {
            throw new NotImplementedException();
        }

        public bool CreateReplicateWeights(string weight, string jkzone, string jkrep, bool jkreverse)
        {
            throw new NotImplementedException();
        }

        public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, ICollection<PlausibleValueVariable>? pvvars, string? repwgts, double? fayfac,
            bool autoEncapsulatePVvars = false)
        {
            throw new NotImplementedException();
        }

        public bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, string? subsettingExpression = null)
        {
            throw new NotImplementedException();
        }

        public bool PrepareForAnalysis(Analysis analysis)
        {
            throw new NotImplementedException();
        }

        public List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration, bool fromStoredRaw = false)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateUnivar(AnalysisUnivar analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateMeanDiff(AnalysisMeanDiff analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateFreq(AnalysisFreq analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateBivariate(AnalysisFreq analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculatePercentiles(AnalysisPercentiles analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateCorr(AnalysisCorr analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateLinreg(AnalysisLinreg analysis)
        {
            throw new NotImplementedException();
        }

        public List<GenericVector>? CalculateLogistReg(AnalysisLogistReg analysis)
        {
            throw new NotImplementedException();
        }

        public List<Variable>? GetDatasetVariables(string fileName, string? fileType = null)
        {
            throw new NotImplementedException();
        }

        public DataFrame? GetValueLabels(string variable)
        {
            throw new NotImplementedException();
        }

        public bool Execute(string rCode, bool oneLiner = false)
        {
            throw new NotImplementedException();
        }

        public SymbolicExpression? Fetch(string objectName)
        {
            throw new NotImplementedException();
        }

        public void SendUserInterrupt()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
