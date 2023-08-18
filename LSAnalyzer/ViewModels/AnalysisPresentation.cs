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

        public void SetAnalysisResult(GenericVector? result)
        {
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
        }

        public DataTable CreateDataTableFromResultUnivar(AnalysisUnivar analysisUnivar)
        {
            if (analysisUnivar.Result == null)
            {
                return new();
            }

            var dataFrame = analysisUnivar.Result["stat"].AsDataFrame();

            DataTable table = new(analysisUnivar.AnalysisName);
            Dictionary<string, DataColumn> columns = new();

            columns.Add("var", new DataColumn("variable", typeof(string)));

            for (int cntGroupyBy = 0; cntGroupyBy < analysisUnivar.GroupBy.Count; cntGroupyBy++)
            {
                string groupByVar = analysisUnivar.GroupBy.Count == 1 ? "groupval" : "groupval" + (cntGroupyBy + 1);
                columns.Add(groupByVar, new DataColumn(analysisUnivar.GroupBy[cntGroupyBy].Name, typeof(double)));
            }

            columns.Add("Ncases", new DataColumn("N - cases/unweighted", typeof(string)));
            columns.Add("Nweight", new DataColumn("N - weighted", typeof(string)));
            columns.Add("M", new DataColumn("mean", typeof(string)));
            columns.Add("M_SE", new DataColumn("mean - standard error", typeof(string)));
            columns.Add("SD", new DataColumn("standard deviation", typeof(string)));
            columns.Add("SD_SE", new DataColumn("standard deviation - standard error", typeof(string)));

            foreach (var column in columns.Values)
            {
                table.Columns.Add(column);
            }

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                DataRow tableRow = table.NewRow();

                List<object> cellValues = new();
                foreach (var column in columns.Keys)
                {
                    cellValues.Add(dataFrameRow[column]);
                }

                tableRow.ItemArray = cellValues.ToArray();
                table.Rows.Add(tableRow);
            }

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
