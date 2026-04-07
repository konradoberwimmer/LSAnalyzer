using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

            CurrentFileName = _analysisConfiguration.FileNameWithoutPath!;
            CurrentDatasetTypeName = _analysisConfiguration.DatasetType.Name;

            var currentVirtualVariables =
                _configuration.GetVirtualVariablesFor(CurrentFileName, _analysisConfiguration.DatasetType);
            foreach (var currentVirtualVariable in currentVirtualVariables)
            {
                currentVirtualVariable.AcceptChanges();
            }
            CurrentVirtualVariables = new ObservableCollection<VirtualVariable>(currentVirtualVariables);

            var availableVariables = _rservice.GetCurrentDatasetVariables(_analysisConfiguration, currentVirtualVariables);
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
        typeof(VirtualVariableCombine),
        typeof(VirtualVariableRecode),
        typeof(VirtualVariableScale)
    ];
    
    [ObservableProperty]
    private Type? _selectedVirtualVariableType = null;
    
    [ObservableProperty]
    private VirtualVariable? _selectedVirtualVariable = null;
    partial void OnSelectedVirtualVariableChanged(VirtualVariable? value)
    {
        SelectedIsForDatasetType = value?.ForDatasetTypeId is not null;
        Preview = DefaultDataView();
        OnPropertyChanged(nameof(HasSelectedVirtualVariable));
    }

    public bool HasSelectedVirtualVariable => SelectedVirtualVariable != null;

    [ObservableProperty]
    private bool _selectedIsForDatasetType = false;
    partial void OnSelectedIsForDatasetTypeChanged(bool value)
    {
        if (SelectedVirtualVariable is null || AnalysisConfiguration?.DatasetType is null) return;
        
        SelectedVirtualVariable.ForDatasetTypeId = value ? AnalysisConfiguration.DatasetType.Id : null;
    }
    
    [ObservableProperty]
    private ObservableCollection<Variable> _availableVariables = [];
    
    [ObservableProperty] 
    private DataView _preview = new();
    
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

    public bool HasChangedVirtualVariables { get; set; } = false;
    
    [ObservableProperty]
    private bool _isBusy = false;
    
    [ExcludeFromCodeCoverage]
    public VirtualVariables()
    {
        // parameter-less design-time only constructor
        _configuration = new Configuration();
        _rservice = new RserviceStub();
        CurrentFileName = "";
        Preview = DefaultDataView();
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
        Preview = DefaultDataView();
        
        WeakReferenceMessenger.Default.Register<Views.CustomControls.VirtualVariable.VirtualVariableRecode.RemoveLastVariableMessage>(this, (_, _) => RemoveLastVariableCommand.Execute(null));
        
        WeakReferenceMessenger.Default.Register<Views.CustomControls.VirtualVariable.VirtualVariableRecode.AddRuleMessage>(this, (_, _) => AddRuleCommand.Execute(null));
        
        WeakReferenceMessenger.Default.Register<Views.CustomControls.VirtualVariable.VirtualVariableRecode.RemoveRuleMessage>(this, (_, m) => RemoveRuleCommand.Execute(m.Rule));
    }

    [RelayCommand]
    private void NewVirtualVariable()
    {
        if (SelectedVirtualVariableType is null) return;

        if (Activator.CreateInstance(SelectedVirtualVariableType) is not VirtualVariable newVirtualVariable) return;

        switch (newVirtualVariable)
        {
            case VirtualVariableScale virtualVariableScale:
                if (AnalysisConfiguration?.DatasetType is null) break;
                
                var datasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, []) ?? [];
                virtualVariableScale.WeightVariable = datasetVariables.FirstOrDefault(var => var.Name == AnalysisConfiguration.DatasetType.Weight)?.Clone();
                virtualVariableScale.MiVariable = AnalysisConfiguration.DatasetType.MIvar is null ? null : datasetVariables.FirstOrDefault(var => var.Name == AnalysisConfiguration.DatasetType.MIvar)?.Clone();
                break;
        }
        
        CurrentVirtualVariables.Add(newVirtualVariable);
        
        SelectedVirtualVariable = newVirtualVariable;
        SelectedVirtualVariable.ForFileName = CurrentFileName;
        
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
            case VirtualVariableScale virtualVariableScale:
                virtualVariableScale.InputVariable = selectedAvailableVariables.First().Clone();
                break;
            case VirtualVariableRecode virtualVariableRecode:
                virtualVariableRecode.AddVariable(selectedAvailableVariables.First().Clone());
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [RelayCommand]
    private void SaveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;

        switch (SelectedVirtualVariable)
        {
            case VirtualVariableCombine:
            case VirtualVariableScale:
                if (!SelectedVirtualVariable.Validate()) return;
                break;
            case VirtualVariableRecode virtualVariableRecode:
                if (!virtualVariableRecode.ValidateDeep()) return;
                break;
            default:
                return;
        }
        
        if (AvailableVariables.Any(variable => variable.Name == SelectedVirtualVariable.Name) ||
            CurrentVirtualVariables.Any(vv => vv != SelectedVirtualVariable && vv.Name == SelectedVirtualVariable.Name))
        {
            WeakReferenceMessenger.Default.Send(new VariableNameNotAvailableMessage());
            return;
        }

        if (SelectedVirtualVariable.Id == 0)
        {
            SelectedVirtualVariable.Id = _configuration.GetNextVirtualVariableId();
        }
        
        _configuration.StoreVirtualVariable(SelectedVirtualVariable);
        
        SelectedVirtualVariable.AcceptChanges();

        HasChangedVirtualVariables = true;
    }

    [RelayCommand]
    private void RemoveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;
        
        _configuration.RemoveVirtualVariable(SelectedVirtualVariable);
        
        CurrentVirtualVariables.Remove(SelectedVirtualVariable);
        SelectedVirtualVariable = null;

        HasChangedVirtualVariables = true;
    }

    [RelayCommand]
    private void FetchPreviewData()
    {
        if (SelectedVirtualVariable is null) return;
        
        switch (SelectedVirtualVariable)
        {
            case VirtualVariableCombine:
            case VirtualVariableScale:
                if (!SelectedVirtualVariable.Validate()) return;
                break;
            case VirtualVariableRecode virtualVariableRecode:
                if (!virtualVariableRecode.ValidateDeep()) return;
                break;
            default:
                return;
        }
        
        Preview = DefaultDataView();
        
        IsBusy = true;
        
        if (!_rservice.CreateVirtualVariable(SelectedVirtualVariable,AnalysisConfiguration?.DatasetType?.PVvarsList.ToList() ?? [], true))
        {
            WeakReferenceMessenger.Default.Send(new PreviewImpossibleMessage());
            
            IsBusy = false;
            return;
        }

        var (success, previewData) = _rservice.GetPreviewData();

        IsBusy = false;
        
        if (!success || previewData is null)
        {
            WeakReferenceMessenger.Default.Send(new PreviewImpossibleMessage());
            return;
        }

        Preview = new DataView(previewData);
    }

    [RelayCommand]
    private void AddRule()
    {
        if (SelectedVirtualVariable is not VirtualVariableRecode virtualVariableRecode) return;
        
        virtualVariableRecode.AddRule();
    }
    
    [RelayCommand]
    private void RemoveLastVariable()
    {
        if (SelectedVirtualVariable is not VirtualVariableRecode virtualVariableRecode) return;
        
        virtualVariableRecode.RemoveLastVariable();
    }

    [RelayCommand]
    private void RemoveRule(VirtualVariableRecode.Rule rule)
    {
        if (SelectedVirtualVariable is not VirtualVariableRecode virtualVariableRecode) return;
        
        virtualVariableRecode.Rules.Remove(rule);
    }

    public List<double> GetDistinctValues(Variable variable)
    {
        return _rservice.GetDistinctValues(variable, AnalysisConfiguration?.DatasetType?.PVvarsList.ToList() ?? []) ?? [];
    }

    private DataView DefaultDataView()
    {
        DataTable defaultTable = new("default");
        defaultTable.Columns.Add("Input", typeof(double));
        defaultTable.Columns.Add("Output", typeof(double));
        return new DataView(defaultTable);
    }

    public class VariableNameNotAvailableMessage;

    public class PreviewImpossibleMessage;
}