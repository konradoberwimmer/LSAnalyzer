using System.ComponentModel;
using System.Data;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.ViewModels;

public class TestMassExport
{
    [Fact]
    public void TestCanSingleExcelFile()
    {
        MassExport massExportViewModel = new();
        massExportViewModel.SelectedExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithoutStyles");
        
        Assert.True(massExportViewModel.CanSingleExcelFile);
        
        massExportViewModel.SelectedExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMultiple");
        
        Assert.False(massExportViewModel.CanSingleExcelFile);
    }

    [Fact]
    public void TestCanExport()
    {
        MassExport massExportViewModel = new();
        
        Assert.False(massExportViewModel.CanExport);
        
        massExportViewModel.Folder = Path.GetTempPath();
        
        Assert.False(massExportViewModel.CanExport);
        
        massExportViewModel.Prefix = Path.GetRandomFileName().Replace(".", "");
        
        Assert.True(massExportViewModel.CanExport);
    }

    [Fact]
    public void TestExportCommandSendsFileInUseMessage()
    {
        AnalysisCorr analysisCorr = new(new AnalysisConfiguration())
        {
            Vars =  [
                new Variable(1, "x1"),
                new Variable(2, "x2"),
            ],
            GroupBy = [],
            CalculateOverall = false,
        };
        
        List<AnalysisPresentation> analysisPresentations =
        [
            new()
            {
                Analysis = analysisCorr,
                DataView = new DataView(new DataTable("main")),
                SecondaryDataView = new DataView(new DataTable("secondary")),
            },
        ];

        ExportService exportService = new();
        
        MassExport massExportViewModel = new(exportService)
        {
            AnalysisPresentations = new(analysisPresentations),
            SelectedExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithoutStyles"),
            SingleExcelFile = false,
            Folder = Path.GetTempPath(),
            Prefix = Path.GetRandomFileName().Replace(".", ""),
        };

        var allFileNames = exportService.AllMassExportFileNames(
            massExportViewModel.Folder,
            massExportViewModel.Prefix,
            massExportViewModel.SelectedExportType,
            massExportViewModel.SingleExcelFile,
            massExportViewModel.AnalysisPresentations.Select(p => p.Analysis).ToList()
        );
        
        using var fileStream = new FileStream(allFileNames[0], FileMode.Create, FileAccess.ReadWrite);

        var messageSent = false;
        WeakReferenceMessenger.Default.Register<MassExport.FileInUseMessage>(this, (_, _) => messageSent = true);
        
        massExportViewModel.ExportCommand.Execute(null);
        
        Assert.True(messageSent);
    }

    [Fact]
    public void TestExportCommand()
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
            new(1, "x1"),
            new(2, "x2"),
        ];

        AnalysisCorr analysisCorr1 = new(analysisConfiguration)
        {
            Vars = variables,
            GroupBy = [],
            CalculateOverall = false,
        };
        
        AnalysisCorr analysisCorr2 = new(analysisConfiguration)
        {
            Vars = variables,
            GroupBy = [new Variable(3, "cat1")],
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
                DataView = new DataView(new DataTable("main")),
                SecondaryDataView = new DataView(new DataTable("secondary")),
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
        
        MassExport massExportViewModel = new(exportService)
        {
            AnalysisPresentations = new(analysisPresentations),
            Folder = Path.GetTempPath(),
        };
        
        massExportViewModel.Prefix = Path.GetRandomFileName().Replace(".", "");
        massExportViewModel.SelectedExportType = AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMainTable");
        massExportViewModel.SingleExcelFile = false;

        var expectedFilesCreated = exportService.AllMassExportFileNames(
            massExportViewModel.Folder, 
            massExportViewModel.Prefix, 
            massExportViewModel.SelectedExportType, 
            massExportViewModel.CanSingleExcelFile && massExportViewModel.SingleExcelFile, 
            analysisPresentations.Select(p => p.Analysis).ToList()
        );
        
        Assert.NotEqual(MassExport.LastExportType, massExportViewModel.SelectedExportType);
            
        massExportViewModel.ExportCommand.Execute(null);
        Assert.True(massExportViewModel.IsBusy);
        Assert.Equal(MassExport.LastExportType, massExportViewModel.SelectedExportType);

        Policy.Handle<AllException>().WaitAndRetry(5000, _ => TimeSpan.FromMilliseconds(500))
            .Execute(() => Assert.All(expectedFilesCreated, f => Assert.True(File.Exists(f))));

        Policy.Handle<FalseException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(5))
            .Execute(() => Assert.False(massExportViewModel.IsBusy));
    }
    
    [Theory, ClassData(typeof(TestMassExportWorkerCases))]
    public void TestMassExportWorker(ExportType exportType, bool singleFile)
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
            new(1, "x1"),
            new(2, "x2"),
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
            GroupBy = [new Variable(3, "cat1")],
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
        
        MassExport massExportViewModel = new(exportService)
        {
            AnalysisPresentations = new(analysisPresentations),
            Folder = Path.GetTempPath(),
        };
        
        massExportViewModel.Prefix = Path.GetRandomFileName().Replace(".", "");
        massExportViewModel.SelectedExportType = exportType;
        massExportViewModel.SingleExcelFile = singleFile;

        var expectedFilesCreated = exportService.AllMassExportFileNames(
            massExportViewModel.Folder, 
            massExportViewModel.Prefix, 
            massExportViewModel.SelectedExportType, 
            massExportViewModel.CanSingleExcelFile && massExportViewModel.SingleExcelFile, 
            analysisPresentations.Select(p => p.Analysis).ToList()
        );
            
        massExportViewModel.MassExportWorker(null, new DoWorkEventArgs(null));
        
        Assert.All(expectedFilesCreated, f => Assert.True(File.Exists(f)));
    }

    public class TestMassExportWorkerCases : TheoryData<ExportType, bool>
    {
        public TestMassExportWorkerCases()
        {
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"), false);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithoutStyles"), false);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithStyles"), true);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "excelWithoutStyles"), true);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMultiple"), false);
            Add(AnalysisPresentation.ExportTypes.Find(t => t.Name == "csvMainTable"), true);
        }
    }
}