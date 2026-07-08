using System;
using LSAnalyzer.Helper;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Views;

public partial class RequestAnalysisPercDiff : RequestAnalysisBaseView, IRequestingAnalysis
{
    public RequestAnalysisPercDiff(RequestAnalysis requestAnalysisViewModel)
    {
        InitializeComponent();
        
        DataContext = requestAnalysisViewModel;
    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisPercDiff")!;
    }
}