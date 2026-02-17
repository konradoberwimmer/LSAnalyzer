using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisPercentiles : Analysis
    {
        public List<double> Percentiles { get; set; } = new();
        public bool CalculateOverall { get; set; } = true;
        public bool CalculateCrosswise { get; set; } = true;
        public bool UseInterpolation { get; set; } = true;
        public bool CalculateSE { get; set; } = true;
        public bool MimicIdbAnalyzer { get; set; } = false;

        public AnalysisPercentiles(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
        }

        public override string AnalysisName => "Percentiles";

        [JsonIgnore]
        public override string ShortInfo
        {
            get
            {
                var groupByInfo = GroupBy.Count > 0 ? $" by {string.Join(", ", GroupBy.ConvertAll(var => var.Name).ToArray())}" : string.Empty;
                return $"{AnalysisName} {string.Join(";",Percentiles.ConvertAll(perc => perc.ToString("0.00", CultureInfo.InvariantCulture)))} ({string.Join(", ", Vars.ConvertAll(var => var.Name).ToArray())}{groupByInfo} - {AnalysisConfiguration.DatasetType?.Name})";
            }
        }

        [JsonIgnore]
        public string PercentileTypeInfo
        {
            get => !CalculateSE ?
                ("Without standard errors (BIFIE.ecdf) and " + (UseInterpolation ? "with interpolation (quanttype = 1)" : "without interpolation (quanttype = 2)")) :
                ("With standard errors and " + (UseInterpolation ? "with interpolation (mimic BIFIE.ecdf, quanttype = 1)" : (!MimicIdbAnalyzer ? "without interpolation (mimic BIFIE.ecdf, quanttype = 2)" : "without interpolation (mimic IDBanalyzer)")));
        }

        [JsonIgnore]
        public override Dictionary<string, DataColumn> TableColumns => throw new NotImplementedException();
    }
}
