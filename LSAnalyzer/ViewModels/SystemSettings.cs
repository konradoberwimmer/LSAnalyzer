using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzer.ViewModels
{
    public partial class SystemSettings : ObservableValidatorExtended, IChangeTracking
    {
        private readonly Rservice _rservice;
        private readonly Logging _logger;
        private readonly Configuration _configuration;

        [ObservableProperty] private string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        [ObservableProperty] private int _countConfiguredDatasetTypes;

        [ObservableProperty]
        private bool _showLabelsDefault = Properties.Settings.Default.showLabelsDefault;
        partial void OnShowLabelsDefaultChanged(bool value)
        {
            OnPropertyChanged(nameof(IsChanged));
        }
        private bool _storedShowLabelsDefault = Properties.Settings.Default.showLabelsDefault;
        
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

        [ExcludeFromCodeCoverage]
        public SystemSettings() 
        {
            _rservice = new();
            _configuration = new("");
            // design-time only, parameterless constructor
            RVersion = "R version 4.3.1";
            BifieSurveyVersion = "3.4-15";
            CountConfiguredDatasetTypes = 12;
            _logger = new();
            _logger.AddEntry(new LogEntry(DateTime.Now, "stats::sd(c(1,2,3))"));
            _logger.AddEntry(new LogEntry(DateTime.Now, "rm(dummy_result)"));
            _sessionLog = new(_logger.LogEntries);
        }

        public SystemSettings(Rservice rservice, Configuration configuration, Logging logger)
        {
            _rservice = rservice;
            RVersion = _rservice.GetRVersion();
            BifieSurveyVersion = _rservice.GetBifieSurveyVersion();
            _configuration = configuration;
            CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;
            _logger = logger;
            _sessionLog = new(_logger.LogEntries);
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
            Properties.Settings.Default.numberRecentFiles = NumberRecentFiles;
            Properties.Settings.Default.numberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
            Properties.Settings.Default.Save();
            
            AcceptChanges();
            _configuration.TrimRecentFiles(NumberRecentFiles);
            _configuration.TrimRecentSubsettingExpressions(NumberRecentSubsettingExpressions);
            
            WeakReferenceMessenger.Default.Send<SavedSettingsMessage>();
        }

        [RelayCommand]
        private void UpdateBifieSurvey(object? dummy)
        {
            var result = _rservice.UpdateBifieSurvey();

            switch (result)
            {
                case Rservice.UpdateResult.Unavailable:
                    MessageBox.Show("Already at latest version.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case Rservice.UpdateResult.Failure:
                    MessageBox.Show("Something went wrong trying to update BIFIEsurvey!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case Rservice.UpdateResult.Success:
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
            _storedNumberRecentFiles = NumberRecentFiles;
            _storedNumberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
            OnPropertyChanged(nameof(IsChanged));
        }

        public bool IsChanged => 
            ShowLabelsDefault != _storedShowLabelsDefault ||
            NumberRecentSubsettingExpressions != _storedNumberRecentSubsettingExpressions ||
            NumberRecentFiles != _storedNumberRecentFiles;
    }

    internal class LoadedDefaultDatasetTypesMessage { }
    
    internal class SavedSettingsMessage { }
}
