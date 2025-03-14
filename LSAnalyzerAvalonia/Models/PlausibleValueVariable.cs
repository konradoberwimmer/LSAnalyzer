using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.Helper;
using LSAnalyzerAvalonia.Models.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace LSAnalyzerAvalonia.Models;

public partial class PlausibleValueVariable : ObservableValidatorExtended
{
    [Required(ErrorMessage = "Regex is required!")]
    [ValidRegex("Invalid regex pattern!")]
    [ObservableProperty]
    private string _Regex = null!;
    [Required(ErrorMessage = "Display name is required!")]
    [ObservableProperty]
    public string _DisplayName = null!;
    [ObservableProperty]
    public bool _Mandatory = false;

    public PlausibleValueVariable() { }

    public PlausibleValueVariable(PlausibleValueVariable plausibleValueVariable)
    {
        Regex = plausibleValueVariable.Regex;
        DisplayName = plausibleValueVariable.DisplayName;
        Mandatory = plausibleValueVariable.Mandatory;
    }
}