using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
            SubsetExpression = null;

            if (AnalysisConfiguration?.ModeKeep == true)
            {
                _rservice.TestAnalysisConfiguration(AnalysisConfiguration);
            }

            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));

            window?.Close();
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
                SubsettingInformation = _rservice.TestSubsetting(SubsetExpression, AnalysisConfiguration?.DatasetType?.MIvar);
            }
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

            var testResult = _rservice.TestSubsetting(SubsetExpression);
            if (!testResult.ValidSubset)
            {
                SubsettingInformation = testResult;
                return;
            }

            if (AnalysisConfiguration.ModeKeep == true)
            {
                _rservice.TestAnalysisConfiguration(AnalysisConfiguration, SubsetExpression);
            }
            
            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));

            window?.Close();
        }
    }

    internal class SetSubsettingExpressionMessage : ValueChangedMessage<string?>
    {
        public SetSubsettingExpressionMessage(string? subsettingExpression) : base(subsettingExpression)
        {

        }
    }
}
