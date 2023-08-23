using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public abstract class Analysis
    {
        public abstract string AnalysisName { get; }

        protected readonly AnalysisConfiguration _analysisConfiguration;
        public AnalysisConfiguration AnalysisConfiguration 
        { 
            get => _analysisConfiguration; 
        }
        public List<Variable> Vars { get; set; } = new();
        public List<Variable> GroupBy { get; set; } = new();
        public bool CalculateOverall { get; set; } = true;
        public List<GenericVector> Result { get; set; } = new();

        public Analysis(AnalysisConfiguration analysisConfiguration) 
        {
            _analysisConfiguration = analysisConfiguration;
        }

        public string ShortInfo
        {
            get => 
                AnalysisName + 
                " (" + String.Join(", ", Vars.ConvertAll(var => var.Name).ToArray()) + 
                " - " + AnalysisConfiguration.DatasetType?.Name +
                ")";
        }
    }
}
