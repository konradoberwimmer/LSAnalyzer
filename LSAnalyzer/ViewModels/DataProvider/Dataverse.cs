using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Services;

namespace LSAnalyzer.ViewModels.DataProvider;

public partial class Dataverse : ObservableObject, IDataProviderViewModel
{
    private readonly Services.DataProvider.Dataverse _dataverseService = null!;
    private readonly Configuration _configuration = null!;

    [ObservableProperty]
    private SelectAnalysisFile? _parentViewModel;

    public bool IsConfigurationReady
    {
        get => File.Length > 0 && Dataset.Length > 0 && !IsBusy;
    }

    public string ProviderName
    {
        get => _dataverseService.Configuration.Name;
    }

    public string? FileInformation
    {
        get => (string.IsNullOrWhiteSpace(File) || string.IsNullOrWhiteSpace(Dataset)) ? null : (ProviderName + ": " + File + ", " + Dataset);
    }

    public string SerializeFileInformation()
    {
        if (string.IsNullOrWhiteSpace(File) || string.IsNullOrWhiteSpace(Dataset))
        {
            return JsonSerializer.Serialize(new { });
        }

        return JsonSerializer.Serialize(new { File, Dataset });
    }

    public Dictionary<string, object> GetUsageAttributes()
    {
        return new Dictionary<string, object> { { "FileFormat", SelectedFileFormat.Key } };
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigurationReady))]
    private string _file = string.Empty;
    partial void OnFileChanged(string value)
    {
        TestResults = new();
        ParentViewModel?.NotifyReadyState();
    }

    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, string>> _fileFormats;

    [ObservableProperty]
    private KeyValuePair<string, string> _selectedFileFormat;

    [ObservableProperty]
    private ObservableCollection<Configuration.RecentFileForAnalysis> _recentFilesForAnalyses;
    
    [ObservableProperty]
    private Configuration.RecentFileForAnalysis? _selectedRecentFileForAnalysis;
    partial void OnSelectedRecentFileForAnalysisChanged(Configuration.RecentFileForAnalysis? value)
    {
        if (value == null)
        {
            return;       
        }
        
        InitializeFromRecentFile(value);
    }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigurationReady))]
    private string _dataset = string.Empty;
    partial void OnDatasetChanged(string value)
    {
        TestResults = new();
        ParentViewModel?.NotifyReadyState();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigurationReady))]
    private bool _isBusy = false;
    partial void OnIsBusyChanged(bool value)
    {
        ParentViewModel?.NotifyReadyState();
    }

    [ObservableProperty]
    private DataProviderTestResults _testResults = new();

    [ExcludeFromCodeCoverage]
    public Dataverse()
    {
        // design-time only parameterless constructor
        FileFormats = new() { new("tsv", "Archive (TSV)"), new("spss", "SPSS Original") };
        SelectedFileFormat = FileFormats.First();
    }

    public Dataverse(Services.DataProvider.Dataverse dataverseService, Configuration configuration)
    {
        _dataverseService = dataverseService;
        _configuration = configuration;
        var recentFilesForAnalyses = _configuration.GetStoredRecentFiles(_dataverseService.Configuration.Id);
        recentFilesForAnalyses.ForEach(rfa => rfa.FormatFileName = FormatRecentFileName);
        RecentFilesForAnalyses = new ObservableCollection<Configuration.RecentFileForAnalysis>(recentFilesForAnalyses);
        FileFormats = [ new("tsv", "Archive (TSV)"), new("spss", "SPSS Original") ];
        SelectedFileFormat = FileFormats.First();
    }

    public static string FormatRecentFileName(string fileName)
    {
        return fileName.Replace("\"", string.Empty).Replace("File:", "File: ").Replace("Dataset:", "Dataset: ").Replace(",", ", ");
    }

    public void InitializeFromRecentFile(Configuration.RecentFileForAnalysis recentFileForAnalysis)
    {
        if (ParentViewModel == null)
        {
            return;
        }

        JsonObject? deserializedFileName = null;
        try
        {
            deserializedFileName =
                JsonSerializer.Deserialize<JsonObject>(recentFileForAnalysis.FileName);
        } catch {}

        if (deserializedFileName == null || !deserializedFileName.ContainsKey("File") || !deserializedFileName.ContainsKey("Dataset") ||
            ParentViewModel.DatasetTypes.All(dst => dst.Id != recentFileForAnalysis.DatasetTypeId) ||
            !ParentViewModel.DatasetTypes.First(dst => dst.Id == recentFileForAnalysis.DatasetTypeId).Weight.Split(';').Contains(recentFileForAnalysis.Weight))
        {
            _configuration.RemoveRecentFile(recentFileForAnalysis);
            var recentFilesForAnalyses = _configuration.GetStoredRecentFiles(_dataverseService.Configuration.Id);
            recentFilesForAnalyses.ForEach(rfa => rfa.FormatFileName = FormatRecentFileName);
            RecentFilesForAnalyses = new ObservableCollection<Configuration.RecentFileForAnalysis>(recentFilesForAnalyses);
            
            WeakReferenceMessenger.Default.Send(new RecentFileInvalidMessage(recentFileForAnalysis.FileName));
            
            return;
        }

        File = deserializedFileName["File"]!.GetValue<string>();
        Dataset = deserializedFileName["Dataset"]!.GetValue<string>();
        if (recentFileForAnalysis.UsageAttributes.TryGetValue("FileFormat", out var fileFormat) && fileFormat is JsonElement
            {
                ValueKind: JsonValueKind.String
            })
        {
            SelectedFileFormat = FileFormats.FirstOrDefault(ff => ff.Key == fileFormat.ToString());
        }
        
        ParentViewModel.ReplaceCharacterVectors = recentFileForAnalysis.ConvertCharacters;
        ParentViewModel.SelectedDatasetType = ParentViewModel.DatasetTypes.First(dst => dst.Id == recentFileForAnalysis.DatasetTypeId);
        ParentViewModel.SelectedWeightVariable = recentFileForAnalysis.Weight;
        ParentViewModel.SelectedAnalysisMode = recentFileForAnalysis.ModeKeep ? SelectAnalysisFile.AnalysisModes.Keep : SelectAnalysisFile.AnalysisModes.Build;
    }

    [RelayCommand]
    public void TestFileAccess()
    {
        if (string.IsNullOrWhiteSpace(File) || string.IsNullOrWhiteSpace(Dataset))
        {
            return;
        }

        IsBusy = true;

        BackgroundWorker testFileAccessWorker = new();
        testFileAccessWorker.WorkerReportsProgress = false;
        testFileAccessWorker.WorkerSupportsCancellation = false;
        testFileAccessWorker.DoWork += TestFileAccessWorker_DoWork;
        testFileAccessWorker.RunWorkerAsync();
    }

    private void TestFileAccessWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        TestResults = _dataverseService.TestFileAccess(new { File, Dataset, FileFormat = SelectedFileFormat.Key });

        IsBusy = false;
    }

    public List<Variable> LoadDataTemporarilyAndGetVariables()
    {
        return _dataverseService.GetDatasetVariables(new { File, Dataset, FileFormat = SelectedFileFormat.Key });
    }

    public bool LoadDataForUsage()
    {
        return _dataverseService.LoadFileIntoGlobalEnvironment(new { File, Dataset, FileFormat = SelectedFileFormat.Key });
    }
}
