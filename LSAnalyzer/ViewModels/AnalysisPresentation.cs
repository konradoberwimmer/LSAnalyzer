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
                    break;
                default:
                    break;
            }
            NotifyPropertyChanged(nameof(DataTable));

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
            }

            columns.Add("Ncases", new DataColumn("N - cases unweighted", typeof(int)));
            columns.Add("Nweight", new DataColumn("N - weighted", typeof(double)));
            columns.Add("M", new DataColumn("mean", typeof(double)));
            columns.Add("M_SE", new DataColumn("mean - standard error", typeof(double)));
            columns.Add("SD", new DataColumn("standard deviation", typeof(double)));
            columns.Add("SD_SE", new DataColumn("standard deviation - standard error", typeof(double)));

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

            string[] sortBy = { "variable" };
            table.DefaultView.Sort = String.Join(", ", sortBy.Concat(analysisUnivar.GroupBy.ConvertAll(var => var.Name)).ToArray());

            return table;
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

            var worksheet = wb.AddWorksheet(DataTable);

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            wb.SaveAs(filename);
        }
    }
}
