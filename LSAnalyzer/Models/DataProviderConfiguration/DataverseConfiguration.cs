using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Services.DataProvider;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models.DataProviderConfiguration
{
    public partial class DataverseConfiguration : ObservableValidatorExtended, IChangeTracking, IDataProviderConfiguration
    {
        public int Id { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsChanged))]
        [MinLength(3, ErrorMessage = "Please use at least three characters for name!")]
        private string _name = "New dataverse provider";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsChanged))]
        [MinLength(1, ErrorMessage = "Please provide an URL!")]
        [RegularExpression("^(?!mailto:)(?:(?:http|https|ftp)://)(?:\\S+(?::\\S*)?@)?(?:(?:(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}(?:\\.(?:[0-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))|(?:(?:[a-z\\u00a1-\\uffff0-9]+-?)*[a-z\\u00a1-\\uffff0-9]+)(?:\\.(?:[a-z\\u00a1-\\uffff0-9]+-?)*[a-z\\u00a1-\\uffff0-9]+)*(?:\\.(?:[a-z\\u00a1-\\uffff]{2,})))|localhost)(?::\\d{2,5})?(?:(/|\\?|#)[^\\s]*)?$", ErrorMessage = "Please provide a valid URL!")]
        private string _url = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsChanged))]
        [MinLength(1, ErrorMessage = "Please provide your API token!")]
        private string _apiToken = string.Empty;

        private DataverseConfiguration? _savedState;

        [JsonIgnore]
        public bool IsChanged
        {
            get
            {
                if (_savedState == null)
                {
                    return true;
                }

                return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, new string[] { "Errors", "IsChanged" });
            }
        }

        public void AcceptChanges()
        {
            _savedState = new()
            {
                Id = Id,
                Name = Name,
                Url = Url,
                ApiToken = ApiToken,
            };

            OnPropertyChanged(nameof(IsChanged));
        }

        public IDataProvider CreateService(IServiceProvider serviceProvider)
        {
            return new Dataverse(serviceProvider.GetRequiredService<Rservice>()) { Configuration = this };
        }
    }
}
