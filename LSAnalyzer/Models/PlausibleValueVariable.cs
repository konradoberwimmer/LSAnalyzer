using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;
using LSAnalyzer.Models.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace LSAnalyzer.Models;

public partial class PlausibleValueVariable : ObservableValidatorExtended
{
    [Required(ErrorMessage = "Regex is required!")]
    [ValidRegex("Invalid regex pattern!")]
    [ObservableProperty]
    private string _regex = null!;
    [Required(ErrorMessage = "Display name is required!")]
    [ObservableProperty]
    private string _displayName = null!;
    [ObservableProperty]
    private bool _mandatory = false;
    [ObservableProperty] 
    private string _label = string.Empty;

    public PlausibleValueVariable() { }

    public PlausibleValueVariable(PlausibleValueVariable plausibleValueVariable)
    {
        Regex = plausibleValueVariable.Regex;
        DisplayName = plausibleValueVariable.DisplayName;
        Mandatory = plausibleValueVariable.Mandatory;
        Label = plausibleValueVariable.Label;
    }
}
