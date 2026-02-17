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
        
        public bool CalculateCrosswise { get; set; } = true;

        protected AnalysisRegression(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string ShortInfo
        {
            get
            {
                var sequenceInfo = Sequence switch
                {
                    RegressionSequence.AllIn => string.Empty,
                    RegressionSequence.Forward => " forward",
                    RegressionSequence.Backward => " backward",
                    _ => throw new ArgumentOutOfRangeException()
                };
                var interceptInfo = WithIntercept ? "1" : "0";
                var groupByInfo = GroupBy.Count > 0 ? $" by {string.Join(", ", GroupBy.ConvertAll(var => var.Name).ToArray())}" : string.Empty;
                return $"{AnalysisName}{sequenceInfo} ({Dependent?.Name ?? "undefined"} ~ {interceptInfo} + {string.Join(" + ", Vars.ConvertAll(var => var.Name).ToArray())}{groupByInfo} - {AnalysisConfiguration.DatasetType?.Name})";
            }
        }
    }
}
