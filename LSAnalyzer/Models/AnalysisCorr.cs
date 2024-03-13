using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisCorr : Analysis
    {
        public bool CalculateOverall { get; set; } = true;

        public AnalysisCorr(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {

        }

        public override string AnalysisName => "Correlations";

        [JsonIgnore]
        public override string PrimaryDataFrameName { get => "stat.cor"; }

        [JsonIgnore]
        public override Dictionary<string, DataColumn> TableColumns
        {
            get
            {
                Dictionary<string, DataColumn> tableColumns = new();

                for (int cntGroupyBy = 0; cntGroupyBy < GroupBy.Count; cntGroupyBy++)
                {
                    tableColumns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(GroupBy[cntGroupyBy].Name, typeof(double)));
                    if (ValueLabels.ContainsKey(GroupBy[cntGroupyBy].Name))
                    {
                        tableColumns.Add("$label_" + GroupBy[cntGroupyBy].Name, new DataColumn(GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                    }
                }

                tableColumns.Add("var1", new DataColumn("variable A", typeof(string)));
                if (VariableLabels.Count > 0)
                {
                    tableColumns.Add("$varlabel_var1", new DataColumn("variable A (label)", typeof(string)));
                }

                tableColumns.Add("var2", new DataColumn("variable B", typeof(string)));
                if (VariableLabels.Count > 0)
                {
                    tableColumns.Add("$varlabel_var2", new DataColumn("variable B (label)", typeof(string)));
                }

                tableColumns.Add("Ncases", new DataColumn("N - cases unweighted", typeof(int)));
                tableColumns.Add("Nweight", new DataColumn("N - weighted", typeof(double)));
                tableColumns.Add("cor", new DataColumn("correlation", typeof(double)));
                tableColumns.Add("cor_SE", new DataColumn("correlation - standard error", typeof(double)));
                tableColumns.Add("p", new DataColumn("correlation - p value", typeof(double)));
                tableColumns.Add("cor_fmi", new DataColumn("correlation - FMI", typeof(double)));

                return tableColumns;
            }
        }
    }
}
