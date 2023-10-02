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
        public List<GenericVector> Result { get; set; } = new();
        public DateTime? ResultAt { get; set; }
        public double? ResultDuration { get; set; }
        public Dictionary<string, DataFrame> ValueLabels { get; set; } = new();
        public string? SubsettingExpression { get; set; } = null;

        public Analysis(AnalysisConfiguration analysisConfiguration) 
        {
            _analysisConfiguration = analysisConfiguration;
        }

        public virtual string ShortInfo
        {
            get => 
                AnalysisName + 
                " (" + String.Join(", ", Vars.ConvertAll(var => var.Name).ToArray()) + 
                " - " + AnalysisConfiguration.DatasetType?.Name +
                ")";
        }
    }
}
