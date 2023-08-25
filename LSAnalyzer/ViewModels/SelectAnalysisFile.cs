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

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                IsBusy = true;
            });
            Thread.Yield();

            var variables = _rservice.GetDatasetVariables(_fileName);

            if (variables == null)
            {
                IsBusy = false;
                return;
            }

            List<DatasetType> possibleDatasetTypes = new();
            
            foreach (var datasetType in _datasetTypes)
            {
                if (variables.Where(var => var.Name == datasetType.Weight).Count() == 0) continue;
                
                if (!String.IsNullOrWhiteSpace(datasetType.MIvar) && variables.Where(var => var.Name == datasetType.MIvar).Count() == 0) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.PVvars))
                {
                    string[] pvVarsSplit = datasetType.PVvars.Split(';');
                    
                    bool foundAllPvVars = true;
                    foreach (var pvVar in pvVarsSplit)
                    {
                        if (variables.Where(var => Regex.IsMatch(var.Name, pvVar)).Count() != datasetType.NMI) 
                        {
                            foundAllPvVars = false;
                            break;
                        }
                    }

                    if (!foundAllPvVars) continue;
                }

                if (!String.IsNullOrWhiteSpace(datasetType.RepWgts) && variables.Where(var => Regex.IsMatch(var.Name, datasetType.RepWgts)).Count() != datasetType.Nrep) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.JKzone) && variables.Where(var => var.Name == datasetType.JKzone).Count() == 0) continue;
                if (!String.IsNullOrWhiteSpace(datasetType.JKrep) && variables.Where(var => var.Name == datasetType.JKrep).Count() == 0) continue;

                possibleDatasetTypes.Add(datasetType);
            }

            if (possibleDatasetTypes.Count == 0)
            {
                SelectedDatasetType = null;
            }
            else if (possibleDatasetTypes.Count == 1)
            {
                SelectedDatasetType = possibleDatasetTypes.FirstOrDefault();
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

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                IsBusy = true;
            });
            Thread.Yield();

            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = this.FileName,
                DatasetType = SelectedDatasetType,
                ModeKeep = (SelectedAnalysisMode == AnalysisModes.Keep),
            };

            var testAnalysisConfiguration = _rservice.TestAnalysisConfiguration(analysisConfiguration);

            if (!testAnalysisConfiguration)
            {
                WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
                IsBusy = false;
                return;
            }

            WeakReferenceMessenger.Default.Send(new SetAnalysisConfigurationMessage(analysisConfiguration));
            IsBusy = false;

            window?.Close();
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

    internal class FailureAnalysisConfigurationMessage : ValueChangedMessage<AnalysisConfiguration>
    {
        public FailureAnalysisConfigurationMessage(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {

        }
    }
}
