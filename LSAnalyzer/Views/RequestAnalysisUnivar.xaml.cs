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
    /// Interaktionslogik für RequestAnalysisUnivar.xaml
    /// </summary>
    public partial class RequestAnalysisUnivar : Window
    {
        public RequestAnalysisUnivar(RequestAnalysis requestAnalysisViewModel)
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
    }
}
