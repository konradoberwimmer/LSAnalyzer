using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using ExcelNumberFormat;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels.ValueConverter;
using Microsoft.Extensions.DependencyInjection;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public partial class AnalysisPresentation : INotifyPropertyChanged
    {
        protected MainWindow? _mainWindowViewModel = null;

        private IResultService? _resultService;
        public IResultService ResultService
        {
            get => _resultService ??= (App.Current as App)?.ServiceProvider.GetService<IResultService>() ?? new ResultService();
            set => _resultService ??= value;
        }

        private Analysis _analysis;
        public Analysis Analysis
        {
            get => _analysis;
            set
            {
                _analysis = value;
                NotifyPropertyChanged(nameof(Analysis));
            }
        }

        private DataTable _dataTable;
        public DataTable DataTable
        {
            get => _dataTable;
            set
            {
                _dataTable = value;
                NotifyPropertyChanged(nameof(DataTable));
            }
        }

        private DataView _dataView;
        public DataView DataView
        {
            get => _dataView;
            set
            {
                _dataView = value;
                NotifyPropertyChanged(nameof(DataView));
            }
        }

        private bool _hasVariableLabels = false;
        public bool HasVariableLabels
        {
            get => _hasVariableLabels;
            set 
            {
                _hasVariableLabels = value;
                NotifyPropertyChanged(nameof(HasVariableLabels));
            }
        }

        private bool _showVariableLabels = true;
        public bool ShowVariableLabels
        {
            get => _showVariableLabels;
            set
            {
                _showVariableLabels = value;
                NotifyPropertyChanged(nameof(ShowVariableLabels));
                ResetDataView();
            }
        }

        private bool _hasTableAverage = false;
        public bool HasTableAverage
        {
            get => _hasTableAverage;
            set
            {
                _hasTableAverage = value;
                NotifyPropertyChanged(nameof(HasTableAverage));
            }
        }

        private bool _useTableAverage = true;
        public bool UseTableAverage
        {
            get => _useTableAverage;
            set
            {
                _useTableAverage = value;
                NotifyPropertyChanged(nameof(UseTableAverage));
                ResetDataView();
            }
        }

        private bool _hasPValues = true;
        public bool HasPValues
        {
            get => _hasPValues;
            set
            {
                _hasPValues = value;
                NotifyPropertyChanged(nameof(HasPValues));
            }
        }

        private bool _showPValues = false;
        public bool ShowPValues
        {
            get => _showPValues;
            set
            {
                _showPValues = value;
                NotifyPropertyChanged(nameof(ShowPValues));
                ResetDataView();
            }
        }

        private bool _hasFMI = true;
        public bool HasFMI
        {
            get => _hasFMI;
            set
            {
                _hasFMI = value;
                NotifyPropertyChanged(nameof(HasFMI));
            }
        }

        private bool _showFMI = false;
        public bool ShowFMI
        {
            get => _showFMI;
            set
            {
                _showFMI = value;
                NotifyPropertyChanged(nameof(ShowFMI));
                ResetDataView();
            }
        }

        private bool _hasRank = false;
        public bool HasRank
        {
            get => _hasRank;
            set
            {
                _hasRank = value;
                NotifyPropertyChanged(nameof(HasRank));
            }
        }

        private bool _showRank = false;
        public bool ShowRank
        {
            get => _showRank;
            set
            {
                _showRank = value;
                NotifyPropertyChanged(nameof(ShowRank));
                ResetDataView();
            }
        }

        private bool _hasNcasesToggle = false;
        public bool HasNcasesToggle
        {
            get => _hasNcasesToggle;
            set 
            {
                _hasNcasesToggle = value;
                NotifyPropertyChanged(nameof(HasNcasesToggle));
            }
        }

        private bool _showNcases = false;
        public bool ShowNcases
        {
            get => _showNcases;
            set
            {
                _showNcases = value;
                NotifyPropertyChanged(nameof(ShowNcases));
                ResetDataView();
            }
        }

        private bool _hasNweightToggle = false;
        public bool HasNweightToggle
        {
            get => _hasNweightToggle;
            set
            {
                _hasNweightToggle = value;
                NotifyPropertyChanged(nameof(HasNweightToggle));
            }
        }

        private bool _showNweight = false;
        public bool ShowNweight
        {
            get => _showNweight;
            set
            {
                _showNweight = value;
                NotifyPropertyChanged(nameof(ShowNweight));
                ResetDataView();
            }
        }

        private DataTable? _secondaryTable;
        public DataTable? TableSecondary
        {
            get => _secondaryTable;
            set
            {
                _secondaryTable = value;
                NotifyPropertyChanged(nameof(TableSecondary));
            }
        }

        private DataView? _secondaryDataView;
        public DataView? SecondaryDataView
        {
            get => _secondaryDataView;
            set
            {
                _secondaryDataView = value;
                NotifyPropertyChanged(nameof(SecondaryDataView));
            }
        }

        private bool _hasColumnTooltips = false;
        public bool HasColumnTooltips
        {
            get => _hasColumnTooltips;
            set
            {
                _hasColumnTooltips = value;
                NotifyPropertyChanged(nameof(HasColumnTooltips));
            }
        }

        private Dictionary<string, string> _columnTooltips = new();
        public Dictionary<string, string> ColumnTooltips
        {
            get => _columnTooltips;
        }

        private bool _busy = false;
        public bool IsBusy
        {
            get => _busy;
            set
            {
                _busy = value;
                NotifyPropertyChanged(nameof(IsBusy));

                _mainWindowViewModel?.NotifyIsBusy();
            }
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

            TableSecondary = new("Explained variance")
            {
                Columns = { { "var", typeof(string) }, { "eta2", typeof(double) }, { "eta", typeof(double) }, { "eta__se", typeof(double) } },
                Rows =
                {
                    { "x", 0.25, 0.50, 0.02 },
                    { "y", 0.16, 0.40, 0.15 },
                },
            };
            SecondaryDataView = new(TableSecondary);
        }

        public AnalysisPresentation(Analysis analysis, MainWindow? mainWindowViewModel = null)
        {
            Analysis = analysis;
            DataTable = new();
            DataView = new(DataTable);
            _mainWindowViewModel = mainWindowViewModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetAnalysisResult(List<GenericVector> result)
        {
            IsBusy = true;

            Analysis.Result = result;
            NotifyPropertyChanged(nameof(Analysis));

            ColumnTooltips.Clear();
            HasColumnTooltips = false;
            
            switch (Analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    DataTable = CreateDataTableFromResultUnivar(analysisUnivar);
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    DataTable = CreateDataTableFromResultMeanDiff(analysisMeanDiff);
                    TableSecondary = CreateTableEtaFromResultMeanDiff(analysisMeanDiff);
                    ShowPValues = true;
                    break;
                case AnalysisFreq analysisFreq:
                    DataTable = CreateDataTableFromResultFreq(analysisFreq);
                    if (analysisFreq.CalculateBivariate)
                    {
                        TableSecondary = CreateTableBivariateFromResultFreq(analysisFreq);
                    }
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    DataTable = CreateDataTableFromResultPercentiles(analysisPercentiles);
                    break;
                case AnalysisCorr analysisCorr:
                    DataTable = CreateDataTableFromResultCorr(analysisCorr);
                    TableSecondary = CreateTableCovarianceFromResultCorr(analysisCorr);
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
            if (analysisUnivar.Result == null || analysisUnivar.Result.Count == 0)
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
            if (analysisMeanDiff.Result == null || analysisMeanDiff.Result.Count == 0)
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
            if (analysisMeanDiff.Result == null || analysisMeanDiff.Result.Count == 0)
            {
                return new();
            }

            ResultService.Analysis = analysisMeanDiff;
            DataTable table = ResultService.CreateSecondaryTable()!;

            return table;
        }

        public DataTable CreateDataTableFromResultFreq(AnalysisFreq analysisFreq)
        {
            if (analysisFreq.Result == null || analysisFreq.Result.Count == 0)
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
            HasNcasesToggle = true;
            HasNweightToggle = true;

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
            if (analysisPercentiles.Result == null || analysisPercentiles.Result.Count == 0)
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
            if (analysisCorr.Result == null || analysisCorr.Result.Count == 0)
            {
                return new();
            }

            ResultService.Analysis = analysisCorr;
            DataTable table = ResultService.CreatePrimaryTable()!;

            string[] sortBy = { "variable A", "variable B" };
            table.DefaultView.Sort = String.Join(", ", analysisCorr.GroupBy.ConvertAll(var => var.Name).ToArray().Concat(sortBy));

            return table;
        }

        public DataTable CreateTableCovarianceFromResultCorr(AnalysisCorr analysisCorr)
        {
            if (analysisCorr.Result == null || analysisCorr.Result.Count == 0)
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
            if (analysisRegression.Result == null || analysisRegression.Result.Count == 0)
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
            foreach (DataRow row in table.Rows)
            {
                if ((string)row["variable"] == (string)dataFrameRow["var"])
                {
                    bool match = true;

                    foreach (var groupVar in Analysis.GroupBy)
                    {
                        if ((groupColumns.ContainsKey(groupVar.Name) && (row[groupVar.Name] == DBNull.Value || (double)row[groupVar.Name] != (double)dataFrameRow[groupColumns[groupVar.Name]])) ||
                            (!groupColumns.ContainsKey(groupVar.Name) && row[groupVar.Name] != DBNull.Value))
                        {
                            match = false;
                        }
                    }

                    if (match)
                    {
                        return row;
                    }
                }
            }

            return null;
        }

        private DataRow? GetExistingDataRowByCoefficient(DataTable table, DataFrameRow dataFrameRow)
        {
            foreach (DataRow row in table.Rows)
            {
                if ((string)row["variable"] == (string)dataFrameRow["var"] && (string)row["coefficient"] == (string)dataFrameRow["parameter"])
                {
                    return row;
                }
            }

            return null;
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
                dataView.Table!.Columns.Remove("variable (label)");
            }
            if (!ShowRank) dataView.Table!.Columns.Remove("rank of mean (per variable)");
            if (!ShowPValues) dataView.Table!.Columns.Remove("mean - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("mean - FMI");
            if (!ShowPValues) dataView.Table!.Columns.Remove("standard deviation - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("standard deviation - FMI");

            DataView = dataView;
        }

        private void SetDataTableViewMeanDiff()
        {
            DataView dataView = new(DataTable.Copy());

            if (HasVariableLabels && !ShowVariableLabels)
            {
                dataView.Table!.Columns.Remove("variable (label)");
            }
            if (!ShowPValues) dataView.Table!.Columns.Remove("Cohens d - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("Cohens d - FMI");

            DataView = dataView;
            
            if (TableSecondary != null)
            {
                DataView secondaryDataView = new(TableSecondary.Copy());

                if (HasVariableLabels && !ShowVariableLabels)
                {
                    secondaryDataView.Table!.Columns.Remove("variable (label)");
                }
                if (!ShowFMI) secondaryDataView.Table!.Columns.Remove("eta - FMI");

                SecondaryDataView = secondaryDataView;
            }
        }

        private void SetDataTableViewFreq()
        {
            DataView dataView = new(DataTable.Copy());

            Dictionary<string, string> toggles = new()
            {
                ["ShowVariableLabels"] = "variable\\s\\(label\\)",
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
            
            DataView = dataView;

            if (TableSecondary != null)
            {
                DataView secondaryDataView = new(TableSecondary.Copy());

                if (HasVariableLabels && !ShowVariableLabels)
                {
                    secondaryDataView.Table!.Columns.Remove("X (label)");
                    secondaryDataView.Table!.Columns.Remove("Y (label)");
                }
                if (!ShowFMI) secondaryDataView.Table!.Columns.Remove("estimate - FMI");

                SecondaryDataView = secondaryDataView;
            }
        }

        private void SetDataTableViewPercentiles()
        {
            DataView dataView = new(DataTable.Copy());

            if (HasVariableLabels && !ShowVariableLabels)
            {
                dataView.Table!.Columns.Remove("variable (label)");
            }

            DataView = dataView;
        }

        private void SetDataTableViewCorr()
        {
            DataView dataView = new(DataTable.Copy());

            Dictionary<string, string> toggles = new()
            {
                ["ShowVariableLabels"] = "variable\\s(A|B)\\s\\(label\\)",
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

            DataView = dataView;

            if (TableSecondary != null)
            {
                DataView secondaryDataView = new(TableSecondary.Copy());

                if (HasVariableLabels && !ShowVariableLabels)
                {
                    secondaryDataView.Table!.Columns.Remove("variable A (label)");
                    secondaryDataView.Table!.Columns.Remove("variable B (label)");
                }

                SecondaryDataView = secondaryDataView;
            }
        }

        private void SetDataTableViewRegression()
        {
            DataView dataView = new(DataTable.Copy());

            Dictionary<string, string> toggles = new()
            {
                ["ShowVariableLabels"] = "variable\\s\\(label\\)",
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

            DataView = dataView;
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
                default:
                    break;
            }
        }

        private RelayCommand<string?> _saveDataTableXlsxCommand;
        public ICommand SaveDataTableXlsxCommand
        {
            get
            {
                if (_saveDataTableXlsxCommand == null)
                    _saveDataTableXlsxCommand = new RelayCommand<string?>(this.SaveDataTableXlsx);
                return _saveDataTableXlsxCommand;
            }
        }

        private void SaveDataTableXlsx(string? filename)
        {
            if (filename == null || DataTable == null)
            {
                return;
            }

            using XLWorkbook wb = new();
            wb.ColumnWidth = 22.14;

            wb.AddWorksheetDataTable(DataView.Table!);
            
            if (Analysis is AnalysisFreq analysisFreq)
            {
                var worksheetPrimary = wb.Worksheet(DataView.Table!.TableName);

                for (int columnIndex = 1; columnIndex <= DataView.Table!.Columns.Count; columnIndex++)
                {
                    if (RegexCategoryPercentageHeader().IsMatch(DataView.Table.Columns[columnIndex - 1].ColumnName))
                    {
                        worksheetPrimary.Columns(columnIndex, columnIndex).Style.NumberFormat.Format = "0.0%";
                    }
                }

                worksheetPrimary.FirstRow().InsertRowsAbove(1);
                for (int columnIndex = 1; columnIndex <= DataView.Table!.Columns.Count; columnIndex++)
                {
                    var columnName = DataView.Table.Columns[columnIndex - 1].ColumnName;
                    if (RegexCategoryHeader().IsMatch(columnName))
                    {
                        var categoryHeader = ColumnTooltips.ContainsKey(columnName) ? RegexCategoryHeaderStart().Replace(ColumnTooltips[columnName], String.Empty) : columnName;
                        categoryHeader = RegexCategoryHeaderEnd().Replace(categoryHeader, String.Empty);
                        worksheetPrimary.Cell(1, columnIndex).Value = categoryHeader;

                        var coefficientHeader = RegexCategoryHeaderStart().Replace(columnName, String.Empty);
                        if (RegexJustCategoryValue().IsMatch(coefficientHeader))
                        {
                            coefficientHeader += " - %";
                        }
                        worksheetPrimary.Cell(2, columnIndex).Value = coefficientHeader;
                    }
                }

                worksheetPrimary.Range(1, 1, 1, DataView.Table!.Columns.Count).Style.Alignment.WrapText = true;
                worksheetPrimary.Range(1, 1, 1, DataView.Table!.Columns.Count).Style.Fill.BackgroundColor = XLColor.LightBlue;

                Dictionary<string, List<int>> superHeaderPositions = new();
                for (int columnIndex = 1; columnIndex <= DataView.Table!.Columns.Count; columnIndex++)
                {
                    var superHeaderValue = worksheetPrimary.Cell(1, columnIndex).Value;
                    if (superHeaderValue.IsText && (string)superHeaderValue != String.Empty)
                    {
                        if (!superHeaderPositions.ContainsKey((string)superHeaderValue))
                        {
                            superHeaderPositions.Add((string)superHeaderValue, new());
                        }
                        superHeaderPositions[(string)superHeaderValue].Add(columnIndex);
                    }
                }
                foreach (var superHeaderPosition in superHeaderPositions)
                {
                    worksheetPrimary.Range(1, superHeaderPosition.Value.Min(), 1, superHeaderPosition.Value.Max()).Merge();
                    worksheetPrimary.Cell(1, superHeaderPosition.Value.Min()).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }
            }

            if (SecondaryDataView != null)
            {
                wb.AddWorksheetDataTable(SecondaryDataView.Table!);
            }

            var metaInformation = Analysis?.MetaInformation;
            int rowCount = 1;
            var wsMeta = wb.AddWorksheet("Meta");

            if (metaInformation != null)
            {
                foreach (var key in metaInformation.Keys)
                {
                    if (metaInformation[key] != null)
                    {
                        wsMeta.Cell(rowCount, 1).Value = key;
                        switch (metaInformation[key])
                        {
                            case string aString:
                                wsMeta.Cell(rowCount, 2).Value = aString;
                                break;
                            case int aInt:
                                wsMeta.Cell(rowCount, 2).Value = aInt;
                                break;
                            case double aDouble:
                                wsMeta.Cell(rowCount, 2).Value = aDouble;
                                break;
                            default:
                                wsMeta.Cell(rowCount, 2).Value = metaInformation[key]!.ToString();
                                break;
                        }
                        rowCount++;
                    }
                }
                wsMeta.Column("A").Width = 25;
            }

            var variableLabels = Analysis?.VariableLabels;

            if (variableLabels != null && variableLabels.Count > 0)
            {
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

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            wb.SaveAs(filename);
        }

        [GeneratedRegex("^Cat\\s[0-9\\.]+(\\s-\\sstandard\\serror)?$")]
        public static partial Regex RegexCategoryPercentageHeader();

        [GeneratedRegex("^Cat\\s[0-9\\.]+(\\s-\\s.*)?$")]
        public static partial Regex RegexCategoryHeader();

        [GeneratedRegex("^Cat\\s")]
        private static partial Regex RegexCategoryHeaderStart();

        [GeneratedRegex("\\s-\\s(standard\\serror|weighted|cases|FMI)$")]
        private static partial Regex RegexCategoryHeaderEnd();

        [GeneratedRegex("^[0-9\\.]+$")]
        private static partial Regex RegexJustCategoryValue();
    }
}
