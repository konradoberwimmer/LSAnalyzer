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
    /// Interaktionslogik für DataProviders.xaml
    /// </summary>
    public partial class DataProviders : Window
    {
        public DataProviders(ViewModels.DataProviders viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
        private void ComboBoxSelectedType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not ViewModels.DataProviders viewModel || sender is not ComboBox comboBox || comboBox.SelectedItem is not Type)
            {
                return;
            }

            viewModel.NewDataProviderCommand.Execute(comboBox.SelectedItem);
        }
    }
}
