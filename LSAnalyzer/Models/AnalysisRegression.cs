using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public abstract class AnalysisRegression : Analysis
    {
        public Variable? Dependent { get; set; }

        public bool WithIntercept { get; set; } = true;

        public enum RegressionSequence { AllIn, Forward, Backward };
        public RegressionSequence Sequence { get; set; } = RegressionSequence.AllIn;

        public bool CalculateOverall { get; set; } = true;

        protected AnalysisRegression(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string ShortInfo
        {
            get =>
                AnalysisName +
                " (" + (Dependent?.Name ?? "undefined") + " by " + String.Join(", ", Vars.ConvertAll(var => var.Name).ToArray()) +
                " - " + AnalysisConfiguration.DatasetType?.Name +
                "; " + AnalysisConfiguration.DatasetType?.Weight +
                ")";
        }
    }
}
