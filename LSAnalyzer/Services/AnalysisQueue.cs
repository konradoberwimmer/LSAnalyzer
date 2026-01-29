using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public class AnalysisQueue : IAnalysisQueue
{
    private readonly IRservice _rservice;
    
    private Queue<AnalysisPresentation> _analysisQueue = new();

    public AnalysisQueue(IRservice rservice)
    {
        _rservice = rservice;
    }
    
    public void Add(AnalysisPresentation analysisPresentation)
    {
        _analysisQueue.Enqueue(analysisPresentation);

        if (_analysisQueue.Count == 1)
        {
            StartNextAnalysis();
        }
    }

    public int Count => _analysisQueue.Count;
    
    public void InterruptAnalysis(AnalysisPresentation analysisPresentation)
    {
        if (analysisPresentation != _analysisQueue.FirstOrDefault()) return;

        _rservice.SendUserInterrupt();
    }

    private void StartNextAnalysis()
    {
        if (_analysisQueue.Count == 0) return;
        
        var analysisPresentation = _analysisQueue.First();
        
        BackgroundWorker analysisWorker = new();
        analysisWorker.WorkerReportsProgress = false;
        analysisWorker.WorkerSupportsCancellation = false;
        analysisWorker.DoWork += AnalysisWorker_DoWork;
        analysisWorker.RunWorkerCompleted += (_, _) =>
        {
            _analysisQueue.Dequeue();
            StartNextAnalysis();
        }; 
        analysisWorker.RunWorkerAsync(analysisPresentation);
    }

    private void AnalysisWorker_DoWork (object? sender, DoWorkEventArgs e)
    {
        if (e.Argument is not AnalysisPresentation analysisPresentation)
        {
            e.Cancel = true;
            return;
        }

        if (!(analysisPresentation.MainWindowViewModel?.Analyses.Contains(analysisPresentation) ?? true))
        {
            e.Result = null;
            return;
        }

        var beforeCalculation = DateTime.Now;

        var result = analysisPresentation.Analysis switch
        {
            AnalysisUnivar analysisUnivar => _rservice.CalculateUnivar(analysisUnivar),
            AnalysisMeanDiff analysisMeanDiff => _rservice.CalculateMeanDiff(analysisMeanDiff),
            AnalysisFreq analysisFreq => _rservice.CalculateFreq(analysisFreq),
            AnalysisPercentiles analysisPercentiles => _rservice.CalculatePercentiles(analysisPercentiles),
            AnalysisCorr analysisCorr => _rservice.CalculateCorr(analysisCorr),
            AnalysisLinreg analysisLinreg => _rservice.CalculateLinreg(analysisLinreg),
            AnalysisLogistReg analysisLogistReg => _rservice.CalculateLogistReg(analysisLogistReg),
            _ => null
        };

        if (result == null)
        {
            WeakReferenceMessenger.Default.Send(new MainWindow.FailureWithAnalysisCalculationMessage(analysisPresentation.Analysis));
            e.Result = null;
            return;
        }
        
        if (analysisPresentation.Analysis is AnalysisFreq { CalculateBivariate: true } analysisFreqWithBivariate)
        {
            analysisFreqWithBivariate.BivariateResult = _rservice.CalculateBivariate(analysisFreqWithBivariate);
        }

        var variablesToConsiderForValueLabels = new List<Variable>(analysisPresentation.Analysis.GroupBy);
        if (analysisPresentation.Analysis is AnalysisFreq)
        {
            variablesToConsiderForValueLabels.AddRange(analysisPresentation.Analysis.Vars);
        }

        foreach (var variable in variablesToConsiderForValueLabels)
        {
            var valueLabels = _rservice.GetValueLabels(variable.Name);
            if (valueLabels != null)
            {
                analysisPresentation.Analysis.ValueLabels.Add(variable.Name, valueLabels);
            }
        }

        analysisPresentation.Analysis.ResultAt = DateTime.Now;
        analysisPresentation.Analysis.ResultDuration = (analysisPresentation.Analysis.ResultAt! - beforeCalculation).Value.TotalSeconds;
        analysisPresentation.SetAnalysisResult(result);

        e.Result = result;
    }
}