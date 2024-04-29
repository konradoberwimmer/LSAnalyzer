using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public partial class PlausibleValueVariable : ObservableValidatorExtended
    {
        [ObservableProperty]
        private string _Regex = null!;
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
}
