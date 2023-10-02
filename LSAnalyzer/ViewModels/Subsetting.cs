using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public class Subsetting : INotifyPropertyChanged
    {
        private readonly Rservice _rservice;

        private AnalysisConfiguration? _analysisConfiguration;
        public AnalysisConfiguration? AnalysisConfiguration
        {
            get => _analysisConfiguration;
            set
            {
                _analysisConfiguration = value;
                NotifyPropertyChanged(nameof(AnalysisConfiguration));

                if (AnalysisConfiguration != null)
                {
                    var currentDatasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration);
                    if (currentDatasetVariables != null)
                    {
                        ObservableCollection<Variable> newAvailableVariables = new();
                        foreach (var variable in currentDatasetVariables)
                        {
                            newAvailableVariables.Add(variable);
                        }
                        AvailableVariables = newAvailableVariables;
                    }
                }
            }
        }

        private ObservableCollection<Variable> _availableVariables;
        public ObservableCollection<Variable> AvailableVariables
        {
            get => _availableVariables;
            set
            {
                _availableVariables = value;
                NotifyPropertyChanged(nameof(AvailableVariables));
            }
        }

        private bool _isCurrentlySubsetting = false;
        public bool IsCurrentlySubsetting
        {
            get => _isCurrentlySubsetting;
        }

        private string? _subsetExpression;
        public string? SubsetExpression
        {
            get => _subsetExpression;
            set
            {
                _subsetExpression = value;
                SubsettingInformation = null;
                NotifyPropertyChanged(nameof(SubsetExpression));
            }
        }

        private SubsettingInformation? _subsettingInformation = null;
        public SubsettingInformation? SubsettingInformation
        {
            get => _subsettingInformation;
            set
            {
                _subsettingInformation = value;
                NotifyPropertyChanged(nameof(SubsettingInformation));
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                NotifyPropertyChanged(nameof(IsBusy));
            }
        }

        [ExcludeFromCodeCoverage]
        public Subsetting()
        {
            // design-time-only constructor
            SubsettingInformation = new() { ValidSubset = true, NCases = 100, NSubset = 57 };
        }

        public Subsetting(Rservice rservice)
        {
            _rservice = rservice;
            AvailableVariables = new ObservableCollection<Variable>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetCurrentSubsetting(string subsettingExpression)
        {
            SubsetExpression = subsettingExpression;
            _isCurrentlySubsetting = true;
        }

        private RelayCommand<ICloseable?> _clearSubsettingCommand;
        public ICommand ClearSubsettingCommand
        {
            get
            {
                if (_clearSubsettingCommand == null)
                    _clearSubsettingCommand = new RelayCommand<ICloseable?>(this.ClearSubsetting);
                return _clearSubsettingCommand;
            }
        }

        private void ClearSubsetting(ICloseable? window)
        {
            if (AnalysisConfiguration == null)
            {
                return;
            }

            SubsetExpression = null;

            BackgroundWorker clearSubsettingWorker = new();
            clearSubsettingWorker.WorkerReportsProgress = false;
            clearSubsettingWorker.WorkerSupportsCancellation = false;
            clearSubsettingWorker.DoWork += ClearSubsettingWorker_DoWork;
            clearSubsettingWorker.RunWorkerAsync(window);
        }

        private void ClearSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            IsBusy = true;
            if (AnalysisConfiguration?.ModeKeep == true)
            {
                _rservice.TestAnalysisConfiguration(AnalysisConfiguration);
            }

            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));
            IsBusy = false;

            if (e.Argument is ICloseable window)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    window.Close();
                });
            }
        }

        private RelayCommand<object?> _testSubsettingCommand;
        public ICommand TestSubsettingCommand
        {
            get
            {
                if (_testSubsettingCommand == null)
                    _testSubsettingCommand = new RelayCommand<object?>(this.TestSubsetting);
                return _testSubsettingCommand;
            }
        }

        private void TestSubsetting(object? dummy)
        {
            if (SubsetExpression != null)
            {
                BackgroundWorker testSubsettingWorker = new();
                testSubsettingWorker.WorkerReportsProgress = false;
                testSubsettingWorker.WorkerSupportsCancellation = false;
                testSubsettingWorker.DoWork += TestSubsettingWorker_DoWork;
                testSubsettingWorker.RunWorkerAsync();
            }
        }

        private void TestSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            IsBusy = true;
            SubsettingInformation = _rservice.TestSubsetting(SubsetExpression!, AnalysisConfiguration?.DatasetType?.MIvar);
            IsBusy = false;
        }

        private RelayCommand<ICloseable?> _useSubsettingCommand;
        public ICommand UseSubsettingCommand
        {
            get
            {
                if (_useSubsettingCommand == null)
                    _useSubsettingCommand = new RelayCommand<ICloseable?>(this.UseSubsetting);
                return _useSubsettingCommand;
            }
        }

        private void UseSubsetting(ICloseable? window)
        {
            if (SubsetExpression == null || AnalysisConfiguration == null)
            {
                return;
            }

            BackgroundWorker useSubsettingWorker = new();
            useSubsettingWorker.WorkerReportsProgress = false;
            useSubsettingWorker.WorkerSupportsCancellation = false;
            useSubsettingWorker.DoWork += UseSubsettingWorker_DoWork;
            useSubsettingWorker.RunWorkerAsync(window);
        }

        private void UseSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            IsBusy = true;

            var testResult = _rservice.TestSubsetting(SubsetExpression!);
            if (!testResult.ValidSubset)
            {
                SubsettingInformation = testResult;
                IsBusy = false;
                return;
            }

            if (AnalysisConfiguration!.ModeKeep == true)
            {
                if (!_rservice.TestAnalysisConfiguration(AnalysisConfiguration!, SubsetExpression))
                {
                    SubsettingInformation = new()
                    {
                        ValidSubset = false,
                        NCases = 0,
                        NSubset = 0,
                    };
                    IsBusy = false;
                    return;
                }
            }

            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));
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

    internal class SetSubsettingExpressionMessage : ValueChangedMessage<string?>
    {
        public SetSubsettingExpressionMessage(string? subsettingExpression) : base(subsettingExpression)
        {

        }
    }
}
