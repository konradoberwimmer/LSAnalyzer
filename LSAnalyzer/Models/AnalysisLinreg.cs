using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisLinreg : AnalysisRegression
    {
        public AnalysisLinreg(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Linear regression";
    }
}
