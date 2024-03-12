using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisUnivar : Analysis
    {
        public bool CalculateOverall { get; set; } = true;

        public AnalysisUnivar(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {
            
        }

        public override string AnalysisName => "Univariate";

        [JsonIgnore]
        public override Dictionary<string, DataColumn> TableColumns
        {
            get
            {
                Dictionary<string, DataColumn> tableColumns = new();

                tableColumns.Add("var", new DataColumn("variable", typeof(string)));
                if (VariableLabels.Count > 0)
                {
                    tableColumns.Add("$varlabel_var", new DataColumn("variable (label)", typeof(string)));
                }

                for (int cntGroupyBy = 0; cntGroupyBy < GroupBy.Count; cntGroupyBy++)
                {
                    tableColumns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(GroupBy[cntGroupyBy].Name, typeof(double)));
                    if (ValueLabels.ContainsKey(GroupBy[cntGroupyBy].Name))
                    {
                        tableColumns.Add("$label_" + GroupBy[cntGroupyBy].Name, new DataColumn(GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                    }
                }

                tableColumns.Add("Ncases", new DataColumn("N - cases unweighted", typeof(int)));
                tableColumns.Add("Nweight", new DataColumn("N - weighted", typeof(double)));
                tableColumns.Add("lsanalyzer_rank", new DataColumn("rank of mean (per variable)", typeof(double)));
                tableColumns.Add("M", new DataColumn("mean", typeof(double)));
                tableColumns.Add("M_SE", new DataColumn("mean - standard error", typeof(double)));
                tableColumns.Add("M_p", new DataColumn("mean - p value", typeof(double)));
                tableColumns.Add("M_fmi", new DataColumn("mean - FMI", typeof(double)));
                tableColumns.Add("SD", new DataColumn("standard deviation", typeof(double)));
                tableColumns.Add("SD_SE", new DataColumn("standard deviation - standard error", typeof(double)));
                tableColumns.Add("SD_p", new DataColumn("standard deviation - p value", typeof(double)));
                tableColumns.Add("SD_fmi", new DataColumn("standard deviation - FMI", typeof(double)));

                return tableColumns;
            }
        }
    }
}
