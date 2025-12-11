using System.Data;
using ClosedXML.Excel;
using LSAnalyzer.Helper;
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

    [Theory, ClassData(typeof(TestAllMassExportFileNamesCases))]
    public void TestAllMassExportFileNames(ExportType exportType, bool singleFile, List<Analysis> analyses, List<string> expected)
    {
        ExportService exportService = new();
        
        Assert.Equivalent(expected, exportService.AllMassExportFileNames(@"C:\myFiles", "myOutput", exportType, singleFile, analyses));
    }

    public class TestAllMassExportFileNamesCases : TheoryData<ExportType, bool, List<Analysis>, List<string>>
    {
        private static readonly AnalysisFreq AnalysisFreq = new(new AnalysisConfiguration());
        
        private static readonly AnalysisLinreg AnalysisLinreg = new(new AnalysisConfiguration());
        
        public TestAllMassExportFileNamesCases()
        {
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"), true, [ AnalysisFreq ], [ @"C:\myFiles\myOutput.xlsx" ]);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"), false, [ AnalysisFreq ], [ @"C:\myFiles\myOutput_frequencies_1.xlsx" ]);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"), false, [ AnalysisFreq, AnalysisLinreg, AnalysisFreq ], [ @"C:\myFiles\myOutput_frequencies_1.xlsx", @"C:\myFiles\myOutput_linearregression_1.xlsx", @"C:\myFiles\myOutput_frequencies_2.xlsx" ]);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMultiple"), false, [ AnalysisFreq, AnalysisFreq ], [ @"C:\myFiles\myOutput_frequencies_1.csv", @"C:\myFiles\myOutput_frequencies_1_bivariate.csv", @"C:\myFiles\myOutput_frequencies_1_meta.csv", @"C:\myFiles\myOutput_frequencies_2.csv", @"C:\myFiles\myOutput_frequencies_2_bivariate.csv", @"C:\myFiles\myOutput_frequencies_2_meta.csv" ]);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMainTable"), false, [ AnalysisFreq, AnalysisFreq ], [ @"C:\myFiles\myOutput_frequencies_1.csv", @"C:\myFiles\myOutput_frequencies_2.csv" ]);
        }
    }

    [Fact]
    public void TestCreateFrequenciesTableSuperHeaderWithoutLabels()
    {
        DataTable table = new("table1");
        table.Columns.Add("var", typeof(string));
        table.Columns.Add("group", typeof(string));
        table.Columns.Add("Cat 1", typeof(double));
        table.Columns.Add("Cat 1 - standard error", typeof(double));
        table.Columns.Add("Cat 2", typeof(double));
        table.Columns.Add("Cat 2 - standard error", typeof(double));

        table.Rows.Add(["myVar", "A", 0.1, 0.01, 0.9, 0.01]);
        table.Rows.Add(["myVar", "B", 0.2, 0.02, 0.8, 0.02]);
        table.Rows.Add(["myVar", "C", 0.3, 0.03, 0.7, 0.03]);

        XLWorkbook wb = new();
        wb.AddWorksheetDataTable(table);
        var worksheet = wb.Worksheet("table1");
        
        Assert.Equal(4, worksheet.RowsUsed().Count());
        
        ExportService exportService = new();
        exportService.CreateFrequenciesTableSuperHeader(worksheet, table, []);
        
        Assert.Equal(5, worksheet.RowsUsed().Count());
        Assert.Equal(XLAlignmentHorizontalValues.Center, worksheet.Cell("C1").Style.Alignment.Horizontal);
        Assert.Equal("Cat 1", worksheet.Cell("C1").Value);        
        Assert.Equal("1 - %", worksheet.Cell("C2").Value);
        Assert.Equal(XLCellValue.FromObject(null), worksheet.Cell("D1").Value);
        Assert.True(worksheet.Cell("C1").IsMerged());
        Assert.Equal("0.0%", worksheet.Column("C").Style.NumberFormat.Format);
        Assert.Equal(XLAlignmentHorizontalValues.Center, worksheet.Cell("E1").Style.Alignment.Horizontal);
        Assert.Equal("Cat 2", worksheet.Cell("E1").Value);        
        Assert.Equal("2 - %", worksheet.Cell("E2").Value);
        Assert.Equal(XLCellValue.FromObject(null), worksheet.Cell("F1").Value);
        Assert.True(worksheet.Cell("E1").IsMerged());
        Assert.Equal("0.0%", worksheet.Column("E").Style.NumberFormat.Format);
    }
    
    [Fact]
    public void TestCreateFrequenciesTableSuperHeaderWithLabels()
    {
        DataTable table = new("table1");
        table.Columns.Add("var", typeof(string));
        table.Columns.Add("group", typeof(string));
        table.Columns.Add("Cat 1", typeof(double));
        table.Columns.Add("Cat 1 - standard error", typeof(double));
        table.Columns.Add("Cat 2", typeof(double));
        table.Columns.Add("Cat 2 - standard error", typeof(double));

        table.Rows.Add(["myVar", "A", 0.1, 0.01, 0.9, 0.01]);
        table.Rows.Add(["myVar", "B", 0.2, 0.02, 0.8, 0.02]);
        table.Rows.Add(["myVar", "C", 0.3, 0.03, 0.7, 0.03]);

        XLWorkbook wb = new();
        wb.AddWorksheetDataTable(table);
        var worksheet = wb.Worksheet("table1");
        
        Assert.Equal(4, worksheet.RowsUsed().Count());
        
        ExportService exportService = new();
        exportService.CreateFrequenciesTableSuperHeader(worksheet, table, new Dictionary<string, string> {
            { "Cat 1", "Cat 1 - Yes" },
            { "Cat 1 - standard error", "Cat 1 - Yes" },
            { "Cat 2", "Cat 2 - No" },
            { "Cat 2 - standard error", "Cat 2 - No" },
        });
        
        Assert.Equal(5, worksheet.RowsUsed().Count());
        Assert.Equal(XLAlignmentHorizontalValues.Center, worksheet.Cell("C1").Style.Alignment.Horizontal);
        Assert.Equal("1 - Yes", worksheet.Cell("C1").Value);        
        Assert.Equal("1 - %", worksheet.Cell("C2").Value);
        Assert.Equal(XLCellValue.FromObject(null), worksheet.Cell("D1").Value);
        Assert.True(worksheet.Cell("C1").IsMerged());
        Assert.Equal("0.0%", worksheet.Column("C").Style.NumberFormat.Format);
        Assert.Equal(XLAlignmentHorizontalValues.Center, worksheet.Cell("E1").Style.Alignment.Horizontal);
        Assert.Equal("2 - No", worksheet.Cell("E1").Value);        
        Assert.Equal("2 - %", worksheet.Cell("E2").Value);
        Assert.Equal(XLCellValue.FromObject(null), worksheet.Cell("F1").Value);
        Assert.True(worksheet.Cell("E1").IsMerged());
        Assert.Equal("0.0%", worksheet.Column("E").Style.NumberFormat.Format);
    }

    [Fact]
    public void TestAddWorksheetMetadataWithoutVariableLabels()
    {
        XLWorkbook wb = new();

        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisCorr(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "item1", false),
                new Variable(2, "item2", false),
                new Variable(3, "item3", false),
            ],
            CalculateOverall = true,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        ExportService exportService = new();
        exportService.AddWorksheetMetadata(wb, analysis);
        
        Assert.Equal(1, wb.Worksheets.Count);
        Assert.Equal("Meta", wb.Worksheets.First().Name);
        
        var worksheet = wb.Worksheets.First();
        
        Assert.Equal(7, worksheet.RowsUsed().Count());
        Assert.Empty(worksheet.Column("A").Cells().Where(cell => cell.Value.ToString() == "Subset:"));

        analysis.SubsettingExpression = "cat == 4";
        
        wb.Worksheets.First().Delete();
        
        exportService.AddWorksheetMetadata(wb, analysis);
        
        worksheet = wb.Worksheets.First();
        
        Assert.Equal(8, worksheet.RowsUsed().Count());
        Assert.Single(worksheet.Column("A").Cells().Where(cell => cell.Value.ToString() == "Subset:"));
    }
    
    [Fact]
    public void TestAddWorksheetMetadataWithVariableLabels()
    {
        XLWorkbook wb = new();

        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisCorr(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "item1", false) { Label = "Item 1" },
                new Variable(2, "item2", false) { Label = "Item 2" },
                new Variable(3, "item3", false),
            ],
            CalculateOverall = true,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        ExportService exportService = new();
        exportService.AddWorksheetMetadata(wb, analysis);
        
        var worksheet = wb.Worksheets.First();
        
        Assert.Equal(10, worksheet.RowsUsed().Count());
        Assert.Equal(XLCellValue.FromObject(null), worksheet.Cell("A8").Value);
        Assert.Single(worksheet.Column("A").Cells().Where(cell => cell.Value.ToString() == "item2"));
        Assert.Empty(worksheet.Column("A").Cells().Where(cell => cell.Value.ToString() == "item3"));
    }
    
    [Fact]
    public void TestAddWorksheetMetadataWithVariableLabelsButWithoutStyles()
    {
        XLWorkbook wb = new();

        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisCorr(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "item1", false) { Label = "Item 1" },
                new Variable(2, "item2", false) { Label = "Item 2" },
                new Variable(3, "item3", false),
            ],
            CalculateOverall = true,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        ExportService exportService = new();
        exportService.AddWorksheetMetadata(wb, analysis, false);
        
        var worksheet = wb.Worksheets.First();
        
        Assert.Equal(9, worksheet.RowsUsed().Count());
        Assert.Equal("item2", worksheet.Cell("A9").Value);
    }

    [Fact]
    public void TestCreateXlsxExportWithStyles()
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisFreq(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "myVar", false) { Label = "My Categorical Variable" },
            ],
            GroupBy = [
                new Variable(2, "group", false) { Label = "My Grouping Variable" }
            ],
            CalculateOverall = false,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        DataTable table = new("Freq");
        table.Columns.Add("var", typeof(string));
        table.Columns.Add("group", typeof(string));
        table.Columns.Add("Cat 1", typeof(double));
        table.Columns.Add("Cat 1 - standard error", typeof(double));
        table.Columns.Add("Cat 2", typeof(double));
        table.Columns.Add("Cat 2 - standard error", typeof(double));

        table.Rows.Add(["myVar", "A", 0.1, 0.01, 0.9, 0.01]);
        table.Rows.Add(["myVar", "B", 0.2, 0.02, 0.8, 0.02]);
        table.Rows.Add(["myVar", "C", 0.3, 0.03, 0.7, 0.03]);
        
        var dataView = table.AsDataView();
        
        DataTable secondaryTable = new("bivariate");
        secondaryTable.Columns.Add("something", typeof(string));
        secondaryTable.Rows.Add(["some bivariate information"]);
        
        var secondaryDataView = secondaryTable.AsDataView();
        
        ExportService exportService = new();
        var workbook = exportService.CreateXlsxExport(analysis, dataView, secondaryDataView, []);
        
        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.Equal("Freq", workbook.Worksheets.First().Name);
        Assert.Equal(5, workbook.Worksheets.First().RowsUsed().Count());
        Assert.Equal("Meta", workbook.Worksheets.Last().Name);
        Assert.Equal(10, workbook.Worksheets.Last().RowsUsed().Count());
    }
    
    [Fact]
    public void TestCreateXlsxExportWithoutStyles()
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisFreq(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "myVar", false) { Label = "My Categorical Variable" },
            ],
            GroupBy = [
                new Variable(2, "group", false) { Label = "My Grouping Variable" }
            ],
            CalculateOverall = false,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        DataTable table = new("Freq");
        table.Columns.Add("var", typeof(string));
        table.Columns.Add("group", typeof(string));
        table.Columns.Add("Cat 1", typeof(double));
        table.Columns.Add("Cat 1 - standard error", typeof(double));
        table.Columns.Add("Cat 2", typeof(double));
        table.Columns.Add("Cat 2 - standard error", typeof(double));

        table.Rows.Add(["myVar", "A", 0.1, 0.01, 0.9, 0.01]);
        table.Rows.Add(["myVar", "B", 0.2, 0.02, 0.8, 0.02]);
        table.Rows.Add(["myVar", "C", 0.3, 0.03, 0.7, 0.03]);
        
        var dataView = table.AsDataView();
        
        ExportService exportService = new();
        var workbook = exportService.CreateXlsxExport(analysis, dataView, null, [], false);
        
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("Freq", workbook.Worksheets.First().Name);
        Assert.Equal(4, workbook.Worksheets.First().RowsUsed().Count());
        Assert.Equal("Meta", workbook.Worksheets.Last().Name);
        Assert.Equal(9, workbook.Worksheets.Last().RowsUsed().Count());
    }

    [Fact]
    public void TestCreateXlsxExportFromAnalysisPresentations()
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };

        List<Variable> variables =
        [
            new(1, "x1", false),
            new(2, "x2", false),
        ];

        AnalysisCorr analysisCorr1 = new(analysisConfiguration)
        {
            Vars = variables,
            GroupBy = [],
            CalculateOverall = false,
        };
        
        DataTable correlationTable = new("Correlations");
        correlationTable.Columns.AddRange([ 
            new DataColumn("var1", typeof(string)), 
            new DataColumn("var2", typeof(string)), 
            new DataColumn("est", typeof(double)), 
            new DataColumn("SE", typeof(double)),
        ]);
        correlationTable.Rows.Add([ "x1", "x2", 0.5, 0.25 ]);
        
        DataTable covarianceTable = new("Covariances");
        covarianceTable.Columns.AddRange([ 
            new DataColumn("var1", typeof(string)), 
            new DataColumn("var2", typeof(string)), 
            new DataColumn("est", typeof(double)), 
            new DataColumn("SE", typeof(double)),
        ]);
        covarianceTable.Rows.Add([ "x1", "x1", 1.0, 0.15 ]);
        covarianceTable.Rows.Add([ "x1", "x2", 0.5, 0.25 ]);
        covarianceTable.Rows.Add([ "x2", "x2", 1.0, 0.15 ]);

        AnalysisCorr analysisCorr2 = new(analysisConfiguration)
        {
            Vars = variables,
            GroupBy = [new Variable(3, "cat1", false)],
            CalculateOverall = true,
        };

        AnalysisUnivar analysisUnivar = new(analysisConfiguration)
        {
            Vars = variables,
            GroupBy = [],
            CalculateOverall = false,
        };

        List<AnalysisPresentation> analysisPresentations =
        [
            new()
            {
                Analysis = analysisCorr1,
                DataView = new DataView(correlationTable),
                SecondaryDataView = new DataView(covarianceTable),
            },
            new()
            {
                Analysis = analysisUnivar,
                DataView = new DataView(new DataTable("nonsense5")),
                SecondaryDataView = null,
            },
            new()
            {
                Analysis = analysisCorr2,
                DataView = new DataView(new DataTable("nonsense3")),
                SecondaryDataView = new DataView(new DataTable("Covariances")),
            },
        ];
        
        ExportService exportService = new();
        var wb = exportService.CreateXlsxExport(analysisPresentations);
        
        Assert.Equal(8, wb.Worksheets.Count);
        Assert.Equal([
            "correlations_1",
            "correlations_1_covariances",
            "correlations_1_meta",
            "univariate_1",
            "univariate_1_meta",
            "correlations_2",
            "correlations_2_covariances",
            "correlations_2_meta",
        ], wb.Worksheets.Select(w => w.Name).ToList());
        
        Assert.Equal(2, wb.Worksheet("correlations_1").RowsUsed().Count());
        Assert.Equal(4, wb.Worksheet("correlations_1_covariances").RowsUsed().Count());
        Assert.Equal(5, wb.Worksheet("correlations_2_meta").RowsUsed().Count());
    }
    
    [Fact]
    public void TestCreateCsvExport()
    {
        AnalysisConfiguration analysisConfiguration = new()
        {
            DatasetType = DatasetType.CreateDefaultDatasetTypes().First(),
            FileName = @"C:\myProject\myData\myFile.sav",
            FileType = "spss",
            ModeKeep = true,
        };
        
        Analysis analysis = new AnalysisFreq(analysisConfiguration)
        {
            Vars = [
                new Variable(1, "myVar", false) { Label = "My Categorical Variable" },
            ],
            GroupBy = [
                new Variable(2, "group", false) { Label = "My Grouping Variable" }
            ],
            CalculateOverall = false,
            ResultAt = DateTime.Now,
            ResultDuration = 0.13,
        };
        
        DataTable table = new("Freq");
        table.Columns.Add("var", typeof(string));
        table.Columns.Add("group", typeof(string));
        table.Columns.Add("Cat 1", typeof(double));
        table.Columns.Add("Cat 1 - standard error", typeof(double));
        table.Columns.Add("Cat 2", typeof(double));
        table.Columns.Add("Cat 2 - standard error", typeof(double));

        table.Rows.Add(["myVar", "A", 0.1, 0.01, 0.9, 0.01]);
        table.Rows.Add(["myVar", "B", 0.2, 0.02, 0.8, 0.02]);
        table.Rows.Add(["myVar", "C", 0.3, 0.03, 0.7, 0.03]);
        
        var dataView = table.AsDataView();
        
        DataTable secondaryTable = new("bivariate");
        secondaryTable.Columns.Add("something", typeof(string));
        secondaryTable.Rows.Add(["some bivariate information"]);
        
        var secondaryDataView = secondaryTable.AsDataView();
        
        ExportService exportService = new();
        var export = exportService.CreateCsvExport(analysis, dataView, secondaryDataView);
        
        Assert.Equal(3, export.Count);
        Assert.Equal(4, export[0].Count(c => c == '\n'));
        Assert.Equal(2, export[1].Count(c => c == '\n'));
        Assert.Equal(9, export[2].Count(c => c == '\n'));
    }
}