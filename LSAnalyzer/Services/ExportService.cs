using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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

    public List<string> AllMassExportFileNames(string folder, string prefix, ExportType exportType, bool singleFile, List<Analysis> analyses)
    {
        var suffix = exportType.Filter[(exportType.Filter.LastIndexOf("|*", StringComparison.Ordinal) + 2)..];
        
        if (singleFile)
        {
            return [ Path.Combine(folder, prefix + suffix) ];
        }

        Dictionary<string, int> analysisTypeCount = new();

        var fileNames = analyses.SelectMany(analysis =>
        {
            var analysisType = analysis.AnalysisName.Replace(" ", "").ToLowerInvariant();
            if (!analysisTypeCount.TryAdd(analysisType, 1))
            {
                analysisTypeCount[analysisType]++;
            }

            ExportOptions exportOptions = new()
            {
                FileName = Path.Combine(folder, prefix + "_" + analysisType + "_" + analysisTypeCount[analysisType] + suffix),
                ExportType = exportType,
            };
            
            return AllFileNames(exportOptions, analysis);
        }).ToList();

        return fileNames;
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

    public void AddWorksheetMetadata(IXLWorkbook workbook, Analysis analysis, bool useStyles = true, string sheetName = "Meta")
    {
        var wsMeta = workbook.AddWorksheet(sheetName);

        if (useStyles)
        {
            wsMeta.Column("A").Width = 25;
        }

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

        if (useStyles)
        {
            rowCount++;
            wsMeta.Cell(rowCount, 1).Value = "Variables with labels:";
            rowCount++;
        }

        foreach (var variableLabel in variableLabels)
        {
            wsMeta.Cell(rowCount, 1).Value = variableLabel.Key;
            wsMeta.Cell(rowCount, 2).Value = variableLabel.Value;
            rowCount++;
        }
    }

    public IXLWorkbook CreateXlsxExport(Analysis analysis, DataView dataView, DataView? secondaryDataView, Dictionary<string, string> columnTooltips, bool useStyles = true)
    {
        XLWorkbook wb = new();

        if (useStyles)
        {
            wb.ColumnWidth = 22.14;
        }

        wb.AddWorksheetDataTable(dataView.ToTable(dataView.Table?.TableName ?? analysis.AnalysisName), useStyles);
            
        if (analysis is AnalysisFreq && useStyles)
        {
            CreateFrequenciesTableSuperHeader(wb.Worksheet(dataView.Table!.TableName), dataView.Table!, columnTooltips);
        }

        if (secondaryDataView != null)
        {
            wb.AddWorksheetDataTable(secondaryDataView.ToTable(secondaryDataView.Table?.TableName ?? analysis.AnalysisName + " (secondary)"), useStyles);
        }
            
        AddWorksheetMetadata(wb, analysis, useStyles);

        return wb;
    }

    public IXLWorkbook CreateXlsxExport(List<AnalysisPresentation> analysisPresentations, bool useStyles = true)
    {
        Dictionary<string, int> analysisTypeCount = new();
        
        XLWorkbook wb = new();

        if (useStyles)
        {
            wb.ColumnWidth = 22.14;
        }

        foreach (var analysisPresentation in analysisPresentations)
        {
            var analysisType = analysisPresentation.Analysis.AnalysisName.Replace(" ", "").ToLowerInvariant();
            if (!analysisTypeCount.TryAdd(analysisType, 1))
            {
                analysisTypeCount[analysisType]++;
            }
            
            wb.AddWorksheetDataTable(analysisPresentation.DataView.ToTable(analysisType + "_" + analysisTypeCount[analysisType]), useStyles);
            
            if (analysisPresentation.Analysis is AnalysisFreq && useStyles)
            {
                CreateFrequenciesTableSuperHeader(wb.Worksheet(analysisType + "_" + analysisTypeCount[analysisType]), analysisPresentation.DataView.Table!, analysisPresentation.ColumnTooltips);
            }

            if (analysisPresentation.SecondaryDataView != null)
            {
                var secondarySheetName = analysisType + "_" + analysisTypeCount[analysisType] + "_" +
                                         analysisPresentation.SecondaryDataView.Table?.TableName.ToLowerInvariant();
                if (secondarySheetName.Length > 31) secondarySheetName = secondarySheetName[..31];
                
                wb.AddWorksheetDataTable(analysisPresentation.SecondaryDataView.ToTable(secondarySheetName), useStyles);
            }
            
            AddWorksheetMetadata(wb, analysisPresentation.Analysis, useStyles, analysisType + "_" + analysisTypeCount[analysisType] + "_meta");
        }

        return wb;
    }

    public List<string> CreateCsvExport(Analysis analysis, DataView dataView, DataView? secondaryDataView, bool metaTable = true)
    {
        List<string> csvStrings = [ dataView.ToTable().ToCsvString(CultureInfo.CurrentCulture) ];

        if (secondaryDataView != null)
        {
            csvStrings.Add(secondaryDataView.ToTable().ToCsvString(CultureInfo.CurrentCulture));
        }

        if (metaTable)
        {
            DataTable metaDataTable = new();
            metaDataTable.Columns.Add("key", typeof(string));
            metaDataTable.Columns.Add("value", typeof(object));
            
            var metaInformation = analysis.MetaInformation;
            
            foreach (var entry in metaInformation)
            {
                if (entry.Value == null) continue;

                metaDataTable.Rows.Add([
                    entry.Key,
                    entry.Value switch
                    {
                        string aString => aString,
                        int aInt => aInt,
                        double aDouble => aDouble,
                        _ => entry.Value.ToString()
                    }
                ]);
            }
        
            var variableLabels = analysis.VariableLabels;

            foreach (var variableLabel in variableLabels)
            {
                metaDataTable.Rows.Add([variableLabel.Key, variableLabel.Value]);
            }
            
            csvStrings.Add(metaDataTable.ToCsvString(CultureInfo.CurrentCulture, false));
        }
        
        return csvStrings;
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
