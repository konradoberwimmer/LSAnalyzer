using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services.Stubs;

public class AnalysisQueueStub : IAnalysisQueue
{
    public void Add(AnalysisPresentation analysisPresentation)
    {
        throw new System.NotImplementedException();
    }

    public int Count => 0;
    
    public void InterruptAnalysis(AnalysisPresentation analysisPresentation)
    {
        
    }
}