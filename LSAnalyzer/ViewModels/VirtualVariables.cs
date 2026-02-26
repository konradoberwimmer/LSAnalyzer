using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.ViewModels;

public partial class VirtualVariables : ObservableObject
{
    private readonly Configuration _configuration;
    
    private readonly IRservice _rservice;

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

            var currentVirtualVariables =
                _configuration.GetVirtualVariablesFor(fileName, _analysisConfiguration.DatasetType);
            foreach (var currentVirtualVariable in currentVirtualVariables)
            {
                currentVirtualVariable.AcceptChanges();
            }
            CurrentVirtualVariables = new ObservableCollection<VirtualVariable>(currentVirtualVariables);

            var availableVariables = _rservice.GetCurrentDatasetVariables(_analysisConfiguration);
            if (availableVariables != null)
            {
                AvailableVariables = new ObservableCollection<Variable>(availableVariables.Where(variable => variable is { IsSystemVariable: false, IsVirtual: false }));
            }
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
    private Type? _selectedVirtualVariableType = null;
    
    [ObservableProperty]
    private VirtualVariable? _selectedVirtualVariable = null;
    partial void OnSelectedVirtualVariableChanged(VirtualVariable? value)
    {
        SelectedIsForDatasetType = value?.ForDatasetTypeId is not null;
        OnPropertyChanged(nameof(HasSelectedVirtualVariable));
    }

    public bool HasSelectedVirtualVariable => SelectedVirtualVariable != null;

    [ObservableProperty]
    private bool _selectedIsForDatasetType = false;
    
    [ObservableProperty]
    private ObservableCollection<Variable> _availableVariables = [];
    
    [ObservableProperty] 
    private DataView _preview = new();
    
    [ExcludeFromCodeCoverage]
    public VirtualVariables()
    {
        // parameter-less design-time only constructor
        _configuration = new Configuration();
        _rservice = new RserviceStub();
        CurrentFileName = "";
        DataTable defaultTable = new("default");
        defaultTable.Columns.Add("Input", typeof(double));
        defaultTable.Columns.Add("Output", typeof(double));
        Preview = new DataView(defaultTable);
        CurrentVirtualVariables =
        [
            new VirtualVariableCombine
            {
                ForFileName = "some_file.sav",
                Name = "newVariable",
                ForDatasetTypeId = 12,
            }
        ];
        SelectedVirtualVariable = CurrentVirtualVariables.First();
    }

    public VirtualVariables(Configuration configuration, IRservice rservice)
    {
        _configuration = configuration;
        _rservice = rservice;
        DataTable defaultTable = new("default");
        defaultTable.Columns.Add("Input", typeof(double));
        defaultTable.Columns.Add("Output", typeof(double));
        Preview = new DataView(defaultTable);
    }

    [RelayCommand]
    private void NewVirtualVariable()
    {
        if (SelectedVirtualVariableType is null) return;

        if (Activator.CreateInstance(SelectedVirtualVariableType) is not VirtualVariable newVirtualVariable) return;
        
        SelectedVirtualVariable = newVirtualVariable;
        SelectedVirtualVariable.ForFileName = CurrentFileName;
        
        CurrentVirtualVariables.Add(newVirtualVariable);

        SelectedVirtualVariableType = null;
    }

    [RelayCommand]
    private void HandleAvailableVariables(List<Variable> selectedAvailableVariables)
    {
        if (SelectedVirtualVariable is null) return;

        switch (SelectedVirtualVariable)
        {
            case VirtualVariableCombine virtualVariableCombine:
                foreach (var selectedVariable in selectedAvailableVariables)
                {
                    virtualVariableCombine.Variables.Add(selectedVariable.Clone());
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [RelayCommand]
    private void SaveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;
        
        if (!SelectedVirtualVariable.Validate()) return;

        if (SelectedVirtualVariable.Id == 0)
        {
            SelectedVirtualVariable.Id = _configuration.GetNextVirtualVariableId();
        }
        
        _configuration.StoreVirtualVariable(SelectedVirtualVariable);
        
        SelectedVirtualVariable.AcceptChanges();
    }

    [RelayCommand]
    private void RemoveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;
        
        _configuration.RemoveVirtualVariable(SelectedVirtualVariable);
        
        CurrentVirtualVariables.Remove(SelectedVirtualVariable);
        SelectedVirtualVariable = null;
    }
}