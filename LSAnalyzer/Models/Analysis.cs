using LSAnalyzer.ViewModels.ValueConverter;
using RDotNet;
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
    [JsonDerivedType(typeof(AnalysisUnivar), typeDiscriminator: "univar")]
    [JsonDerivedType(typeof(AnalysisFreq), typeDiscriminator: "freq")]
    [JsonDerivedType(typeof(AnalysisPercentiles), typeDiscriminator: "perc")]
    [JsonDerivedType(typeof(AnalysisCorr), typeDiscriminator: "corr")]
    [JsonDerivedType(typeof(AnalysisMeanDiff), typeDiscriminator: "meandiff")]
    [JsonDerivedType(typeof(AnalysisLinreg), typeDiscriminator: "linreg")]
    [JsonDerivedType(typeof(AnalysisLogistReg), typeDiscriminator: "logistreg")]
    public abstract class Analysis
    {
        public abstract string AnalysisName { get; }

        protected AnalysisConfiguration _analysisConfiguration;
        public AnalysisConfiguration AnalysisConfiguration 
        { 
            get => _analysisConfiguration; 
            set => _analysisConfiguration = value;
        }
        public List<Variable> Vars { get; set; } = new();
        public List<Variable> GroupBy { get; set; } = new();
        [JsonIgnore]
        public List<GenericVector> Result { get; set; } = new();
        public DateTime? ResultAt { get; set; }
        public double? ResultDuration { get; set; }
        [JsonIgnore]
        public Dictionary<string, DataFrame> ValueLabels { get; set; } = new();
        public string? SubsettingExpression { get; set; } = null;

        public Analysis(AnalysisConfiguration analysisConfiguration) 
        {
            _analysisConfiguration = analysisConfiguration;
        }

        [JsonIgnore]
        public virtual string ShortInfo
        {
            get => 
                AnalysisName + 
                " (" + String.Join(", ", Vars.ConvertAll(var => var.Name).ToArray()) + 
                " - " + AnalysisConfiguration.DatasetType?.Name +
                "; " + AnalysisConfiguration.DatasetType?.Weight +
                ")";
        }

        [JsonIgnore]
        public Dictionary<string, object?> MetaInformation
        {
            get => new()
            {
                { "Analysis:", AnalysisName },
                { "Dependent variable:", this is AnalysisRegression analysisRegression ? analysisRegression.Dependent?.Name : null },
                { "Type of percentiles:", this is AnalysisPercentiles analysisPercentiles ? analysisPercentiles.PercentileTypeInfo : null },
                { "Dataset type:", AnalysisConfiguration.DatasetType?.Name},
                { "File:", AnalysisConfiguration.FileName },
                { "Subset:", SubsettingExpression },
                { "Mode:", (new BoolToAnalysisMode()).Convert(AnalysisConfiguration.ModeKeep == true, Type.GetType("bool")!, "", CultureInfo.InvariantCulture).ToString() },
                { "Calculation finished:", ResultAt?.ToString() },
                { "Duration in seconds:", ResultDuration },
            };
        }

        [JsonIgnore]
        public Dictionary<string, string> VariableLabels
        {
            get
            {
                Dictionary<string, string> variableLabels = new();

                if (this is AnalysisRegression analysisRegression)
                {
                    if (analysisRegression?.Dependent?.Label != null)
                    {
                        variableLabels.Add(analysisRegression.Dependent.Name, analysisRegression.Dependent.Label);
                    }
                }

                foreach (var variable in Vars)
                {
                    if (variable.Label != null)
                    {
                        variableLabels.Add(variable.Name, variable.Label);
                    }
                }

                foreach (var variable in GroupBy)
                {
                    if (variable.Label != null)
                    {
                        variableLabels.Add(variable.Name, variable.Label);
                    }
                }

                return variableLabels;
            }
        }

        [JsonIgnore]
        public virtual string PrimaryDataFrameName { get => "stat"; }

        [JsonIgnore]
        public abstract Dictionary<string, DataColumn> TableColumns { get; }
    }
}
