using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
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
    private string _file = string.Empty;
    partial void OnFileChanged(string value)
    {
        TestResults = new();
    }

    [ObservableProperty]
    private string _dataset = string.Empty;
    partial void OnDatasetChanged(string value)
    {
        TestResults = new();
    }

    [ObservableProperty]
    private bool _isBusy = false;

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
}
