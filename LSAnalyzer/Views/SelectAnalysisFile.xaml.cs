using CommunityToolkit.Mvvm.Messaging;
using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Helper;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaktionslogik für SelectAnalysisFile.xaml
    /// </summary>
    public partial class SelectAnalysisFile : Window, ICloseable
    {
        public string? InitialDirectory { get; set; }

        public SelectAnalysisFile(ViewModels.SelectAnalysisFile selectAnalysisFileViewModel)
        {
            InitializeComponent();

            DataContext = selectAnalysisFileViewModel;

            WeakReferenceMessenger.Default.Register<FailureAnalysisFileMessage>(this, (r, m) =>
            {
                MessageBox.Show("Unable to read column names from data file '" + m.Value + "'.\n\nTake note:\n- Supported file types are R data frames (.rds), SPSS (.sav), CSV (.csv) and Excel (.xlsx)\n- File ending must match file type\n- All formats have to provide column headers (in first row for Excel and CSV)\n- With Excel (.xlsx), package openxlsx has to be installed\n- With Excel (.xlsx), data has to be on the first worksheet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });

            WeakReferenceMessenger.Default.Register<FailureAnalysisConfigurationMessage>(this, (r, m) =>
            {
                MessageBox.Show("Unable to create BIFIEdata object from file '" + m.Value.FileName + "' when applying dataset type '" + m.Value.DatasetType?.Name + "'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });

            WeakReferenceMessenger.Default.Register<MultiplePossibleDatasetTypesMessage>(this, (r, m) =>
            {
                var listPossibleDatasetTypeNames = m.Value.ConvertAll(datasetType => datasetType.Name).ToArray();
                MessageBox.Show("Unable to determine dataset type exactly. May be one of:\n\n" + String.Join("\n", listPossibleDatasetTypeNames), "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            WeakReferenceMessenger.Default.Register<MissingRPackageMessage>(this, (r, m) =>
            {
                if (m.PackageName == "openxlsx")
                {
                    var result = MessageBox.Show("Using XLSX files requires package 'openxlsx'. Do you want to install it now?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            InstallOpenXlsx();
                        });
                    }
                }

                if (m.PackageName == "dataverse" && m.DataProvider != null)
                {
                    var result = MessageBox.Show("Using dataverse requires package 'dataverse' (>= 0.3.0). Do you want to install it now?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
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

        private void InstallOpenXlsx()
        {
            var viewModel = DataContext as ViewModels.SelectAnalysisFile;

            var succesfulInstall = viewModel!.InstallOpenXlsx();
            if (succesfulInstall)
            {
                MessageBox.Show("R package installation successful. Please restart application!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("R package installation did not succeed. Please handle this manually in your R installation and restart app afterwards!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonSelectFile_Click (object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Data File (*.csv;*.rds;*.sav;*.xlsx)|*.csv;*.rds;*.sav;*.xlsx";
            openFileDialog.InitialDirectory = InitialDirectory ?? Properties.Settings.Default.lastDataFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = openFileDialog.ShowDialog(this);

            if (result == true)
            {
                Properties.Settings.Default.lastDataFileLocation = Path.GetDirectoryName(openFileDialog.FileName);
                Properties.Settings.Default.Save();
                var selectAnalysisFileViewModel = DataContext as ViewModels.SelectAnalysisFile;
                selectAnalysisFileViewModel!.FileName = openFileDialog.FileName;
            }
        }

        private void Window_Closed (object sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
