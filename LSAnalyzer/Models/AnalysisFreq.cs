using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisFreq : Analysis
    {
        public bool CalculateOverall { get; set; } = true;

        public AnalysisFreq(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
            
        }

        public override string AnalysisName => "Frequencies";
    }
}
