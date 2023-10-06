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
        public bool MimicIdbAnalyzer { get; set; } = false;

        public AnalysisPercentiles(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Percentiles";

        public override string ShortInfo
        {
            get =>
                AnalysisName +
                " (" + (UseInterpolation ? "interpolation" : (MimicIdbAnalyzer ? "like IDBanalyzer" : "no interpolation")) + "; " + String.Join(", ", Vars.ConvertAll(var => var.Name).ToArray()) +
                " - " + AnalysisConfiguration.DatasetType?.Name +
                "; " + AnalysisConfiguration.DatasetType?.Weight +
                ")";
        }

        public string PercentileTypeInfo
        {
            get => !CalculateSE ?
                ("Without standard errors (BIFIE.ecdf) and " + (UseInterpolation ? "with interpolation (quanttype = 1)" : "without interpolation (quanttype = 2)")) :
                ("With standard errors and " + (UseInterpolation ? "with interpolation (mimic BIFIE.ecdf, quanttype = 1)" : (!MimicIdbAnalyzer ? "without interpolation (mimic BIFIE.ecdf, quanttype = 2)" : "without interpolation (mimic IDBanalyzer)")));
        }
    }
}
