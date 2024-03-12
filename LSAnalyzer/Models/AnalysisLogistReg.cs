using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisLogistReg : AnalysisRegression
    {
        public AnalysisLogistReg(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Logistic regression";

        [JsonIgnore]
        public override Dictionary<string, DataColumn> TableColumns => throw new NotImplementedException();
    }
}
