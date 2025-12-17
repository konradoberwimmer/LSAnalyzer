using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.ViewModels;
    
public class AnalysisWithViewSettings
{
    public required Analysis Analysis { init; get; }

    public required Dictionary<string, object> ViewSettings { init; get; }
}