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
        public ObservableCollection<Variable> GroupByVariables
        {
            get => _groupByVariables;
            set
            {
                _groupByVariables = value;
                NotifyPropertyChanged(nameof(GroupByVariables));
            }
        }

        private bool _calculateOverall = true;
        public bool CalculateOverall
        {
            get => _calculateOverall;
            set
            {
                _calculateOverall = value;
                NotifyPropertyChanged(nameof(CalculateOverall));
            }
        }

        private bool _calculateSeparately = false;
        public bool CalculateSeparately
        {
            get => _calculateSeparately;
            set
            {
                _calculateSeparately = value;
                NotifyPropertyChanged(nameof(CalculateSeparately));
            }
        }

        private ObservableCollection<PercentileWrapper> _percentiles = new() { new() { Value = 0.25 }, new() { Value = 0.50 }, new() { Value = 0.75 } };
        public ObservableCollection<PercentileWrapper> Percentiles
        {
            get => _percentiles;
            set
            {
                _percentiles = value;
                NotifyPropertyChanged(nameof(Percentiles));
            }
        }

        private bool _calculateSE = true;
        public bool CalculateSE
        {
            get => _calculateSE;
            set
            {
                _calculateSE = value;
                NotifyPropertyChanged(nameof(CalculateSE));
            }
        }

        private bool _useInterpolation = true;
        public bool UseInterpolation
        {
            get => _useInterpolation;
            set
            {
                _useInterpolation = value;
                NotifyPropertyChanged(nameof(UseInterpolation));
            }
        }

        private bool _mimicIdbAnalyzer = false;
        public bool MimicIdbAnalyzer
        {
            get => _mimicIdbAnalyzer;
            set
            {
                _mimicIdbAnalyzer = value;
                NotifyPropertyChanged(nameof(MimicIdbAnalyzer));
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

        public void InitializeWithAnalysis(Analysis analysis)
        {
            MoveToAndFromAnalysisVariables(new()
            {
                SelectedTo = AnalysisVariables.ToList(),
            });
            MoveToAndFromGroupByVariables(new()
            {
                SelectedTo = GroupByVariables.ToList(),
            });

            foreach (var variable in analysis.Vars)
            {
                MoveToAndFromAnalysisVariables(new()
                {
                    SelectedFrom = AvailableVariables.Where(var => var.Name == variable.Name).ToList(),
                });
            }

            foreach (var variable in analysis.GroupBy)
            {
                MoveToAndFromGroupByVariables(new()
                {
                    SelectedFrom = AvailableVariables.Where(var => var.Name == variable.Name).ToList(),
                });
            }

            switch (analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    CalculateOverall = analysisUnivar.CalculateOverall;
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    CalculateSeparately = analysisMeanDiff.CalculateSeparately;
                    break;
                case AnalysisFreq analysisFreq:
                    CalculateOverall = analysisFreq.CalculateOverall;
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    Percentiles = new();
                    foreach (var percentile in analysisPercentiles.Percentiles)
                    {
                        Percentiles.Add(new() { Value = percentile });
                    }
                    CalculateOverall = analysisPercentiles.CalculateOverall;
                    UseInterpolation = analysisPercentiles.UseInterpolation;
                    CalculateSE = analysisPercentiles.CalculateSE;
                    MimicIdbAnalyzer = analysisPercentiles.MimicIdbAnalyzer;
                    break;
                case AnalysisCorr analysisCorr:
                    CalculateOverall = analysisCorr.CalculateOverall;
                    break;
                default:
                    break;
            }
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
                    GroupByVariables.Add(variable);
                    AvailableVariables.Remove(variable);
                }
            }
            if (commandParams.SelectedTo.Count > 0)
            {
                foreach (var variable in commandParams.SelectedTo)
                {
                    AvailableVariables.Add(variable);
                    GroupByVariables.Remove(variable);
                }
            }
        }

        private RelayCommand<IRequestingAnalysis?> _sendAnalysisRequestCommand;
        public ICommand SendAnalysisRequestCommand
        {
            get
            {
                if (_sendAnalysisRequestCommand == null)
                    _sendAnalysisRequestCommand = new(this.SendAnalysisRequest);
                return _sendAnalysisRequestCommand;
            }
        }

        private void SendAnalysisRequest(IRequestingAnalysis? window)
        {
            if (AnalysisConfiguration == null || AnalysisVariables.Count == 0 || window == null)
            {
                return;
            }

            Analysis analysis = (Analysis)Activator.CreateInstance(window.GetAnalysisType(), new object[] { new AnalysisConfiguration(AnalysisConfiguration) })!;
            switch (analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    analysisUnivar.Vars = new(AnalysisVariables);
                    analysisUnivar.GroupBy = new(GroupByVariables);
                    analysisUnivar.CalculateOverall = this.CalculateOverall;
                    WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisUnivar));
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    analysisMeanDiff.Vars = new(AnalysisVariables);
                    analysisMeanDiff.GroupBy = new(GroupByVariables);
                    analysisMeanDiff.CalculateSeparately = this.CalculateSeparately;
                    WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisMeanDiff));
                    break;
                case AnalysisFreq analysisFreq:
                    analysisFreq.Vars = new(AnalysisVariables);
                    analysisFreq.GroupBy = new(GroupByVariables);
                    analysisFreq.CalculateOverall = this.CalculateOverall;
                    WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisFreq));
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    analysisPercentiles.Percentiles = new(Percentiles.Select(val => val.Value));
                    analysisPercentiles.CalculateSE = this.CalculateSE;
                    analysisPercentiles.UseInterpolation = this.UseInterpolation;
                    analysisPercentiles.MimicIdbAnalyzer = this.MimicIdbAnalyzer;
                    analysisPercentiles.Vars = new(AnalysisVariables);
                    analysisPercentiles.GroupBy = new(GroupByVariables);
                    analysisPercentiles.CalculateOverall = this.CalculateOverall;
                    WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisPercentiles));
                    break;
                case AnalysisCorr analysisCorr:
                    analysisCorr.Vars = new(AnalysisVariables);
                    analysisCorr.GroupBy = new(GroupByVariables);
                    analysisCorr.CalculateOverall = this.CalculateOverall;
                    WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisCorr));
                    break;
            }
            
            if (analysis == null)
            {
                return;
            }

            window.Close();
        }
    }

    public class PercentileWrapper
    {
        public double Value { get; set; }
    }

    internal class RequestAnalysisMessage : ValueChangedMessage<Analysis>
    {
        public RequestAnalysisMessage(Analysis analysis) : base(analysis)
        {

        }
    }

    public class MoveToAndFromVariablesCommandParameters
    {
        public List<Variable> SelectedFrom { get; set; } = new();
        public List<Variable> SelectedTo { get; set; } = new();
    }
}
