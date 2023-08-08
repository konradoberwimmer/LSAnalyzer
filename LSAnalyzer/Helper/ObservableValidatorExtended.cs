using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace LSAnalyzer.Helper
{
    public class ObservableValidatorExtended : ObservableValidator, INotifyPropertyChanged
    {
        public bool Validate()
        {
            ValidateAllProperties();
            OnPropertyChanged(nameof(Errors));
            return GetErrors().Count() == 0;
        }

        [NotMapped]
        [JsonIgnore]
        public IDictionary<string, string> Errors
        {
            get => GetErrors().ToList().ToDictionary(obj => obj.MemberNames.First(), obj => obj.ErrorMessage)!;
            private set
            {

            }
        }
    }
}
