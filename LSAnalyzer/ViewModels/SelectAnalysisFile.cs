using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

[assembly: InternalsVisibleTo("TestLSAnalyzer")]
namespace LSAnalyzer.ViewModels;

public partial class SelectAnalysisFile : ObservableObject
{
    private readonly Configuration _configuration;
    private readonly IRservice _rservice;
    private readonly IServiceProvider _serviceProvider = null!;

    private string _tabControlValue = "File system";
    public string TabControlValue
    {
        get => _tabControlValue;
        set
        {
            _tabControlValue = value;
            if (TabControlValue == "File system")
            {
                RecentFilesForAnalyses = new(_configuration.GetStoredRecentFiles(0));
            }
            
            OnPropertyChanged();

            OnPropertyChanged(nameof(ReadyToGuess));
            OnPropertyChanged(nameof(ReadyToGo));
        }
    }

    [ObservableProperty]
    private int _tabControlIndex = 0;
    
    [ObservableProperty]
    private ObservableCollection<Configuration.RecentFileForAnalysis> _recentFilesForAnalyses;
    
    [ObservableProperty]
    private Configuration.RecentFileForAnalysis? _selectedRecentFileForAnalysis;
    partial void OnSelectedRecentFileForAnalysisChanged(Configuration.RecentFileForAnalysis? value)
    {
        if (value != null && TabControlValue == "File system")
        {
            InitializeFromRecentFile(value);
        }
    }

    private string? _fileName;
    public string? FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();

            if (SelectedRecentFileForAnalysis?.FileName != null && SelectedRecentFileForAnalysis.FileName != value)
            {
                var currentRecentFilesForAnalyses = RecentFilesForAnalyses.ToList();
                RecentFilesForAnalyses = new([]);
                RecentFilesForAnalyses = new(currentRecentFilesForAnalyses);
                SelectedWeightVariable = null;
                SelectedDatasetType = null;
            }
            
            if (!String.IsNullOrWhiteSpace(FileName) && FileName.Substring(FileName.LastIndexOf(".", StringComparison.Ordinal) + 1).ToLower() == "csv")
            {
                IsCsv = true;
            } else
            {
                IsCsv = false;
            }

            OnPropertyChanged(nameof(ReadyToGuess));
            OnPropertyChanged(nameof(ReadyToGo));
        }
    }

    [ObservableProperty]
    private bool _isCsv = false;

    [ObservableProperty]
    private bool _useCsv2 = true;

    [ObservableProperty]
    private bool _replaceCharacterVectors = true;

    [ObservableProperty]
    private ObservableCollection<DatasetType> _datasetTypes = [];

    [ObservableProperty]
    private bool _showDatasetTypesGrouped = (Environment.GetEnvironmentVariable("SHOW_DATASET_TYPES_GROUPED") ?? "1") == "1";

    private CollectionViewSource? _datasetTypesView = null;
    public CollectionViewSource? DatasetTypesView 
    { 
        get
        {
            if (_datasetTypesView == null)
            {
                CollectionViewSource datasetTypesView = new();
                if (ShowDatasetTypesGrouped)
                {
                    datasetTypesView.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                }
                datasetTypesView.Source = DatasetTypes;
                _datasetTypesView = datasetTypesView;
            }

            return _datasetTypesView;
        }
    }

    private DatasetType? _selectedDatasetType;
    public DatasetType? SelectedDatasetType
    {
        get => _selectedDatasetType;
        set
        {
            _selectedDatasetType = value;
            OnPropertyChanged();

            if (SelectedDatasetType != null)
            {
                SelectedWeightVariable = null;
                List<string> possibleWeights = new();
                foreach (var weight in SelectedDatasetType.Weight.Split(";"))
                {
                    possibleWeights.Add(weight);
                }
                PossibleWeightVariables = possibleWeights;
                SelectedWeightVariable = PossibleWeightVariables.FirstOrDefault();
            }

            OnPropertyChanged(nameof(ReadyToGo));
        }
    }

    [ObservableProperty]
    private List<IDataProviderConfiguration> _dataProviderConfigurations = [];

    private IDataProviderConfiguration? _selectedDataProviderConfiguration = null;
    public IDataProviderConfiguration? SelectedDataProviderConfiguration
    {
        get => _selectedDataProviderConfiguration;
        set
        {
            _selectedDataProviderConfiguration = value;
            OnPropertyChanged();

            if (SelectedDataProviderConfiguration != null)
            {
                DataProviderViewModel = SelectedDataProviderConfiguration.GetViewModel(_serviceProvider, _configuration);
                DataProviderViewModel.ParentViewModel = this;
            } else
            {
                DataProviderViewModel = null;
            }
        }
    }

    [ObservableProperty]
    private IDataProviderViewModel? _dataProviderViewModel;

    [ObservableProperty]
    private List<string> _possibleWeightVariables = [];

    private string? _selectedWeightVariable;
    public string? SelectedWeightVariable
    {
        get => _selectedWeightVariable;
        set
        {
            _selectedWeightVariable = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReadyToGo));
        }
    }

    public enum AnalysisModes { Keep, Build }
    private AnalysisModes _selectedAnalysisMode = AnalysisModes.Keep;
    public AnalysisModes SelectedAnalysisMode
    {
        get => _selectedAnalysisMode;
        set
        {
            _selectedAnalysisMode = value;
            OnPropertyChanged();
        }
    }

    public bool ReadyToGuess => (TabControlValue == "File system" && (FileName?.Length ?? 0) > 0) || (TabControlValue == "Data provider" && (DataProviderViewModel?.IsConfigurationReady ?? false));

    public bool ReadyToGo => ReadyToGuess && SelectedDatasetType != null && SelectedWeightVariable != null;

    [ObservableProperty]
    private bool _isBusy = false;

    [ExcludeFromCodeCoverage]
    public SelectAnalysisFile()
    {
        // design-time only parameter-less constructor
        _configuration = new Configuration();
        _rservice = new RserviceStub();
        DataProviderConfigurations = [new DataverseConfiguration { Name = "My dataverse" }];
        SelectedDataProviderConfiguration = DataProviderConfigurations.First();
        RecentFilesForAnalyses = [];
    }

    public SelectAnalysisFile(Configuration configuration, IRservice rservice, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        RecentFilesForAnalyses = new(_configuration.GetStoredRecentFiles(0));
        _rservice = rservice;
        DatasetTypes = new ObservableCollection<DatasetType>(configuration.GetStoredDatasetTypes()?.OrderBy(dst => dst.Name).ToList() ?? new List<DatasetType>());
        DataProviderConfigurations = configuration.GetDataProviderConfigurations().OrderBy(dpc => dpc.Name).ToList();
        _serviceProvider = serviceProvider;
        
        if (Application.Current?.Properties.Contains("SelectAnalysisFile_TabControlIndex") ?? false)
        {
            TabControlIndex = (int)Application.Current.Properties["SelectAnalysisFile_TabControlIndex"]!;
        }

        if (Application.Current?.Properties.Contains("SelectAnalysisFile_SelectedDataProviderConfiguration_id") ?? false)
        {
            var selectedDataProviderConfigurationId =
                (int)Application.Current.Properties["SelectAnalysisFile_SelectedDataProviderConfiguration_id"]!;

            if (DataProviderConfigurations.Any(dpc => dpc.Id == selectedDataProviderConfigurationId))
            {
                SelectedDataProviderConfiguration = DataProviderConfigurations.First(dpc => dpc.Id == selectedDataProviderConfigurationId);
            }
        }
        
        if (SelectedDataProviderConfiguration == null && DataProviderConfigurations.Count == 1)
        {
            SelectedDataProviderConfiguration = DataProviderConfigurations.First();
        }
    }

    public void InitializeFromRecentFile(Configuration.RecentFileForAnalysis recentFileForAnalysis)
    {
        if (!File.Exists(recentFileForAnalysis.FileName) || 
            DatasetTypes.All(dst => dst.Id != recentFileForAnalysis.DatasetTypeId) ||
            !DatasetTypes.First(dst => dst.Id == recentFileForAnalysis.DatasetTypeId).Weight.Split(';').Contains(recentFileForAnalysis.Weight))
        {
            _configuration.RemoveRecentFile(recentFileForAnalysis);
            RecentFilesForAnalyses = new(_configuration.GetStoredRecentFiles(0));
            
            WeakReferenceMessenger.Default.Send(new RecentFileInvalidMessage(recentFileForAnalysis.FileName));
            
            return;
        }
        
        FileName = recentFileForAnalysis.FileName;
        if (recentFileForAnalysis.UsageAttributes.TryGetValue("UseCsv2", out var useCsv2) && useCsv2 is JsonElement
            {
                ValueKind: JsonValueKind.True or JsonValueKind.False
            } useCsv2Element)
        {
            UseCsv2 = useCsv2Element.GetBoolean();
        }
        
        ReplaceCharacterVectors = recentFileForAnalysis.ConvertCharacters;
        SelectedDatasetType = DatasetTypes.First(dst => dst.Id == recentFileForAnalysis.DatasetTypeId);
        SelectedWeightVariable = recentFileForAnalysis.Weight;
        SelectedAnalysisMode = recentFileForAnalysis.ModeKeep ? AnalysisModes.Keep : AnalysisModes.Build;
    }

    public void NotifyReadyState()
    {
        OnPropertyChanged(nameof(ReadyToGuess));
        OnPropertyChanged(nameof(ReadyToGo));
    }

    [RelayCommand]
    private void GuessDatasetType(object? dummy)
    {
        if (string.IsNullOrWhiteSpace(FileName) && !(DataProviderViewModel?.IsConfigurationReady ?? false))
        {
            return;
        }

        IsBusy = true;

        BackgroundWorker guessDatasetTypeWorker = new();
        guessDatasetTypeWorker.WorkerReportsProgress = false;
        guessDatasetTypeWorker.WorkerSupportsCancellation = false;
        guessDatasetTypeWorker.DoWork += GuessDatasetTypeWorker_DoWork;
        guessDatasetTypeWorker.RunWorkerCompleted += (_, e) =>
        {
            switch (e.Result)
            {
                case FailureAnalysisFileMessage failureAnalysisFileMessage:
                    WeakReferenceMessenger.Default.Send(failureAnalysisFileMessage);
                    break;
                case FailureDataProviderMessage failureDataProviderMessage:
                    WeakReferenceMessenger.Default.Send(failureDataProviderMessage);
                    break;
                case MissingRPackageMessage missingRPackageMessage:
                    WeakReferenceMessenger.Default.Send(missingRPackageMessage);
                    break;
                case MultiplePossibleDatasetTypesMessage multiplePossibleDatasetTypesMessage:
                    WeakReferenceMessenger.Default.Send(multiplePossibleDatasetTypesMessage);
                    break;
            }
        };
        guessDatasetTypeWorker.RunWorkerAsync();
    }

    private void GuessDatasetTypeWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        List<Variable> variables;

        if (TabControlValue == "Data provider" && DataProviderViewModel != null)
        {
            variables = DataProviderViewModel.LoadDataTemporarilyAndGetVariables();
            if (variables.Count == 0)
            {
                e.Result = new FailureDataProviderMessage();
                IsBusy = false;
                return;
            }
        } else
        {
            var fileTypeFromFile = FileName!.Substring(FileName!.LastIndexOf('.') + 1);
            if (fileTypeFromFile.ToLower() == "xlsx" && !_rservice.CheckNecessaryRPackages("openxlsx"))
            {
                e.Result = new MissingRPackageMessage("openxlsx");
                IsBusy = false;
                return;
            }

            variables = _rservice.GetDatasetVariables(FileName!, IsCsv && UseCsv2 ? "csv2" : null) ?? new();
            if (variables.Count == 0)
            {
                e.Result = new FailureAnalysisFileMessage(FileName!);
                IsBusy = false;
                return;
            }
        }

        List<DatasetType> possibleDatasetTypes = new();
        int maxPriority = 0;

        foreach (var datasetType in DatasetTypes)
        {
            var priority = 0;

            var weightVariables = datasetType.Weight.Split(";");
            var foundAllWeightVariables = weightVariables.All(weightVariable => variables.Any(var => var.Name == weightVariable));
            if (!foundAllWeightVariables)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(datasetType.MIvar) && !variables.Any(var => var.Name == datasetType.MIvar)) continue;
            if (!string.IsNullOrWhiteSpace(datasetType.IDvar) && !variables.Any(var => var.Name == datasetType.IDvar)) continue;

            var foundAllNecessaryPvVars = true;
            foreach (var pvVar in datasetType.PVvarsList.Where(pvvar => pvvar.Mandatory).Select(pvvar => pvvar.Regex))
            {
                if (variables.Where(var => Regex.IsMatch(var.Name, StringFormats.EncapsulateRegex(pvVar, datasetType.AutoEncapsulateRegex)!)).Count() != datasetType.NMI)
                {
                    foundAllNecessaryPvVars = false;
                    break;
                }
            }
            if (!foundAllNecessaryPvVars) continue;

            if (!string.IsNullOrWhiteSpace(datasetType.RepWgts))
            {
                if (!variables.Any(var => Regex.IsMatch(var.Name, StringFormats.EncapsulateRegex(datasetType.RepWgts, datasetType.AutoEncapsulateRegex)!)))
                {
                    continue;
                }

                priority++;
            }

            if (!string.IsNullOrWhiteSpace(datasetType.JKzone) && !variables.Any(var => var.Name == datasetType.JKzone)) continue;
            if (!string.IsNullOrWhiteSpace(datasetType.JKrep) && !variables.Any(var => var.Name == datasetType.JKrep)) continue;

            if (priority == maxPriority)
            {
                possibleDatasetTypes.Add(datasetType);
            } else if (priority > maxPriority)
            {
                possibleDatasetTypes = [datasetType];
                maxPriority = priority;
            }
        }

        if (possibleDatasetTypes.Count == 0)
        {
            SelectedDatasetType = null;
        }
        else if (possibleDatasetTypes.Count == 1)
        {
            SelectedDatasetType = possibleDatasetTypes.First();
        }
        else
        {
            SelectedDatasetType = null;
            e.Result = new MultiplePossibleDatasetTypesMessage(possibleDatasetTypes);
        }

        IsBusy = false;
    }

    [RelayCommand]
    private void UseFileForAnalysis(ICloseable? window)
    {
        if ((string.IsNullOrWhiteSpace(FileName) && !(DataProviderViewModel?.IsConfigurationReady ?? false)) || SelectedDatasetType == null)
        {
            return;
        }

        IsBusy = true;

        BackgroundWorker useFileForAnalysisWorker = new();
        useFileForAnalysisWorker.WorkerReportsProgress = false;
        useFileForAnalysisWorker.WorkerSupportsCancellation = false;
        useFileForAnalysisWorker.DoWork += UseFileForAnalysisWorker_DoWork;
        useFileForAnalysisWorker.RunWorkerCompleted += (_, e) =>
        {
            switch (e.Result)
            {
                case FailureAnalysisConfigurationMessage failureAnalysisConfigurationMessage:
                    WeakReferenceMessenger.Default.Send(failureAnalysisConfigurationMessage);
                    break;
                case FailureDataProviderMessage failureDataProviderMessage:
                    WeakReferenceMessenger.Default.Send(failureDataProviderMessage);
                    break;
                case MissingRPackageMessage missingRPackageMessage:
                    WeakReferenceMessenger.Default.Send(missingRPackageMessage);
                    break;
                case SetAnalysisConfigurationMessage setAnalysisConfigurationMessage:
                    WeakReferenceMessenger.Default.Send(setAnalysisConfigurationMessage);
                    window?.Close();
                    break;
            }
        };
        useFileForAnalysisWorker.RunWorkerAsync();
    }

    private void UseFileForAnalysisWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = TabControlValue == "Data provider" ? "[" + (DataProviderViewModel?.FileInformation ?? string.Empty) + "]" : FileName,
            FileRetrieval = TabControlValue == "Data provider" ? (DataProviderViewModel?.SerializeFileRetrieval() ?? string.Empty) : null,
            FileType = IsCsv && UseCsv2 ? "csv2" : null,
            DatasetType = SelectedDatasetType != null ? new(SelectedDatasetType) : null,
            ModeKeep = (SelectedAnalysisMode == AnalysisModes.Keep),
            ReplaceCharacterVectors = ReplaceCharacterVectors,
        };
        if (analysisConfiguration.DatasetType != null)
        {
            analysisConfiguration.DatasetType.Weight = SelectedWeightVariable ?? String.Empty;
        }

        Configuration.RecentFileForAnalysis recentFileForAnalysis = new()
        {
            ConvertCharacters = ReplaceCharacterVectors,
            DatasetTypeId = analysisConfiguration.DatasetType!.Id,
            Weight = analysisConfiguration.DatasetType.Weight,
            ModeKeep = (SelectedAnalysisMode == AnalysisModes.Keep),
        };

        if (TabControlValue == "Data provider" && DataProviderViewModel != null)
        {
            recentFileForAnalysis.FileName = DataProviderViewModel.SerializeFileInformation();
            recentFileForAnalysis.UsageAttributes = DataProviderViewModel.GetUsageAttributes();
            
            if (!DataProviderViewModel.LoadDataForUsage())
            {
                e.Result = new FailureDataProviderMessage();
                IsBusy = false;
                return;
            }
        } else
        {
            recentFileForAnalysis.FileName = analysisConfiguration.FileName!;
            recentFileForAnalysis.UsageAttributes.Add("UseCsv2", UseCsv2);
            
            var fileTypeFromFile = FileName!.Substring(FileName.LastIndexOf('.') + 1);
            if (fileTypeFromFile.ToLower() == "xlsx" && !_rservice.CheckNecessaryRPackages("openxlsx"))
            {
                e.Result = new MissingRPackageMessage("openxlsx");
                IsBusy = false;
                return;
            }

            if (!_rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName ?? string.Empty, analysisConfiguration.FileType))
            {
                e.Result = new FailureAnalysisConfigurationMessage(analysisConfiguration);
                IsBusy = false;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(analysisConfiguration.DatasetType?.IDvar) && !_rservice.SortRawDataStored(analysisConfiguration.DatasetType.IDvar))
        {
            e.Result = new FailureAnalysisConfigurationMessage(analysisConfiguration);
            IsBusy = false;
            return;
        }

        if (analysisConfiguration.ReplaceCharacterVectors && !_rservice.ReplaceCharacterVariables())
        {
            e.Result = new FailureAnalysisConfigurationMessage(analysisConfiguration);
            IsBusy = false;
            return;
        }
        
        var virtualVariables = _configuration.GetVirtualVariablesFor(analysisConfiguration.FileNameWithoutPath!, analysisConfiguration.DatasetType!);

        var testAnalysisConfiguration = _rservice.TestAnalysisConfiguration(analysisConfiguration, virtualVariables);

        if (!testAnalysisConfiguration)
        {
            e.Result = new FailureAnalysisConfigurationMessage(analysisConfiguration);
            IsBusy = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(recentFileForAnalysis.FileName))
        {
            _configuration.StoreRecentFile(TabControlValue == "File system" ? 0 : SelectedDataProviderConfiguration?.Id ?? -1, recentFileForAnalysis);
        }

        e.Result = new SetAnalysisConfigurationMessage(analysisConfiguration);
        IsBusy = false;
        if (Application.Current != null)
        {
            Application.Current.Properties["SelectAnalysisFile_TabControlIndex"] = TabControlIndex;
            Application.Current.Properties["SelectAnalysisFile_SelectedDataProviderConfiguration_id"] =
                SelectedDataProviderConfiguration?.Id ?? -1;
        }
    }

    public bool InstallOpenXlsx()
    {
        return _rservice.InstallNecessaryRPackages("openxlsx");
    }
}


internal class MultiplePossibleDatasetTypesMessage : ValueChangedMessage<List<DatasetType>>
{
    public MultiplePossibleDatasetTypesMessage(List<DatasetType> possibleDatasetTypes) : base(possibleDatasetTypes)
    {

    }
}

internal class SetAnalysisConfigurationMessage : ValueChangedMessage<AnalysisConfiguration>
{
    public SetAnalysisConfigurationMessage(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
    {

    }
}

internal class FailureAnalysisFileMessage : ValueChangedMessage<string>
{
    public FailureAnalysisFileMessage(string fileName) : base(fileName)
    {

    }
}

internal class FailureDataProviderMessage
{

}

internal class FailureAnalysisConfigurationMessage : ValueChangedMessage<AnalysisConfiguration>
{
    public FailureAnalysisConfigurationMessage(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
    {

    }
}

internal class RecentFileInvalidMessage(string fileName)
{
    public string FileName => fileName;
};
