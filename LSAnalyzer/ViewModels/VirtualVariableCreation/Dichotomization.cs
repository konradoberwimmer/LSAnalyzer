using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;

namespace LSAnalyzer.ViewModels.VirtualVariableCreation;

public partial class Dichotomization : ObservableValidatorExtended
{
    private readonly VirtualVariables _virtualVariables;

    public enum ReferenceCategoryType
    {
        Lowest,
        Select,
        Highest,
        None,
    }
    
    [ObservableProperty]
    private ObservableCollection<Variable> _variables;
    
    [ObservableProperty]
    [Required]
    private Variable? _selectedVariable;
    partial void OnSelectedVariableChanged(Variable? value)
    {
        Categories = [];
        SelectedCategory = 0.0;
        
        if (value == null) return;

        var distinctValues = _virtualVariables.GetDistinctValues(value);

        if (distinctValues.Count > 10)
        {
            SelectedVariable = null;
            ShowCategoryCountError = true;
            WeakReferenceMessenger.Default.Send<RejectsContinuousVariable>();
            return;
        }
        
        ShowCategoryCountError = false;
        Categories = [..distinctValues];
        SelectedCategory = Categories.Count > 0 ? Categories.First() : 0.0;
    }

    [ObservableProperty]
    private bool _showCategoryCountError = false;
    
    [ObservableProperty]
    private ReferenceCategoryType _referenceCategory = ReferenceCategoryType.Lowest;
    partial void OnReferenceCategoryChanged(ReferenceCategoryType value)
    {
        SelectedCategory = value switch
        {
            ReferenceCategoryType.Lowest => Categories.Count > 0 ? Categories.First() : 0.0,
            ReferenceCategoryType.Highest => Categories.Count > 0 ? Categories.Last() : 0.0,
            _ => SelectedCategory
        };

        OnPropertyChanged(nameof(CategoriesMakesSense));
    }
    
    [ObservableProperty] 
    private List<double> _categories = [];
    
    [ObservableProperty] 
    private double _selectedCategory;
    
    public bool CategoriesMakesSense => ReferenceCategory == ReferenceCategoryType.Select;

    [ObservableProperty]
    [Required]
    [RegularExpression("[a-zA-Z][a-zA-Z0-9_]{2,}", ErrorMessage = "Prefix must start with a letter and consist of letters, digits and underscores (at least 3)!")]
    private string _prefix = string.Empty;

    [ObservableProperty] 
    [RegularExpression("^[^\"'`´]*$", ErrorMessage = "The label must not contain any kind of quote character.")]
    private string _newLabel = string.Empty;
    
    public Dichotomization(VirtualVariables virtualVariables)
    {
        _virtualVariables = virtualVariables;
        Variables = [.._virtualVariables.AvailableVariables];
    }

    [RelayCommand]
    private void CreateDichotomization(ICloseable? window)
    {
        if (!Validate()) return;

        foreach (var category in Categories)
        {
            if ((ReferenceCategory == ReferenceCategoryType.Lowest && Categories.IndexOf(category) == 0) ||
                (ReferenceCategory == ReferenceCategoryType.Select && category == SelectedCategory) ||
                (ReferenceCategory == ReferenceCategoryType.Highest && Categories.IndexOf(category) == Categories.Count - 1))
            {
                continue;
            }

            VirtualVariableRecode virtualVariableRecode = new()
            {
                Name = $"{Prefix}_c{category.ToString("0.####", CultureInfo.InvariantCulture)}",
                Label = string.IsNullOrWhiteSpace(NewLabel) ? string.Empty : $"{NewLabel} - Category {category.ToString("0.####", CultureInfo.InvariantCulture)}",
                ForFileName = _virtualVariables.CurrentFileName,
                Variables = [SelectedVariable!.Clone()],
                Else = VirtualVariableRecode.ElseAction.Missing,
            };

            virtualVariableRecode.Rules = [
                ..Categories.Select(cat => 
                    new VirtualVariableRecode.Rule
                    {
                        Criteria = [
                            new VirtualVariableRecode.Term
                            {
                                VariableIndex = 0,
                                Type = VirtualVariableRecode.Term.TermType.Exactly,
                                Value = cat,
                            }
                        ],
                        ResultValue = cat == category ? 1.0 : 0.0
                    }
                ).ToList()
            ];
            
            _virtualVariables.CurrentVirtualVariables.Add(virtualVariableRecode);
            _virtualVariables.SelectedVirtualVariable = virtualVariableRecode;
            _virtualVariables.SaveSelectedVirtualVariableCommand.Execute(null);
        }
        
        window?.Close();
    }

    public class RejectsContinuousVariable;
}