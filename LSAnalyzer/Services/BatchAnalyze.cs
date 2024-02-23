using CommunityToolkit.Mvvm.Messaging;
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
        private bool _currentModeKeep = true;

        public BatchAnalyze(Rservice rservice)
        {
            _rservice = rservice;
        }

        public void RunBatch(Dictionary<int, Analysis> analyses, bool useCurrentFile, bool currentModeKeep = true)
        {
            _useCurrentFile = useCurrentFile;
            _currentModeKeep = currentModeKeep;


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
            if (e.Argument is not Dictionary<int, Analysis> analyses)
            {
                e.Cancel = true;
                return;
            }

            foreach (var analysis in analyses)
            {
                if (!_useCurrentFile)
                {
                    if (!_rservice.LoadFileIntoGlobalEnvironment(analysis.Value.AnalysisConfiguration.FileName ?? string.Empty, analysis.Value.AnalysisConfiguration.FileType, analysis.Value.AnalysisConfiguration.DatasetType?.IDvar))
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

                if (_useCurrentFile && !_currentModeKeep)
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
}
