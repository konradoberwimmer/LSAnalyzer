using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using LSAnalyzer.Models;

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

            WeakReferenceMessenger.Default.Register<ViewModels.ConfigDatasetTypes.SuccessImportDatasetTypeMessage>(this, (r, m) =>
            {
                MessageBox.Show($"Import of dataset type '{ m.Value }' successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            WeakReferenceMessenger.Default.Register<ViewModels.ConfigDatasetTypes.FailureImportDatasetTypeMessage>(this, (r, m) =>
            {
                MessageBox.Show("Import failed: " + m.Value, "Import failure", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    return;
                }
            }
            
            WeakReferenceMessenger.Default.UnregisterAll(this);
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
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
                configDatasetTypesViewModel.ExportDatasetTypeCommand.Execute(saveFileDialog.FileName);
            }
        }

        private void ButtonWeightVariables_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.ConfigDatasetTypes viewModel || viewModel.SelectedDatasetType is null) return;

            var storedPossibleWeightVariables = viewModel.SelectedDatasetType.PossibleWeightVariables.Select(weightVariable => new WeightVariable(weightVariable)).ToList();
            
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedDatasetType.Weight) && viewModel.SelectedDatasetType.PossibleWeightVariables.Count == 0)
            {
                foreach (var weightVariable in viewModel.SelectedDatasetType.Weight.Split(";"))
                {
                    viewModel.SelectedDatasetType.PossibleWeightVariables.Add(new WeightVariable { Name = weightVariable, Mandatory = true});
                }
            }
            WeightVariables weightVariablesView = new()
            {
                DataContext = viewModel
            };
            
            weightVariablesView.ShowDialog();

            if (string.Join(";", viewModel.SelectedDatasetType.PossibleWeightVariables.Select(weightVariable => weightVariable.Name)) != viewModel.SelectedDatasetType.Weight)
            {
                viewModel.SelectedDatasetType.PossibleWeightVariables = [..storedPossibleWeightVariables];
            }
        }
    }
}
