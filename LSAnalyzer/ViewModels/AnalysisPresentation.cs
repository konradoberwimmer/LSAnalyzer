using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
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

                switch (Analysis)
                {
                    case AnalysisUnivar analysisUnivar:
                        DataView = DataTableViewUnivar(DataTable);
                        break;
                    default:
                        break;
                }
                NotifyPropertyChanged(nameof(DataView));
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

                switch (Analysis)
                {
                    case AnalysisUnivar analysisUnivar:
                        DataView = DataTableViewUnivar(DataTable);
                        break;
                    case AnalysisMeanDiff analysisMeanDiff:
                        DataView = DataTableViewMeanDiff(DataTable);
                        break;
                    default:
                        break;
                }
                NotifyPropertyChanged(nameof(DataView));
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

                switch (Analysis)
                {
                    case AnalysisUnivar analysisUnivar:
                        DataView = DataTableViewUnivar(DataTable);
                        break;
                    case AnalysisMeanDiff analysisMeanDiff:
                        DataView = DataTableViewMeanDiff(DataTable);
                        break;
                    default:
                        break;
                }
                NotifyPropertyChanged(nameof(DataView));
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
                
                var groupNameColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupvar[0-9]*$")).ToArray();
                var groupValColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupval[0-9]*$")).ToArray();
                Dictionary<string, string> groupColumns = new Dictionary<string, string>();
                for (int i = 0; i < groupNameColumns.Length; i++)
                {
                    groupColumns.Add(dataFrame[groupNameColumns[i]].AsCharacter().First(), groupValColumns[i]);
                }

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
