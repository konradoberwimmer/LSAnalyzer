using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaktionslogik für ConfigDatasetTypes.xaml
    /// </summary>
    public partial class ConfigDatasetTypes : Window
    {
        public ConfigDatasetTypes(ViewModels.ConfigDatasetTypes configDatasetTypesViewModel)
        {
            InitializeComponent();

            DataContext = configDatasetTypesViewModel;

            WeakReferenceMessenger.Default.Register<FailureImportDatasetTypeMessage>(this, (r, m) =>
            {
                MessageBox.Show("Import failed: " + m.Value, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void WindowClosing(object? sender, CancelEventArgs e)
        {
            var viewModel = DataContext as LSAnalyzer.ViewModels.ConfigDatasetTypes;
            if (viewModel!.UnsavedDatasetTypeNames.Count > 0)
            {
                var dialogResult = MessageBox.Show("There are unsaved dataset types (" + String.Join(", ", viewModel!.UnsavedDatasetTypeNames.ToArray()) + "). Do you really want to close and lose pending changes?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dialogResult  == MessageBoxResult.No) 
                {
                    e.Cancel = true;
                }
            }
        }

        private void ButtonRemoveDatasetTypeClick(object? sender, RoutedEventArgs e)
        {
            var dialogResult = MessageBox.Show("Do you really want to remove this dataset type?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.Yes)
            {
                var viewModel = DataContext as LSAnalyzer.ViewModels.ConfigDatasetTypes;
                viewModel!.RemoveDatasetTypeCommand.Execute(null);
            }
        }

        private void ButtonImportDatasetType_Click(object? sender, RoutedEventArgs e)
        {
            var configDatasetTypesViewModel = DataContext as ViewModels.ConfigDatasetTypes;
            if (configDatasetTypesViewModel == null)
            {
                return;
            }

            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "JSON File (*.json)|*.json";
            openFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = openFileDialog.ShowDialog(this);

            if (result == true)
            {
                configDatasetTypesViewModel.ImportDatasetTypeCommand.Execute(openFileDialog.FileName);
            }
        }

        private void ButtonExportDatasetType_Click(object? sender, RoutedEventArgs e)
        {
            var configDatasetTypesViewModel = DataContext as ViewModels.ConfigDatasetTypes;
            if (configDatasetTypesViewModel?.SelectedDatasetType == null || !configDatasetTypesViewModel.SelectedDatasetType.Validate())
            {
                return;
            }

            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "JSON File (*.json)|*.json";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this);

            if (wantsSave == true)
            {
                configDatasetTypesViewModel.ExportDatasetTypeCommand.Execute(saveFileDialog.FileName);
            }
        }
    }
}
