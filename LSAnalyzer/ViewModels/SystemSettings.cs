using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public class SystemSettings : INotifyPropertyChanged
    {
        private readonly Rservice _rservice;
        private readonly Logging _logger;
        private readonly Configuration _configuration;

        private string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                NotifyPropertyChanged(nameof(Version));
            }
        }

        private int _countConfiguredDatasetTypes;
        public int CountConfiguredDatasetTypes
        {
            get => _countConfiguredDatasetTypes;
            set
            {
                _countConfiguredDatasetTypes = value;
                NotifyPropertyChanged(nameof(CountConfiguredDatasetTypes));
            }
        }

        private string? _rVersion;
        public string? RVersion
        {
            get => _rVersion;
            set
            {
                _rVersion = value;
                NotifyPropertyChanged(nameof(RVersion));
            }
        }

        private string? _bifieSurveyVersion;
        public string? BifieSurveyVersion
        {
            get => _bifieSurveyVersion;
            set
            {
                _bifieSurveyVersion = value;
                NotifyPropertyChanged(nameof(BifieSurveyVersion));
            }
        }

        public string? SessionLog
        {
            get => _logger.Stringify();
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
        }

        public SystemSettings(Rservice rservice, Configuration configuration, Logging logger)
        {
            _rservice = rservice;
            RVersion = _rservice.GetRVersion();
            BifieSurveyVersion = _rservice.GetBifieSurveyVersion();
            _configuration = configuration;
            CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;
            _logger = logger;
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
                _configuration.StoreDatasetType(defaultDatasetType);
            }

            CountConfiguredDatasetTypes = _configuration.GetStoredDatasetTypes()?.Count ?? 0;

            WeakReferenceMessenger.Default.Send(new LoadedDefaultDatasetTypesMessage());
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
            streamWriter.Write(_logger.Stringify());
        }
    }

    internal class LoadedDefaultDatasetTypesMessage { }
}
