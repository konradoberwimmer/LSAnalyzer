using CommunityToolkit.Mvvm.Messaging;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
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

            WeakReferenceMessenger.Default.Register<MissingRPackageMessage>(this, (r, m) =>
            {
                if (m.PackageName == "dataverse")
                {
                    var result = MessageBox.Show("Using dataverse requires package 'dataverse' (>= 0.3.0). Do you want to install it now?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && m.DataProvider != null)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            var succesfulInstall = m.DataProvider.InstallDependencies();
                            if (succesfulInstall)
                            {
                                MessageBox.Show("R package installation successful. Please restart application!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("R package installation did not succeed. Please handle this manually in your R installation and restart app afterwards!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                }
            });
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
