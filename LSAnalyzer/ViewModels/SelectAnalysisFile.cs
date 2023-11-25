using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public SelectAnalysisFile()
        {

        }

        public SelectAnalysisFile(Configuration configuration, Rservice rservice)
        {
            _rservice = rservice;
            DatasetTypes = configuration.GetStoredDatasetTypes()?.OrderBy(dst => dst.Name).ToList() ?? DatasetTypes;
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
            var variables = _rservice.GetDatasetVariables((string)e.Argument!, IsCsv && UseCsv2 ? "csv2" : null);

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
            useFileForAnalysisWorker.RunWorkerAsync(window);
        }

        private void UseFileForAnalysisWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
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
