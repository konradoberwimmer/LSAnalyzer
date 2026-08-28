using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
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
                if (currentVirtualVariable is VirtualVariableCompute virtualVariableCompute)
                {
                    virtualVariableCompute.PossiblePlausibleValueVariables = new List<PlausibleValueVariable>(_analysisConfiguration.DatasetType.PVvarsList);
                }
                
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
        typeof(VirtualVariableCompute),
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
        OnPropertyChanged(nameof(CanCreateMassRecoding));
    }

    public bool HasSelectedVirtualVariable => SelectedVirtualVariable != null;

    public bool CanCreateMassRecoding => SelectedVirtualVariable is VirtualVariableRecode { IsChanged: false, Variables.Count: 1 };

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
        if (SelectedVirtualVariableType is null || AnalysisConfiguration?.DatasetType is null) return;

        if (Activator.CreateInstance(SelectedVirtualVariableType) is not VirtualVariable newVirtualVariable) return;
        
        var datasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, []) ?? [];
        
        switch (newVirtualVariable)
        {
            case VirtualVariableCompute virtualVariableCompute:
                virtualVariableCompute.PossiblePlausibleValueVariables = new List<PlausibleValueVariable>(AnalysisConfiguration.DatasetType.PVvarsList);
                virtualVariableCompute.WeightVariable = datasetVariables.FirstOrDefault(var => var.Name == AnalysisConfiguration.DatasetType.Weight)?.Clone();
                virtualVariableCompute.MiVariable = AnalysisConfiguration.DatasetType.MIvar is null ? null : datasetVariables.FirstOrDefault(var => var.Name == AnalysisConfiguration.DatasetType.MIvar)?.Clone();
                break;
            case VirtualVariableScale virtualVariableScale:
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
            case VirtualVariableCompute virtualVariableCompute:
                virtualVariableCompute.Expression += selectedAvailableVariables.First().Name;
                break;
            case VirtualVariableScale virtualVariableScale:
                virtualVariableScale.InputVariable = selectedAvailableVariables.First().Clone();
                break;
            case VirtualVariableRecode virtualVariableRecode:
                virtualVariableRecode.AddVariable(selectedAvailableVariables.First().Clone());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void SaveSelectedVirtualVariable()
    {
        if (SelectedVirtualVariable is null) return;

        switch (SelectedVirtualVariable)
        {
            case VirtualVariableCombine:
            case VirtualVariableCompute:
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
            case VirtualVariableCompute:
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

        var (success, previewData) = _rservice.GetPreviewData(SelectedVirtualVariable);

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

    [RelayCommand]
    private void ExportVirtualVariables(ExportVirtualVariablesParameters parameters)
    {
        if (parameters.VirtualVariables.Count == 0)
        {
            return;
        }
        
        JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        
        File.WriteAllText(parameters.FileName, JsonSerializer.Serialize(parameters.VirtualVariables, jsonSerializerOptions));
    }

    [RelayCommand]
    private void ImportVirtualVariables(string fileName)
    {
        if (AnalysisConfiguration is null) return;
        
        var existingVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration, []);
        if (existingVariables is null) return;
        existingVariables = existingVariables.Where(variable => variable is { IsVirtual: false }).ToList();
        
        List<VirtualVariable>? virtualVariables;
        try
        {
            virtualVariables = JsonSerializer.Deserialize<List<VirtualVariable>>(File.ReadAllText(fileName));
        }
        catch
        {
            WeakReferenceMessenger.Default.Send(new VirtualVariablesFileInvalidMessage { FileName = fileName });
            return;
        }

        if (virtualVariables is null || virtualVariables.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new VirtualVariablesFileInvalidMessage { FileName = fileName });
            return;
        }
        
        List<VirtualVariable> ignoredVirtualVariables = [];
        List<VirtualVariable> duplicatedVirtualVariables = [];
        
        foreach (var virtualVariable in virtualVariables)
        {
            if (!virtualVariable.InputVariableNamesExistIn(existingVariables))
            {
                ignoredVirtualVariables.Add(virtualVariable);
                continue;
            }

            if (CurrentVirtualVariables.Select(vv => vv.Name.ToLowerInvariant()).Contains(virtualVariable.Name.ToLowerInvariant()) ||
                existingVariables.Select(v => v.Name.ToLowerInvariant()).Contains(virtualVariable.Name.ToLowerInvariant()))
            {
                duplicatedVirtualVariables.Add(virtualVariable);
                continue;
            }

            virtualVariable.Id = _configuration.GetNextVirtualVariableId();
            virtualVariable.ForFileName = CurrentFileName;
            if (virtualVariable.ForDatasetTypeId is not null)
            {
                virtualVariable.ForDatasetTypeId = AnalysisConfiguration?.DatasetType?.Id;
            }

            switch (virtualVariable)
            {
                case VirtualVariableCombine virtualVariableCombine:
                    virtualVariableCombine.Variables = [..virtualVariableCombine.Variables.Select(v => existingVariables.First(av => string.Equals(av.Name, v.Name, StringComparison.InvariantCultureIgnoreCase))).ToList()];
                    break;
                case VirtualVariableRecode virtualVariableRecode:
                    virtualVariableRecode.Variables = [..virtualVariableRecode.Variables.Select(v => existingVariables.First(av => string.Equals(av.Name, v.Name, StringComparison.InvariantCultureIgnoreCase))).ToList()];
                    break;
                case VirtualVariableScale virtualVariableScale:
                    virtualVariableScale.InputVariable = existingVariables.First(av => string.Equals(av.Name, virtualVariableScale.InputVariable!.Name, StringComparison.InvariantCultureIgnoreCase)).Clone();
                    if (virtualVariableScale.WeightVariable is not null)
                    {
                        virtualVariableScale.WeightVariable = existingVariables.First(av => string.Equals(av.Name, virtualVariableScale.WeightVariable.Name, StringComparison.InvariantCultureIgnoreCase)).Clone();
                    }
                    if (virtualVariableScale.MiVariable is not null)
                    {
                        virtualVariableScale.MiVariable = existingVariables.First(av => string.Equals(av.Name, virtualVariableScale.MiVariable.Name, StringComparison.InvariantCultureIgnoreCase)).Clone();
                    }
                    break;
                case VirtualVariableCompute virtualVariableCompute:
                    virtualVariableCompute.PossiblePlausibleValueVariables = new List<PlausibleValueVariable>(AnalysisConfiguration!.DatasetType!.PVvarsList);
                    var variableNamesInImportFile = new List<string>(virtualVariableCompute.Variables);
                    foreach (var variableNameInInputFile in variableNamesInImportFile.Where(variableNameInInputFile => !existingVariables.Select(vv => vv.Name).Contains(variableNameInInputFile)))
                    {
                        VirtualVariableComputeLexer lexer = new(new AntlrInputStream(virtualVariableCompute.Expression));
                        CommonTokenStream tokens = new(lexer);
                        VirtualVariableComputeParser parser = new(tokens);
                        Rservice.ReplaceVariableNamesListener listener = new(tokens) { VariableName = variableNameInInputFile, Replacement = existingVariables.First(ev => string.Equals(ev.Name, variableNameInInputFile, StringComparison.InvariantCultureIgnoreCase)).Name };
                        ParseTreeWalker.Default.Walk(listener, parser.expression());
        
                        virtualVariableCompute.Expression = listener.GetReplacedExpression();
                    }
                    if (virtualVariableCompute.WeightVariable is not null)
                    {
                        virtualVariableCompute.WeightVariable = existingVariables.First(av => string.Equals(av.Name, virtualVariableCompute.WeightVariable.Name, StringComparison.InvariantCultureIgnoreCase)).Clone();
                    }
                    if (virtualVariableCompute.MiVariable is not null)
                    {
                        virtualVariableCompute.MiVariable = existingVariables.First(av => string.Equals(av.Name, virtualVariableCompute.MiVariable.Name, StringComparison.InvariantCultureIgnoreCase)).Clone();
                    }
                    break;
            }
            
            virtualVariable.AcceptChanges();
            
            CurrentVirtualVariables.Add(virtualVariable);
            
            _configuration.StoreVirtualVariable(virtualVariable);
            
            HasChangedVirtualVariables = true;
        }

        if (ignoredVirtualVariables.Count != 0)
        {
            WeakReferenceMessenger.Default.Send(new IgnoredVirtualVariablesAtImportMessage { VirtualVariables = ignoredVirtualVariables });
        }
        
        if (duplicatedVirtualVariables.Count != 0)
        {
            WeakReferenceMessenger.Default.Send(new DuplicatedVirtualVariablesAtImportMessage { VirtualVariables = duplicatedVirtualVariables });
        }
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

    public class ExportVirtualVariablesParameters
    {
        public required List<VirtualVariable> VirtualVariables { get; init; }
        public required string FileName { get; init; }
    }

    public class VariableNameNotAvailableMessage;

    public class PreviewImpossibleMessage;

    public class VirtualVariablesFileInvalidMessage { public required string FileName { get; init; } }
    
    public class IgnoredVirtualVariablesAtImportMessage { public required List<VirtualVariable> VirtualVariables { get; init; } }
    
    public class DuplicatedVirtualVariablesAtImportMessage { public required List<VirtualVariable> VirtualVariables { get; init; } }
}