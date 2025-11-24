using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;

namespace TestLSAnalyzer.Services;

public class TestExportService
{
    [Fact]
    public void TestAllFileNames()
    {
        ExportService exportService = new();

        var baseFileNameXlsx = @"C:\myFiles\myOutput.xlsx";
        var baseFileNameCsv = @"C:\myFiles\myOutput.csv";
        
        var exportOptionsXlsxWithStyles = new ExportOptions
        {
            ExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"),
            FileName = baseFileNameXlsx,
        };
        var exportOptionsXlsxWithoutStyles = new ExportOptions
        {
            ExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithoutStyles"),
            FileName = baseFileNameXlsx,
        };
        var exportOptionsCsvMainTable = new ExportOptions
        {
            ExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMainTable"),
            FileName = baseFileNameCsv,
        };
        
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisCorr(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisFreq(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisLinreg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisLogistReg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisMeanDiff(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisPercentiles(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithStyles, new AnalysisUnivar(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisCorr(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisFreq(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisLinreg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisLogistReg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisMeanDiff(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisPercentiles(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameXlsx ], exportService.AllFileNames(exportOptionsXlsxWithoutStyles, new AnalysisUnivar(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisCorr(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisFreq(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisLinreg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisLogistReg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisMeanDiff(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisPercentiles(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMainTable, new AnalysisUnivar(new AnalysisConfiguration())));
        
        var metaFileNameCsv = @"C:\myFiles\myOutput_meta.csv";
        var bivariateFileNameCsv = @"C:\myFiles\myOutput_bivariate.csv";
        var anovaFileNameCsv = @"C:\myFiles\myOutput_anova.csv";
        var covarianceFileNameCsv = @"C:\myFiles\myOutput_covariance.csv";
        
        var exportOptionsCsvMultiple = new ExportOptions
        {
            ExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMultiple"),
            FileName = baseFileNameCsv,
        };
        
        Assert.Equal([ baseFileNameCsv, covarianceFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisCorr(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, bivariateFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisFreq(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisLinreg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisLogistReg(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, anovaFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisMeanDiff(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisPercentiles(new AnalysisConfiguration())));
        Assert.Equal([ baseFileNameCsv, metaFileNameCsv ], exportService.AllFileNames(exportOptionsCsvMultiple, new AnalysisUnivar(new AnalysisConfiguration())));
    }
}