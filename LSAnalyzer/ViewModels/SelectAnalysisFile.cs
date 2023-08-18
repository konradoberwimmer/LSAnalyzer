using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

            if (window != null && window is Views.SelectAnalysisFile selectAnalysisFileWindow)
            {
                selectAnalysisFileWindow.busySpinner.Visibility = Visibility.Visible;
            }

            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = this.FileName,
                DatasetType = SelectedDatasetType,
                ModeKeep = true
            };

            var testAnalysisConfiguration = _rservice.TestAnalysisConfiguration(analysisConfiguration);

            if (!testAnalysisConfiguration)
            {
                WeakReferenceMessenger.Default.Send(new FailureAnalysisConfigurationMessage(analysisConfiguration));
                return;
            }

            WeakReferenceMessenger.Default.Send(new SetAnalysisConfigurationMessage(analysisConfiguration));

            window?.Close();
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
