using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
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
    public class AnalysisPresentation : INotifyPropertyChanged
    {
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

        private DataTable? _tableEta;
        public DataTable? TableEta
        {
            get => _tableEta;
            set
            {
                _tableEta = value;
                NotifyPropertyChanged(nameof(TableEta));
            }
        }

        private bool _busy = false;
        public bool IsBusy
        {
            get => _busy;
            set
            {
                _busy = value;
                NotifyPropertyChanged(nameof(IsBusy));
            }
        }

        [ExcludeFromCodeCoverage]
        public AnalysisPresentation()
        {
            // design-time only parameter-less constructor
        }

        public AnalysisPresentation(Analysis analysis)
        {
            Analysis = analysis;
            DataTable = new();
            DataView = new(DataTable);
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
            
            switch (Analysis)
            {
                case AnalysisUnivar analysisUnivar:
                    DataTable = CreateDataTableFromResultUnivar(analysisUnivar);
                    DataView = DataTableViewUnivar(DataTable);
                    break;
                case AnalysisMeanDiff analysisMeanDiff:
                    DataTable = CreateDataTableFromResultMeanDiff(analysisMeanDiff);
                    TableEta = CreateTableEtaFromResultMeanDiff(analysisMeanDiff);
                    ShowPValues = true;
                    DataView = DataTableViewMeanDiff(DataTable);
                    break;
                case AnalysisFreq analysisFreq:
                    DataTable = CreateDataTableFromResultFreq(analysisFreq);
                    DataView = DataTableViewFreq(DataTable);
                    break;
                case AnalysisPercentiles analysisPercentiles:
                    DataTable = CreateDataTableFromResultPercentiles(analysisPercentiles);
                    DataView = DataTableViewPercentiles(DataTable);
                    break;
                default:
                    break;
            }
            NotifyPropertyChanged(nameof(DataView));

            IsBusy = false;
        }

        public DataTable CreateDataTableFromResultUnivar(AnalysisUnivar analysisUnivar)
        {
            if (analysisUnivar.Result == null || analysisUnivar.Result.Count == 0)
            {
                return new();
            }

            DataTable table = new(analysisUnivar.AnalysisName);
            Dictionary<string, DataColumn> columns = new();

            columns.Add("var", new DataColumn("variable", typeof(string)));

            for (int cntGroupyBy = 0; cntGroupyBy < analysisUnivar.GroupBy.Count; cntGroupyBy++)
            {
                columns.Add("groupval" + (cntGroupyBy + 1), new DataColumn(analysisUnivar.GroupBy[cntGroupyBy].Name, typeof(double)));
                if (analysisUnivar.ValueLabels.ContainsKey(analysisUnivar.GroupBy[cntGroupyBy].Name))
                {
                    columns.Add("$label_" + analysisUnivar.GroupBy[cntGroupyBy].Name, new DataColumn(analysisUnivar.GroupBy[cntGroupyBy].Name + " (label)", typeof(string)));
                }
            }

            columns.Add("Ncases", new DataColumn("N - cases unweighted", typeof(int)));
            columns.Add("Nweight", new DataColumn("N - weighted", typeof(double)));
            columns.Add("M", new DataColumn("mean", typeof(double)));
            columns.Add("M_SE", new DataColumn("mean - standard error", typeof(double)));
            columns.Add("M_p", new DataColumn("mean - p value", typeof(double)));
            columns.Add("M_fmi", new DataColumn("mean - FMI", typeof(double)));
            columns.Add("SD", new DataColumn("standard deviation", typeof(double)));
            columns.Add("SD_SE", new DataColumn("standard deviation - standard error", typeof(double)));
            columns.Add("SD_p", new DataColumn("standard deviation - p value", typeof(double)));
            columns.Add("SD_fmi", new DataColumn("standard deviation - FMI", typeof(double)));

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
                    DataRow tableRow = table.NewRow();

                    List<object?> cellValues = new();
                    foreach (var column in columns.Keys)
                    {
                        if (Regex.IsMatch(column, "^groupval[0-9]*$") && groupColumns.ContainsKey(columns[column].ColumnName))
                        {
                            cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
                        } else if (Regex.IsMatch(column, "^\\$label_"))
                        {
                            if ((double?)cellValues.Last() == null)
                            {
                                cellValues.Add(null);
                                continue;
                            }

                            var groupByVariable = column.Substring(column.IndexOf("_") + 1);
                            var valueLabels = analysisUnivar.ValueLabels[groupByVariable];
                            // TODO this is a rather ugly shortcut of getting the value that we need the label for!!!
                            var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf((double)cellValues.Last()!);

                            if (posValueLabel != -1)
                            {
                                var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                                cellValues.Add(valueLabel);
                            } else
                            {
                                cellValues.Add(null);
                            }
                        } else if (dataFrame.ColumnNames.Contains(column))
                        {
                            cellValues.Add(dataFrameRow[column]);
                        } else
                        {
                            cellValues.Add(null);
                        }
                    }

                    tableRow.ItemArray = cellValues.ToArray();
                    table.Rows.Add(tableRow);
                }
            }

            if (Analysis.Vars.Count == 1 && Analysis.GroupBy.Count == 1)
            {
                var relevantRows = table.AsEnumerable().Where(row => row.Field<double?>(Analysis.GroupBy.First().Name) != null);
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

            columns.Add("var", new DataColumn("variable", typeof(string)));

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
            columns.Add("M2", new DataColumn("mean - group B", typeof(double)));
            columns.Add("SD", new DataColumn("standard deviation (pooled)", typeof(double)));
            columns.Add("d", new DataColumn("Cohens d", typeof(double)));
            columns.Add("d_SE", new DataColumn("Cohens d - standard error", typeof(double)));
            columns.Add("d_p", new DataColumn("Cohens d - p value", typeof(double)));
            columns.Add("d_fmi", new DataColumn("Cohens d - FMI", typeof(double)));

            foreach (var column in columns.Values)
            {
                table.Columns.Add(column);
            }

            foreach (var result in Analysis.Result)
            {
                var dataFrame = result["stat.dstat"].AsDataFrame();

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

        public DataTable CreateTableEtaFromResultMeanDiff(AnalysisMeanDiff analysisMeanDiff)
        {
            if (analysisMeanDiff.Result == null || analysisMeanDiff.Result.Count == 0)
            {
                return new();
            }

            DataTable table = new(analysisMeanDiff.AnalysisName + " - eta");
            Dictionary<string, DataColumn> columns = new();

            columns.Add("var", new DataColumn("variable", typeof(string)));

            if (analysisMeanDiff.CalculateSeparately)
            {
                columns.Add("group", new DataColumn("groups by", typeof(string)));
            }

            columns.Add("eta2", new DataColumn("eta²", typeof(double)));
            columns.Add("eta", new DataColumn("eta", typeof(double)));
            columns.Add("eta_SE", new DataColumn("eta - standard error", typeof(double)));
            columns.Add("fmi", new DataColumn("eta - FMI", typeof(double)));

            foreach (var column in columns.Values)
            {
                table.Columns.Add(column);
            }

            foreach (var result in Analysis.Result)
            {
                var dataFrame = result["stat.eta"].AsDataFrame();

                foreach (var dataFrameRow in dataFrame.GetRows())
                {
                    DataRow tableRow = table.NewRow();

                    List<object?> cellValues = new();
                    foreach (var column in columns.Keys)
                    {
                        if (dataFrame.ColumnNames.Contains(column))
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

            columns.Add("var", new DataColumn("variable", typeof(string)));

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

            for (int cc = 0; cc < categories.Count; cc++)
            {
                var category = categories[cc];
                columns.Add("$cat_" + cc + "_perc", new DataColumn("Cat " + category, typeof(double)));
                columns.Add("$cat_" + cc + "_perc_SE", new DataColumn("Cat " + category + " - standard error", typeof(double)));
                columns.Add("$cat_" + cc + "_Ncases", new DataColumn("Cat " + category + " - cases", typeof(int)));
                columns.Add("$cat_" + cc + "_Nweight", new DataColumn("Cat " + category + " - weighted", typeof(double)));
                columns.Add("$cat_" + cc + "_perc_fmi", new DataColumn("Cat " + category + " - FMI", typeof(double)));
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
                        var existingTableRow = GetExistingDataRowFreq(table, dataFrameRow, groupColumns);

                        if (existingTableRow != null)
                        {
                            var category = (double)dataFrameRow["varval"];

                            existingTableRow["Cat " + category] = (double)dataFrameRow["perc"];
                            existingTableRow["Cat " + category + " - standard error"] = (double)dataFrameRow["perc_SE"];
                            existingTableRow["Cat " + category + " - cases"] = (double)dataFrameRow["Ncases"];
                            existingTableRow["Cat " + category + " - weighted"] = (double)dataFrameRow["Nweight"];
                            existingTableRow["Cat " + category + " - FMI"] = (double)dataFrameRow["perc_fmi"];

                            repeat = false;
                        }
                        else
                        {
                            DataRow tableRow = table.NewRow();

                            List<object?> cellValues = new();
                            foreach (var column in columns.Keys)
                            {
                                if (Regex.IsMatch(column, "^groupval[0-9]*$") && groupColumns.ContainsKey(columns[column].ColumnName))
                                {
                                    cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
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

            HasPValues = false;
            HasNcasesToggle = true;
            HasNweightToggle = true;

            string[] sortBy = { "variable" };
            table.DefaultView.Sort = String.Join(", ", sortBy.Concat(analysisFreq.GroupBy.ConvertAll(var => var.Name)).ToArray());

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

            columns.Add("var", new DataColumn("variable", typeof(string)));

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
                columns.Add("$cat_" + cc, new DataColumn("Perc " + Regex.Replace(category.ToString(CultureInfo.InvariantCulture), "\\.", "_."), typeof(double)));
                if (analysisPercentiles.CalculateSE)
                {
                    columns.Add("$cat_" + cc + "_SE", new DataColumn("Perc " + Regex.Replace(category.ToString(CultureInfo.InvariantCulture), "\\.", "_.") + " - standard error", typeof(double)));
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
                        var existingTableRow = GetExistingDataRowFreq(table, dataFrameRow, groupColumns);

                        if (existingTableRow != null)
                        {
                            var category = (double)dataFrameRow["yval"];

                            existingTableRow["Perc " + Regex.Replace(category.ToString(CultureInfo.InvariantCulture), "\\.", "_.")] = (double)dataFrameRow["quant"];
                            if (analysisPercentiles.CalculateSE)
                            {
                                existingTableRow["Perc " + Regex.Replace(category.ToString(CultureInfo.InvariantCulture), "\\.", "_.") + " - standard error"] = (double)dataFrameRow["quant_SE"];
                            }

                            repeat = false;
                        }
                        else
                        {
                            DataRow tableRow = table.NewRow();

                            List<object?> cellValues = new();
                            foreach (var column in columns.Keys)
                            {
                                if (Regex.IsMatch(column, "^groupval[0-9]*$") && groupColumns.ContainsKey(columns[column].ColumnName))
                                {
                                    cellValues.Add(dataFrameRow[groupColumns[columns[column].ColumnName]]);
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

        private DataRow? GetExistingDataRowFreq(DataTable table, DataFrameRow dataFrameRow, Dictionary<string, string> groupColumns)
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

        private DataView DataTableViewUnivar(DataTable table)
        {
            DataView dataView = new(table.Copy());

            if (HasTableAverage && !UseTableAverage)
            {
                dataView.Table!.Rows.RemoveAt(dataView.Table!.Rows.Count - 1);
            }
            if (!ShowPValues) dataView.Table!.Columns.Remove("mean - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("mean - FMI");
            if (!ShowPValues) dataView.Table!.Columns.Remove("standard deviation - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("standard deviation - FMI");

            return dataView;
        }

        private DataView DataTableViewMeanDiff(DataTable table)
        {
            DataView dataView = new(table.Copy());

            if (!ShowPValues) dataView.Table!.Columns.Remove("Cohens d - p value");
            if (!ShowFMI) dataView.Table!.Columns.Remove("Cohens d - FMI");

            return dataView;
        }

        private DataView DataTableViewFreq(DataTable table)
        {
            DataView dataView = new(table.Copy());

            Dictionary<string, string> toggles = new()
            {
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
            
            return dataView;
        }

        private DataView DataTableViewPercentiles(DataTable table)
        {
            DataView dataView = new(table.Copy());

            return dataView;
        }

        private void ResetDataView()
        {
            switch (Analysis)
            {
                case AnalysisUnivar:
                    DataView = DataTableViewUnivar(DataTable);
                    break;
                case AnalysisMeanDiff:
                    DataView = DataTableViewMeanDiff(DataTable);
                    break;
                case AnalysisFreq:
                    DataView = DataTableViewFreq(DataTable);
                    break;
                case AnalysisPercentiles:
                    DataView = DataTableViewPercentiles(DataTable);
                    break;
                default:
                    break;
            }
            NotifyPropertyChanged(nameof(DataView));
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

            var worksheet = wb.AddWorksheet(DataView.Table);

            if (TableEta != null)
            {
                wb.AddWorksheet(TableEta);
            }

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            wb.SaveAs(filename);
        }
    }
}
