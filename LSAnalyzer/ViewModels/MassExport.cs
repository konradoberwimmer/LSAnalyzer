using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;

namespace LSAnalyzer.ViewModels;

public partial class MassExport : ObservableObject
{
    public static ExportType LastExportType { get; set; } = 
        AnalysisPresentation.ExportTypes.First(t => t.Name == Properties.Settings.Default.defaultExportType);

    private IExportService _exportService;
    
    private List<AnalysisPresentation> _analysisPresentations = [];
    public List<AnalysisPresentation> AnalysisPresentations
    {
        get => _analysisPresentations;
        set
        {
            _analysisPresentations = value;
            OnPropertyChanged(nameof(CanExport));
        }
    }
    
    [ObservableProperty] private string _folder = string.Empty;
    partial void OnFolderChanged(string value)
    {
        OnPropertyChanged(nameof(CanExport));
    }
    
    [ObservableProperty] private string _prefix = string.Empty;
    partial void OnPrefixChanged(string value)
    {
        OnPropertyChanged(nameof(CanExport));
    }
    
    [ObservableProperty] private ObservableCollection<ExportType> _exportTypes = new(AnalysisPresentation.ExportTypes);
    
    [ObservableProperty] private ExportType _selectedExportType = LastExportType;
    partial void OnSelectedExportTypeChanged(ExportType value)
    {
        OnPropertyChanged(nameof(CanSingleExcelFile));
    }

    public bool CanSingleExcelFile => SelectedExportType.Name.StartsWith("excel");
    
    [ObservableProperty] private bool _singleExcelFile = true;

    public bool CanExport => !string.IsNullOrEmpty(Folder) && !string.IsNullOrEmpty(Prefix);
    
    public bool IsBusy { get; set; } = false;

    [ExcludeFromCodeCoverage]
    public MassExport()
    {
        // parameter-less constructor for design-time only
        _exportService = null!;
    }

    public MassExport(IExportService exportService)
    {
        _exportService = exportService;
    }
    
    [RelayCommand]
    private void Export(ICloseable? window)
    {
        IsBusy = true;
        LastExportType = SelectedExportType;
        
        var allFileNames = _exportService.AllMassExportFileNames(Folder, Prefix, SelectedExportType, CanSingleExcelFile && SingleExcelFile, AnalysisPresentations.Select(analysisPresentation => analysisPresentation.Analysis).ToList());
        foreach (var fileName in allFileNames.Where(File.Exists))
        {
            try
            {
                File.Delete(fileName);
            }
            catch (IOException)
            {
                WeakReferenceMessenger.Default.Send(new FileInUseMessage { FileName = fileName });
                IsBusy = false;
                return;
            }
        }

        BackgroundWorker massExportWorker = new();
        massExportWorker.WorkerReportsProgress = false;
        massExportWorker.WorkerSupportsCancellation = false;
        massExportWorker.DoWork += MassExportWorker;
        massExportWorker.RunWorkerAsync(window);
    }

    public void MassExportWorker(object? sender, DoWorkEventArgs e)
    {
        var window = e.Argument as ICloseable;
        
        var allFileNames = _exportService.AllMassExportFileNames(Folder, Prefix, SelectedExportType, CanSingleExcelFile && SingleExcelFile, AnalysisPresentations.Select(analysisPresentation => analysisPresentation.Analysis).ToList());
        
        List<object> outputObjects = [];
        
        if (CanSingleExcelFile && SingleExcelFile)
        {
            outputObjects.Add(_exportService.CreateXlsxExport(AnalysisPresentations, SelectedExportType.Name != "excelWithoutStyles"));
        }
        else
        {
            foreach (var analysisPresentation in AnalysisPresentations)
            {
                switch (SelectedExportType.Name)
                {
                    case "excelWithStyles":
                    case "excelWithoutStyles":
                        outputObjects.Add(_exportService.CreateXlsxExport(analysisPresentation.Analysis, analysisPresentation.DataView, analysisPresentation.SecondaryDataView, analysisPresentation.ColumnTooltips, SelectedExportType.Name != "excelWithoutStyles"));
                        break;
                    case "csvMultiple":
                        outputObjects.AddRange(_exportService.CreateCsvExport(analysisPresentation.Analysis, analysisPresentation.DataView, analysisPresentation.SecondaryDataView));
                        break;
                    case "csvMainTable":
                        outputObjects.Add(_exportService.CreateCsvExport(analysisPresentation.Analysis, analysisPresentation.DataView, null, false)[0]);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        if (allFileNames.Count != outputObjects.Count)
        {
            throw new InvalidOperationException();
        }

        var currentFileNameIndex = 0;
        foreach (var outputObject in outputObjects)
        {
            switch (outputObject)
            {
                case IXLWorkbook workbook:
                    workbook.SaveAs(allFileNames[currentFileNameIndex]);
                    workbook.Dispose();
                    break;
                case string csvString:
                    File.WriteAllText(allFileNames[currentFileNameIndex], csvString);
                    break;
                default:
                    throw new NotImplementedException();
            }
            
            currentFileNameIndex++;
        }
        
        IsBusy = false;
        
        if (window != null)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                window.Close();
            });
        }
    }
    
    public class FileInUseMessage
    {
        public required string FileName { get; init; }
    }
}