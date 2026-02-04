using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LSAnalyzer.Services;

public class BatchAnalyzeService : IBatchAnalyzeService
{
    private readonly IRservice _rservice;
    private readonly Configuration _configuration;
    private readonly IServiceProvider _serviceProvider;
    
    private bool _useCurrentFile = true;
    private AnalysisConfiguration? _currentConfiguration;
    private BackgroundWorker? _worker;

    public BatchAnalyzeService(IRservice rservice, Configuration configuration, IServiceProvider serviceProvider)
    {
        _rservice = rservice;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public void RunBatch(IEnumerable<BatchAnalyze.BatchEntry> analyses, bool useCurrentFile, AnalysisConfiguration? currentConfiguration)
    {
        if (_worker?.IsBusy is true) return;
        
        _useCurrentFile = useCurrentFile;
        _currentConfiguration = currentConfiguration;

        if (analyses.Any() && !_useCurrentFile)
        {
            WeakReferenceMessenger.Default.Send(new BatchAnalyzeChangedStoredRawDataFileMessage());
        }

        _worker = new BackgroundWorker();
        _worker.WorkerReportsProgress = false;
        _worker.WorkerSupportsCancellation = true;
        _worker.DoWork += DoBatch;
        _worker.RunWorkerAsync(analyses);
    }

    public void AbortBatch()
    {
        _worker?.CancelAsync();
        _rservice.SendUserInterrupt();
    }

    private void DoBatch(object? sender, DoWorkEventArgs e)
    {
        if (e.Argument is not IEnumerable<BatchAnalyze.BatchEntry> analyses || (_useCurrentFile && _currentConfiguration == null))
        {
            e.Cancel = true;
            return;
        }

        foreach (var entry in analyses)
        {
            entry.Success = null;
            entry.WasIgnored = false;
            entry.Message = string.Empty;
        }

        AnalysisConfiguration? previousAnalysisConfiguration = null;
        string? previousSubsettingExpression = "$$$initialize$$$";

        foreach (var entry in analyses)
        {
            if (_worker!.CancellationPending)
            {
                entry.WasIgnored = true;
                continue;
            }
            
            if (!entry.Selected)
            {
                entry.WasIgnored = true;
                WeakReferenceMessenger.Default.Send<BatchAnalyzeProgression>();
                continue;
            }
            
            entry.Message = "Working ...";
            WeakReferenceMessenger.Default.Send<BatchAnalyzeProgression>();
            
            var analysis = entry.Analysis.Analysis;

            if (!_useCurrentFile && (
                    previousAnalysisConfiguration == null || 
                    !analysis.AnalysisConfiguration.IsEqual(previousAnalysisConfiguration) || 
                    analysis.SubsettingExpression != previousSubsettingExpression))
            {
                if (analysis.AnalysisConfiguration.FileName?.StartsWith("[") ?? false)
                {
                    if (string.IsNullOrWhiteSpace(analysis.AnalysisConfiguration.FileRetrieval))
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr("Reloading from data provider is not supported for json files created before v1.2!");
                        continue;
                    }

                    var providerRetrieval = RetrieveDataProvider(analysis.AnalysisConfiguration.FileRetrieval);

                    if (!providerRetrieval.success)
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr(providerRetrieval.errorMessage ?? "Unknown error when retrieving data provider!");
                        continue;
                    }

                    var fileInformation = RetrieveFileInformation(providerRetrieval.dataProvider!,
                        analysis.AnalysisConfiguration.FileRetrieval);

                    if (!fileInformation.success)
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr(fileInformation.errorMessage ?? "Unknown error when retrieving file information!");
                        continue;
                    }

                    if (!providerRetrieval.dataProvider!.LoadFileIntoGlobalEnvironment(fileInformation
                            .fileInformation!))
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr("Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!");
                        continue;
                    }
                } else if (!_rservice.LoadFileIntoGlobalEnvironment(analysis.AnalysisConfiguration.FileName ?? string.Empty, analysis.AnalysisConfiguration.FileType))
                {
                    entry.Success = false;
                    entry.Message = AbortedOr("Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(analysis.AnalysisConfiguration.DatasetType?.IDvar) && !_rservice.SortRawDataStored(analysis.AnalysisConfiguration.DatasetType!.IDvar))
                {
                    entry.Success = false;
                    entry.Message = AbortedOr("Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!");
                    continue;
                }

                if (!_rservice.TestAnalysisConfiguration(analysis.AnalysisConfiguration, analysis.SubsettingExpression))
                {
                    entry.Success = false;
                    entry.Message = AbortedOr("Could not use file for dataset type '" + analysis.AnalysisConfiguration.DatasetType?.Name + "'!");
                    continue;
                }

                if (analysis.AnalysisConfiguration.ModeKeep == false)
                {
                    List<string>? additionalVariables = null;
                    if (analysis is AnalysisRegression analysisRegression)
                    {
                        additionalVariables = [analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_"];
                    }
                    if (!_rservice.PrepareForAnalysis(analysis, additionalVariables))
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr("Could not build file '" + analysis.AnalysisConfiguration.FileName + "' for analysis!");
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
                        entry.Success = false;
                        entry.Message = AbortedOr("Could not reapply subsetting '" + analysis.SubsettingExpression + "'!");
                        continue;
                    }

                    previousSubsettingExpression = analysis.SubsettingExpression;
                
                    WeakReferenceMessenger.Default.Send(new BatchAnalyzeChangedSubsettingMessage { SubsettingExpression = analysis.SubsettingExpression ?? string.Empty });
                }

                if (!(_currentConfiguration?.ModeKeep ?? true))
                {
                    List<string>? additionalVariables = null;
                    if (analysis is AnalysisRegression analysisRegression)
                    {
                        additionalVariables = [analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_"];
                    }
                    if (!_rservice.PrepareForAnalysis(analysis, additionalVariables))
                    {
                        entry.Success = false;
                        entry.Message = AbortedOr("Could not build current file for analysis!");
                        continue;
                    }
                }

                analysis.AnalysisConfiguration = _currentConfiguration!;
            }

            previousSubsettingExpression = analysis.SubsettingExpression;
            previousAnalysisConfiguration = analysis.AnalysisConfiguration;

            var beforeCalculation = DateTime.Now;

            switch (analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    analysisUnivar.Result = _rservice.CalculateUnivar(analysisUnivar) ?? [];
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    analysisMeanDiff.Result = _rservice.CalculateMeanDiff(analysisMeanDiff) ?? [];
                    break;
                case AnalysisFreq analysisFreq:
                    analysisFreq.Result = _rservice.CalculateFreq(analysisFreq) ?? [];
                    if (analysisFreq.CalculateBivariate)
                    {
                        analysisFreq.BivariateResult = _rservice.CalculateBivariate(analysisFreq);
                    }
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    analysisPercentiles.Result = _rservice.CalculatePercentiles(analysisPercentiles) ?? [];
                    break;
                case AnalysisCorr analysisCorr:
                    analysisCorr.Result = _rservice.CalculateCorr(analysisCorr) ?? [];
                    break;
                case AnalysisLinreg analysisLinreg:
                    analysisLinreg.Result = _rservice.CalculateLinreg(analysisLinreg) ?? [];
                    break;
                case AnalysisLogistReg analysisLogistReg:
                    analysisLogistReg.Result = _rservice.CalculateLogistReg(analysisLogistReg) ?? [];
                    break;
            }

            if (analysis.Result.Count == 0 || analysis is AnalysisFreq { CalculateBivariate: true, BivariateResult.Count: 0 })
            {
                entry.Success = false;
                entry.Message = AbortedOr("Could not calculate a result!");
                continue;
            }
                
            foreach (var groupByVariable in analysis.GroupBy)
            {
                var valueLabels = _rservice.GetValueLabels(groupByVariable.Name);
                if (valueLabels != null)
                {
                    analysis.ValueLabels.TryAdd(groupByVariable.Name, valueLabels);
                }
            }

            analysis.ResultAt = DateTime.Now;
            analysis.ResultDuration = (analysis.ResultAt! - beforeCalculation).Value.TotalSeconds;

            entry.Success = true;
            entry.Message = "Success!";
        }
        
        WeakReferenceMessenger.Default.Send<BatchAnalyzeProgression>();

        e.Result = analyses;
    }

    private string AbortedOr(string message)
    {
        return _worker?.CancellationPending is true ? "Aborted!" : message;
    }
    
    public (bool success, string? errorMessage, IDataProvider? dataProvider) RetrieveDataProvider(string fileRetrieval)
    {
        JsonObject? fileRetrievalObject;
        try
        {
            fileRetrievalObject = JsonSerializer.Deserialize<JsonObject>(fileRetrieval);
        }
        catch (Exception)
        {
            return (false, "Could not read data provider configuration!", null);
        }
        
        if (fileRetrievalObject is null || 
            !fileRetrievalObject.ContainsKey("Provider"))
            return (false, "Could not read data provider configuration!", null);

        IDataProviderConfiguration? dataProviderConfiguration;
        try
        {
            dataProviderConfiguration = JsonSerializer.Deserialize<IDataProviderConfiguration>(fileRetrievalObject["Provider"]!.ToJsonString());
        }
        catch (Exception)
        {
            return (false, "Could not read data provider configuration!", null);
        }
        
        if (dataProviderConfiguration is null)
            return (false, "Could not read data provider configuration!", null);
        
        var matchingConfiguration = _configuration.GetMatchingDataProviderConfiguration(dataProviderConfiguration);
        
        if (matchingConfiguration is null)
            return (false, "Data provider configuration is no longer available!", null);

        var dataProvider = matchingConfiguration.CreateService(_serviceProvider);
        
        return (true, null, dataProvider);
    }

    public (bool success, string? errorMessage, dynamic? fileInformation) RetrieveFileInformation(IDataProvider dataProvider, string fileRetrieval)
    {
        JsonObject? fileRetrievalObject;
        try
        {
            fileRetrievalObject = JsonSerializer.Deserialize<JsonObject>(fileRetrieval);
        }
        catch (Exception)
        {
            return (false, "Could not read file information!", null);
        }
        
        if (fileRetrievalObject is null || 
            !fileRetrievalObject.ContainsKey("File"))
            return (false, "Could not read file information!", null);

        var fileInformation = dataProvider.DeserializeFileRetrieval(fileRetrievalObject["File"]!.ToJsonString());
        
        if (fileInformation is null)
            return (false, "Could not read file information!", null);

        return (true, null, fileInformation);
    }

    public class BatchAnalyzeProgression;

    public class BatchAnalyzeChangedStoredRawDataFileMessage;

    public class BatchAnalyzeChangedSubsettingMessage
    {
        public string SubsettingExpression { get; init; } = null!;
    }
}
