using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels;

public partial class BatchAnalyze : ObservableObject
{
    private readonly IBatchAnalyzeService _batchAnalyzeService;
    
    private readonly Configuration _configuration;
    
    [ObservableProperty]
    private ObservableCollection<string> _recentBatchAnalyzeFiles = [];

    [ObservableProperty]
    private string? _selectedRecentBatchAnalyzeFile;
    partial void OnSelectedRecentBatchAnalyzeFileChanged(string? value)
    {
        if (value == null) return;
            
        if (!File.Exists(value))
        {
            SelectedRecentBatchAnalyzeFile = null;
            
            _configuration.RemoveRecentBatchAnalyzeFile(value);
            RecentBatchAnalyzeFiles = new ObservableCollection<string>(_configuration.GetStoredRecentBatchAnalyzeFiles());
                
            WeakReferenceMessenger.Default.Send(new RecentFileInvalidMessage(value));

            return;
        }

        FileName = value;
    }

    [ObservableProperty]
    private string? _fileName;

    [ObservableProperty]
    private bool _hasCurrentFile = false;
    partial void OnHasCurrentFileChanged(bool value)
    {
        if (!value)
        {
            UseCurrentFile = false;
        }
    }
    
    [ObservableProperty]
    private bool _useCurrentFile = false;

    [ObservableProperty]
    private AnalysisConfiguration? _currentConfiguration;

    private Dictionary<int, AnalysisWithViewSettings>? _analysesDictionary = null;

    [ObservableProperty]
    private DataTable? _analysesTable = null;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private bool _finishedAllCalculations = false;

    [ExcludeFromCodeCoverage]
    public BatchAnalyze() 
    {
        // design-time only, parameterless constructor
        HasCurrentFile = false;
        UseCurrentFile = false;

        _batchAnalyzeService = new BatchAnalyzeServiceStub();
        _configuration = new Configuration();
    }

    public BatchAnalyze(BatchAnalyzeService batchAnalyzeService, Configuration configuration)
    {
        _batchAnalyzeService = batchAnalyzeService;
        _configuration = configuration;

        RecentBatchAnalyzeFiles = new ObservableCollection<string>(_configuration.GetStoredRecentBatchAnalyzeFiles());
        
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeMessage>(this, (_, m) =>
        {
            if (AnalysesTable != null)
            {
                var row = AnalysesTable.Select("Number = " + m.Id).FirstOrDefault();
                if (row != null)
                {
                    row["Success"] = m.Success;
                    row["Message"] = m.Message;
                }
            }

            if ((m.Success || m.Message != "Working ...") && m.Id == _analysesDictionary?.Keys.Last())
            {
                IsBusy = false;
                FinishedAllCalculations = true;
            }
        });
        
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeChangedStoredRawDataFileMessage>(this, (_, _) => HasCurrentFile = false);
    }

    public void ClearAnalysisData()
    { 
        _analysesDictionary = null;
        AnalysesTable = null;
        IsBusy = false;
        FinishedAllCalculations = false;
    }

    [RelayCommand]
    private void RunBatch(object? dummy)
    {
        if (FileName == null || !File.Exists(FileName))
        {
            return;
        }

        AnalysesTable = null;
        FinishedAllCalculations = false;

        AnalysisWithViewSettings[] analyses;
        try
        {
            analyses = JsonSerializer.Deserialize<AnalysisWithViewSettings[]>(File.ReadAllText(FileName))!;
        }
        catch (JsonException)
        {
            try
            {
                var analysesWithoutViewSettings = JsonSerializer.Deserialize<Analysis[]>(File.ReadAllText(FileName))!;
                AnalysisPresentation dummyAnalysisPresentation = new();

                analyses = analysesWithoutViewSettings
                    .Select(analysis => new AnalysisWithViewSettings
                    {
                        Analysis = analysis,
                        ViewSettings = dummyAnalysisPresentation.ViewSettings
                    }).ToArray();
            } catch (Exception)
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeFailureMessage() { Message = "File is not valid JSON or did not contain analysis requests!" });
                return;
            }
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(new BatchAnalyzeFailureMessage() { Message = "File is not valid JSON or did not contain analysis requests!" });
            return;
        }

        _configuration.StoreRecentBatchAnalyzeFile(FileName);
        SelectedRecentBatchAnalyzeFile = null;
        RecentBatchAnalyzeFiles = new ObservableCollection<string>(_configuration.GetStoredRecentBatchAnalyzeFiles());
        
        _analysesDictionary = new();
        for (int i = 0; i < analyses.Length; i++)
        {
            _analysesDictionary.Add(i+1, analyses[i]);
        }

        DataTable analysesTable = new();
        analysesTable.Columns.Add("Number", typeof(int));
        analysesTable.Columns.Add("Info", typeof(string));
        var successColumn = analysesTable.Columns.Add("Success", typeof(bool));
        successColumn.AllowDBNull = true;
        analysesTable.Columns.Add("Message", typeof(string));

        foreach (var analysis in _analysesDictionary)
        {
            analysesTable.Rows.Add(new object?[] { analysis.Key, analysis.Value.Analysis.ShortInfo, DBNull.Value, string.Empty });
        }

        AnalysesTable = analysesTable;

        IsBusy = true;

        _batchAnalyzeService.RunBatch(_analysesDictionary, UseCurrentFile, CurrentConfiguration);
    }

    [RelayCommand]
    private void TransferResults(ICloseable? window)
    {
        if (AnalysesTable == null || _analysesDictionary == null)
        {
            return;
        }

        foreach (var row in AnalysesTable.Rows)
        {
            var dataRow = row as DataRow;
            if ((bool)dataRow!["Success"] && _analysesDictionary.ContainsKey((int)dataRow["Number"]))
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeAnalysisReadyMessage(_analysesDictionary[(int)dataRow["Number"]]));
            }
        }

        window?.Close();
    }
    
    public class BatchAnalyzeFailureMessage
    {
        public string Message { get; init; } = string.Empty;
    }

    public class BatchAnalyzeAnalysisReadyMessage(AnalysisWithViewSettings analysisWithViewSettings)
    {
        public readonly AnalysisWithViewSettings AnalysisWithViewSettings = analysisWithViewSettings;
    }
}
