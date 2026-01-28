using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzer.ViewModels;

public partial class RequestAnalysis : ObservableObject
{
    public required AnalysisConfiguration AnalysisConfiguration { set; get; }

    [ObservableProperty]
    private ObservableCollection<Variable> _availableVariables = [];

    private bool _sortAlphabetically = false;
    public bool SortAlphabetically
    {
        get => _sortAlphabetically;
        set
        {
            if (value != _sortAlphabetically)
            {
                AvailableVariables = value ? 
                    new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Name)) : 
                    new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Position));
            }
            
            _sortAlphabetically = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private ObservableCollection<Variable> _analysisVariables = [];

    [ObservableProperty]
    private ObservableCollection<Variable> _groupByVariables = [];

    [ObservableProperty]
    private bool _calculateOverall = true;
    
    [ObservableProperty]
    private bool _calculateCrosswise = true;

    [ObservableProperty]
    private bool _calculateBivariate = true;
    
    [ObservableProperty]
    private bool _calculateSeparately = false;

    [ObservableProperty]
    private ObservableCollection<PercentileWrapper> _percentiles = [ new() { Value = 0.25 }, new() { Value = 0.50 }, new() { Value = 0.75 } ];

    [ObservableProperty]
    private bool _calculateSE = true;

    [ObservableProperty]
    private bool _useInterpolation = true;

    [ObservableProperty]
    private bool _mimicIdbAnalyzer = false;

    [ObservableProperty]
    private bool _withIntercept = true;
    
    [ObservableProperty]
    private AnalysisRegression.RegressionSequence _regressionSequence = AnalysisRegression.RegressionSequence.AllIn;
    partial void OnRegressionSequenceChanged(AnalysisRegression.RegressionSequence value)
    {
        OnPropertyChanged(nameof(RegressionSequenceIsAllIn));
    }

    public bool RegressionSequenceIsAllIn => RegressionSequence == AnalysisRegression.RegressionSequence.AllIn;

    [ObservableProperty]
    private ObservableCollection<Variable> _dependentVariables = [];

    public string BifieSurveyVersion { get; set; } = string.Empty; 
    
    public bool BIFIEsurveyVersionWarning => new Version(BifieSurveyVersion).CompareTo(new Version(3, 6)) < 0;

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
        MoveToAndFromDependentVariables(new()
        {
            SelectedTo = DependentVariables.ToList(),
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
                CalculateCrosswise = analysisUnivar.CalculateCrosswise;
                break;
            case AnalysisMeanDiff analysisMeanDiff:
                CalculateSeparately = analysisMeanDiff.CalculateSeparately;
                break;
            case AnalysisFreq analysisFreq:
                CalculateOverall = analysisFreq.CalculateOverall;
                CalculateCrosswise = analysisFreq.CalculateCrosswise;
                CalculateBivariate = analysisFreq.CalculateBivariate;
                break;
            case AnalysisPercentiles analysisPercentiles:
                Percentiles = new();
                foreach (var percentile in analysisPercentiles.Percentiles)
                {
                    Percentiles.Add(new() { Value = percentile });
                }
                CalculateOverall = analysisPercentiles.CalculateOverall;
                CalculateCrosswise = analysisPercentiles.CalculateCrosswise;
                UseInterpolation = analysisPercentiles.UseInterpolation;
                CalculateSE = analysisPercentiles.CalculateSE;
                MimicIdbAnalyzer = analysisPercentiles.MimicIdbAnalyzer;
                break;
            case AnalysisCorr analysisCorr:
                CalculateOverall = analysisCorr.CalculateOverall;
                CalculateCrosswise = analysisCorr.CalculateCrosswise;
                break;
            case AnalysisLinreg analysisLinreg:
                WithIntercept = analysisLinreg.WithIntercept;
                RegressionSequence = analysisLinreg.Sequence;
                if (analysisLinreg.Dependent != null)
                {
                    MoveToAndFromDependentVariables(new()
                    {
                        SelectedFrom = AvailableVariables.Where(var => var.Name == analysisLinreg.Dependent.Name).ToList(),
                    });
                }
                CalculateOverall = analysisLinreg.CalculateOverall;
                CalculateCrosswise = analysisLinreg.CalculateCrosswise;
                break;
            case AnalysisLogistReg analysisLogistReg:
                WithIntercept = analysisLogistReg.WithIntercept;
                RegressionSequence = analysisLogistReg.Sequence;
                if (analysisLogistReg.Dependent != null)
                {
                    MoveToAndFromDependentVariables(new()
                    {
                        SelectedFrom = AvailableVariables.Where(var => var.Name == analysisLogistReg.Dependent.Name).ToList(),
                    });
                }
                CalculateOverall = analysisLogistReg.CalculateOverall;
                CalculateCrosswise = analysisLogistReg.CalculateCrosswise;
                break;
        }
    }

    [RelayCommand]
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
                AvailableVariables = SortAlphabetically ?
                    new(AvailableVariables.OrderBy(v => v.Name)) :
                    new(AvailableVariables.OrderBy(v => v.Position));
                AnalysisVariables.Remove(variable);
            }
        }
    }

    [RelayCommand]
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
                AvailableVariables = SortAlphabetically ?
                    new(AvailableVariables.OrderBy(v => v.Name)) :
                    new(AvailableVariables.OrderBy(v => v.Position));
                GroupByVariables.Remove(variable);
            }
        }

        OnPropertyChanged(nameof(GroupByVariables));
    }

    [RelayCommand]
    private void MoveToAndFromDependentVariables(MoveToAndFromVariablesCommandParameters? commandParams)
    {
        if (commandParams == null)
        {
            return;
        }

        if (commandParams.SelectedFrom.Count > 0)
        {
            foreach (var variable in commandParams.SelectedFrom)
            {
                DependentVariables.Add(variable);
                AvailableVariables.Remove(variable);
            }
        }
        if (commandParams.SelectedTo.Count > 0)
        {
            foreach (var variable in commandParams.SelectedTo)
            {
                AvailableVariables.Add(variable);
                AvailableVariables = SortAlphabetically ?
                    new(AvailableVariables.OrderBy(v => v.Name)) :
                    new(AvailableVariables.OrderBy(v => v.Position));
                DependentVariables.Remove(variable);
            }
        }
    }

    [RelayCommand]
    private void SendAnalysisRequest(IRequestingAnalysis? window)
    {
        if (AnalysisConfiguration == null || AnalysisVariables.Count == 0 || window == null)
        {
            return;
        }

        Analysis analysis = (Analysis)Activator.CreateInstance(window.GetAnalysisType(), new object[] { new AnalysisConfiguration(AnalysisConfiguration) })!;

        if (analysis is AnalysisLinreg && DependentVariables.Count == 0)
        {
            return;
        }

        switch (analysis)
        {
            case AnalysisUnivar analysisUnivar:
                analysisUnivar.Vars = new(AnalysisVariables);
                analysisUnivar.GroupBy = new(GroupByVariables);
                analysisUnivar.CalculateOverall = this.CalculateOverall;
                analysisUnivar.CalculateCrosswise = this.CalculateCrosswise;
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
                analysisFreq.CalculateCrosswise = this.CalculateCrosswise;
                analysisFreq.CalculateBivariate = this.CalculateBivariate;
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
                analysisPercentiles.CalculateCrosswise = this.CalculateCrosswise;
                WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisPercentiles));
                break;
            case AnalysisCorr analysisCorr:
                analysisCorr.Vars = new(AnalysisVariables);
                analysisCorr.GroupBy = new(GroupByVariables);
                analysisCorr.CalculateOverall = this.CalculateOverall;
                analysisCorr.CalculateCrosswise = this.CalculateCrosswise;
                WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisCorr));
                break;
            case AnalysisLinreg analysisLinreg:
                analysisLinreg.WithIntercept = this.WithIntercept;
                analysisLinreg.Sequence = RegressionSequence;
                analysisLinreg.Dependent = DependentVariables.First();
                analysisLinreg.Vars = new(AnalysisVariables);
                analysisLinreg.GroupBy = new(GroupByVariables);
                analysisLinreg.CalculateOverall = this.CalculateOverall;
                analysisLinreg.CalculateCrosswise = this.CalculateCrosswise;
                WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisLinreg));
                break;
            case AnalysisLogistReg analysisLogistReg:
                analysisLogistReg.WithIntercept = this.WithIntercept;
                analysisLogistReg.Sequence = RegressionSequence;
                analysisLogistReg.Dependent = DependentVariables.First();
                analysisLogistReg.Vars = new(AnalysisVariables);
                analysisLogistReg.GroupBy = new(GroupByVariables);
                analysisLogistReg.CalculateOverall = this.CalculateOverall;
                analysisLogistReg.CalculateCrosswise = this.CalculateCrosswise;
                WeakReferenceMessenger.Default.Send(new RequestAnalysisMessage(analysisLogistReg));
                break;
        }

        window.Close();
    }

    [RelayCommand]
    private void ResetAnalysisRequest(object? dummy)
    {
        MoveToAndFromAnalysisVariables(new()
        {
            SelectedTo = AnalysisVariables.ToList(),
        });
        MoveToAndFromGroupByVariables(new()
        {
            SelectedTo = GroupByVariables.ToList(),
        });
        MoveToAndFromDependentVariables(new()
        {
            SelectedTo = DependentVariables.ToList(),
        });
        CalculateOverall = true;
        CalculateCrosswise = true;
        CalculateBivariate = true;
        CalculateSeparately = false;
        Percentiles = new() { new() { Value = 0.25 }, new() { Value = 0.50 }, new() { Value = 0.75 } };
        CalculateSE = true;
        UseInterpolation = true;
        MimicIdbAnalyzer = false;
        WithIntercept = true;
        RegressionSequence = AnalysisRegression.RegressionSequence.AllIn;
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
