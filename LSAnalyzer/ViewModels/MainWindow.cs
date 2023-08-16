using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels
{
    public class MainWindow : INotifyPropertyChanged
    {

        private AnalysisConfiguration? _analysisConfiguration;
        public AnalysisConfiguration? AnalysisConfiguration
        {
            get => _analysisConfiguration;
            set
            {
                _analysisConfiguration = value;
                NotifyPropertyChanged(nameof(AnalysisConfiguration));
            }
        }

        private ObservableCollection<AnalysisPresentation> _analyses = new();
        public ObservableCollection<AnalysisPresentation> Analyses
        {
            get => _analyses;
            set
            {
                _analyses = value;
                NotifyPropertyChanged(nameof(Analyses));
            }
        }

        [ExcludeFromCodeCoverage]
        public MainWindow()
        {
            // design-time only constructor
            AnalysisConfiguration dummyConfiguration = new()
            {
                FileName = "C:\\dummyDirectory\\dummyDataset.sav",
                DatasetType = new()
                {
                    Name = "Dummy Dataset Type",
                    Weight = "dummyWgt",
                    NMI = 10,
                    MIvar = "dummyMiwar",
                    Nrep = 5,
                    RepWgts = "dummyRepwgts",
                    FayFac = 1,
                },
                ModeKeep = true,
            };

            Analyses = new()
            {
                new()
                {
                    Analysis = new AnalysisUnivar(dummyConfiguration)
                    {
                        Vars = new()
                        {
                            new(1, "x1", false),
                            new(2, "x2", false),
                            new(3, "x3", false),
                        },
                        GroupBy = new()
                        {
                            new(4, "y1", false),
                        },
                    },
                    DataTable = new()
                    {
                        Columns = { { "var", typeof(string) }, { "y1" , typeof(int) }, { "mean", typeof(double) }, { "mean__se", typeof(double) }, { "sd", typeof(double) }, { "sd__se", typeof(double) } },
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
                            { "x3", 1, -2.28, 0.23, 0.5, 0.012 },
                            { "x3", 2, 3.12, 0.73, 0.3, 0.031 },
                            { "x3", 3, 1.02, 0.32, 0.3, 0.021 },
                            { "x3", 4, -0.45, 0.64, 0.7, 0.011 },
                        }
                    },
                }
            };
        }

        public MainWindow(Rservice rservice) 
        {
            WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (r, m) =>
            {
                AnalysisConfiguration = m.Value;
            });

            WeakReferenceMessenger.Default.Register<RequestAnalysisMessage>(this, (r, m) =>
            {
                Analyses.Add(new(m.Value));
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
