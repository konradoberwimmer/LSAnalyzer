using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace LSAnalyzerAvalonia.Helper;

public class ObservableValidatorExtended : ObservableValidator, INotifyPropertyChanged
{
    public bool Validate()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(Errors));
        return !GetErrors().Any();
    }

    [NotMapped]
    [JsonIgnore]
    public IDictionary<string, string> Errors => GetErrors().ToList().ToDictionary(obj => obj.MemberNames.First(), obj => obj.ErrorMessage)!;
}