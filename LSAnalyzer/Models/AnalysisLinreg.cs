using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisLinreg : Analysis
    {
        public Variable? Dependent { get; set; }

        public bool WithIntercept { get; set; } = true;

        public enum RegressionSequence { AllIn, Forward, Backward };
        public RegressionSequence Sequence { get; set; } = RegressionSequence.AllIn;

        public bool CalculateOverall { get; set; } = true;

        public AnalysisLinreg(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Linear regression";
    }
}
