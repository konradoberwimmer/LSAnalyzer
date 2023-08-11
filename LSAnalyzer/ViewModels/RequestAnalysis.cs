using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels
{
    public class RequestAnalysis : INotifyPropertyChanged
    {
        private readonly Rservice _rservice;

        private AnalysisConfiguration? _analysisConfiguration;
        public AnalysisConfiguration? AnalysisConfiguration
        {
            get => _analysisConfiguration;
            set
            {
                _analysisConfiguration = value;
                NotifyPropertyChanged(nameof(AnalysisConfiguration));
            }
        }

        private Analysis? _analysis;
        public Analysis? Analysis
        {
            get => _analysis;
            set
            {
                _analysis = value;
                NotifyPropertyChanged(nameof(Analysis));
            }
        }

        public RequestAnalysis()
        {
            // design-time-only constructor
        }

        public RequestAnalysis(Rservice rservice)
        {
            _rservice = rservice;
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
