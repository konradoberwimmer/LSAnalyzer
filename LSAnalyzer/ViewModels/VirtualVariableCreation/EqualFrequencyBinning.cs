using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using RDotNet;

namespace LSAnalyzer.ViewModels.VirtualVariableCreation;

public partial class EqualFrequencyBinning : ObservableValidatorExtended
{
    private readonly VirtualVariables _virtualVariables;
    private readonly IRservice _rservice;
    
    [ObservableProperty]
    private ObservableCollection<Variable> _variables;
    
    [ObservableProperty]
    [Required]
    private Variable? _selectedVariable;

    [ObservableProperty]
    [Range(2, 20)]
    private int _numberOfBins = 2;

    [ObservableProperty]
    [Required]
    [RegularExpression("[a-zA-Z][a-zA-Z0-9_]{2,}", ErrorMessage = "Name must start with a letter and consist of letters, digits and underscores (at least 3)!")]
    private string _name = string.Empty;

    [ObservableProperty] 
    [RegularExpression("^[^\"'`´]*$", ErrorMessage = "The label must not contain any kind of quote character.")]
    private string _newLabel = string.Empty;
    
    public EqualFrequencyBinning(VirtualVariables virtualVariables, IRservice rservice)
    {
        _virtualVariables = virtualVariables;
        _rservice = rservice;
        Variables = [.._virtualVariables.AvailableVariables];
    }

    [RelayCommand]
    private void CreateEqualFrequencyBinning(ICloseable? window)
    {
        if (!Validate()) return;

        if (_virtualVariables.AnalysisConfiguration is null) return;
        
        AnalysisPercentiles analysisPercentiles = new(_virtualVariables.AnalysisConfiguration)
        {
            Vars = [SelectedVariable!],
            UseInterpolation = true,
            CalculateSE = false,
            Percentiles = Enumerable.Range(1, NumberOfBins - 1).Select(i => i * (1.0 / NumberOfBins)).ToList(),
        };

        if (_virtualVariables.AnalysisConfiguration.ModeKeep is false && !_rservice.PrepareForAnalysis(analysisPercentiles))
        {
            WeakReferenceMessenger.Default.Send<UnableToCalculatePercentilesMessage>();
            return;
        }
        
        var percentiles = _rservice.CalculatePercentiles(analysisPercentiles)?.First()["stat"].AsDataFrame()["quant"].AsNumeric().ToList();
        
        if (percentiles is null)
        {
            WeakReferenceMessenger.Default.Send<UnableToCalculatePercentilesMessage>();
            return;
        }
        
        VirtualVariableRecode virtualVariableRecode = new()
        {
            Name = Name,
            Label = NewLabel,
            ForFileName = _virtualVariables.CurrentFileName,
            Variables = [SelectedVariable!.Clone()],
            Else = VirtualVariableRecode.ElseAction.Missing,
        };
        
        for (var i = 0; i <= percentiles.Count; i++)
        {
            VirtualVariableRecode.Rule rule = new()
            {
                Criteria = [
                    new VirtualVariableRecode.Term
                    {
                        VariableIndex = 0,
                        Type = i switch
                        {
                            0 => VirtualVariableRecode.Term.TermType.AtMost,
                            _ => i == percentiles.Count ? VirtualVariableRecode.Term.TermType.AtLeast : VirtualVariableRecode.Term.TermType.Between
                        },
                        Value = i == 0 ? double.MinValue : percentiles[i - 1],
                        MaxValue = i == percentiles.Count ? double.MaxValue : percentiles[i]
                    }
                ],
                ResultNa = false,
                ResultValue = i + 1,
            };
            
            virtualVariableRecode.Rules.Add(rule);
        }
        
        _virtualVariables.CurrentVirtualVariables.Add(virtualVariableRecode);
        _virtualVariables.SelectedVirtualVariable = virtualVariableRecode;
        _virtualVariables.SaveSelectedVirtualVariableCommand.Execute(null);
        
        window?.Close();
    }

    public class UnableToCalculatePercentilesMessage;
}