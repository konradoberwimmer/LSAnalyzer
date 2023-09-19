using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels
{
    public class SystemSettings : INotifyPropertyChanged
    {
        private Rservice _rservice;

        private string? _rVersion;
        public string? RVersion
        {
            get => _rVersion;
            set
            {
                _rVersion = value;
                NotifyPropertyChanged(nameof(RVersion));
            }
        }

        private string? _bifieSurveyVersion;
        public string? BifieSurveyVersion
        {
            get => _bifieSurveyVersion;
            set
            {
                _bifieSurveyVersion = value;
                NotifyPropertyChanged(nameof(BifieSurveyVersion));
            }
        }

        [ExcludeFromCodeCoverage]
        public SystemSettings() 
        {
            // design-time only, parameterless constructor
            RVersion = "R version 4.3.1";
            BifieSurveyVersion = "3.4-15";
        }

        public SystemSettings(Rservice rservice)
        {
            _rservice = rservice;
            RVersion = _rservice.GetRVersion();
            BifieSurveyVersion = _rservice.GetBifieSurveyVersion();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
