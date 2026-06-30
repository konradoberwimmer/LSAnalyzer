using System.Data;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public interface IResultService
{
    AnalysisPresentation? AnalysisPresentation { get; set; }

    DataTable? CreatePrimaryTable();

    DataTable? CreateSecondaryTable();
}
