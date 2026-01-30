using System.Collections.Generic;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services.Stubs;

public class BatchAnalyzeServiceStub : IBatchAnalyzeService
{
    public void RunBatch(Dictionary<int, AnalysisWithViewSettings> analyses, bool useCurrentFile, AnalysisConfiguration? currentConfiguration)
    {
        throw new System.NotImplementedException();
    }

    public (bool success, string? errorMessage, IDataProvider? dataProvider) RetrieveDataProvider(string fileRetrieval)
    {
        throw new System.NotImplementedException();
    }

    public (bool success, string? errorMessage, dynamic? fileInformation) RetrieveFileInformation(IDataProvider dataProvider,
        string fileRetrieval)
    {
        throw new System.NotImplementedException();
    }
}