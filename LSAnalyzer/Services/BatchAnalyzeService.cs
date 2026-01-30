using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public BatchAnalyzeService(IRservice rservice, Configuration configuration, IServiceProvider serviceProvider)
    {
        _rservice = rservice;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
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
                if (analysis.AnalysisConfiguration.FileName?.StartsWith("[") ?? false)
                {
                    if (string.IsNullOrWhiteSpace(analysis.AnalysisConfiguration.FileRetrieval))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Reloading from data provider is not supported for json files created before v1.2!"
                        });
                        continue;
                    }

                    var providerRetrieval = RetrieveDataProvider(analysis.AnalysisConfiguration.FileRetrieval);

                    if (!providerRetrieval.success)
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = providerRetrieval.errorMessage ?? "Unknown error when retrieving data provider!",
                        });
                        continue;
                    }

                    var fileInformation = RetrieveFileInformation(providerRetrieval.dataProvider!,
                        analysis.AnalysisConfiguration.FileRetrieval);

                    if (!fileInformation.success)
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = fileInformation.errorMessage ?? "Unknown error when retrieving file information!",
                        });
                        continue;
                    }

                    if (!providerRetrieval.dataProvider!.LoadFileIntoGlobalEnvironment(fileInformation
                            .fileInformation!))
                    {
                        WeakReferenceMessenger.Default.Send(new BatchAnalyzeMessage()
                        {
                            Id = key,
                            Success = false,
                            Message = "Could not load file '" + analysis.AnalysisConfiguration.FileName + "'!"
                        });
                        continue;
                    }
                } else if (!_rservice.LoadFileIntoGlobalEnvironment(analysis.AnalysisConfiguration.FileName ?? string.Empty, analysis.AnalysisConfiguration.FileType))
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
                        additionalVariables = [analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_"];
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
                        additionalVariables = [analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_"];
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
    
    public class BatchAnalyzeMessage
    {
        public int Id { get; init; }
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public class BatchAnalyzeChangedStoredRawDataFileMessage;

    public class BatchAnalyzeChangedSubsettingMessage
    {
        public string SubsettingExpression { get; init; } = null!;
    }
}
