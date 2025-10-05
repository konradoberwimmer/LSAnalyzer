using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace LSAnalyzer.Views.CustomControls
{
    /// <summary>
    /// Interaktionslogik für AnalysisPresentation.xaml
    /// </summary>
    public partial class AnalysisPresentation : UserControl
    {
        public AnalysisPresentation()
        {
            InitializeComponent();
        }


        private void ButtonDownloadXlsx_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ViewModels.AnalysisPresentation analysisPresentationViewModel)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this.Parent as Window);

            if (wantsSave == true)
            {
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
                Properties.Settings.Default.Save();
                analysisPresentationViewModel.SaveDataTableXlsxCommand.Execute(saveFileDialog.FileName);
            }
        }
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
            {
                return;
            }

            var headerStyle = new Style(typeof(DataGridColumnHeader), (Style)Resources["WrappedColumnHeader"]);
            if (dataGrid.DataContext is ViewModels.AnalysisPresentation analysisPresentationViewModel && e.Column.Header is string columnName && analysisPresentationViewModel.ColumnTooltips.ContainsKey(columnName))
            {
                headerStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, analysisPresentationViewModel.ColumnTooltips[columnName]));
            }
            else
            {
                headerStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, e.Column.Header));
            }
            e.Column.HeaderStyle = headerStyle;

            if (e.PropertyName.Contains('.') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = (e.Column as DataGridBoundColumn)!;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
                dataGridBoundColumn.SortMemberPath = e.PropertyName;
            }

            if (e.PropertyType == typeof(double) && e.Column is DataGridTextColumn dataGridTextColumn)
            {
                if (dataGrid.DataContext is ViewModels.AnalysisPresentation analysisPresentation && analysisPresentation.Analysis is AnalysisFreq && e.Column.Header is string headerText && ViewModels.AnalysisPresentation.RegexCategoryPercentageHeader().IsMatch(headerText))
                {
                    dataGridTextColumn.Binding.StringFormat = "{0:0.0%}";
                    e.Column.CellStyle = (Style)Resources["ColumnRight"];
                }
                else
                {
                    var columnIndex = dataGrid.Columns.Count; // because this is done before adding the new column
                    var values = dataGrid.Items.Cast<DataRowView>().Select(row => row.Row.ItemArray[columnIndex]).Where(val => val != DBNull.Value).Cast<double>().ToArray();
                    var maxRelevantDigits = StringFormats.GetMaxRelevantDigits(values, 3);
                    dataGridTextColumn.Binding.StringFormat = "{0:0" + (maxRelevantDigits > 0 ? ("." + new string('0', maxRelevantDigits)) : "") + "}";

                    if (maxRelevantDigits > 0)
                    {
                        e.Column.CellStyle = (Style)Resources["ColumnRight"];
                    }
                }
            }
        }

        private void DataGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
            {
                return;
            }

            var dataGridScrollViewer = WPFHelper.FindVisualChild<ScrollViewer>(dataGrid);
            if (dataGridScrollViewer == null || dataGridScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            {
                return;
            }

            DependencyObject? parent = dataGrid.Parent;
            do
            {
                if (parent is ScrollViewer scrollViewer)
                { 
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                    break;
                }

                parent = VisualTreeHelper.GetParent(parent);
            } while (parent != null);
        }
    }
}
