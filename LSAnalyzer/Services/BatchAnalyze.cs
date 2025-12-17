using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public class BatchAnalyze
    {
        private Rservice _rservice;
        private bool _useCurrentFile = true;
        private AnalysisConfiguration? _currentConfiguration;

        public BatchAnalyze(Rservice rservice)
        {
            _rservice = rservice;
        }

        public void RunBatch(Dictionary<int, AnalysisWithViewSettings> analyses, bool useCurrentFile, AnalysisConfiguration? currentConfiguration)
        {
            _useCurrentFile = useCurrentFile;
            _currentConfiguration = currentConfiguration;

            if (analyses.Count > 0 && !_useCurrentFile)
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeChangedStoredRawDataFileMessage());
            }

            BackgroundWorker worker = new();
            worker.WorkerReportsProgress = false;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += DoBatch;
            worker.RunWorkerAsync(analyses);
        }

        private void DoBatch(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is not Dictionary<int, AnalysisWithViewSettings> analyses || (_useCurrentFile && _currentConfiguration == null))
            {
                e.Cancel = true;
                return;
            }

            AnalysisConfiguration? previousAnalysisConfiguration = null;
            string? previousSubsettingExpression = "$$$initialize$$$";

            foreach (var (key, analysisWithViewSettings) in analyses)
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                {
                    Id = key,
                    Success = false,
                    Message = "Working ..."
                });

                var analysis = analysisWithViewSettings.Analysis;

                if (!_useCurrentFile && (
                        previousAnalysisConfiguration == null || 
                        !analysis.AnalysisConfiguration.IsEqual(previousAnalysisConfiguration) || 
                        analysis.SubsettingExpression != previousSubsettingExpression))
                {
                    if (analysis.AnalysisConfiguration!.FileName?.StartsWith("[") ?? false)
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Reloading from data provider is not supported!"
                        });
                        continue;
                    }

                    if (!_rservice.LoadFileIntoGlobalEnvironment(analysis.AnalysisConfiguration.FileName ?? string.Empty, analysis.AnalysisConfiguration.FileType))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!"
                        });
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(analysis.AnalysisConfiguration.DatasetType?.IDvar) && !_rservice.SortRawDataStored(analysis.AnalysisConfiguration.DatasetType!.IDvar))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!"
                        });
                        continue;
                    }

                    if (!_rservice.TestAnalysisConfiguration(analysis.AnalysisConfiguration, analysis.SubsettingExpression))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Could not use file for dataset type '" + analysis.AnalysisConfiguration.DatasetType?.Name + "'!"
                        });
                        continue;
                    }

                    if (analysis.AnalysisConfiguration.ModeKeep == false)
                    {
                        List<string>? additionalVariables = null;
                        if (analysis is AnalysisRegression analysisRegression)
                        {
                            additionalVariables = new() { analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_" };
                        }
                        if (!_rservice.PrepareForAnalysis(analysis, additionalVariables))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = key,
                                Success = false,
                                Message = "Could not build file '" + analysis.AnalysisConfiguration.FileName + "' for analysis!"
                            });
                            continue;
                        }
                    }
                }

                if (_useCurrentFile)
                {
                    if (analysis.SubsettingExpression != previousSubsettingExpression)
                    {
                        if (!_rservice.TestAnalysisConfiguration(_currentConfiguration!, analysis.SubsettingExpression))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = key,
                                Success = false,
                                Message = "Could not reapply subsetting '" + analysis.SubsettingExpression + "'!"
                            });
                            continue;
                        }

                        previousSubsettingExpression = analysis.SubsettingExpression;
                    
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeChangedSubsettingMessage() { SubsettingExpression = analysis.SubsettingExpression ?? string.Empty });
                    }

                    if (!(_currentConfiguration?.ModeKeep ?? true))
                    {
                        List<string>? additionalVariables = null;
                        if (analysis is AnalysisRegression analysisRegression)
                        {
                            additionalVariables = new() { analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_" };
                        }
                        if (!_rservice.PrepareForAnalysis(analysis, additionalVariables))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = key,
                                Success = false,
                                Message = "Could not build current file for analysis!"
                            });
                            continue;
                        }
                    }

                    analysis.AnalysisConfiguration = _currentConfiguration!;
                }

                previousSubsettingExpression = analysis.SubsettingExpression;
                previousAnalysisConfiguration = analysis.AnalysisConfiguration;

                DateTime beforeCalculation = DateTime.Now;

                switch (analysis)
                {
                    case AnalysisUnivar analysisUnivar:
                        analysisUnivar.Result = _rservice.CalculateUnivar(analysisUnivar) ?? new();
                        break;
                    case AnalysisMeanDiff analysisMeanDiff:
                        analysisMeanDiff.Result = _rservice.CalculateMeanDiff(analysisMeanDiff) ?? new();
                        break;
                    case AnalysisFreq analysisFreq:
                        analysisFreq.Result = _rservice.CalculateFreq(analysisFreq) ?? new();
                        if (analysisFreq.CalculateBivariate)
                        {
                            analysisFreq.BivariateResult = _rservice.CalculateBivariate(analysisFreq);
                        }
                        break;
                    case AnalysisPercentiles analysisPercentiles:
                        analysisPercentiles.Result = _rservice.CalculatePercentiles(analysisPercentiles) ?? new();
                        break;
                    case AnalysisCorr analysisCorr:
                        analysisCorr.Result = _rservice.CalculateCorr(analysisCorr) ?? new();
                        break;
                    case AnalysisLinreg analysisLinreg:
                        analysisLinreg.Result = _rservice.CalculateLinreg(analysisLinreg) ?? new();
                        break;
                    case AnalysisLogistReg analysisLogistReg:
                        analysisLogistReg.Result = _rservice.CalculateLogistReg(analysisLogistReg) ?? new();
                        break;
                    default:
                        break;
                }

                if (analysis.Result.Count == 0)
                {
                    WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                    {
                        Id = key,
                        Success = false,
                        Message = "Could not calculate a result!"
                    });
                    continue;
                }
                    
                foreach (var groupByVariable in analysis.GroupBy)
                {
                    var valueLabels = _rservice.GetValueLabels(groupByVariable.Name);
                    if (valueLabels != null)
                    {
                        analysis.ValueLabels.Add(groupByVariable.Name, valueLabels);
                    }
                }

                analysis.ResultAt = DateTime.Now;
                analysis.ResultDuration = (analysis.ResultAt! - beforeCalculation).Value.TotalSeconds;
                    
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage() { Id = key, Success = true, Message = "Success!" });
            }

            e.Result = analyses;
        }
    }

    public class BatchAnalyzeMessage
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = String.Empty;
    }

    public class BatchAnalyzeChangedStoredRawDataFileMessage
    {

    }

    public class BatchAnalyzeChangedSubsettingMessage
    {
        public string SubsettingExpression { get; set; } = null!;
    }
}
