using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public interface IAnalysisQueue
{
    public void Add(AnalysisPresentation analysisPresentation);
    
    public int Count { get; }
}