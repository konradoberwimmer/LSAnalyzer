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

        public void RunBatch(Dictionary<int, Analysis> analyses, bool useCurrentFile, AnalysisConfiguration? currentConfiguration)
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
            if (e.Argument is not Dictionary<int, Analysis> analyses || (_useCurrentFile && _currentConfiguration == null))
            {
                e.Cancel = true;
                return;
            }

            AnalysisConfiguration? previousAnalysisConfiguration = null;
            string? previousSubsettingExpression = "$$$initialize$$$";

            foreach (var analysis in analyses)
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                {
                    Id = analysis.Key,
                    Success = false,
                    Message = "Working ..."
                });

                if (!_useCurrentFile && 
                    (!ObjectTools.PublicInstancePropertiesEqual(analysis.Value.AnalysisConfiguration!, previousAnalysisConfiguration!, new[] { "DatasetType" }) ||
                     !ObjectTools.PublicInstancePropertiesEqual(analysis.Value.AnalysisConfiguration?.DatasetType!, previousAnalysisConfiguration?.DatasetType!, new[] { "Errors", "IsChanged" }) ||
                     analysis.Value.SubsettingExpression != previousSubsettingExpression))
                {
                    if (analysis.Value.AnalysisConfiguration!.FileName?.StartsWith("[") ?? false)
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = analysis.Key,
                            Success = false,
                            Message = "Reloading from data provider is not supported!"
                        });
                        continue;
                    }

                    if (!_rservice.LoadFileIntoGlobalEnvironment(analysis.Value.AnalysisConfiguration.FileName ?? string.Empty, analysis.Value.AnalysisConfiguration.FileType))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = analysis.Key,
                            Success = false,
                            Message = "Could not load file '" + analysis.Value.AnalysisConfiguration.FileName + "'!"
                        });
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(analysis.Value.AnalysisConfiguration.DatasetType?.IDvar) && !_rservice.SortRawDataStored(analysis.Value.AnalysisConfiguration.DatasetType!.IDvar))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = analysis.Key,
                            Success = false,
                            Message = "Could not load file '" + analysis.Value.AnalysisConfiguration.FileName + "'!"
                        });
                        continue;
                    }

                    if (!_rservice.TestAnalysisConfiguration(analysis.Value.AnalysisConfiguration, analysis.Value.SubsettingExpression))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = analysis.Key,
                            Success = false,
                            Message = "Could not use file for dataset type '" + analysis.Value.AnalysisConfiguration.DatasetType?.Name + "'!"
                        });
                        continue;
                    }

                    if (analysis.Value.AnalysisConfiguration.ModeKeep == false)
                    {
                        List<string>? additionalVariables = null;
                        if (analysis.Value is AnalysisRegression analysisRegression)
                        {
                            additionalVariables = new() { analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_" };
                        }
                        if (!_rservice.PrepareForAnalysis(analysis.Value, additionalVariables))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = analysis.Key,
                                Success = false,
                                Message = "Could not build file '" + analysis.Value.AnalysisConfiguration.FileName + "' for analysis!"
                            });
                            continue;
                        }
                    }
                }

                if (_useCurrentFile)
                {
                    if (analysis.Value.SubsettingExpression != previousSubsettingExpression)
                    {
                        if (!_rservice.TestAnalysisConfiguration(_currentConfiguration!, analysis.Value.SubsettingExpression))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = analysis.Key,
                                Success = false,
                                Message = "Could not reapply subsetting '" + analysis.Value.SubsettingExpression + "'!"
                            });
                            continue;
                        }

                        previousSubsettingExpression = analysis.Value.SubsettingExpression;
                    
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeChangedSubsettingMessage() { SubsettingExpression = analysis.Value.SubsettingExpression ?? string.Empty });
                    }

                    if (!(_currentConfiguration?.ModeKeep ?? true))
                    {
                        List<string>? additionalVariables = null;
                        if (analysis.Value is AnalysisRegression analysisRegression)
                        {
                            additionalVariables = new() { analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_" };
                        }
                        if (!_rservice.PrepareForAnalysis(analysis.Value, additionalVariables))
                        {
                            WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                            {
                                Id = analysis.Key,
                                Success = false,
                                Message = "Could not build current file for analysis!"
                            });
                            continue;
                        }
                    }

                    analysis.Value.AnalysisConfiguration = _currentConfiguration!;
                }

                previousSubsettingExpression = analysis.Value.SubsettingExpression;
                previousAnalysisConfiguration = analysis.Value.AnalysisConfiguration;

                DateTime beforeCalculation = DateTime.Now;

                switch (analysis.Value)
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

                if (analysis.Value.Result.Count == 0)
                {
                    WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                    {
                        Id = analysis.Key,
                        Success = false,
                        Message = "Could not calculate a result!"
                    });
                    continue;
                }
                    
                foreach (var groupByVariable in analysis.Value.GroupBy)
                {
                    var valueLabels = _rservice.GetValueLabels(groupByVariable.Name);
                    if (valueLabels != null)
                    {
                        analysis.Value.ValueLabels.Add(groupByVariable.Name, valueLabels);
                    }
                }

                analysis.Value.ResultAt = DateTime.Now;
                analysis.Value.ResultDuration = (analysis.Value.ResultAt! - beforeCalculation).Value.TotalSeconds;
                    
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage() { Id = analysis.Key, Success = true, Message = "Success!" });
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
