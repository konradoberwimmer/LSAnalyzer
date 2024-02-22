﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

[assembly: InternalsVisibleTo("TestLSAnalyzer")]
namespace LSAnalyzer.ViewModels
{
    public class SelectAnalysisFile : INotifyPropertyChanged
    {
        private Rservice _rservice;
        private readonly IServiceProvider _serviceProvider;

        private string? _tabControlValue;
        public string? TabControlValue
        {
            get => _tabControlValue;
            set
            {
                _tabControlValue = value;
                NotifyPropertyChanged(nameof(TabControlValue));

                NotifyPropertyChanged(nameof(ReadyToGuess));
                NotifyPropertyChanged(nameof(ReadyToGo));
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

        private List<DatasetType> _datasetTypes = new();
        public List<DatasetType> DatasetTypes
        {
            get => _datasetTypes;
            set
            {
                _datasetTypes = value;
                NotifyPropertyChanged(nameof(DatasetTypes));
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

        private bool _readyToGuess = false;
        public bool ReadyToGuess
        {
            get => TabControlValue == "File system" && (FileName?.Length ?? 0) > 0;
        }

        private bool _readyToGo = false;
        public bool ReadyToGo
        {
            get => TabControlValue == "File system" && (FileName?.Length ?? 0) > 0 && SelectedDatasetType != null && SelectedWeightVariable != null;
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
            _rservice = rservice;
            DatasetTypes = configuration.GetStoredDatasetTypes()?.OrderBy(dst => dst.Name).ToList() ?? DatasetTypes;
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
            if (_fileName == null)
            {
                return;
            }

            IsBusy = true;

            BackgroundWorker guessDatasetTypeWorker = new();
            guessDatasetTypeWorker.WorkerReportsProgress = false;
            guessDatasetTypeWorker.WorkerSupportsCancellation = false;
            guessDatasetTypeWorker.DoWork += GuessDatasetTypeWorker_DoWork;
            guessDatasetTypeWorker.RunWorkerAsync(_fileName);
        }

        private void GuessDatasetTypeWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            var fileName = (string)e.Argument!;
            var fileTypeFromFile = fileName.Substring(fileName.LastIndexOf('.') + 1);

            if (fileTypeFromFile.ToLower() == "xlsx" && !_rservice.CheckNecessaryRPackages("openxlsx"))
            {
                WeakReferenceMessenger.Default.Send(new MissingRPackageMessage("openxlsx"));
                IsBusy = false;
                return;
            }

            var variables = _rservice.GetDatasetVariables(fileName, IsCsv && UseCsv2 ? "csv2" : null);

            if (variables == null)
            {
                WeakReferenceMessenger.Default.Send(new FailureAnalysisFileMessage((string)e.Argument!));
                IsBusy = false;
                return;
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
                    if (variables.Where(var => var.Name == weightVariable).Count() == 0) continue;
                }
                if (!foundAllWeightVariables)
                {
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(datasetType.MIvar) && variables.Where(var => var.Name == datasetType.MIvar).Count() == 0) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.IDvar) && variables.Where(var => var.Name == datasetType.IDvar).Count() == 0) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.PVvars))
                {
                    string[] pvVarsSplit = datasetType.PVvars.Split(';');

                    bool foundAllNecessaryPvVars = true;
                    foreach (var pvVar in pvVarsSplit)
                    {
                        if (Regex.IsMatch(pvVar, "^\\(.*\\)$"))
                        {
                            continue;
                        }

                        if (variables.Where(var => Regex.IsMatch(var.Name, pvVar)).Count() != datasetType.NMI)
                        {
                            foundAllNecessaryPvVars = false;
                            break;
                        }
                    }

                    if (!foundAllNecessaryPvVars) continue;
                }

                if (!String.IsNullOrWhiteSpace(datasetType.RepWgts))
                {
                    if (variables.Where(var => Regex.IsMatch(var.Name, datasetType.RepWgts)).Count() != datasetType.Nrep)
                    {
                        continue;
                    } else
                    {
                        priority++;
                    }
                }

                if (!String.IsNullOrWhiteSpace(datasetType.JKzone) && variables.Where(var => var.Name == datasetType.JKzone).Count() == 0) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.JKrep) && variables.Where(var => var.Name == datasetType.JKrep).Count() == 0) continue;

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
            if (_fileName == null || _selectedDatasetType == null)
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
            var fileTypeFromFile = FileName!.Substring(FileName.LastIndexOf('.') + 1);

            if (fileTypeFromFile.ToLower() == "xlsx" && !_rservice.CheckNecessaryRPackages("openxlsx"))
            {
                e.Result = new MissingRPackageMessage("openxlsx");
                IsBusy = false;
                return;
            }

            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = this.FileName,
                FileType = IsCsv && UseCsv2 ? "csv2" : null,
                DatasetType = SelectedDatasetType != null ? new(SelectedDatasetType) : null,
                ModeKeep = (SelectedAnalysisMode == AnalysisModes.Keep),
            };
            if (analysisConfiguration.DatasetType != null)
            {
                analysisConfiguration.DatasetType.Weight = SelectedWeightVariable ?? String.Empty;
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

    internal class FailureAnalysisConfigurationMessage : ValueChangedMessage<AnalysisConfiguration>
    {
        public FailureAnalysisConfigurationMessage(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {

        }
    }
}
