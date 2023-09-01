using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisMeanDiff : Analysis
    {
        public bool CalculateSeparately { get; set; }

        public AnalysisMeanDiff(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
            
        }

        public override string AnalysisName => "Mean difference";
    }
}
