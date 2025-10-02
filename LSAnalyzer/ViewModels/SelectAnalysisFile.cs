using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

[assembly: InternalsVisibleTo("TestLSAnalyzer")]
namespace LSAnalyzer.ViewModels;

public partial class SelectAnalysisFile : ObservableObject, INotifyPropertyChanged
{
    private Configuration _configuration;
    private Rservice _rservice;
    private readonly IServiceProvider _serviceProvider;

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
            
            NotifyPropertyChanged(nameof(TabControlValue));

            NotifyPropertyChanged(nameof(ReadyToGuess));
            NotifyPropertyChanged(nameof(ReadyToGo));
        }
    }

    private ObservableCollection<Configuration.RecentFileForAnalysis> _recentFilesForAnalyses;
    public ObservableCollection<Configuration.RecentFileForAnalysis> RecentFilesForAnalyses
    {
        get => _recentFilesForAnalyses;
        set
        {
            _recentFilesForAnalyses = value;
            NotifyPropertyChanged();
        }
    }
    
    [ObservableProperty]
    private Configuration.RecentFileForAnalysis? _selectedRecentFileForAnalysis;
    partial void OnSelectedRecentFileForAnalysisChanged(Configuration.RecentFileForAnalysis? value)
    {
        if (value != null)
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
            NotifyPropertyChanged(nameof(FileName));
            
            if (!String.IsNullOrWhiteSpace(FileName) && FileName.Substring(FileName.LastIndexOf(".") + 1).ToLower() == "csv")
            {
                IsCsv = true;
            } else
            {
                IsCsv = false;
            }

            NotifyPropertyChanged(nameof(ReadyToGuess));
            NotifyPropertyChanged(nameof(ReadyToGo));
        }
    }

    private bool _isCsv = false;
    public bool IsCsv
    {
        get => _isCsv;
        set
        {
            _isCsv = value;
            NotifyPropertyChanged(nameof(IsCsv));
        }
    }

    private bool _useCsv2 = true;
    public bool UseCsv2
    {
        get => _useCsv2;
        set
        {
            _useCsv2 = value;
            NotifyPropertyChanged(nameof(UseCsv2));
        }
    }

    private bool _replaceCharacterVectors = true;
    public bool ReplaceCharacterVectors
    {
        get => _replaceCharacterVectors;
        set
        {
            _replaceCharacterVectors = value;
            NotifyPropertyChanged(nameof(ReplaceCharacterVectors));
        }
    }

    private ObservableCollection<DatasetType> _datasetTypes = new();
    public ObservableCollection<DatasetType> DatasetTypes
    {
        get => _datasetTypes;
        set
        {
            _datasetTypes = value;
            NotifyPropertyChanged(nameof(DatasetTypes));
        }
    }

    private bool _showDatasetTypesGrouped = (Environment.GetEnvironmentVariable("SHOW_DATASET_TYPES_GROUPED") ?? "1") == "1";
    public bool ShowDatasetTypesGrouped
    {
        get => _showDatasetTypesGrouped;
        set
        {
            _showDatasetTypesGrouped = value;
            NotifyPropertyChanged(nameof(ShowDatasetTypesGrouped));
        }
    }

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
            NotifyPropertyChanged(nameof(SelectedDatasetType));

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

            NotifyPropertyChanged(nameof(ReadyToGo));
        }
    }

    private List<IDataProviderConfiguration> _dataProviderConfigurations = new();
    public List<IDataProviderConfiguration> DataProviderConfigurations
    {
        get => _dataProviderConfigurations;
        set
        {
            _dataProviderConfigurations = value;
            NotifyPropertyChanged(nameof(DataProviderConfigurations));
        }
    }

    private IDataProviderConfiguration? _selectedDataProviderConfiguration = null;
    public IDataProviderConfiguration? SelectedDataProviderConfiguration
    {
        get => _selectedDataProviderConfiguration;
        set
        {
            _selectedDataProviderConfiguration = value;
            NotifyPropertyChanged(nameof(SelectedDataProviderConfiguration));

            if (SelectedDataProviderConfiguration != null)
            {
                DataProviderViewModel = SelectedDataProviderConfiguration.GetViewModel(_serviceProvider);
                DataProviderViewModel.ParentViewModel = this;
            } else
            {
                DataProviderViewModel = null;
            }
        }
    }

    private IDataProviderViewModel? _dataProviderViewModel;
    public IDataProviderViewModel? DataProviderViewModel
    {
        get => _dataProviderViewModel;
        set
        {
            _dataProviderViewModel = value;
            NotifyPropertyChanged(nameof(DataProviderViewModel));
        }
    }

    private List<string> _possibleWeightVariables;
    public List<string> PossibleWeightVariables
    {
        get => _possibleWeightVariables;
        set
        {
            _possibleWeightVariables = value;
            NotifyPropertyChanged(nameof(PossibleWeightVariables));
        }
    }

    private string? _selectedWeightVariable;
    public string? SelectedWeightVariable
    {
        get => _selectedWeightVariable;
        set
        {
            _selectedWeightVariable = value;
            NotifyPropertyChanged(nameof(SelectedWeightVariable));
            NotifyPropertyChanged(nameof(ReadyToGo));
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
            NotifyPropertyChanged(nameof(SelectedAnalysisMode));
        }
    }

    public bool ReadyToGuess
    {
        get => (TabControlValue == "File system" && (FileName?.Length ?? 0) > 0) || (TabControlValue == "Data provider" && (DataProviderViewModel?.IsConfigurationReady ?? false));
    }

    public bool ReadyToGo
    {
        get => ReadyToGuess && SelectedDatasetType != null && SelectedWeightVariable != null;
    }

    private bool _busy = false;
    public bool IsBusy
    {
        get => _busy;
        set
        {
            _busy = value;
            NotifyPropertyChanged(nameof(IsBusy));
        }
    }

    [ExcludeFromCodeCoverage]
    public SelectAnalysisFile()
    {
        // design-time only parameter-less constructor
        DataProviderConfigurations = new()
        {
            new DataverseConfiguration() { Name = "My dataverse" }
        };
        SelectedDataProviderConfiguration = DataProviderConfigurations.First();
    }

    public SelectAnalysisFile(Configuration configuration, Rservice rservice, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        RecentFilesForAnalyses = new(_configuration.GetStoredRecentFiles(0));
        _rservice = rservice;
        DatasetTypes = new ObservableCollection<DatasetType>(configuration.GetStoredDatasetTypes()?.OrderBy(dst => dst.Name).ToList() ?? new List<DatasetType>());
        DataProviderConfigurations = configuration.GetDataProviderConfigurations().OrderBy(dpc => dpc.Name).ToList();
        _serviceProvider = serviceProvider;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public void InitializeFromRecentFile(Configuration.RecentFileForAnalysis recentFileForAnalysis)
    {
        if (TabControlValue == "File system")
        {
            InitializeFromRecentFileFileSystem(recentFileForAnalysis);
        }
    }

    private void InitializeFromRecentFileFileSystem(Configuration.RecentFileForAnalysis recentFileForAnalysis)
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
        if (recentFileForAnalysis.UsageAttributes.TryGetValue("UseCsv2", out var useCsv2) && useCsv2 is bool)
        {
            UseCsv2 = (bool)useCsv2;
        }
        ReplaceCharacterVectors = recentFileForAnalysis.ConvertCharacters;
        SelectedDatasetType = DatasetTypes.First(dst => dst.Id == recentFileForAnalysis.DatasetTypeId);
        SelectedWeightVariable = recentFileForAnalysis.Weight;
        SelectedAnalysisMode = recentFileForAnalysis.ModeKeep ? AnalysisModes.Keep : AnalysisModes.Build;
    }

    public void NotifyReadyState()
    {
        NotifyPropertyChanged(nameof(ReadyToGuess));
        NotifyPropertyChanged(nameof(ReadyToGo));
    }

    private RelayCommand<object?> _guessDatasetTypeCommand;
    public ICommand GuessDatasetTypeCommand
    {
        get
        {
            if (_guessDatasetTypeCommand == null)
                _guessDatasetTypeCommand = new RelayCommand<object?>(this.GuessDatasetType);
            return _guessDatasetTypeCommand;
        }
    }

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
        guessDatasetTypeWorker.RunWorkerAsync();
    }

    private void GuessDatasetTypeWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        List<Variable> variables = new();

        if (TabControlValue == "Data provider" && DataProviderViewModel != null)
        {
            variables = DataProviderViewModel.LoadDataTemporarilyAndGetVariables();
            if (variables.Count == 0)
            {
                WeakReferenceMessenger.Default.Send(new FailureDataProviderMessage());
                IsBusy = false;
                return;
            }
        } else
        {
            var fileTypeFromFile = FileName!.Substring(FileName!.LastIndexOf('.') + 1);
            if (fileTypeFromFile.ToLower() == "xlsx" && !_rservice.CheckNecessaryRPackages("openxlsx"))
            {
                WeakReferenceMessenger.Default.Send(new MissingRPackageMessage("openxlsx"));
                IsBusy = false;
                return;
            }

            variables = _rservice.GetDatasetVariables(FileName!, IsCsv && UseCsv2 ? "csv2" : null) ?? new();
            if (variables.Count == 0)
            {
                WeakReferenceMessenger.Default.Send(new FailureAnalysisFileMessage(FileName!));
                IsBusy = false;
                return;
            }
        }

        List<DatasetType> possibleDatasetTypes = new();
        int maxPriority = 0;

        foreach (var datasetType in _datasetTypes)
        {
            int priority = 0;
                
            bool foundAllWeightVariables = true;
            var weightVariables = datasetType.Weight.Split(";");
            foreach (var weightVariable in weightVariables)
            {
                if (!variables.Where(var => var.Name == weightVariable).Any())
                {
                    foundAllWeightVariables = false;
                    break;
                }
            }
            if (!foundAllWeightVariables)
            {
                continue;
            }

            if (!String.IsNullOrWhiteSpace(datasetType.MIvar) && !variables.Where(var => var.Name == datasetType.MIvar).Any()) continue;
            if (!String.IsNullOrWhiteSpace(datasetType.IDvar) && !variables.Where(var => var.Name == datasetType.IDvar).Any()) continue;

            bool foundAllNecessaryPvVars = true;
            foreach (var pvVar in datasetType.PVvarsList.Where(pvvar => pvvar.Mandatory).Select(pvvar => pvvar.Regex))
            {
                if (variables.Where(var => Regex.IsMatch(var.Name, StringFormats.EncapsulateRegex(pvVar, datasetType.AutoEncapsulateRegex)!)).Count() != datasetType.NMI)
                {
                    foundAllNecessaryPvVars = false;
                    break;
                }
            }
            if (!foundAllNecessaryPvVars) continue;

            if (!String.IsNullOrWhiteSpace(datasetType.RepWgts))
            {
                if (!variables.Where(var => Regex.IsMatch(var.Name, StringFormats.EncapsulateRegex(datasetType.RepWgts, datasetType.AutoEncapsulateRegex)!)).Any())
                {
                    continue;
                } else
                {
                    priority++;
                }
            }

            if (!String.IsNullOrWhiteSpace(datasetType.JKzone) && !variables.Where(var => var.Name == datasetType.JKzone).Any()) continue;
            if (!String.IsNullOrWhiteSpace(datasetType.JKrep) && !variables.Where(var => var.Name == datasetType.JKrep).Any()) continue;

            if (priority == maxPriority)
            {
                possibleDatasetTypes.Add(datasetType);
            } else if (priority > maxPriority)
            {
                possibleDatasetTypes = new() { datasetType };
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
            WeakReferenceMessenger.Default.Send(new MultiplePossibleDatasetTypesMessage(possibleDatasetTypes));
        }

        IsBusy = false;
    }

    private RelayCommand<ICloseable?> _useFileForAnalysisCommand;
    public ICommand UseFileForAnalysisCommand
    {
        get
        {
            if (_useFileForAnalysisCommand == null)
                _useFileForAnalysisCommand = new RelayCommand<ICloseable?>(this.UseFileForAnalysis);
            return _useFileForAnalysisCommand;
        }
    }

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
        useFileForAnalysisWorker.RunWorkerCompleted += UseFileForAnalysisWorker_Completed;
        useFileForAnalysisWorker.RunWorkerAsync(window);
    }

    private void UseFileForAnalysisWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            FileName = string.IsNullOrWhiteSpace(FileName) ? "[" + DataProviderViewModel!.FileInformation + "]" : FileName,
            FileType = IsCsv && UseCsv2 ? "csv2" : null,
            DatasetType = SelectedDatasetType != null ? new(SelectedDatasetType) : null,
            ModeKeep = (SelectedAnalysisMode == AnalysisModes.Keep),
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
            if (!DataProviderViewModel.LoadDataForUsage())
            {
                WeakReferenceMessenger.Default.Send(new FailureDataProviderMessage());
                IsBusy = false;
                return;
            }
        } else
        {
            recentFileForAnalysis.FileName = analysisConfiguration.FileName;
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
                WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
                IsBusy = false;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(analysisConfiguration.DatasetType?.IDvar) && !_rservice.SortRawDataStored(analysisConfiguration.DatasetType.IDvar))
        {
            WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
            IsBusy = false;
            return;
        }

        var testAnalysisConfiguration = _rservice.TestAnalysisConfiguration(analysisConfiguration);

        if (!testAnalysisConfiguration)
        {
            WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
            IsBusy = false;
            return;
        }

        if (ReplaceCharacterVectors && !_rservice.ReplaceCharacterVariables())
        {
            WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
            IsBusy = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(recentFileForAnalysis.FileName))
        {
            _configuration.StoreRecentFile(0, recentFileForAnalysis);
        }
        
        WeakReferenceMessenger.Default.Send(new SetAnalysisConfigurationMessage(analysisConfiguration));
        IsBusy = false;

        if (e.Argument is ICloseable window)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                window.Close();
            });
        }
    }

    private void UseFileForAnalysisWorker_Completed(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Result is MissingRPackageMessage)
        {
            WeakReferenceMessenger.Default.Send(e.Result);
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
