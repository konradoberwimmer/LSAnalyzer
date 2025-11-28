using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public partial class ExportService : IExportService
{
    public List<string> AllFileNames(ExportOptions options, Analysis analysis)
    {
        var path = Path.GetDirectoryName(options.FileName);
        var baseFileName = Path.GetFileNameWithoutExtension(options.FileName);
        var extension = Path.GetExtension(options.FileName);
        
        if (path is null)
        {
            throw new NotImplementedException();
        }
        
        return options.ExportType.Name switch
        {
            "excelWithStyles" or "excelWithoutStyles" or "csvMainTable" => [options.FileName],
            "csvMultiple" => analysis switch
                {
                    AnalysisUnivar or AnalysisPercentiles or AnalysisLinreg or AnalysisLogistReg => [ 
                        options.FileName, 
                        Path.Combine(path, baseFileName + "_meta" + extension)
                    ],
                    AnalysisFreq or AnalysisMeanDiff or AnalysisCorr => [
                        options.FileName, 
                        analysis switch
                        {
                            AnalysisFreq => Path.Combine(path, baseFileName + "_bivariate" + extension),
                            AnalysisMeanDiff => Path.Combine(path, baseFileName + "_anova" + extension),
                            AnalysisCorr => Path.Combine(path, baseFileName + "_covariance" + extension),
                            _ => throw new NotImplementedException()
                        },
                        Path.Combine(path, baseFileName + "_meta" + extension)
                    ],
                    _ => throw new NotImplementedException()
                },
            _ => throw new NotImplementedException()
        };
    }

    public void CreateFrequenciesTableSuperHeader(IXLWorksheet worksheet, DataTable table, Dictionary<string, string> columnTooltips)
    {
        for (var columnIndex = 1; columnIndex <= table.Columns.Count; columnIndex++)
        {
            if (IExportService.RegexCategoryPercentageHeader().IsMatch(table.Columns[columnIndex - 1].ColumnName))
            {
                worksheet.Columns(columnIndex, columnIndex).Style.NumberFormat.Format = "0.0%";
            }
        }

        worksheet.FirstRow().InsertRowsAbove(1);
        for (var columnIndex = 1; columnIndex <= table.Columns.Count; columnIndex++)
        {
            var columnName = table.Columns[columnIndex - 1].ColumnName;
            if (!RegexCategoryHeader().IsMatch(columnName)) continue;
            
            var categoryHeader = columnTooltips.TryGetValue(columnName, out var value)
                ? RegexCategoryHeaderStart().Replace(value, String.Empty)
                : columnName;
            categoryHeader = RegexCategoryHeaderEnd().Replace(categoryHeader, String.Empty);
            worksheet.Cell(1, columnIndex).Value = categoryHeader;

            var coefficientHeader = RegexCategoryHeaderStart().Replace(columnName, String.Empty);
            if (RegexJustCategoryValue().IsMatch(coefficientHeader))
            {
                coefficientHeader += " - %";
            }

            worksheet.Cell(2, columnIndex).Value = coefficientHeader;
        }

        worksheet.Range(1, 1, 1, table.Columns.Count).Style.Alignment.WrapText = true;
        worksheet.Range(1, 1, 1, table.Columns.Count).Style.Fill.BackgroundColor = XLColor.LightBlue;

        Dictionary<string, List<int>> superHeaderPositions = new();
        for (var columnIndex = 1; columnIndex <= table.Columns.Count; columnIndex++)
        {
            var superHeaderValue = worksheet.Cell(1, columnIndex).Value;
            if (!superHeaderValue.IsText || (string)superHeaderValue == string.Empty) continue;
            
            if (!superHeaderPositions.ContainsKey((string)superHeaderValue))
            {
                superHeaderPositions.Add((string)superHeaderValue, []);
            }

            superHeaderPositions[(string)superHeaderValue].Add(columnIndex);
        }

        foreach (var superHeaderPosition in superHeaderPositions)
        {
            worksheet.Range(1, superHeaderPosition.Value.Min(), 1, superHeaderPosition.Value.Max()).Merge();
            worksheet.Cell(1, superHeaderPosition.Value.Min()).Style.Alignment
                .SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
    }

    public void AddWorksheetMetadata(IXLWorkbook workbook, Analysis analysis)
    {
        var wsMeta = workbook.AddWorksheet("Meta");
        wsMeta.Column("A").Width = 25;
        
        var metaInformation = analysis.MetaInformation;
        var rowCount = 1;

        foreach (var key in metaInformation.Keys)
        {
            if (metaInformation[key] == null) continue;
            
            wsMeta.Cell(rowCount, 1).Value = key;
            wsMeta.Cell(rowCount, 2).Value = metaInformation[key] switch
            {
                string aString => aString,
                int aInt => aInt,
                double aDouble => aDouble,
                _ => metaInformation[key]!.ToString()
            };
            
            rowCount++;
        }
        
        var variableLabels = analysis.VariableLabels;

        if (variableLabels.Count == 0) return;
        
        rowCount++;
        wsMeta.Cell(rowCount, 1).Value = "Variables with labels:";
        rowCount++;

        foreach (var variableLabel in variableLabels)
        {
            wsMeta.Cell(rowCount, 1).Value = variableLabel.Key;
            wsMeta.Cell(rowCount, 2).Value = variableLabel.Value;
            rowCount++;
        }
    }

    public IXLWorkbook CreateXlsxExport(Analysis analysis, DataView dataView, DataView? secondaryDataView, Dictionary<string, string> columnTooltips)
    {
        XLWorkbook wb = new();
        wb.ColumnWidth = 22.14;

        wb.AddWorksheetDataTable(dataView.ToTable(dataView.Table?.TableName ?? analysis.AnalysisName));
            
        if (analysis is AnalysisFreq)
        {
            CreateFrequenciesTableSuperHeader(wb.Worksheet(dataView.Table!.TableName), dataView.Table!, columnTooltips);
        }

        if (secondaryDataView != null)
        {
            wb.AddWorksheetDataTable(secondaryDataView.ToTable(secondaryDataView.Table?.TableName ?? analysis.AnalysisName + " (secondary)"));
        }
            
        AddWorksheetMetadata(wb, analysis);

        return wb;
    }

    [GeneratedRegex("^Cat\\s[0-9\\.]+(\\s-\\s.*)?$")]
    private static partial Regex RegexCategoryHeader();
    
    [GeneratedRegex("^Cat\\s")]
    private static partial Regex RegexCategoryHeaderStart();
    
    [GeneratedRegex("\\s-\\s(standard\\serror|weighted|cases|FMI)$")]
    private static partial Regex RegexCategoryHeaderEnd();

    [GeneratedRegex("^[0-9\\.]+$")]
    private static partial Regex RegexJustCategoryValue();
}
