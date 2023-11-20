using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public class BatchAnalyze : INotifyPropertyChanged
    {
        private Services.BatchAnalyze _batchAnalyzeService;

        private string? _fileName;
        public string? FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                NotifyPropertyChanged(nameof(FileName));
            }
        }

        private bool _hasCurrentFile = false;
        public bool HasCurrentFile
        {
            get => _hasCurrentFile;
            set
            {
                _hasCurrentFile = value;
                NotifyPropertyChanged(nameof(HasCurrentFile));

                if (!HasCurrentFile)
                {
                    UseCurrentFile = false;
                }
            }
        }

        private bool _useCurrentFile = false;
        public bool UseCurrentFile
        {
            get => _useCurrentFile;
            set
            {
                if (_useCurrentFile != value)
                {
                    _useCurrentFile = value;
                    NotifyPropertyChanged(nameof(UseCurrentFile));
                }
            }
        }

        private bool _currentModeKeep = true;
        public bool CurrentModeKeep
        {
            get => _currentModeKeep;
            set
            {
                _currentModeKeep = value;
                NotifyPropertyChanged(nameof(CurrentModeKeep));
            }
        }

        private Dictionary<int, Analysis>? _analysesDictionary = null;

        private DataTable? _analysesTable = null;
        public DataTable? AnalysesTable
        {
            get => _analysesTable;
            set
            {
                _analysesTable = value;
                NotifyPropertyChanged(nameof(AnalysesTable));
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                NotifyPropertyChanged(nameof(IsBusy));
            }
        }

        private bool _finishedAllCalculations = false;
        public bool FinishedAllCalculations
        {
            get => _finishedAllCalculations;
            set
            {
                _finishedAllCalculations = value;
                NotifyPropertyChanged(nameof(FinishedAllCalculations));
            }
        }

        [ExcludeFromCodeCoverage]
        public BatchAnalyze() 
        {
            // design-time only, parameterless constructor
            HasCurrentFile = false;
            UseCurrentFile = false;
            CurrentModeKeep = true;
        }

        public BatchAnalyze(Services.BatchAnalyze batchAnalyzeService)
        {
            _batchAnalyzeService = batchAnalyzeService;

            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                if (AnalysesTable != null)
                {
                    var row = AnalysesTable.Select("Number = " + m.Id).FirstOrDefault();
                    if (row != null)
                    {
                        row["Success"] = m.Success;
                    }
                }

                if (m.Id == _analysesDictionary?.Keys.Last())
                {
                    IsBusy = false;
                    FinishedAllCalculations = true;
                }
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

        private RelayCommand<object?>? _runBatchCommand;
        public ICommand RunBatchCommand
        {
            get
            {
                if (_runBatchCommand == null)
                    _runBatchCommand = new RelayCommand<object?>(this.RunBatch);
                return _runBatchCommand;
            }
        }

        private void RunBatch(object? dummy)
        {
            if (FileName == null || !File.Exists(FileName))
            {
                return;
            }

            AnalysesTable = null;
            FinishedAllCalculations = false;

            Analysis[] analyses = Array.Empty<Analysis>();
            try
            {
                analyses = JsonSerializer.Deserialize<Analysis[]>(File.ReadAllText(FileName))!;
            } catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new BatchAnalyzeFailureMessage() { Message = "File is not valid JSON or did not contain analysis requests!" });
                return;
            }

            _analysesDictionary = new();
            for (int i = 0; i < analyses.Length; i++)
            {
                _analysesDictionary.Add(i+1, analyses[i]);
            }

            DataTable analysesTable = new();
            analysesTable.Columns.Add("Number", typeof(int));
            var successColumn = analysesTable.Columns.Add("Success", typeof(bool));
            successColumn.AllowDBNull = true;
            analysesTable.Columns.Add("Info", typeof(string));
            
            foreach (var analysis in _analysesDictionary)
            {
                analysesTable.Rows.Add(new object?[] { analysis.Key, DBNull.Value, analysis.Value.ShortInfo });
            }

            AnalysesTable = analysesTable;

            IsBusy = true;

            _batchAnalyzeService.RunBatch(_analysesDictionary, UseCurrentFile, CurrentModeKeep);
        }

        private RelayCommand<ICloseable?>? _transferResultsCommand;
        public ICommand TransferResultsCommand
        {
            get
            {
                if (_transferResultsCommand == null)
                    _transferResultsCommand = new RelayCommand<ICloseable?>(this.TransferResults);
                return _transferResultsCommand;
            }
        }

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
    }

    public class BatchAnalyzeFailureMessage
    {
        public string Message { get; set; } = String.Empty;
    }

    public class BatchAnalyzeAnalysisReadyMessage
    {
        public readonly Analysis Analysis;

        public BatchAnalyzeAnalysisReadyMessage(Analysis analysis)
        {
            Analysis = analysis;
        }
    }
}
