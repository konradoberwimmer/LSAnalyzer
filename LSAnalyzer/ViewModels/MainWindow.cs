using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels;

public partial class MainWindow : ObservableObject
{
    private readonly IRservice _rservice;
    
    private readonly IAnalysisQueue _analysisQueue;

    private readonly Configuration _configuration;

    [ObservableProperty]
    private AnalysisConfiguration? _analysisConfiguration;
    partial void OnAnalysisConfigurationChanged(AnalysisConfiguration? value)
    {
        SubsettingExpression = null;
        RecentAnalyses.Clear();
    }

    public List<Variable> CurrentDatasetVariables { get; set; } = [];

    public string BifieSurveyVersion { get; set; } = string.Empty; 
    
    [ObservableProperty]
    private string? _subsettingExpression;

    [ObservableProperty]
    private ObservableCollection<AnalysisPresentation> _analyses = [];

    public bool ConnectedToR => _rservice.IsConnected;

    public bool HasNecessaryPackages => _rservice.NecessaryPackagesConfirmed;

    public bool RIsBusy => _analysisQueue.Count > 0;

    public Dictionary<Type, Analysis> RecentAnalyses { get; } = new();

    [ExcludeFromCodeCoverage]
    public MainWindow()
    {
        // design-time only constructor
        _rservice = new RserviceStub();
        _analysisQueue = new AnalysisQueueStub();
        _configuration = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryServiceStub());
        
        AnalysisConfiguration dummyConfiguration = new()
        {
            FileName = "C:\\dummyDirectory\\dummyDataset.sav",
            DatasetType = new()
            {
                Name = "Dummy Dataset Type",
                Weight = "dummyWgt",
                NMI = 10,
                MIvar = "dummyMiwar",
                RepWgts = "dummyRepwgts",
                FayFac = 1,
            },
            ModeKeep = true,
        };

        Analyses =
        [
            new()
            {
                Analysis = new AnalysisUnivar(dummyConfiguration)
                {
                    Vars =
                    [
                        new(1, "x1"),
                        new(2, "x2"),
                        new(3, "x3")
                    ],
                    GroupBy =
                    [
                        new(4, "y1")
                    ],
                    SubsettingExpression = "cat == 1 & val < 0.5",
                },
                DataTable = new()
                {
                    Columns =
                    {
                        { "var", typeof(string) }, { "y1", typeof(int) }, { "mean", typeof(double) },
                        { "mean__se", typeof(double) }, { "sd", typeof(double) }, { "sd__se", typeof(double) }
                    },
                    Rows =
                    {
                        { "x1", 1, 0.5, 0.01, 0.1, 0.001 },
                        { "x1", 2, 0.6, 0.006, 0.12, 0.0011 },
                        { "x1", 3, 0.7, 0.012, 0.09, 0.0009 },
                        { "x1", 4, 0.8, 0.011, 0.11, 0.0011 },
                        { "x2", 1, 12.5, 0.12, 1.41, 0.023 },
                        { "x2", 2, 11.3, 0.13, 1.02, 0.064 },
                        { "x2", 3, 9.8, 0.22, 2.01, 0.044 },
                        { "x2", 4, 12.1, 0.21, 2.01, 0.031 },
                    }
                },
                HasTableAverage = true,
                SecondaryTable = new("Explained variance")
                {
                    Columns =
                    {
                        { "var", typeof(string) }, { "eta2", typeof(double) }, { "eta", typeof(double) },
                        { "eta__se", typeof(double) }
                    },
                    Rows =
                    {
                        { "x", 0.25, 0.50, 0.02 },
                        { "y", 0.16, 0.40, 0.15 },
                    },
                },
            }
        ];

        Analyses.First().DataView = new(Analyses.First().DataTable);
        Analyses.First().SecondaryDataView = new(Analyses.First().SecondaryTable);

        AnalysisConfiguration = dummyConfiguration;
        SubsettingExpression = "cat == 1";
    }

    public MainWindow(IRservice rservice, IAnalysisQueue analysisQueue, Configuration configuration) 
    {
        _rservice = rservice;
        _analysisQueue = analysisQueue;
        _configuration = configuration;
        
        BifieSurveyVersion = rservice.GetBifieSurveyVersion() ?? string.Empty;

        WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (_, m) =>
        {
            AnalysisConfiguration = m.Value;

            var virtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration.FileNameWithoutPath ?? string.Empty, AnalysisConfiguration.DatasetType ?? new DatasetType { Id = -1 });

            CurrentDatasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, virtualVariables) ?? [];
        });

        WeakReferenceMessenger.Default.Register<SetSubsettingExpressionMessage>(this, (_, m) =>
        {
            SubsettingExpression = string.IsNullOrWhiteSpace(m.Value) ? null : m.Value;
        });

        WeakReferenceMessenger.Default.Register<RequestAnalysisMessage>(this, (_, m) =>
        {
            var analysis = m.Value;
            analysis.VirtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration?.FileNameWithoutPath ?? string.Empty, AnalysisConfiguration?.DatasetType ?? new DatasetType { Id = -1 });
            
            AnalysisPresentation analysisPresentation = new(analysis, this);

            Analyses.Add(analysisPresentation);
            if (RecentAnalyses.ContainsKey(m.Value.GetType()))
            {
                RecentAnalyses.Remove(m.Value.GetType());
            }
            RecentAnalyses.Add(m.Value.GetType(), m.Value);

            StartAnalysisCommand.Execute(analysisPresentation);
        });
        
        WeakReferenceMessenger.Default.Register<AnalysisQueue.AnalysisQueueCountChangedMessage>(this, (_,_) => OnPropertyChanged(nameof(RIsBusy)));
        
        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeChangedStoredRawDataFileMessage>(this, (_, _) =>
        {
            AnalysisConfiguration = null;
        });

        WeakReferenceMessenger.Default.Register<BatchAnalyzeService.BatchAnalyzeChangedSubsettingMessage>(this, (_, m) =>
        {
            SubsettingExpression = string.IsNullOrEmpty(m.SubsettingExpression) ? null : m.SubsettingExpression;
        });

        WeakReferenceMessenger.Default.Register<BatchAnalyze.BatchAnalyzeAnalysisReadyMessage>(this, (_, m) =>
        {
            var analysisPresentation = m.AnalysisPresentation;
            analysisPresentation.MainWindowViewModel = this;
            
            Analyses.Add(analysisPresentation);
        });
    }

    [RelayCommand]
    private void StartAnalysis(AnalysisPresentation? analysisPresentation)
    {
        if (analysisPresentation == null)
        {
            return;
        }

        analysisPresentation.Analysis.SubsettingExpression = SubsettingExpression;
        analysisPresentation.IsBusy = true;

        _analysisQueue.Add(analysisPresentation);
    }

    [RelayCommand]
    private void RemoveAnalysis(AnalysisPresentation? analysisPresentation)
    {
        if (analysisPresentation == null || !Analyses.Contains(analysisPresentation)) return;
        
        _analysisQueue.InterruptAnalysis(analysisPresentation);
        Analyses.Remove(analysisPresentation);
    }

    [RelayCommand]
    private void SaveAnalysesDefinitions(string? fileName)
    {
        if (fileName == null || Analyses.Count == 0)
        {
            return;
        }

        var contentToSerialize = Analyses.Select(analysisPresentation => new AnalysisWithViewSettings
        {
            Analysis = analysisPresentation.Analysis,
            ViewSettings = analysisPresentation.ViewSettings,
        }).ToArray();

        File.WriteAllText(fileName, JsonSerializer.Serialize(contentToSerialize));
    }
    
    [RelayCommand]
    private void RemoveAllAnalyses(object? dummy)
    {
        Analyses.Clear();
    }

    [RelayCommand]
    private void ReloadCurrentDataset()
    {
        if (AnalysisConfiguration?.FileName is null || AnalysisConfiguration.DatasetType is null) return;
        
        var virtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration.FileNameWithoutPath!, AnalysisConfiguration.DatasetType);

        if (!_rservice.TestAnalysisConfiguration(AnalysisConfiguration, virtualVariables, SubsettingExpression))
        {
            WeakReferenceMessenger.Default.Send(new ReloadErrorMessage());
            
            AnalysisConfiguration = null;
            return;
        }
        
        CurrentDatasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, virtualVariables) ?? [];
    }

    public class ReloadErrorMessage;
}
