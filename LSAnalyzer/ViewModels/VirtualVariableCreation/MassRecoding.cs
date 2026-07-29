using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;

namespace LSAnalyzer.ViewModels.VirtualVariableCreation;

public partial class MassRecoding : ObservableValidatorExtended
{
    private readonly VirtualVariables _virtualVariables;

    [ObservableProperty] private bool _forDatasetType = false;
    
    [ObservableProperty]
    private ObservableCollection<Variable> _availableVariables = [];
    
    [ObservableProperty]
    private string _recodeInfo = string.Empty;

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

    [ObservableProperty] 
    private ObservableCollection<Recoding> _recodings = [];
    
    public bool CanCreate => Recodings.Count > 0 && Recodings.All(r => !string.IsNullOrWhiteSpace(r.OutputVariableName));
    
    public MassRecoding(VirtualVariables virtualVariables)
    {
        _virtualVariables = virtualVariables;
        if (_virtualVariables.SelectedVirtualVariable is not VirtualVariableRecode { Variables.Count: 1 } virtualVariableRecode)
        {
            return;
        }
        
        var relevantVariables = _virtualVariables.AvailableVariables.Where(v => v.Name != virtualVariableRecode.Variables.First().Name); 
        AvailableVariables = [..relevantVariables];
        RecodeInfo = virtualVariableRecode.RecodeInfo;
    }

    [ExcludeFromCodeCoverage]
    public MassRecoding()
    {
        // design-time only, parameterless constructor
        _virtualVariables = new VirtualVariables();
        Recodings =
        [
            new Recoding
            {
                Parent = this,
                InputVariable = new Variable(1, "Item X1"),
                OutputVariableName = "Y1",
                OutputVariableLabel = "Label Y1"
            },
            new Recoding
            {
                Parent = this,
                InputVariable = new Variable(1, "Item X2"),
                OutputVariableName = "Y2",
                OutputVariableLabel = ""
            },
            new Recoding
            {
                Parent = this,
                InputVariable = new Variable(1, "Item X3"),
                OutputVariableName = "",
                OutputVariableLabel = ""
            },
        ];
    }

    private void NotifyCanCreateChanged()
    {
        OnPropertyChanged(nameof(CanCreate));
    }

    [RelayCommand]
    private void HandleAvailableVariables(List<Variable> selectedAvailableVariables)
    {
        foreach (var selectedAvailableVariable in selectedAvailableVariables)
        {
            Recoding recoding = new()
            {
                Parent = this,
                InputVariable = selectedAvailableVariable,
            };
            Recodings.Add(recoding);
            
            var availableVariable = AvailableVariables.FirstOrDefault(v => v.Name == selectedAvailableVariable.Name);
            if (availableVariable is not null)
            {
                AvailableVariables.Remove(availableVariable);
            }
        }
        OnPropertyChanged(nameof(CanCreate));
    }

    [RelayCommand]
    private void RemoveRecoding(Recoding recoding)
    {
        Recodings.Remove(recoding);
        if (_virtualVariables.AvailableVariables.Select(v => v.Name).Contains(recoding.InputVariable.Name))
        {
            AvailableVariables.Add(_virtualVariables.AvailableVariables.First(v => v.Name == recoding.InputVariable.Name));
            AvailableVariables = SortAlphabetically ?
                new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Name)) :
                new ObservableCollection<Variable>(AvailableVariables.OrderBy(v => v.Position));
        }
        OnPropertyChanged(nameof(CanCreate));
    }

    [RelayCommand]
    private void CreateRecodings(ICloseable? window)
    {
        if (_virtualVariables.SelectedVirtualVariable is not VirtualVariableRecode { Variables.Count: 1 } virtualVariableRecode)
        {
            return;
        }
        
        if (Recodings.Count == 0 || !Recodings.All(r => r.Validate()))
        {
            return;
        }

        var existingVariableNames = Recodings
            .Where(r => _virtualVariables.AvailableVariables.Any(v => string.Equals(v.Name, r.OutputVariableName, StringComparison.InvariantCultureIgnoreCase)))
            .Select(r => r.OutputVariableName).ToList();

        if (existingVariableNames.Count > 0)
        {
            WeakReferenceMessenger.Default.Send(new VariableNameExistsMessage { ExistingNames = existingVariableNames });
            return;
        }

        var duplicatedVariableNames = Recodings
            .Where(r => Recodings.Any(r2 => r2 != r && string.Equals(r2.OutputVariableName, r.OutputVariableName, StringComparison.InvariantCultureIgnoreCase)))
            .Select(r => r.OutputVariableName).ToList();

        if (duplicatedVariableNames.Count > 0)
        {
            WeakReferenceMessenger.Default.Send(new VariableNameDuplicatedMessage { DuplicatedNames = duplicatedVariableNames });
            return;
        }

        foreach (var recoding in Recodings)
        {
            var newVirtualVariableRecode = (virtualVariableRecode.Clone() as VirtualVariableRecode)!;
            newVirtualVariableRecode.Variables[0] = recoding.InputVariable.Clone();
            newVirtualVariableRecode.Name = recoding.OutputVariableName;
            newVirtualVariableRecode.Label = recoding.OutputVariableLabel;
            newVirtualVariableRecode.ForDatasetTypeId = ForDatasetType ? _virtualVariables.AnalysisConfiguration?.DatasetType?.Id : null;
            
            _virtualVariables.CurrentVirtualVariables.Add(newVirtualVariableRecode);
            _virtualVariables.SelectedVirtualVariable = newVirtualVariableRecode;
            _virtualVariables.SaveSelectedVirtualVariableCommand.Execute(null);
        }
        
        window?.Close();
    }
    
    public class Recoding : ObservableValidatorExtended 
    {
        public required MassRecoding Parent { get; init; }
        public required Variable InputVariable { get; init; }

        private string _outputVariableName = string.Empty;
        
        [Required]
        [RegularExpression("[a-zA-Z][a-zA-Z0-9_]{2,}", ErrorMessage = "Name must start with a letter and consist of letters, digits and underscores (at least 3)!")]
        public string OutputVariableName
        {
            get => _outputVariableName;
            set
            {
                _outputVariableName = value;
                Parent.NotifyCanCreateChanged();
            }
        }
        
        public string OutputVariableLabel { get; set; } = string.Empty;
    }

    public class VariableNameExistsMessage
    {
        public List<string> ExistingNames { get; init; } = [];
    }

    public class VariableNameDuplicatedMessage
    {
        public List<string> DuplicatedNames { get; init; } = [];
    }
}