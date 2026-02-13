using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels;

public partial class SystemSettings : ObservableValidatorExtended, IChangeTracking
{
    private readonly IRservice _rservice;
    private readonly ILogging _logger;
    private readonly Configuration _configuration;
    private readonly IDatasetTypeRepository _datasetTypeRepository;

    [ObservableProperty] private string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    [ObservableProperty] private int _countConfiguredDatasetTypes;

    [ObservableProperty] private string _repositoryUrl = "https://konradoberwimmer.github.io/LSAnalyzer/index.json";
    
    [ObservableProperty] private string _collectionName = "default";
    
    [ObservableProperty]
    private bool _showLabelsDefault = Properties.Settings.Default.showLabelsDefault;
    partial void OnShowLabelsDefaultChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    private bool _storedShowLabelsDefault = Properties.Settings.Default.showLabelsDefault;

    [ObservableProperty]
    private bool _confirmRemovingAnalysis = Properties.Settings.Default.confirmRemovingAnalysis;
    partial void OnConfirmRemovingAnalysisChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    private bool _storedConfirmRemovingAnalysis = Properties.Settings.Default.confirmRemovingAnalysis;
    
    [ObservableProperty]
    private ExportType _defaultExportType = 
        AnalysisPresentation.ExportTypes.First(e => e.Name == Properties.Settings.Default.defaultExportType);
    partial void OnDefaultExportTypeChanged(ExportType value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    private ExportType _storedDefaultExportType = 
        AnalysisPresentation.ExportTypes.First(e => e.Name == Properties.Settings.Default.defaultExportType);
    
    [Range(0, int.MaxValue)]
    [ObservableProperty]
    private int _numberRecentFiles = Properties.Settings.Default.numberRecentFiles;
    partial void OnNumberRecentFilesChanged(int value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    private int _storedNumberRecentFiles = Properties.Settings.Default.numberRecentFiles;
    
    [Range(0, int.MaxValue)]
    [ObservableProperty]
    private int _numberRecentSubsettingExpressions = Properties.Settings.Default.numberRecentSubsettingExpressions;
    partial void OnNumberRecentSubsettingExpressionsChanged(int value)
    {
        OnPropertyChanged(nameof(IsChanged));
    }
    private int _storedNumberRecentSubsettingExpressions = Properties.Settings.Default.numberRecentSubsettingExpressions;

    [ObservableProperty] private string? _alternativeRLocation;
    
    [ObservableProperty] private string? _rVersion;

    [ObservableProperty] private string? _bifieSurveyVersion;

    private ObservableCollection<LogEntry> _sessionLog;
    public ObservableCollection<LogEntry> SessionLog
    {
        get => _sessionLog;
        set
        {
            _sessionLog = value;
            OnPropertyChanged();
        }
    }

    public bool ConnectedToR => _rservice.IsConnected;

    public string BIFIEsurveyButtonTitle => _rservice.NecessaryPackagesConfirmed ? "Update" : "Install";
    
    [ExcludeFromCodeCoverage]
    public SystemSettings() 
    {
        // design-time only, parameterless constructor
        _rservice = new RserviceStub();
        _configuration = new("", null, new SettingsServiceStub(), new RegistryServiceStub());
        _datasetTypeRepository = new DatasetTypeRepositoryStub();
        AlternativeRLocation = string.Empty;
        RVersion = "R version 4.3.1";
        BifieSurveyVersion = "3.4-15";
        CountConfiguredDatasetTypes = 12;
        _logger = new LoggingStub();
        _logger.AddEntry(new LogEntry(DateTime.Now, "stats::sd(c(1,2,3))"));
        _logger.AddEntry(new LogEntry(DateTime.Now, "rm(dummy_result)"));
        _sessionLog = new(_logger.LogEntries);
    }

    public SystemSettings(IRservice rservice, Configuration configuration, ILogging logger, IDatasetTypeRepository datasetTypeRepository)
    {
        _rservice = rservice;
        RVersion = _rservice.IsConnected ? _rservice.GetRVersion() : "---";
        BifieSurveyVersion = _rservice.IsConnected ?_rservice.GetBifieSurveyVersion() : "---";
        _configuration = configuration;
        _datasetTypeRepository = datasetTypeRepository;
        AlternativeRLocation = _configuration.GetStoredRLocation();
        CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;
        _logger = logger;
        _sessionLog = new(_logger.LogEntries);
    }

    [RelayCommand]
    private void FetchDatasetTypes()
    {
        if (string.IsNullOrEmpty(RepositoryUrl) || string.IsNullOrEmpty(CollectionName)) return;

        var (fetchResult, datasetTypeCollections) = _datasetTypeRepository.FetchDatasetTypeCollections(RepositoryUrl);

        if (fetchResult is IDatasetTypeRepository.FetchResult.NotFound or IDatasetTypeRepository.FetchResult.Malformed)
        {
            WeakReferenceMessenger.Default.Send(new DatasetTypeRepositoryUrlInvalidMessage { Url = RepositoryUrl });
            return;
        }

        if (datasetTypeCollections.All(collection => collection.Name != CollectionName))
        {
            WeakReferenceMessenger.Default.Send(new CollectionNotInDatasetTypeRepositoryMessage { ValidNames = datasetTypeCollections.Select(collection => collection.Name).ToList() });
            return;
        }

        var baseUrl = RepositoryUrl[..(RepositoryUrl.LastIndexOf('/') + 1)];

        foreach (var entry in datasetTypeCollections.First(collection => collection.Name == CollectionName).Entries)
        {
            var fetchDatasetType = _datasetTypeRepository.FetchDatasetType(baseUrl, entry.FileName);

            if (fetchDatasetType.result is IDatasetTypeRepository.FetchResult.NotFound
                or IDatasetTypeRepository.FetchResult.Malformed)
            {
                WeakReferenceMessenger.Default.Send(new DatasetTypeUrlInvalidMessage { Url = new Uri(new Uri(baseUrl), entry.FileName).ToString() });
                return;
            }

            var datasetType = fetchDatasetType.datasetType!;
            
            _configuration.RemoveDatasetType(datasetType);
            _configuration.RemoveRecentSubsettingExpressions(datasetType.Id);
            _configuration.StoreDatasetType(datasetType);
        }
        
        CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;

        WeakReferenceMessenger.Default.Send(new FetchDatasetTypeCollectionSuccessfulMessage
            { Count = datasetTypeCollections.First(collection => collection.Name == CollectionName).Entries.Count });
    }
    
    [RelayCommand]
    private void LoadDefaultDatasetTypes(object? dummy)
    {
        foreach (var defaultDatasetType in DatasetType.CreateDefaultDatasetTypes())
        {
            _configuration.RemoveDatasetType(defaultDatasetType);
            _configuration.RemoveRecentSubsettingExpressions(defaultDatasetType.Id);
            _configuration.StoreDatasetType(defaultDatasetType);
        }

        CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;

        WeakReferenceMessenger.Default.Send(new LoadedDefaultDatasetTypesMessage());
    }

    [RelayCommand]
    private void SaveSettings()
    {
        if (!Validate())
        {
            return;
        }

        Properties.Settings.Default.showLabelsDefault = ShowLabelsDefault;
        Properties.Settings.Default.confirmRemovingAnalysis = ConfirmRemovingAnalysis;
        Properties.Settings.Default.defaultExportType = DefaultExportType.Name;
        Properties.Settings.Default.numberRecentFiles = NumberRecentFiles;
        Properties.Settings.Default.numberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
        Properties.Settings.Default.Save();
        
        AcceptChanges();
        _configuration.TrimRecentFiles(NumberRecentFiles);
        _configuration.TrimRecentBatchAnalyzeFiles(NumberRecentFiles);
        _configuration.TrimRecentSubsettingExpressions(NumberRecentSubsettingExpressions);
        
        WeakReferenceMessenger.Default.Send<SavedSettingsMessage>();
    }

    [RelayCommand]
    private void ClearAlternativeRLocation()
    {
        AlternativeRLocation = string.Empty;
        _configuration.SetAlternativeRLocation(string.Empty);
        WeakReferenceMessenger.Default.Send(new RequestRestartMessage());
    }
    
    [RelayCommand]
    private void SetAlternativeRLocation(string alternativeRLocation)
    {
        if (!Path.Exists(alternativeRLocation) || !File.Exists(Path.Combine(alternativeRLocation, "bin", "x64", "R.dll")))
        {
            WeakReferenceMessenger.Default.Send(new ImpossibleRLocationMessage { Path = alternativeRLocation });
            return;
        }
        
        AlternativeRLocation = alternativeRLocation;
        _configuration.SetAlternativeRLocation(alternativeRLocation);
        WeakReferenceMessenger.Default.Send(new RequestRestartMessage());
    }
    
    [RelayCommand]
    private void UpdateBifieSurvey(object? dummy)
    {
        if (!_rservice.NecessaryPackagesConfirmed)
        {
            if (_rservice.InstallNecessaryRPackages())
            {
                WeakReferenceMessenger.Default.Send(new RequestRestartMessage());
            }
            
            return;
        }
        
        var result = _rservice.UpdateBifieSurvey();

        switch (result)
        {
            case IRservice.UpdateResult.Unavailable:
                MessageBox.Show("Already at latest version.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            case IRservice.UpdateResult.Failure:
                MessageBox.Show("Something went wrong trying to update BIFIEsurvey!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                break;
            case IRservice.UpdateResult.Success:
                BifieSurveyVersion = _rservice.GetBifieSurveyVersion();
                MessageBox.Show("Update successful, now at version '" + BifieSurveyVersion + "'.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    [RelayCommand]
    private void SaveSessionLog(string? filename)
    {
        if (filename == null)
        {
            return;
        }

        using StreamWriter streamWriter = new(filename, false);
        streamWriter.Write(_logger.GetFullText());
    }

    [RelayCommand]
    private void SaveSessionRcode(string? filename)
    {
        if (filename == null)
        {
            return;
        }

        using StreamWriter streamWriter = new(filename, false);
        streamWriter.Write(_logger.GetRcode());
    }

    public void AcceptChanges()
    {
        _storedShowLabelsDefault = ShowLabelsDefault;
        _storedConfirmRemovingAnalysis = ConfirmRemovingAnalysis;
        _storedDefaultExportType = DefaultExportType;
        _storedNumberRecentFiles = NumberRecentFiles;
        _storedNumberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
        OnPropertyChanged(nameof(IsChanged));
    }

    public bool IsChanged => 
        ShowLabelsDefault != _storedShowLabelsDefault ||
        ConfirmRemovingAnalysis != _storedConfirmRemovingAnalysis ||
        !DefaultExportType.Equals(_storedDefaultExportType) ||
        NumberRecentSubsettingExpressions != _storedNumberRecentSubsettingExpressions ||
        NumberRecentFiles != _storedNumberRecentFiles;
    
    public class DatasetTypeRepositoryUrlInvalidMessage { public required string Url { init; get; } }
    
    public class CollectionNotInDatasetTypeRepositoryMessage { public required List<string> ValidNames { init; get; } }
    
    public class DatasetTypeUrlInvalidMessage { public required string Url { init; get; } }
    
    public class FetchDatasetTypeCollectionSuccessfulMessage { public int Count { init; get; } }
    
    public class LoadedDefaultDatasetTypesMessage { }

    public class SavedSettingsMessage { }

    public class ImpossibleRLocationMessage { public required string Path { init; get; } }

    public class RequestRestartMessage { }
}
