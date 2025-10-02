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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
                NotifyPropertyChanged();
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
            SessionLog = new(_logger.LogEntries);
        }

        public SystemSettings(Rservice rservice, Configuration configuration, Logging logger)
        {
            _rservice = rservice;
            RVersion = _rservice.GetRVersion();
            BifieSurveyVersion = _rservice.GetBifieSurveyVersion();
            _configuration = configuration;
            CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;
            _logger = logger;
            SessionLog = new(_logger.LogEntries);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private RelayCommand<object?> _loadDefaultDatasetTypesCommand = null!;
        public ICommand LoadDefaultDatasetTypesCommand
        {
            get
            {
                _loadDefaultDatasetTypesCommand ??= new RelayCommand<object?>(this.LoadDefaultDatasetTypes);
                return _loadDefaultDatasetTypesCommand;
            }
        }

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
        public void SaveSettings()
        {
            if (!Validate())
            {
                return;
            }

            Properties.Settings.Default.numberRecentFiles = NumberRecentFiles;
            Properties.Settings.Default.numberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
            Properties.Settings.Default.Save();
            
            AcceptChanges();
            _configuration.TrimRecentFiles(NumberRecentFiles);
            _configuration.TrimRecentSubsettingExpressions(NumberRecentSubsettingExpressions);
            
            WeakReferenceMessenger.Default.Send<SavedSettingsMessage>();
        }

        private RelayCommand<object?> _updateBifieSurveyCommand = null!;
        public ICommand UpdateBifieSurveyCommand
        {
            get
            {
                _updateBifieSurveyCommand ??= new RelayCommand<object?>(this.UpdateBifieSurvey);
                return _updateBifieSurveyCommand;
            }
        }

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
                default: 
                    break;
            }
        }

        private RelayCommand<string?> _saveSessionLogCommand = null!;
        public ICommand SaveSessionLogCommand
        {
            get
            {
                _saveSessionLogCommand ??= new RelayCommand<string?>(this.SaveSessionLog);
                return _saveSessionLogCommand;
            }
        }

        private void SaveSessionLog(string? filename)
        {
            if (filename == null)
            {
                return;
            }

            using StreamWriter streamWriter = new(filename, false);
            streamWriter.Write(_logger.GetFullText());
        }

        private RelayCommand<string?> _saveSessionRcodeCommand = null!;
        public ICommand SaveSessionRcodeCommand
        {
            get
            {
                _saveSessionRcodeCommand ??= new RelayCommand<string?>(this.SaveSessionRcode);
                return _saveSessionRcodeCommand;
            }
        }

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
            _storedNumberRecentFiles = NumberRecentFiles;
            _storedNumberRecentSubsettingExpressions = NumberRecentSubsettingExpressions;
            OnPropertyChanged(nameof(IsChanged));
        }

        public bool IsChanged => 
            NumberRecentSubsettingExpressions != _storedNumberRecentSubsettingExpressions ||
            NumberRecentFiles != _storedNumberRecentFiles;
    }

    internal class LoadedDefaultDatasetTypesMessage { }
    
    internal class SavedSettingsMessage { }
}
