using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Interaktionslogik für RequestAnalysisLinreg.xaml
    /// </summary>
    public partial class RequestAnalysisLinreg : Window, IRequestingAnalysis
    {
        public RequestAnalysisLinreg(RequestAnalysis requestAnalysisViewModel)
        {
            InitializeComponent();

            DataContext = requestAnalysisViewModel;
        }

        private void AvailableVariablesCollectionView_FilterSystemVariables (object sender, FilterEventArgs e)
        {
            e.Accepted = true;
            if (e.Item is Variable variable && variable.IsSystemVariable)
            {
                e.Accepted = false;
            }
        }

        private void CheckBoxIncludeSystemVariables_Checked (object sender, RoutedEventArgs e)
        {
            var availableVariablesCollectionView = Resources["AvailableVariablesCollectionView"] as CollectionViewSource;
            if (((CheckBox)sender).IsChecked == true)
            {
                availableVariablesCollectionView!.Filter -= AvailableVariablesCollectionView_FilterSystemVariables;
            } else
            {
                availableVariablesCollectionView!.Filter += AvailableVariablesCollectionView_FilterSystemVariables;
            }
        }

        private void ButtonMoveToAndFromDependentVariable_Click(object sender, RoutedEventArgs e)
        {
            buttonMoveToAndFromDependentVariable.CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDependent.Items.Count == 0 && listBoxVariablesDataset.SelectedItems.Count > 0 ? new List<Variable>() { listBoxVariablesDataset.SelectedItems.Cast<Variable>().First() } : new List<Variable>(),
                SelectedTo = listBoxVariablesDependent.SelectedItems.Cast<Variable>().ToList(),
            };
        }

        private void ButtonMoveToAndFromAnalysisVariables_Click (object sender, RoutedEventArgs e)
        {
            buttonMoveToAndFromAnalysisVariables.CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDataset.SelectedItems.Cast<Variable>().ToList(),
                SelectedTo = listBoxVariablesAnalyze.SelectedItems.Cast<Variable>().ToList(),
            };
        }

        private void ButtonMoveToAndFromGroupByVariables_Click(object sender, RoutedEventArgs e)
        {
            buttonMoveToAndFromGroupByVariables.CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDataset.SelectedItems.Cast<Variable>().ToList(),
                SelectedTo = listBoxVariablesGroupBy.SelectedItems.Cast<Variable>().ToList(),
            };
        }

        public Type GetAnalysisType()
        {
            return Type.GetType("LSAnalyzer.Models.AnalysisLinreg")!;
        }
    }
}
