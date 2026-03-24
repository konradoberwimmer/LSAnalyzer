using System.Collections.Generic;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public interface IBatchAnalyzeService
{
    public void RunBatch(IEnumerable<BatchAnalyze.BatchEntry> analyses, bool useCurrentFile, AnalysisConfiguration? currentConfiguration, string? currentSubsettingExpression, List<VirtualVariable> currentVirtualVariables);
    
    public void AbortBatch();

    public (bool success, string? errorMessage, IDataProvider? dataProvider) RetrieveDataProvider(string fileRetrieval);

    public (bool success, string? errorMessage, dynamic? fileInformation) RetrieveFileInformation(IDataProvider dataProvider, string fileRetrieval);
}