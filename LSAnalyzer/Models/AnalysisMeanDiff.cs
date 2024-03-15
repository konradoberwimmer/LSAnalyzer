using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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

        [JsonIgnore]
        public override Dictionary<string, DataColumn> TableColumns => throw new NotImplementedException();

        [JsonIgnore]
        public override string? SecondaryTableName => "Explained variance";

        [JsonIgnore]
        public override string? SecondaryDataFrameName => "stat.eta";

        [JsonIgnore]
        public override Dictionary<string, DataColumn>? SecondaryTableColumns
        {
            get
            {
                Dictionary<string, DataColumn> tableColumns = new();

                tableColumns.Add("var", new DataColumn("variable", typeof(string)));
                if (VariableLabels.Count > 0)
                {
                    tableColumns.Add("$varlabel_var", new DataColumn("variable (label)", typeof(string)));
                }

                if (CalculateSeparately)
                {
                    tableColumns.Add("group", new DataColumn("groups by", typeof(string)));
                }

                tableColumns.Add("eta2", new DataColumn("eta²", typeof(double)));
                tableColumns.Add("eta", new DataColumn("eta", typeof(double)));
                tableColumns.Add("eta_SE", new DataColumn("eta - standard error", typeof(double)));
                tableColumns.Add("fmi", new DataColumn("eta - FMI", typeof(double)));

                return tableColumns;
            }
        }
    }
}
