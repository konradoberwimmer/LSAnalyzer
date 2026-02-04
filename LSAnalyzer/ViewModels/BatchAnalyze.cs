using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.ObjectModel;
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
    partial void OnFileNameChanged(string? value)
    {
        ClearAnalysisData();
    }

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

    [ObservableProperty]
    private ObservableCollection<BatchEntry> _analysesTable = [];

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

        AnalysesTable =
        [
            new BatchEntry
            {
                Id = 1, Selected = true,
                Analysis = new AnalysisWithViewSettings
                    { Analysis = new AnalysisCorr(new AnalysisConfiguration()), ViewSettings = [] },
                Success = null, Message = string.Empty
            },
            new BatchEntry
            {
                Id = 2, Selected = true,
                Analysis = new AnalysisWithViewSettings
                    { Analysis = new AnalysisCorr(new AnalysisConfiguration()), ViewSettings = [] },
                Success = false, Message = "Could not calculate!"
            },
            new BatchEntry
            {
                Id = 3, Selected = false,
                Analysis = new AnalysisWithViewSettings
                {
                    Analysis = new AnalysisCorr(new AnalysisConfiguration())
                    {
                        Vars =
                        [
                            new Variable(1, "item1", false), new Variable(2, "item2", false),
                            new Variable(3, "item3", false), new Variable(4, "item4", false)
                        ]
                    },
                    ViewSettings = []
                },
                Success = true, Message = "Success!"
            },
        ];
        
        _batchAnalyzeService = new BatchAnalyzeServiceStub();
        _configuration = new Configuration();
    }

    public BatchAnalyze(BatchAnalyzeService batchAnalyzeService, Configuration configuration)
    {
        _batchAnalyzeService = batchAnalyzeService;
        _configuration = configuration;

        RecentBatchAnalyzeFiles = new ObservableCollection<string>(_configuration.GetStoredRecentBatchAnalyzeFiles());
        
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeProgression>(this, (_, _) =>
        {
            OnPropertyChanged(nameof(AnalysesTable));

            if (!AnalysesTable.All(entry => entry.Success is not null || entry.WasIgnored)) return;
            
            IsBusy = false;
            FinishedAllCalculations = true;
        });
        
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeChangedStoredRawDataFileMessage>(this, (_, _) => HasCurrentFile = false);
    }

    public void ClearAnalysisData()
    { 
        AnalysesTable = [];
        IsBusy = false;
        FinishedAllCalculations = false;
    }

    [RelayCommand]
    private void LoadBatchFile()
    {
        if (FileName == null || !File.Exists(FileName))
        {
            return;
        }

        AnalysesTable = [];
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
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeFailureMessage { Message = "File is not valid JSON or did not contain analysis requests!" });
                return;
            }
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(new BatchAnalyzeFailureMessage { Message = "File is not valid JSON or did not contain analysis requests!" });
            return;
        }

        _configuration.StoreRecentBatchAnalyzeFile(FileName);
        SelectedRecentBatchAnalyzeFile = null;
        RecentBatchAnalyzeFiles = new ObservableCollection<string>(_configuration.GetStoredRecentBatchAnalyzeFiles());
        
        for (var i = 0; i < analyses.Length; i++)
        {
            AnalysesTable.Add(new BatchEntry
            {
                Id = i + 1,
                Selected = true,
                Analysis = analyses[i],
                Message = string.Empty,
            });
        }
    }

    [RelayCommand]
    private void RunBatch()
    {
        if (AnalysesTable.Count == 0) return;

        IsBusy = true;

        _batchAnalyzeService.RunBatch(AnalysesTable, UseCurrentFile, CurrentConfiguration);
    }

    [RelayCommand]
    private void AbortBatch()
    {
        _batchAnalyzeService.AbortBatch();
    }

    [RelayCommand]
    private void TransferResults(ICloseable? window)
    {
        foreach (var entry in AnalysesTable.Where(entry => entry is { Success: true, Selected: true }))
        { 
            WeakReferenceMessenger.Default.Send(new BatchAnalyzeAnalysisReadyMessage(entry.Analysis));
        }

        window?.Close();
    }

    public partial class BatchEntry : ObservableValidatorExtended
    {
        [ObservableProperty] private int _id;

        [ObservableProperty] private bool _selected;

        [ObservableProperty] private AnalysisWithViewSettings _analysis = null!;

        [ObservableProperty] private bool? _success = null;
        partial void OnSuccessChanged(bool? value)
        {
            OnPropertyChanged(nameof(IsSuccess));
            OnPropertyChanged(nameof(IsFail));
        }

        [ObservableProperty] private bool _wasIgnored = false;
        
        [ObservableProperty] private string _message = string.Empty;

        public bool IsSuccess => Success is true;
        
        public bool IsFail => Success is false;
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
