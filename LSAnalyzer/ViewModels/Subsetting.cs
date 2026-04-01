using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels;

public partial class Subsetting : ObservableObject
{
    private readonly IRservice _rservice;
    
    private readonly Configuration _configuration;

    private AnalysisConfiguration? _analysisConfiguration;
    public AnalysisConfiguration? AnalysisConfiguration
    {
        get => _analysisConfiguration;
        set
        {
            _analysisConfiguration = value;
            OnPropertyChanged();

            if (AnalysisConfiguration == null)
            {
                return;
            }
            
            var virtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration.FileNameWithoutPath ?? string.Empty, AnalysisConfiguration.DatasetType ?? new DatasetType { Id = -1 });
            
            var currentDatasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, virtualVariables, true);
            if (currentDatasetVariables != null)
            {
                ObservableCollection<Variable> newAvailableVariables = new();
                foreach (var variable in currentDatasetVariables)
                {
                    newAvailableVariables.Add(variable);
                }
                AvailableVariables = newAvailableVariables;
            }

            RecentSubsettingExpressions = new ObservableCollection<string>(_configuration.GetStoredRecentSubsettingExpressions(AnalysisConfiguration.DatasetType?.Id ?? -1));
        }
    }

    [ObservableProperty] 
    private ObservableCollection<Variable> _availableVariables = [];
    
    private bool _sortAlphabetically = false;
    public bool SortAlphabetically
    {
        get => _sortAlphabetically;
        set
        {
            if (value != _sortAlphabetically)
            {
                AvailableVariables = value ? 
                    new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Name)) : 
                    new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Position));
            }
            
            _sortAlphabetically = value;
            OnPropertyChanged();
        }
    }

    private bool _isCurrentlySubsetting = false;
    public bool IsCurrentlySubsetting => _isCurrentlySubsetting;

    [ObservableProperty]
    private ObservableCollection<string> _recentSubsettingExpressions = [];
    
    [ObservableProperty]
    private string? _selectedRecentSubsettingExpression;
    partial void OnSelectedRecentSubsettingExpressionChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }
        
        SubsetExpression = value;
        OnPropertyChanged(nameof(SubsetExpression));
    }
    
    private string? _subsetExpression;
    public string? SubsetExpression
    {
        get => _subsetExpression;
        set
        {
            _subsetExpression = value;
            CurrentSubsettingInformation = null;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private SubsettingInformation? _currentSubsettingInformation = null;

    [ObservableProperty]
    private bool _isBusy = false;

    [ExcludeFromCodeCoverage]
    public Subsetting()
    {
        // design-time-only constructor
        _rservice = new RserviceStub();
        _configuration = new Configuration();
        CurrentSubsettingInformation = new() { ValidSubset = true, NCases = 100, NSubset = 57 };
        RecentSubsettingExpressions = ["x == 1", "y == 2"];
    }

    public Subsetting(IRservice rservice, Configuration configuration)
    {
        _rservice = rservice;
        _configuration = configuration;
        AvailableVariables = new ObservableCollection<Variable>();
        RecentSubsettingExpressions = [];
    }

    public void SetCurrentSubsetting(string subsettingExpression)
    {
        SubsetExpression = subsettingExpression;
        _isCurrentlySubsetting = true;
    }

    [RelayCommand]
    private void ClearSubsetting(ICloseable? window)
    {
        if (AnalysisConfiguration == null)
        {
            return;
        }

        SubsetExpression = null;

        BackgroundWorker clearSubsettingWorker = new();
        clearSubsettingWorker.WorkerReportsProgress = false;
        clearSubsettingWorker.WorkerSupportsCancellation = false;
        clearSubsettingWorker.DoWork += ClearSubsettingWorker_DoWork;
        clearSubsettingWorker.RunWorkerCompleted += (_, _) =>
        {
            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));
            window?.Close();
        };
        clearSubsettingWorker.RunWorkerAsync(window);
    }

    private void ClearSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        IsBusy = true;
        
        if (AnalysisConfiguration?.ModeKeep == true)
        {
            var virtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration.FileNameWithoutPath ?? string.Empty, AnalysisConfiguration.DatasetType ?? new DatasetType { Id = -1 });
            _rservice.TestAnalysisConfiguration(AnalysisConfiguration, virtualVariables);
        }
        
        IsBusy = false;
    }

    [RelayCommand]
    private void TestSubsetting()
    {
        if (SubsetExpression != null)
        {
            BackgroundWorker testSubsettingWorker = new();
            testSubsettingWorker.WorkerReportsProgress = false;
            testSubsettingWorker.WorkerSupportsCancellation = false;
            testSubsettingWorker.DoWork += TestSubsettingWorker_DoWork;
            testSubsettingWorker.RunWorkerAsync();
        }
    }

    private void TestSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        IsBusy = true;
        CurrentSubsettingInformation = _rservice.TestSubsetting(SubsetExpression!, AnalysisConfiguration?.DatasetType?.MIvar);
        IsBusy = false;
    }

    [RelayCommand]
    private void UseSubsetting(ICloseable? window)
    {
        if (SubsetExpression == null || AnalysisConfiguration == null)
        {
            return;
        }

        BackgroundWorker useSubsettingWorker = new();
        useSubsettingWorker.WorkerReportsProgress = false;
        useSubsettingWorker.WorkerSupportsCancellation = false;
        useSubsettingWorker.DoWork += UseSubsettingWorker_DoWork;
        useSubsettingWorker.RunWorkerCompleted += (_, e) =>
        {
            if (e.Result is SetSubsettingExpressionMessage setSubsettingExpressionMessage)
            {
                WeakReferenceMessenger.Default.Send(setSubsettingExpressionMessage);
                window?.Close();
            }
        };
        useSubsettingWorker.RunWorkerAsync(window);
    }

    private void UseSubsettingWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        IsBusy = true;

        var testResult = _rservice.TestSubsetting(SubsetExpression!);
        if (!testResult.ValidSubset)
        {
            CurrentSubsettingInformation = testResult;
            IsBusy = false;
            return;
        }

        _configuration.StoreRecentSubsettingExpression(AnalysisConfiguration!.DatasetType!.Id, SubsetExpression!);
        
        if (AnalysisConfiguration!.ModeKeep == true)
        {
            var virtualVariables = _configuration.GetVirtualVariablesFor(AnalysisConfiguration.FileNameWithoutPath ?? string.Empty, AnalysisConfiguration.DatasetType ?? new DatasetType { Id = -1 });
            
            if (!_rservice.TestAnalysisConfiguration(AnalysisConfiguration!, virtualVariables, SubsetExpression))
            {
                CurrentSubsettingInformation = new()
                {
                    ValidSubset = false,
                    NCases = 0,
                    NSubset = 0,
                };
                IsBusy = false;
                return;
            }
        }

        e.Result = new SetSubsettingExpressionMessage(SubsetExpression);
        IsBusy = false;
    }
}

internal class SetSubsettingExpressionMessage : ValueChangedMessage<string?>
{
    public SetSubsettingExpressionMessage(string? subsettingExpression) : base(subsettingExpression)
    {

    }
}
