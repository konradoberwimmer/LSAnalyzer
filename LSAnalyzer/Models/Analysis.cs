using LSAnalyzer.ViewModels.ValueConverter;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

namespace LSAnalyzer.Models
{
    [JsonDerivedType(typeof(AnalysisUnivar), typeDiscriminator: "univar")]
    [JsonDerivedType(typeof(AnalysisFreq), typeDiscriminator: "freq")]
    [JsonDerivedType(typeof(AnalysisPercentiles), typeDiscriminator: "perc")]
    [JsonDerivedType(typeof(AnalysisCorr), typeDiscriminator: "corr")]
    [JsonDerivedType(typeof(AnalysisMeanDiff), typeDiscriminator: "meandiff")]
    [JsonDerivedType(typeof(AnalysisPercDiff), typeDiscriminator: "percdiff")]
    [JsonDerivedType(typeof(AnalysisLinreg), typeDiscriminator: "linreg")]
    [JsonDerivedType(typeof(AnalysisLogistReg), typeDiscriminator: "logistreg")]
    public abstract class Analysis
    {
        public abstract string AnalysisName { get; }

        public AnalysisConfiguration AnalysisConfiguration { get; set; }

        public List<Variable> Vars { get; set; } = [];
        public List<Variable> GroupBy { get; set; } = [];
        [JsonIgnore]
        public List<GenericVector> Result { get; set; } = [];
        public DateTime? ResultAt { get; set; }
        public double? ResultDuration { get; set; }
        [JsonIgnore]
        public Dictionary<string, DataFrame> ValueLabels { get; set; } = new();
        public string? SubsettingExpression { get; set; } = null;
        public List<VirtualVariable> VirtualVariables { get; set; } = [];

        public virtual List<Variable> AllVariables => [..Vars, ..GroupBy];

        protected Analysis(AnalysisConfiguration analysisConfiguration) 
        {
            AnalysisConfiguration = analysisConfiguration;
        }

        [JsonIgnore]
        public virtual string ShortInfo
        {
            get
            {
                var groupByInfo = GroupBy.Count > 0 ? $" by {string.Join(", ", GroupBy.ConvertAll(var => var.Name).ToArray())}" : string.Empty;
                return $"{AnalysisName} ({string.Join(", ", Vars.ConvertAll(var => var.Name).ToArray())}{groupByInfo} - {AnalysisConfiguration.DatasetType?.Name})";
            }
        }

        [JsonIgnore]
        public Dictionary<string, object?> MetaInformation =>
            new()
            {
                { "Analysis:", AnalysisName },
                { "Dependent variable:", this is AnalysisRegression analysisRegression ? analysisRegression.Dependent?.Name : null },
                { "Type of percentiles:", this is AnalysisPercentiles analysisPercentiles ? analysisPercentiles.PercentileTypeInfo : null },
                { "File:", AnalysisConfiguration.FileName },
                { "Dataset type:", AnalysisConfiguration.DatasetType?.Name},
                { "Weight:", AnalysisConfiguration.DatasetType?.Weight},
                { "Subset:", SubsettingExpression },
                { "Mode:", (new BoolToAnalysisMode()).Convert(AnalysisConfiguration.ModeKeep == true, Type.GetType("bool")!, "", CultureInfo.InvariantCulture).ToString() },
                { "Calculation finished:", ResultAt?.ToString() },
                { "Duration in seconds:", ResultDuration },
            };

        [JsonIgnore]
        public Dictionary<string, string> VariableLabels
        {
            get
            {
                Dictionary<string, string> variableLabels = new();

                if (this is AnalysisRegression { Dependent.Label: not null } analysisRegression)
                {
                    variableLabels.Add(analysisRegression.Dependent.Name, analysisRegression.Dependent.Label);
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
        public virtual string PrimaryDataFrameName => "stat";

        [JsonIgnore]
        public abstract Dictionary<string, DataColumn> TableColumns { get; }

        [JsonIgnore]
        public virtual string? SecondaryTableName => null;

        [JsonIgnore]
        public virtual string? SecondaryDataFrameName => null;

        [JsonIgnore]
        public virtual Dictionary<string, DataColumn>? SecondaryTableColumns => null;

        public string? GetValueLabel(string? variable, double value)
        {
            if (variable is null || !ValueLabels.TryGetValue(variable, out var valueLabels))
            {
                return null;
            }
            
            var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf(value);
            return posValueLabel != -1 ? valueLabels["label"].AsCharacter()[posValueLabel] : null;
        }
    }
}
