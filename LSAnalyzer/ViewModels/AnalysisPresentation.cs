using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace LSAnalyzer.ViewModels;

public partial class AnalysisPresentation : ObservableObject
{
    public static List<ExportType> ExportTypes =>
    [
        new() { Name = "excelWithStyles", Filter = "Excel with styles (*.xlsx)|*.xlsx" },
        new() { Name = "excelWithoutStyles", Filter = "Excel without styles (*.xlsx)|*.xlsx" },
        new() { Name = "csvMultiple", Filter = "CSV: Multiple files (*.csv)|*.csv" },
        new() { Name = "csvMainTable", Filter = "CSV: Main table only (*.csv)|*.csv" },
    ];
    
    protected MainWindow? _mainWindowViewModel = null;
    public MainWindow MainWindowViewModel => _mainWindowViewModel!;

    private IResultService? _resultService;
    public IResultService ResultService
    {
        get => _resultService ??= (App.Current as App)?.ServiceProvider.GetService<IResultService>() ?? new ResultService();
        set => _resultService ??= value;
    }

    private IExportService? _exportService;
    public IExportService ExportService
    {
        get => _exportService ??= (App.Current as App)?.ServiceProvider.GetService<IExportService>() ?? new ExportService();
        set => _exportService ??= value;
    }

    [ObservableProperty]
    private Analysis _analysis;

    [ObservableProperty]
    private DataTable _dataTable;

    [ObservableProperty]
    private DataView _dataView;

    [ObservableProperty]
    private bool _hasVariableLabels = false;

    [ObservableProperty]
    private bool _showVariableLabels = Properties.Settings.Default.showLabelsDefault;
    partial void OnShowVariableLabelsChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasTableAverage = false;

    [ObservableProperty]
    private bool _useTableAverage = true;
    partial void OnUseTableAverageChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasPValues = true;

    [ObservableProperty]
    private bool _showPValues = false;
    partial void OnShowPValuesChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasFMI = true;

    [ObservableProperty]
    private bool _showFMI = false;
    partial void OnShowFMIChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasRank = false;

    [ObservableProperty]
    private bool _showRank = false;
    partial void OnShowRankChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasNcases = false;

    [ObservableProperty]
    private bool _showNcases = false;
    partial void OnShowNcasesChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private bool _hasNweight = false;

    [ObservableProperty]
    private bool _showNweight = false;
    partial void OnShowNweightChanged(bool value)
    {
        ResetDataView();
    }

    [ObservableProperty]
    private DataTable? _secondaryTable;

    [ObservableProperty]
    private DataView? _secondaryDataView;

    [ObservableProperty]
    private bool _hasColumnTooltips = false;

    private Dictionary<string, string> _columnTooltips = new();
    public Dictionary<string, string> ColumnTooltips
    {
        get => _columnTooltips;
    }
    
    [ObservableProperty]
    private bool _isBusy = false;
    partial void OnIsBusyChanged(bool value)
    {
        _mainWindowViewModel?.NotifyIsBusy();
    }

    [ExcludeFromCodeCoverage]
    public AnalysisPresentation()
    {
        // design-time only parameter-less constructor
        AnalysisConfiguration dummyConfiguration = new()
        {
            FileName = "C:\\dummyDirectory\\dummyDataset.sav",
            DatasetType = new()
            {
                Name = "Dummy Dataset Type",
                Weight = "dummyWgt",
                NMI = 10,
                MIvar = "dummyMiwar",
                RepWgts = "dummyRepwgts",
                FayFac = 1,
            },
            ModeKeep = true,
        };

        Analysis = new AnalysisUnivar(dummyConfiguration)
        {
            Vars = new()
                    {
                        new(1, "x1", false),
                        new(2, "x2", false),
                        new(3, "x3", false),
                    },
            GroupBy = new()
                    {
                        new(4, "y1", false),
                    },
            SubsettingExpression = "cat == 1 & val < 0.5",
        };

        DataTable = new()
        {
            Columns = { { "var", typeof(string) }, { "y1", typeof(int) }, { "mean", typeof(double) }, { "mean__se", typeof(double) }, { "sd", typeof(double) }, { "sd__se", typeof(double) } },
            Rows =
            {
                { "x1", 1, 0.5, 0.01, 0.1, 0.001 },
                { "x1", 2, 0.6, 0.006, 0.12, 0.0011 },
                { "x1", 3, 0.7, 0.012, 0.09, 0.0009 },
                { "x1", 4, 0.8, 0.011, 0.11, 0.0011 },
                { "x2", 1, 12.5, 0.12, 1.41, 0.023 },
                { "x2", 2, 11.3, 0.13, 1.02, 0.064 },
                { "x2", 3, 9.8, 0.22, 2.01, 0.044 },
                { "x2", 4, 12.1, 0.21, 2.01, 0.031 },
            }
        };
        HasTableAverage = true;
        HasColumnTooltips = true;
        DataView = new(DataTable);

        SecondaryTable = new("Explained variance")
        {
            Columns = { { "var", typeof(string) }, { "eta2", typeof(double) }, { "eta", typeof(double) }, { "eta__se", typeof(double) } },
            Rows =
            {
                { "x", 0.25, 0.50, 0.02 },
                { "y", 0.16, 0.40, 0.15 },
            },
        };
        SecondaryDataView = new(SecondaryTable);
    }

    public AnalysisPresentation(Analysis analysis, MainWindow? mainWindowViewModel = null)
    {
        Analysis = analysis;
        DataTable = new();
        DataView = new(DataTable);
        _mainWindowViewModel = mainWindowViewModel;
    }

    public void SetAnalysisResult(List<GenericVector> result)
    {
        IsBusy = true;

        Analysis.Result = result;
        OnPropertyChanged(nameof(Analysis));

        ColumnTooltips.Clear();
        HasColumnTooltips = false;
        
        switch (Analysis)
        {
            case AnalysisUnivar analysisUnivar:
                DataTable = CreateDataTableFromResultUnivar(analysisUnivar);
                break;
            case AnalysisMeanDiff analysisMeanDiff:
                DataTable = CreateDataTableFromResultMeanDiff(analysisMeanDiff);
                SecondaryTable = CreateTableEtaFromResultMeanDiff(analysisMeanDiff);
                ShowPValues = true;
                break;
            case AnalysisFreq analysisFreq:
                DataTable = CreateDataTableFromResultFreq(analysisFreq);
                if (analysisFreq.CalculateBivariate)
                {
                    SecondaryTable = CreateTableBivariateFromResultFreq(analysisFreq);
                }
                break;
            case AnalysisPercentiles analysisPercentiles:
                DataTable = CreateDataTableFromResultPercentiles(analysisPercentiles);
                break;
            case AnalysisCorr analysisCorr:
                DataTable = CreateDataTableFromResultCorr(analysisCorr);
                SecondaryTable = CreateTableCovarianceFromResultCorr(analysisCorr);
                break;
            case AnalysisRegression analysisRegression:
                DataTable = CreateDataTableFromResultRegression(analysisRegression);
                break;
            default:
                break;
        }
        ResetDataView();

        IsBusy = false;
    }

    public DataTable CreateDataTableFromResultUnivar(AnalysisUnivar analysisUnivar)
    {
        if (analysisUnivar.Result.Count == 0)
        {
            return new();
        }

        ResultService.Analysis = analysisUnivar;
        DataTable table = ResultService.CreatePrimaryTable()!;

        if (analysisUnivar.VariableLabels.Count > 0)
        {
            HasVariableLabels = true;
        }

        if (analysisUnivar.Vars.Count == 1 && analysisUnivar.GroupBy.Count == 1)
        {
            var relevantRows = table.AsEnumerable().Where(row => row.Field<double?>(analysisUnivar.GroupBy.First().Name) != null);
            var values = relevantRows.Select(row => row.Field<double>("mean")).ToList();
            var valuesSE = relevantRows.Select(row => row.Field<double>("mean - standard error")).ToList();
            var rowCount = relevantRows.ToList().Count;

            double average = values.Sum() / rowCount;
            double averageSE = Math.Sqrt(valuesSE.ConvertAll(se => Math.Pow(se, 2.0)).Sum() / Math.Pow(rowCount, 2.0));

            var newRow = table.NewRow();
            newRow["variable"] = "- TABLE AVERAGE:";
            newRow["mean"] = average;
            newRow["mean - standard error"] = averageSE;
            table.Rows.Add(newRow);

            HasTableAverage = true;
        }

        if (analysisUnivar.GroupBy.Count >= 1)
        {
            HasRank = true;
        }

        string[] sortBy = { "variable" };
        table.DefaultView.Sort = String.Join(", ", sortBy.Concat(analysisUnivar.GroupBy.ConvertAll(var => var.Name)).ToArray());

        return table;
    }

    public DataTable CreateDataTableFromResultMeanDiff(AnalysisMeanDiff analysisMeanDiff)
    {
        if (analysisMeanDiff.Result.Count == 0)
        {
            return new();
        }

        DataTable table = new(analysisMeanDiff.AnalysisName);
        Dictionary<string, DataColumn> columns = new();

        AddVariableLabelColumn(analysisMeanDiff, columns, "var", "variable");

        if (analysisMeanDiff.CalculateSeparately)
        {
            columns.Add("group", new DataColumn("groups by", typeof(string)));
            columns.Add("groupval1", new DataColumn("group A - value", typeof(double)));
            columns.Add("$label_groupval1_1", new DataColumn("group A - label", typeof(string)));
            columns.Add("groupval2", new DataColumn("group B - value", typeof(double)));
            columns.Add("$label_groupval2_1", new DataColumn("group B - label", typeof(string)));
        }
        else
        {
            for (int cntGroupyBy = 0; cntGroupyBy < analysisMeanDiff.GroupBy.Count; cntGroupyBy++)
            {
                columns.Add("groupval1_" + (cntGroupyBy + 1), new DataColumn("group A - " + analysisMeanDiff.GroupBy[cntGroupyBy].Name, typeof(double)));
                if (analysisMeanDiff.ValueLabels.ContainsKey(analysisMeanDiff.GroupBy[cntGroupyBy].Name))
                {
                    columns.Add("$label_groupval1_" + (cntGroupyBy + 1), new DataColumn("group A - " + analysisMeanDiff.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                }
                columns.Add("groupval2_" + (cntGroupyBy + 1), new DataColumn("group B - " + analysisMeanDiff.GroupBy[cntGroupyBy].Name, typeof(double)));
                if (analysisMeanDiff.ValueLabels.ContainsKey(analysisMeanDiff.GroupBy[cntGroupyBy].Name))
                {
                    columns.Add("$label_groupval2_" + (cntGroupyBy + 1), new DataColumn("group B - " + analysisMeanDiff.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                }
            }
        }

        columns.Add("M1", new DataColumn("mean - group A", typeof(double)));
        columns.Add("$M1_SE", new DataColumn("mean - group A - standard error", typeof(double)));
        columns.Add("M2", new DataColumn("mean - group B", typeof(double)));
        columns.Add("$M2_SE", new DataColumn("mean - group B - standard error", typeof(double)));
        columns.Add("SD", new DataColumn("standard deviation (pooled)", typeof(double)));
        columns.Add("d", new DataColumn("Cohens d", typeof(double)));
        columns.Add("d_SE", new DataColumn("Cohens d - standard error", typeof(double)));
        columns.Add("d_p", new DataColumn("Cohens d - p value", typeof(double)));
        columns.Add("d_fmi", new DataColumn("Cohens d - FMI", typeof(double)));

        foreach (var column in columns.Values)
        {
            table.Columns.Add(column);
        }

        for (int cntResult = 0; cntResult < Analysis.Result.Count; cntResult++)
        {

            var result = Analysis.Result[cntResult];

            var dataFrame = result["stat.dstat"].AsDataFrame();
            var univarDataFrame = result["stat_M"].AsDataFrame();

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                DataRow tableRow = table.NewRow();

                List<object?> cellValues = new();
                foreach (var column in columns.Keys)
                {
                    if (Regex.IsMatch(column, "^groupval(1|2)_"))
                    {
                        var groupPosition = Convert.ToInt32(Regex.Replace(column, "groupval(1|2)_", ""));
                        var groupValVariable = column.Substring(0, 9);

                        double groupVal;
                        if (dataFrameRow[groupValVariable] is double)
                        {
                            groupVal = (double)dataFrameRow[groupValVariable];
                        }
                        else
                        {
                            var groupVals = (string)dataFrameRow[groupValVariable];
                            var groupValsSplit = groupVals.Split('#');
                            groupVal = Convert.ToDouble(groupValsSplit[groupPosition - 1]);
                        }

                        cellValues.Add(groupVal);
                    }
                    else if (Regex.IsMatch(column, "^\\$label_groupval(1|2)_"))
                    {
                        var groupPosition = Convert.ToInt32(Regex.Replace(column, "\\$label_groupval(1|2)_", ""));
                        var groupVars = (string)dataFrameRow["group"];
                        var groupVarsSplit = groupVars.Split("#");
                        var groupVar = groupVarsSplit[groupPosition - 1];
                        
                        if (analysisMeanDiff.ValueLabels.ContainsKey(groupVar))
                        {
                            var groupvalColumn = Regex.Replace(Regex.Replace(column, "\\$label_", ""), "_[0-9]+$", "");

                            double groupVal;
                            if (dataFrameRow[groupvalColumn] is double)
                            {
                                groupVal = (double)dataFrameRow[groupvalColumn];
                            }
                            else
                            {
                                var groupVals = (string)dataFrameRow[groupvalColumn];
                                var groupValsSplit = groupVals.Split('#');
                                groupVal = Convert.ToDouble(groupValsSplit[groupPosition - 1]);
                            }

                            var valueLabels = analysisMeanDiff.ValueLabels[groupVar];
                            var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf(groupVal);

                            if (posValueLabel != -1)
                            {
                                var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                                cellValues.Add(valueLabel);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        } else
                        {
                            cellValues.Add(null);
                        }
                    }
                    else if (Regex.IsMatch(column, "^\\$M(1|2)_SE$"))
                    {
                        var groupNr = Convert.ToInt32(Regex.Match(column, "[0-9]").Value);
                        var groupValues = Convert.ToString(dataFrameRow["groupval" + groupNr])?.Split("#");

                        if (groupValues != null)
                        {
                            foreach (var univarDataFrameRow in univarDataFrame.GetRows())
                            {
                                if ((string)univarDataFrameRow["var"] != (string)dataFrameRow["var"])
                                {
                                    continue;
                                }
                                
                                bool matchGroup = true;

                                for (int gg = 0; gg < groupValues.Length; gg++)
                                {
                                    var groupColumnUnivar = (gg > 1 || groupValues.Length > 1) ? "groupval" + (gg + 1) : "groupval";
                                    if ((double)univarDataFrameRow[groupColumnUnivar] != Convert.ToDouble(groupValues[gg]))
                                    {
                                        matchGroup = false;
                                        break;
                                    }
                                }

                                if (matchGroup)
                                {
                                    cellValues.Add((double)univarDataFrameRow["M_SE"]);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            cellValues.Add(null);
                        }

                    }
                    else if (Regex.IsMatch(column, "^\\$varlabel_"))
                    {
                        if (dataFrameRow["var"] is string varName && analysisMeanDiff.VariableLabels.ContainsKey(varName))
                        {
                            cellValues.Add(analysisMeanDiff.VariableLabels[varName]);
                        }
                        else
                        {
                            cellValues.Add(null);
                        }
                    } else if (dataFrame.ColumnNames.Contains(column))
                    {
                        cellValues.Add(dataFrameRow[column]);
                    }
                    else
                    {
                        cellValues.Add(null);
                    }
                }

                tableRow.ItemArray = cellValues.ToArray();
                table.Rows.Add(tableRow);
            }
        }

        return table;
    }

    public DataTable CreateTableEtaFromResultMeanDiff(AnalysisMeanDiff analysisMeanDiff)
    {
        if (analysisMeanDiff.Result.Count == 0)
        {
            return new();
        }

        ResultService.Analysis = analysisMeanDiff;
        DataTable table = ResultService.CreateSecondaryTable()!;

        return table;
    }

    public DataTable CreateDataTableFromResultFreq(AnalysisFreq analysisFreq)
    {
        if (analysisFreq.Result.Count == 0)
        {
            return new();
        }

        List<double> categories = new();
        foreach (var result in Analysis.Result)
        {
            var dataFrame = result["stat"].AsDataFrame();
            var categoriesInResult = dataFrame["varval"].AsNumeric();
            foreach (double cat1 in categoriesInResult)
            {
                if (!categories.Contains(cat1))
                {
                    categories.Add(cat1);
                }
            }
        }
        categories.Sort();

        DataTable table = new(analysisFreq.AnalysisName);

        Dictionary<string, DataColumn> columns = new();

        AddVariableLabelColumn(analysisFreq, columns, "var", "variable");

        for (int cntGroupyBy = 0; cntGroupyBy < analysisFreq.GroupBy.Count; cntGroupyBy++)
        {
            columns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(analysisFreq.GroupBy[cntGroupyBy].Name, typeof(double)));
            if (analysisFreq.ValueLabels.ContainsKey(analysisFreq.GroupBy[cntGroupyBy].Name))
            {
                columns.Add("$label_" + analysisFreq.GroupBy[cntGroupyBy].Name, new DataColumn(analysisFreq.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
            }
        }

        columns.Add("$overall_Ncases", new DataColumn("N - cases unweighted", typeof(int)));
        columns.Add("$overall_Nweight", new DataColumn("N - weighted", typeof(double)));

        columns.Add("lsanalyzer_rank", new DataColumn("rank of lowest category frequency (per variable)", typeof(double)));

        var commonValueLabels = GetCommonValueLabels(analysisFreq, categories);

        for (int cc = 0; cc < categories.Count; cc++)
        {
            var category = categories[cc];
            columns.Add("$cat_" + cc + "_perc", new DataColumn("Cat " + category, typeof(double)));
            columns.Add("$cat_" + cc + "_perc_SE", new DataColumn("Cat " + category + " - standard error", typeof(double)));
            columns.Add("$cat_" + cc + "_Ncases", new DataColumn("Cat " + category + " - cases", typeof(int)));
            columns.Add("$cat_" + cc + "_Nweight", new DataColumn("Cat " + category + " - weighted", typeof(double)));
            columns.Add("$cat_" + cc + "_perc_fmi", new DataColumn("Cat " + category + " - FMI", typeof(double)));

            if (commonValueLabels.ContainsKey(category))
            {
                HasColumnTooltips = true;
                ColumnTooltips.Add("Cat " + category, "Cat " + category + " - " + commonValueLabels[category]);
                ColumnTooltips.Add("Cat " + category + " - standard error", "Cat " + category + " - " + commonValueLabels[category] + " - standard error");
                ColumnTooltips.Add("Cat " + category + " - cases", "Cat " + category + " - " + commonValueLabels[category] + " - cases");
                ColumnTooltips.Add("Cat " + category + " - weighted", "Cat " + category + " - " + commonValueLabels[category] + " - weighted");
                ColumnTooltips.Add("Cat " + category + " - FMI", "Cat " + category + " - " + commonValueLabels[category] + " - FMI");
            }
        }

        foreach (var column in columns.Values)
        {
            table.Columns.Add(column);
        }
        
        foreach (var result in Analysis.Result)
        {
            var dataFrame = result["stat"].AsDataFrame();
            var groupColumns = GetGroupColumns(dataFrame);

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                bool repeat = true;
                while (repeat)
                {
                    var existingTableRow = GetExistingDataRowByGroups(table, dataFrameRow, groupColumns);

                    if (existingTableRow != null)
                    {
                        var category = (double)dataFrameRow["varval"];

                        existingTableRow["Cat " + category] = (double)dataFrameRow["perc"];
                        existingTableRow["Cat " + category + " - standard error"] = (double)dataFrameRow["perc_SE"];
                        existingTableRow["Cat " + category + " - cases"] = (double)dataFrameRow["Ncases"];
                        existingTableRow["Cat " + category + " - weighted"] = (double)dataFrameRow["Nweight"];
                        if (dataFrame.ColumnNames.Contains("perc_fmi"))
                        {
                            existingTableRow["Cat " + category + " - FMI"] = (double)dataFrameRow["perc_fmi"];
                        }

                        repeat = false;
                    }
                    else
                    {
                        DataRow tableRow = table.NewRow();

                        List<object?> cellValues = new();
                        foreach (var column in columns.Keys)
                        {
                            if (Regex.IsMatch(column, "^groupval[0-9]*$"))
                            {
                                if (groupColumns.ContainsKey(columns[column].ColumnName))
                                {
                                    cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (Regex.IsMatch(column, "^\\$label_"))
                            {
                                if ((double?)cellValues.Last() == null)
                                {
                                    cellValues.Add(null);
                                    continue;
                                }

                                var groupByVariable = column.Substring(column.IndexOf("_") + 1);
                                var valueLabels = analysisFreq.ValueLabels[groupByVariable];
                                // TODO this is a rather ugly shortcut of getting the value that we need the label for!!!
                                var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf((double)cellValues.Last()!);

                                if (posValueLabel != -1)
                                {
                                    var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                                    cellValues.Add(valueLabel);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (Regex.IsMatch(column, "^\\$varlabel_"))
                            {
                                if (dataFrameRow["var"] is string varName && analysisFreq.VariableLabels.ContainsKey(varName))
                                {
                                    cellValues.Add(analysisFreq.VariableLabels[varName]);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (dataFrame.ColumnNames.Contains(column))
                            {
                                cellValues.Add(dataFrameRow[column]);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        }

                        tableRow.ItemArray = cellValues.ToArray();
                        table.Rows.Add(tableRow);
                    }
                }
            }
        }

        foreach (DataRow row in table.Rows)
        {
            int Ncases = 0;
            double Nweight = 0;
            foreach (var category in categories) 
            {
                Ncases += row["Cat " + category + " - cases"] != DBNull.Value ? (int)row["Cat " + category + " - cases"] : 0;
                Nweight += row["Cat " + category + " - weighted"] != DBNull.Value ? (double)row["Cat " + category + " - weighted"] : 0.0;
            }
            row["N - cases unweighted"] = Ncases;
            row["N - weighted"] = Nweight;
        }

        if (Analysis.GroupBy.Count > 0 && !table.AsEnumerable().Select(row => row.Field<double?>("Cat " + categories.First())).ToArray().Where(val => val == null).Any())
        {
            HasRank = true;
        }

        HasPValues = false;
        HasNcases = true;
        HasNweight = true;

        string[] sortBy = { "variable" };
        table.DefaultView.Sort = String.Join(", ", sortBy.Concat(analysisFreq.GroupBy.ConvertAll(var => var.Name)).ToArray());

        return table;
    }

    public DataTable? CreateTableBivariateFromResultFreq(AnalysisFreq analysisFreq)
    {
        if (analysisFreq.BivariateResult == null || analysisFreq.BivariateResult.Count == 0)
        {
            return null;
        }

        DataTable table = new("Bivariate measures");

        Dictionary<string, DataColumn> columns = new();

        AddVariableLabelColumn(analysisFreq, columns, "$varname_X", "X");
        AddVariableLabelColumn(analysisFreq, columns, "$varname_Y", "Y");
        
        columns.Add("parm", new DataColumn("coefficient", typeof(string)));
        columns.Add("est", new DataColumn("estimate", typeof(double)));
        columns.Add("SE", new DataColumn("estimate - standard error", typeof(double)));
        columns.Add("fmi", new DataColumn("estimate - FMI", typeof(double)));

        foreach (var column in columns.Values)
        {
            table.Columns.Add(column);
        }

        foreach (var result in analysisFreq.BivariateResult)
        {
            var dataFrame = result["stat.es"].AsDataFrame();
            var dataFrameProbs = result["stat.probs"].AsDataFrame();

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                DataRow tableRow = table.NewRow();

                List<object?> cellValues = new();
                foreach (var column in columns.Keys)
                {
                    if (column == "$varname_X")
                    {
                        cellValues.Add(dataFrameProbs["var1"].AsCharacter().First());
                    } 
                    else if (column == "$varname_Y")
                    {
                        cellValues.Add(dataFrameProbs["var2"].AsCharacter().First());
                    }
                    else if (Regex.IsMatch(column, "^\\$varlabel_"))
                    {
                        string varVariable = column == "$varlabel_$varname_X" ? "var1" : "var2";
                        if (dataFrameProbs[varVariable].AsCharacter().First() is string varName && analysisFreq.VariableLabels.ContainsKey(varName))
                        {
                            cellValues.Add(analysisFreq.VariableLabels[varName]);
                        }
                        else
                        {
                            cellValues.Add(null);
                        }
                    }
                    else if (dataFrame.ColumnNames.Contains(column))
                    {
                        cellValues.Add(dataFrameRow[column]);
                    }
                    else
                    {
                        cellValues.Add(null);
                    }
                }

                tableRow.ItemArray = cellValues.ToArray();
                table.Rows.Add(tableRow);
            }
        }

        return table;
    }

    public DataTable CreateDataTableFromResultPercentiles(AnalysisPercentiles analysisPercentiles)
    {
        if (analysisPercentiles.Result.Count == 0)
        {
            return new();
        }

        List<double> categories = new(analysisPercentiles.Percentiles);
        categories.Sort();

        DataTable table = new(analysisPercentiles.AnalysisName);

        Dictionary<string, DataColumn> columns = new();

        AddVariableLabelColumn(analysisPercentiles, columns, "var", "variable");

        for (int cntGroupyBy = 0; cntGroupyBy < analysisPercentiles.GroupBy.Count; cntGroupyBy++)
        {
            columns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(analysisPercentiles.GroupBy[cntGroupyBy].Name, typeof(double)));
            if (analysisPercentiles.ValueLabels.ContainsKey(analysisPercentiles.GroupBy[cntGroupyBy].Name))
            {
                columns.Add("$label_" + analysisPercentiles.GroupBy[cntGroupyBy].Name, new DataColumn(analysisPercentiles.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
            }
        }

        for (int cc = 0; cc < categories.Count; cc++)
        {
            var category = categories[cc];
            columns.Add("$cat_" + cc, new DataColumn("Perc " + category.ToString(CultureInfo.InvariantCulture), typeof(double)));
            if (analysisPercentiles.CalculateSE)
            {
                columns.Add("$cat_" + cc + "_SE", new DataColumn("Perc " + category.ToString(CultureInfo.InvariantCulture) + " - standard error", typeof(double)));
            }
        }

        foreach (var column in columns.Values)
        {
            table.Columns.Add(column);
        }

        foreach (var result in Analysis.Result)
        {
            var dataFrame = result["stat"].AsDataFrame();
            var groupColumns = GetGroupColumns(dataFrame);

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                bool repeat = true;
                while (repeat)
                {
                    var existingTableRow = GetExistingDataRowByGroups(table, dataFrameRow, groupColumns);

                    if (existingTableRow != null)
                    {
                        var category = (double)dataFrameRow["yval"];

                        existingTableRow["Perc " + category.ToString(CultureInfo.InvariantCulture)] = (double)dataFrameRow["quant"];
                        if (analysisPercentiles.CalculateSE)
                        {
                            existingTableRow["Perc " + category.ToString(CultureInfo.InvariantCulture) + " - standard error"] = (double)dataFrameRow["SE"];
                        }

                        repeat = false;
                    }
                    else
                    {
                        DataRow tableRow = table.NewRow();

                        List<object?> cellValues = new();
                        foreach (var column in columns.Keys)
                        {
                            if (Regex.IsMatch(column, "^groupval[0-9]*$"))
                            {
                                if (groupColumns.ContainsKey(columns[column].ColumnName))
                                {
                                    cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (Regex.IsMatch(column, "^\\$label_"))
                            {
                                if ((double?)cellValues.Last() == null)
                                {
                                    cellValues.Add(null);
                                    continue;
                                }

                                var groupByVariable = column.Substring(column.IndexOf("_") + 1);
                                var valueLabels = analysisPercentiles.ValueLabels[groupByVariable];
                                // TODO this is a rather ugly shortcut of getting the value that we need the label for!!!
                                var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf((double)cellValues.Last()!);

                                if (posValueLabel != -1)
                                {
                                    var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                                    cellValues.Add(valueLabel);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (Regex.IsMatch(column, "^\\$varlabel_"))
                            {
                                if (dataFrameRow["var"] is string varName && analysisPercentiles.VariableLabels.ContainsKey(varName))
                                {
                                    cellValues.Add(analysisPercentiles.VariableLabels[varName]);
                                }
                                else
                                {
                                    cellValues.Add(null);
                                }
                            }
                            else if (dataFrame.ColumnNames.Contains(column))
                            {
                                cellValues.Add(dataFrameRow[column]);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        }

                        tableRow.ItemArray = cellValues.ToArray();
                        table.Rows.Add(tableRow);
                    }
                }
            }
        }

        HasPValues = false;
        HasFMI = false;

        string[] sortBy = { "variable" };
        table.DefaultView.Sort = String.Join(", ", sortBy.Concat(analysisPercentiles.GroupBy.ConvertAll(var => var.Name)).ToArray());

        return table;
    }

    public DataTable CreateDataTableFromResultCorr(AnalysisCorr analysisCorr)
    {
        if (analysisCorr.Result.Count == 0)
        {
            return new();
        }

        ResultService.Analysis = analysisCorr;
        DataTable table = ResultService.CreatePrimaryTable()!;
        
        if (analysisCorr.VariableLabels.Count > 0)
        {
            HasVariableLabels = true;
        }

        string[] sortBy = { "variable A", "variable B" };
        table.DefaultView.Sort = String.Join(", ", analysisCorr.GroupBy.ConvertAll(var => var.Name).ToArray().Concat(sortBy));

        return table;
    }

    public DataTable CreateTableCovarianceFromResultCorr(AnalysisCorr analysisCorr)
    {
        if (analysisCorr.Result.Count == 0)
        {
            return new();
        }

        ResultService.Analysis = analysisCorr;
        DataTable table = ResultService.CreateSecondaryTable()!;

        string[] sortBy = { "variable A", "variable B" };
        table.DefaultView.Sort = String.Join(", ", analysisCorr.GroupBy.ConvertAll(var => var.Name).ToArray().Concat(sortBy));

        return table;
    }

    public DataTable CreateDataTableFromResultRegression(AnalysisRegression analysisRegression)
    {
        if (analysisRegression.Result.Count == 0)
        {
            return new();
        }

        DataTable table = new(analysisRegression.AnalysisName);
        Dictionary<string, DataColumn> columns = new();

        if (analysisRegression.Sequence == AnalysisRegression.RegressionSequence.AllIn)
        {
            for (int cntGroupyBy = 0; cntGroupyBy < analysisRegression.GroupBy.Count; cntGroupyBy++)
            {
                columns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(analysisRegression.GroupBy[cntGroupyBy].Name, typeof(double)));
                if (analysisRegression.ValueLabels.ContainsKey(analysisRegression.GroupBy[cntGroupyBy].Name))
                {
                    columns.Add("$label_" + analysisRegression.GroupBy[cntGroupyBy].Name, new DataColumn(analysisRegression.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                }
            }
        }

        columns.Add("Ncases", new DataColumn("N - cases unweighted", typeof(int)));
        columns.Add("Nweight", new DataColumn("N - weighted", typeof(double)));
        columns.Add("parameter", new DataColumn("coefficient", typeof(string)));
        AddVariableLabelColumn(analysisRegression, columns, "var", "variable");

        if (analysisRegression.Sequence == AnalysisRegression.RegressionSequence.AllIn)
        {
            columns.Add("est", new DataColumn("estimate", typeof(double)));
            columns.Add("SE", new DataColumn("standard error", typeof(double)));
            if (analysisRegression is AnalysisLogistReg)
            {
                columns.Add("$exp_est", new DataColumn("exp(estimate)", typeof(double)));
            }
            columns.Add("p", new DataColumn("p value", typeof(double)));
            columns.Add("fmi", new DataColumn("FMI", typeof(double)));
        } else
        {
            for (int rr = 0; rr < analysisRegression.Result.Count; rr++) 
            {
                columns.Add("$model_" + (rr + 1) + "_est", new DataColumn("model " + (rr+1) + " - estimate", typeof(double)));
                columns.Add("$model_" + (rr + 1) + "_SE", new DataColumn("model " + (rr + 1) + " - standard error", typeof(double)));
                if (analysisRegression is AnalysisLogistReg)
                {
                    columns.Add("$model_" + (rr + 1) + "_expest", new DataColumn("model " + (rr + 1) + " - exp(estimate)", typeof(double)));
                }
                columns.Add("$model_" + (rr + 1) + "_p", new DataColumn("model " + (rr + 1) + " - p value", typeof(double)));
                columns.Add("$model_" + (rr + 1) + "_fmi", new DataColumn("model " + (rr + 1) + " - FMI", typeof(double)));
            }
        }

        foreach (var column in columns.Values)
        {
            table.Columns.Add(column);
        }

        if (analysisRegression.Sequence != AnalysisRegression.RegressionSequence.AllIn)
        {
            var fullResult = analysisRegression.Sequence == AnalysisRegression.RegressionSequence.Forward ? Analysis.Result.Last() : Analysis.Result.First();
            var fullDataFrame = fullResult["stat"].AsDataFrame();

            foreach (var dataFrameRow in fullDataFrame.GetRows())
            {                    
                DataRow tableRow = table.NewRow();

                List<object?> cellValues = new();
                foreach (var column in columns.Keys)
                {
                    if (fullDataFrame.ColumnNames.Contains(column))
                    {
                        cellValues.Add(dataFrameRow[column]);
                        continue;
                    }
                    cellValues.Add(null);
                }

                tableRow.ItemArray = cellValues.ToArray();
                table.Rows.Add(tableRow);
            }
        }

        foreach (var result in Analysis.Result)
        {
            var dataFrame = result["stat"].AsDataFrame();
            var groupColumns = GetGroupColumns(dataFrame);

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                var existingTableRow = analysisRegression.Sequence == AnalysisRegression.RegressionSequence.AllIn ? null : GetExistingDataRowByCoefficient(table, dataFrameRow);

                if (existingTableRow != null) 
                {
                    var resultIndex = analysisRegression.Result.IndexOf(result);

                    foreach (var column in columns.Keys)
                    {
                        if (Regex.IsMatch(column, "^\\$model_" + (resultIndex+1) + "_"))
                        {
                            var content = column.Substring(column.LastIndexOf("_") + 1);
                            if (content != "expest" && dataFrame.ColumnNames.Contains(content))
                            {
                                existingTableRow[columns[column]] = (double)dataFrameRow[content];
                            } else if ((string)existingTableRow["coefficient"] == "b")
                            {
                                existingTableRow[columns[column]] = Math.Exp((double)dataFrameRow["est"]);
                            }
                        }
                    }
                } else
                {
                    DataRow tableRow = table.NewRow();

                    List<object?> cellValues = new();
                    foreach (var column in columns.Keys)
                    {
                        if (Regex.IsMatch(column, "^groupval[0-9]*$"))
                        {
                            if (groupColumns.ContainsKey(columns[column].ColumnName))
                            {
                                cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        }
                        else if (Regex.IsMatch(column, "^\\$label_"))
                        {
                            if ((double?)cellValues.Last() == null)
                            {
                                cellValues.Add(null);
                                continue;
                            }

                            var groupByVariable = column.Substring(column.IndexOf("_") + 1);
                            var valueLabels = analysisRegression.ValueLabels[groupByVariable];
                            // TODO this is a rather ugly shortcut of getting the value that we need the label for!!!
                            var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf((double)cellValues.Last()!);

                            if (posValueLabel != -1)
                            {
                                var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                                cellValues.Add(valueLabel);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        }
                        else if (Regex.IsMatch(column, "^\\$varlabel_"))
                        {
                            if (dataFrameRow["var"] is string varName && analysisRegression.VariableLabels.ContainsKey(varName))
                            {
                                cellValues.Add(analysisRegression.VariableLabels[varName]);
                            }
                            else
                            {
                                cellValues.Add(null);
                            }
                        }
                        else if (Regex.IsMatch(column, "^\\$exp_") && (string)dataFrameRow["parameter"] == "b")
                        {
                            cellValues.Add(Math.Exp((double)dataFrameRow["est"]));
                        }
                        else if (dataFrame.ColumnNames.Contains(column))
                        {
                            cellValues.Add(dataFrameRow[column]);
                        }
                        else
                        {
                            cellValues.Add(null);
                        }
                    }

                    tableRow.ItemArray = cellValues.ToArray();
                    table.Rows.Add(tableRow);
                }
            }
        }

        foreach (var rowOne in table.Select("variable = 'one'"))
        {
            rowOne["variable"] = "(intercept)";
        }

        foreach (var rowSigma in table.Select("coefficient = 'sigma'"))
        {
            rowSigma["variable"] = "";
        }

        foreach (var rowR2 in table.Select("coefficient = 'R^2' OR coefficient = 'R2'"))
        {
            rowR2["variable"] = "";
        }

        var betaInterceptRows = table.Select("coefficient = 'beta' AND variable = '(intercept)'");
        foreach (var betaInterceptRow in betaInterceptRows)
        {
            table.Rows.Remove(betaInterceptRow);
        }

        return table;
    }

    private Dictionary<double, string> GetCommonValueLabels(AnalysisFreq analysisFreq, List<double> categories)
    {
        Dictionary<double, string> commonValueLabels = new();

        foreach (var category in categories)
        {
            foreach (var variable in analysisFreq.Vars)
            {
                if (!analysisFreq.ValueLabels.ContainsKey(variable.Name))
                {
                    commonValueLabels.Remove(category);
                    break;
                }

                var valueLabels = analysisFreq.ValueLabels[variable.Name];
                var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf(category);

                if (posValueLabel != -1)
                {
                    var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                    
                    if (!commonValueLabels.ContainsKey(category)) 
                    {
                        commonValueLabels.Add(category, valueLabel);
                    } else if (commonValueLabels[category] != valueLabel)
                    {
                        commonValueLabels.Remove(category);
                        break;
                    }
                } else
                {
                    commonValueLabels.Remove(category);
                    break;
                }
            }
        }

        return commonValueLabels;
    }

    private void AddVariableLabelColumn(Analysis analysis, Dictionary<string, DataColumn> columns, string resultColumnName, string tableColumnName)
    {
        columns.Add(resultColumnName, new DataColumn(tableColumnName, typeof(string)));
        if (analysis.VariableLabels.Count > 0)
        {
            columns.Add("$varlabel_" + resultColumnName, new DataColumn(tableColumnName + " (label)", typeof(string)));
            HasVariableLabels = true;
        }
    }

    private Dictionary<string, string> GetGroupColumns(DataFrame dataFrame)
    {
        Dictionary<string, string> groupColumns = new();

        var groupNameColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupvar[0-9]*$")).ToArray();
        var groupValColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupval[0-9]*$")).ToArray();
        for (int i = 0; i < groupNameColumns.Length; i++)
        {
            groupColumns.Add(dataFrame[groupNameColumns[i]].AsCharacter().First(), groupValColumns[i]);
        }

        return groupColumns;
    }

    private DataRow? GetExistingDataRowByGroups(DataTable table, DataFrameRow dataFrameRow, Dictionary<string, string> groupColumns)
    {
        for (var rowIndex = table.Rows.Count - 1; rowIndex >= 0; rowIndex--)
        {
            var row = table.Rows[rowIndex];

            if ((string)row["variable"] != (string)dataFrameRow["var"]) continue;
            
            var match = true;

            foreach (var groupVar in Analysis.GroupBy)
            {
                if ((groupColumns.ContainsKey(groupVar.Name) && (row[groupVar.Name] == DBNull.Value || (double)row[groupVar.Name] != (double)dataFrameRow[groupColumns[groupVar.Name]])) ||
                    (!groupColumns.ContainsKey(groupVar.Name) && row[groupVar.Name] != DBNull.Value))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return row;
            }
        }

        return null;
    }

    private DataRow? GetExistingDataRowByCoefficient(DataTable table, DataFrameRow dataFrameRow)
    {
        for (var rowIndex = table.Rows.Count - 1; rowIndex >= 0; rowIndex--)
        {
            var row = table.Rows[rowIndex];
            
            if ((string)row["variable"] == (string)dataFrameRow["var"] && (string)row["coefficient"] == (string)dataFrameRow["parameter"])
            {
                return row;
            }
        }

        return null;
    }

    private void RemoveLabelColumns(DataView dataView)
    {
        if (dataView.Table == null)
        {
            return;
        }
        
        List<string> columnsToRemove = [];
        foreach (DataColumn column in dataView.Table.Columns)
        {
            if (column.ColumnName.EndsWith(" (label)") || column.ColumnName.EndsWith(" - label"))
            {
                columnsToRemove.Add(column.ColumnName);
            }
        }
            
        foreach (var columnToRemove in columnsToRemove)
        {
            dataView.Table!.Columns.Remove(columnToRemove);
        }
    }

    private DataView PreserveCurrentSorting(DataView dataView, DataView? oldDataView)
    {
        if (string.IsNullOrEmpty(oldDataView?.Sort) || dataView.Table == null)
        {
            return dataView;
        }

        var sortings = oldDataView.Sort.Split(',');
        var newColumnNames = dataView.Table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

        if (sortings.All(sorting => newColumnNames.Contains(Regex.Match(sorting, @"\[(.*?)\]").Groups[1].Value)))
        {
            dataView.Sort = oldDataView.Sort;
        }

        return dataView;
    }

    private void SetDataTableViewUnivar()
    {
        DataView dataView = new(DataTable.Copy());

        if (HasTableAverage && !UseTableAverage)
        {
            dataView.Table!.Rows.RemoveAt(dataView.Table!.Rows.Count - 1);
        }
        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }
        if (!ShowRank) dataView.Table!.Columns.Remove("rank of mean (per variable)");
        if (!ShowPValues) dataView.Table!.Columns.Remove("mean - p value");
        if (!ShowFMI) dataView.Table!.Columns.Remove("mean - FMI");
        if (!ShowPValues) dataView.Table!.Columns.Remove("standard deviation - p value");
        if (!ShowFMI) dataView.Table!.Columns.Remove("standard deviation - FMI");

        DataView = PreserveCurrentSorting(dataView, DataView);
    }

    private void SetDataTableViewMeanDiff()
    {
        DataView dataView = new(DataTable.Copy());

        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }
        if (!ShowPValues) dataView.Table!.Columns.Remove("Cohens d - p value");
        if (!ShowFMI) dataView.Table!.Columns.Remove("Cohens d - FMI");

        DataView = PreserveCurrentSorting(dataView, DataView);
        
        if (SecondaryTable != null)
        {
            DataView secondaryDataView = new(SecondaryTable.Copy());

            if (HasVariableLabels && !ShowVariableLabels)
            {
                RemoveLabelColumns(secondaryDataView);
            }
            if (!ShowFMI) secondaryDataView.Table!.Columns.Remove("eta - FMI");

            SecondaryDataView = PreserveCurrentSorting(secondaryDataView, SecondaryDataView);
        }
    }

    private void SetDataTableViewFreq()
    {
        DataView dataView = new(DataTable.Copy());

        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }
        
        Dictionary<string, string> toggles = new()
        {
            ["ShowRank"] = "^rank\\sof\\slowest\\scategory",
            ["ShowPValues"] = "p\\svalue$",
            ["ShowFMI"] = "FMI$",
            ["ShowNcases"] = "^Cat.*\\-\\scases$",
            ["ShowNweight"] = "^Cat.*\\-\\sweighted$",
        };

        foreach (KeyValuePair<string, string> toggle in toggles)
        {
            if (!(bool)this.GetType().GetProperty(toggle.Key)!.GetValue(this)!)
            {
                List<DataColumn> columnsToRemove = new();
                foreach (DataColumn column in dataView.Table!.Columns)
                {
                    if (Regex.IsMatch(column.ColumnName, toggle.Value))
                    {
                        columnsToRemove.Add(column);
                    }
                }
                foreach (var pValueColumn in columnsToRemove)
                {
                    dataView.Table!.Columns.Remove(pValueColumn);
                }
            }
        }
        
        DataView = PreserveCurrentSorting(dataView, DataView);

        if (SecondaryTable != null)
        {
            DataView secondaryDataView = new(SecondaryTable.Copy());

            if (HasVariableLabels && !ShowVariableLabels)
            {
                RemoveLabelColumns(secondaryDataView);
            }
            
            if (!ShowFMI) secondaryDataView.Table!.Columns.Remove("estimate - FMI");

            SecondaryDataView = PreserveCurrentSorting(secondaryDataView, SecondaryDataView);
        }
    }

    private void SetDataTableViewPercentiles()
    {
        DataView dataView = new(DataTable.Copy());

        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }

        DataView = PreserveCurrentSorting(dataView, DataView);
    }

    private void SetDataTableViewCorr()
    {
        DataView dataView = new(DataTable.Copy());
        
        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }

        Dictionary<string, string> toggles = new()
        {
            ["ShowPValues"] = "p\\svalue$",
            ["ShowFMI"] = "FMI$",
        };

        foreach (KeyValuePair<string, string> toggle in toggles)
        {
            if (!(bool)this.GetType().GetProperty(toggle.Key)!.GetValue(this)!)
            {
                List<DataColumn> columnsToRemove = new();
                foreach (DataColumn column in dataView.Table!.Columns)
                {
                    if (Regex.IsMatch(column.ColumnName, toggle.Value))
                    {
                        columnsToRemove.Add(column);
                    }
                }
                foreach (var pValueColumn in columnsToRemove)
                {
                    dataView.Table!.Columns.Remove(pValueColumn);
                }
            }
        }

        DataView = PreserveCurrentSorting(dataView, DataView);

        if (SecondaryTable != null)
        {
            DataView secondaryDataView = new(SecondaryTable.Copy());

            if (HasVariableLabels && !ShowVariableLabels)
            {
                RemoveLabelColumns(secondaryDataView);
            }

            SecondaryDataView = PreserveCurrentSorting(secondaryDataView, SecondaryDataView);
        }
    }

    private void SetDataTableViewRegression()
    {
        DataView dataView = new(DataTable.Copy());

        if (HasVariableLabels && !ShowVariableLabels)
        {
            RemoveLabelColumns(dataView);
        }
        
        Dictionary<string, string> toggles = new()
        {
            ["ShowPValues"] = "p\\svalue$",
            ["ShowFMI"] = "FMI$",
        };

        foreach (KeyValuePair<string, string> toggle in toggles)
        {
            if (!(bool)this.GetType().GetProperty(toggle.Key)!.GetValue(this)!)
            {
                List<DataColumn> columnsToRemove = new();
                foreach (DataColumn column in dataView.Table!.Columns)
                {
                    if (Regex.IsMatch(column.ColumnName, toggle.Value))
                    {
                        columnsToRemove.Add(column);
                    }
                }
                foreach (var pValueColumn in columnsToRemove)
                {
                    dataView.Table!.Columns.Remove(pValueColumn);
                }
            }
        }

        DataView = PreserveCurrentSorting(dataView, DataView);
    }

    private void ResetDataView()
    {
        switch (Analysis)
        {
            case AnalysisUnivar:
                SetDataTableViewUnivar();
                break;
            case AnalysisMeanDiff:
                SetDataTableViewMeanDiff();
                break;
            case AnalysisFreq:
                SetDataTableViewFreq();
                break;
            case AnalysisPercentiles:
                SetDataTableViewPercentiles();
                break;
            case AnalysisCorr:
                SetDataTableViewCorr();
                break;
            case AnalysisLinreg:
            case AnalysisLogistReg:
                SetDataTableViewRegression();
                break;
        }
    }

    [RelayCommand]
    private void SaveDataTableXlsx(ExportOptions exportOptions)
    {
        if (string.IsNullOrWhiteSpace(exportOptions.FileName)) return;
        
        var allFileNames = ExportService.AllFileNames(exportOptions, Analysis);
        foreach (var fileName in allFileNames.Where(File.Exists))
        {
            try
            {
                File.Delete(fileName);
            }
            catch (IOException)
            {
                WeakReferenceMessenger.Default.Send(new FileInUseMessage { FileName = fileName });
                return;
            }
        }

        switch (exportOptions.ExportType.Name)
        {
            case "excelWithStyles":
            case "excelWithoutStyles":
                var workbook = ExportService.CreateXlsxExport(Analysis, DataView, SecondaryDataView, ColumnTooltips, exportOptions.ExportType.Name != "excelWithoutStyles");
                workbook.SaveAs(exportOptions.FileName);
                workbook.Dispose();
                break;
            case "csvMultiple":
                var csvStrings = ExportService.CreateCsvExport(Analysis, DataView, SecondaryDataView);
                foreach (var (fileName, csvString) in allFileNames.Zip(csvStrings))
                {
                    File.WriteAllText(fileName, csvString);
                }
                break;
            case "csvMainTable":
                File.WriteAllText(exportOptions.FileName, ExportService.CreateCsvExport(Analysis, DataView, null, false)[0]);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public Dictionary<string, object> ViewSettings =>
        new()
        {
            { "ShowFMI", ShowFMI },
            { "ShowPValues", ShowPValues },
            { "ShowRank", ShowRank },
            { "ShowNcases", ShowNcases },
            { "ShowNweight", ShowNweight },
            { "ShowVariableLabels", ShowVariableLabels },
            { "UseTableAverage", UseTableAverage },
            { "TableSorting", DataView.Sort },
            { "SecondaryTableSorting", SecondaryDataView?.Sort ?? string.Empty }
        };

    public void ApplyDeserializedViewSettings(Dictionary<string, object> viewSettings)
    {
        object? value;
        if (HasFMI && viewSettings.TryGetValue("ShowFMI", out value) && value is JsonElement showFMI && showFMI.ValueKind is JsonValueKind.True != ShowFMI)
        {
            ShowFMI = !ShowFMI;
        }
        
        if (HasPValues && viewSettings.TryGetValue("ShowPValues", out value) && value is JsonElement showPValues && showPValues.ValueKind is JsonValueKind.True != ShowPValues)
        {
            ShowPValues = !ShowPValues;
        }
        
        if (HasRank && viewSettings.TryGetValue("ShowRank", out value) && value is JsonElement showRank && showRank.ValueKind is JsonValueKind.True != ShowRank)
        {
            ShowRank = !ShowRank;
        }
        
        if (HasNcases && viewSettings.TryGetValue("ShowNcases", out value) && value is JsonElement showNcases && showNcases.ValueKind is JsonValueKind.True != ShowNcases)
        {
            ShowNcases = !ShowNcases;
        }
        
        if (HasNweight && viewSettings.TryGetValue("ShowNweight", out value) && value is JsonElement showNweight && showNweight.ValueKind is JsonValueKind.True != ShowNweight)
        {
            ShowNweight = !ShowNweight;
        }
        
        if (HasVariableLabels && viewSettings.TryGetValue("ShowVariableLabels", out value) && value is JsonElement showVariableLabels && showVariableLabels.ValueKind is JsonValueKind.True != ShowVariableLabels)
        {
            ShowVariableLabels = !ShowVariableLabels;
        }
        
        if (HasTableAverage && viewSettings.TryGetValue("UseTableAverage", out value) && value is JsonElement useTableAverage && useTableAverage.ValueKind is JsonValueKind.True != UseTableAverage)
        {
            UseTableAverage = !UseTableAverage;
        }
        
        if (DataView.Table is null) return;
        
        if (viewSettings.TryGetValue("TableSorting", out var valueTableSorting) && valueTableSorting is JsonElement { ValueKind: JsonValueKind.String } tableSorting)
        {
            var sortings = tableSorting.ToString().Split(',');
            var newColumnNames = DataView.Table.Columns.Cast<DataColumn>().Select(column => column.ColumnName)
                .ToArray();

            if (sortings.All(sorting =>
                    newColumnNames.Contains(Regex.Match(sorting, @"\[(.*?)\]").Groups[1].Value)))
            {
                DataView.Sort = tableSorting.ToString();
            }
        }
        
        if (SecondaryDataView?.Table is null) return;

        if (viewSettings.TryGetValue("SecondaryTableSorting", out var valueSecondaryTableSorting) &&
            valueSecondaryTableSorting is JsonElement { ValueKind: JsonValueKind.String } secondaryTableSorting)
        {
            var sortings = secondaryTableSorting.ToString().Split(',');
            var newColumnNames = SecondaryDataView.Table.Columns.Cast<DataColumn>().Select(column => column.ColumnName)
                .ToArray();

            if (sortings.All(sorting =>
                    newColumnNames.Contains(Regex.Match(sorting, @"\[(.*?)\]").Groups[1].Value)))
            {
                SecondaryDataView.Sort = secondaryTableSorting.ToString();
            }
        }
    }
    
    public class FileInUseMessage
    {
        public required string FileName { get; init; }
    }
}

public struct ExportType
{
    public string Name { init; get; }
    
    public string Filter { init; get; }
    
    public string DisplayName => Filter[..Filter.IndexOf('|')];
}

public struct ExportOptions
{
    public string FileName { init; get; }
    
    public ExportType ExportType { init; get; }
}
