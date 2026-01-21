using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels
{
    public partial class MainWindow : ObservableObject
    {

        private readonly IRservice _rservice;

        [ObservableProperty]
        private AnalysisConfiguration? _analysisConfiguration;
        partial void OnAnalysisConfigurationChanged(AnalysisConfiguration? value)
        {
            SubsettingExpression = null;
            RecentAnalyses.Clear();
        }

        [ObservableProperty]
        private string? _subsettingExpression;

        [ObservableProperty]
        private ObservableCollection<AnalysisPresentation> _analyses = [];

        public bool ConnectedToR => _rservice.IsConnected;

        public bool HasNecessaryPackages => _rservice.NecessaryPackagesConfirmed;

        public bool IsBusy => Analyses.Any(analysis => analysis.IsBusy);

        public void NotifyIsBusy()
        {
            OnPropertyChanged(nameof(IsBusy));
        }

        public Dictionary<Type, Analysis> RecentAnalyses { get; } = new();

        [ExcludeFromCodeCoverage]
        public MainWindow()
        {
            // design-time only constructor
            _rservice = new RserviceStub();
            
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
                            new(1, "x1", false),
                            new(2, "x2", false),
                            new(3, "x3", false)
                        ],
                        GroupBy =
                        [
                            new(4, "y1", false)
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

        public MainWindow(IRservice rservice) 
        {
            _rservice = rservice;

            WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (_, m) =>
            {
                AnalysisConfiguration = m.Value;
            });

            WeakReferenceMessenger.Default.Register<SetSubsettingExpressionMessage>(this, (_, m) =>
            {
                SubsettingExpression = string.IsNullOrWhiteSpace(m.Value) ? null : m.Value;
            });

            WeakReferenceMessenger.Default.Register<RequestAnalysisMessage>(this, (_, m) =>
            {
                AnalysisPresentation analysisPresentation = new(m.Value, this);

                Analyses.Add(analysisPresentation);
                if (RecentAnalyses.ContainsKey(m.Value.GetType()))
                {
                    RecentAnalyses.Remove(m.Value.GetType());
                }
                RecentAnalyses.Add(m.Value.GetType(), m.Value);

                StartAnalysisCommand.Execute(analysisPresentation);
            });

            WeakReferenceMessenger.Default.Register<BatchAnalyzeChangedStoredRawDataFileMessage>(this, (_, _) =>
            {
                AnalysisConfiguration = null;
            });

            WeakReferenceMessenger.Default.Register<BatchAnalyzeChangedSubsettingMessage>(this, (_, m) =>
            {
                SubsettingExpression = string.IsNullOrEmpty(m.SubsettingExpression) ? null : m.SubsettingExpression;
            });

            WeakReferenceMessenger.Default.Register<BatchAnalyzeAnalysisReadyMessage>(this, (_, m) =>
            {
                AnalysisPresentation analysisPresentation = new(m.AnalysisWithViewSettings.Analysis, this);
                
                Analyses.Add(analysisPresentation);

                analysisPresentation.SetAnalysisResult(m.AnalysisWithViewSettings.Analysis.Result);
                
                analysisPresentation.ApplyDeserializedViewSettings(m.AnalysisWithViewSettings.ViewSettings);
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

            BackgroundWorker analysisWorker = new();
            analysisWorker.WorkerReportsProgress = false;
            analysisWorker.WorkerSupportsCancellation = false;
            analysisWorker.DoWork += AnalysisWorker_DoWork;
            analysisWorker.RunWorkerAsync(analysisPresentation);
        }

        private void AnalysisWorker_DoWork (object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is not AnalysisPresentation analysisPresentation)
            {
                e.Cancel = true;
                return;
            }

            DateTime beforeCalculation = DateTime.Now;
            List<GenericVector>? result = null;
            
            switch (analysisPresentation.Analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    result = _rservice.CalculateUnivar(analysisUnivar);
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    result = _rservice.CalculateMeanDiff(analysisMeanDiff);
                    break;
                case AnalysisFreq analysisFreq:
                    result = _rservice.CalculateFreq(analysisFreq);
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    result = _rservice.CalculatePercentiles(analysisPercentiles);
                    break;
                case AnalysisCorr analysisCorr:
                    result = _rservice.CalculateCorr(analysisCorr);
                    break;
                case AnalysisLinreg analysisLinreg:
                    result = _rservice.CalculateLinreg(analysisLinreg);
                    break;
                case AnalysisLogistReg analysisLogistReg:
                    result = _rservice.CalculateLogistReg(analysisLogistReg);
                    break;
            }

            if (result == null)
            {
                WeakReferenceMessenger.Default.Send(new FailureWithAnalysisCalculationMessage(analysisPresentation.Analysis));
            } else
            {
                if (analysisPresentation.Analysis is AnalysisFreq { CalculateBivariate: true } analysisFreq)
                {
                    analysisFreq.BivariateResult = _rservice.CalculateBivariate(analysisFreq);
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
            }

            e.Result = result;
        }

        [RelayCommand]
        private void RemoveAnalysis(AnalysisPresentation? analysisPresentation)
        {
            if (analysisPresentation != null && Analyses.Contains(analysisPresentation))
            {
                Analyses.Remove(analysisPresentation);
            }
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
    }

    internal class FailureWithAnalysisCalculationMessage(Analysis analysis) : ValueChangedMessage<Analysis>(analysis);
}
