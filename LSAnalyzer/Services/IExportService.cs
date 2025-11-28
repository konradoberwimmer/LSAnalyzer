using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public partial interface IExportService
{
    public List<string> AllFileNames(ExportOptions options, Analysis analysis);
    
    public void CreateFrequenciesTableSuperHeader(IXLWorksheet worksheet, DataTable table, Dictionary<string, string> columnTooltips);

    public void AddWorksheetMetadata(IXLWorkbook workbook, Analysis analysis, bool useStyles = true);

    public IXLWorkbook CreateXlsxExport(Analysis analysis, DataView dataView, DataView? secondaryDataView, Dictionary<string, string> columnTooltips, bool useStyles = true);
    
    [GeneratedRegex("^Cat\\s[0-9\\.]+(\\s-\\sstandard\\serror)?$")]
    public static partial Regex RegexCategoryPercentageHeader();
}