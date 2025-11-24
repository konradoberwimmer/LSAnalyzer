using System.Collections.Generic;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public interface IExportService
{
    public List<string> AllFileNames(ExportOptions options, Analysis analysis);
}