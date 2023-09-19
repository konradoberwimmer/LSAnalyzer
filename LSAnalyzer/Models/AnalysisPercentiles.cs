using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisPercentiles : Analysis
    {
        public List<double> Percentiles { get; set; } = new();
        public bool CalculateOverall { get; set; } = true;
        public bool UseInterpolation { get; set; } = true;
        public bool CalculateSE { get; set; } = true;

        public AnalysisPercentiles(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Percentiles";
    }
}
