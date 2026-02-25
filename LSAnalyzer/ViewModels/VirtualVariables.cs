using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using LSAnalyzer.Services;

namespace LSAnalyzer.ViewModels;

public partial class VirtualVariables : ObservableObject
{
    private readonly Configuration _configuration;

    private AnalysisConfiguration? _analysisConfiguration;
    public AnalysisConfiguration? AnalysisConfiguration
    {
        get => _analysisConfiguration;
        set
        {
            _analysisConfiguration = value;
            OnPropertyChanged();

            if (_analysisConfiguration?.DatasetType == null || _analysisConfiguration.FileName == null)
            {
                CurrentFileName = string.Empty;
                CurrentDatasetTypeName = string.Empty;
                CurrentVirtualVariables = [];
                return;
            }

            var fileName = _analysisConfiguration.FileName;
            if (!fileName.StartsWith('[') && !fileName.StartsWith('{'))
            {
                fileName = Path.GetFileName(fileName);
            }

            CurrentFileName = fileName;
            CurrentDatasetTypeName = _analysisConfiguration.DatasetType.Name;
            CurrentVirtualVariables = new ObservableCollection<VirtualVariable>(_configuration.GetVirtualVariablesFor(fileName, _analysisConfiguration.DatasetType));
        }
    }
    
    [ObservableProperty]
    private string _currentFileName = string.Empty;
    
    [ObservableProperty]
    private string _currentDatasetTypeName = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<VirtualVariable> _currentVirtualVariables = [];

    [ObservableProperty] 
    private List<Type> _virtualVariableTypes = [
        typeof(VirtualVariableCombine)
    ];
    
    [ObservableProperty]
    private Type _selectedVirtualVariableType = typeof(VirtualVariableCombine);
    
    [ObservableProperty]
    private VirtualVariable? _selectedVirtualVariable = null;

    [ObservableProperty] 
    private DataView _preview = new();
    
    [ExcludeFromCodeCoverage]
    public VirtualVariables()
    {
        // parameter-less design-time only constructor
        _configuration = new Configuration();
        DataTable defaultTable = new("default");
        defaultTable.Columns.Add("Input", typeof(double));
        defaultTable.Columns.Add("Output", typeof(double));
        Preview = new DataView(defaultTable);
    }

    public VirtualVariables(Configuration configuration)
    {
        _configuration = configuration;
        CurrentFileName = "some_file.sav";
        DataTable defaultTable = new("default");
        defaultTable.Columns.Add("Input", typeof(double));
        defaultTable.Columns.Add("Output", typeof(double));
        Preview = new DataView(defaultTable);
    }

    [RelayCommand]
    private void NewVirtualVariable()
    {
        SelectedVirtualVariable = Activator.CreateInstance(SelectedVirtualVariableType) as VirtualVariable;
    }

    [RelayCommand]
    private void SaveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;

        if (SelectedVirtualVariable.Id == 0)
        {
            SelectedVirtualVariable.Id = _configuration.GetNextVirtualVariableId();
        }
        
        _configuration.StoreVirtualVariable(SelectedVirtualVariable);
    }
}