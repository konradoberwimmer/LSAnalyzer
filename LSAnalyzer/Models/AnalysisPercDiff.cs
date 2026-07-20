using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;

namespace LSAnalyzer.Models;

public class AnalysisPercDiff : Analysis
{
    public bool CalculateSeparately { get; set; } = false;

    public bool CalculateSE { get; set; } = true;
    
    public AnalysisPercDiff(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
    {
    }

    public override string AnalysisName => "Percentage difference";
    
    [JsonIgnore]
    public override Dictionary<string, DataColumn> TableColumns => throw new NotImplementedException();
}