using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels.DataProvider;

public partial class Dataverse : ObservableObject, IDataProviderViewModel
{
    private readonly Services.DataProvider.Dataverse _dataverseService = null!;

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
        get => (File == null || Dataset == null) ? null : (ProviderName + ": " + File + ", " + Dataset);
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
    }

    public Dataverse(Services.DataProvider.Dataverse dataverseService)
    {
        _dataverseService = dataverseService;
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
        TestResults = _dataverseService.TestFileAccess(new { File, Dataset });

        IsBusy = false;
    }

    public List<Variable> LoadDataTemporarilyAndGetVariables()
    {
        return _dataverseService.GetDatasetVariables(new { File, Dataset });
    }

    public bool LoadDataForUsage()
    {
        return _dataverseService.LoadFileIntoGlobalEnvironment(new { File, Dataset });
    }
}
