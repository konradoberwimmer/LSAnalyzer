using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public class RequestAnalysis : INotifyPropertyChanged
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
                
                if (AnalysisConfiguration != null) {
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

        private Analysis? _analysis;
        public Analysis? Analysis
        {
            get => _analysis;
            set
            {
                _analysis = value;
                NotifyPropertyChanged(nameof(Analysis));
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

        private ObservableCollection<Variable> _analysisVariables = new();
        public ObservableCollection<Variable> AnalysisVariables
        {
            get => _analysisVariables;
            set
            {
                _analysisVariables = value;
                NotifyPropertyChanged(nameof(AnalysisVariables));
            }
        }

        private ObservableCollection<Variable> _groupByVariables = new();
        public ObservableCollection<Variable> GrouyByVariables
        {
            get => _groupByVariables;
            set
            {
                _groupByVariables = value;
                NotifyPropertyChanged(nameof(GrouyByVariables));
            }
        }

        [ExcludeFromCodeCoverage]
        public RequestAnalysis()
        {
            // design-time-only constructor
        }

        public RequestAnalysis(Rservice rservice)
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

        private RelayCommand<MoveToAndFromVariablesCommandParameters?> _moveToAndFromAnalysisVariablesCommand;
        public ICommand MoveToAndFromAnalysisVariablesCommand
        {
            get
            {
                if (_moveToAndFromAnalysisVariablesCommand == null)
                    _moveToAndFromAnalysisVariablesCommand = new(this.MoveToAndFromAnalysisVariables);
                return _moveToAndFromAnalysisVariablesCommand;
            }
        }

        private void MoveToAndFromAnalysisVariables(MoveToAndFromVariablesCommandParameters? commandParams)
        {
            if (commandParams == null)
            {
                return;
            }

            if (commandParams.SelectedFrom.Count > 0)
            {
                foreach (var variable in commandParams.SelectedFrom)
                {
                    AnalysisVariables.Add(variable);
                    AvailableVariables.Remove(variable);
                }
            }
            if (commandParams.SelectedTo.Count > 0)
            {
                foreach (var variable in commandParams.SelectedTo)
                {
                    AvailableVariables.Add(variable);
                    AnalysisVariables.Remove(variable);
                }
            }
        }

        private RelayCommand<MoveToAndFromVariablesCommandParameters?> _moveToAndFromGroupByVariablesCommand;
        public ICommand MoveToAndFromGroupByVariablesCommand
        {
            get
            {
                if (_moveToAndFromGroupByVariablesCommand == null)
                    _moveToAndFromGroupByVariablesCommand = new(this.MoveToAndFromGroupByVariables);
                return _moveToAndFromGroupByVariablesCommand;
            }
        }

        private void MoveToAndFromGroupByVariables(MoveToAndFromVariablesCommandParameters? commandParams)
        {
            if (commandParams == null)
            {
                return;
            }

            if (commandParams.SelectedFrom.Count > 0)
            {
                foreach (var variable in commandParams.SelectedFrom)
                {
                    GrouyByVariables.Add(variable);
                    AvailableVariables.Remove(variable);
                }
            }
            if (commandParams.SelectedTo.Count > 0)
            {
                foreach (var variable in commandParams.SelectedTo)
                {
                    AvailableVariables.Add(variable);
                    GrouyByVariables.Remove(variable);
                }
            }
        }
    }

    public class MoveToAndFromVariablesCommandParameters
    {
        public List<Variable> SelectedFrom { get; set; } = new();
        public List<Variable> SelectedTo { get; set; } = new();
    }
}
