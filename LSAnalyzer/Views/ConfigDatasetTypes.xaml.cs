using LSAnalyzer.ViewModels;
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
    }
}
