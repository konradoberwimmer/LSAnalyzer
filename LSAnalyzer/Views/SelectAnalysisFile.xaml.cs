using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
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

            WeakReferenceMessenger.Default.Register<FailureAnalysisConfigurationMessage>(this, (r, m) =>
            {
                busySpinner.Visibility = Visibility.Hidden;
                MessageBox.Show("Unable to create BIFIEdata object from file '" + m.Value.FileName + "' when applying dataset type '" + m.Value.DatasetType?.Name + "'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void ButtonSelectFile_Click (object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "SPSS Data Files (*.sav)|*.sav";
            openFileDialog.InitialDirectory = InitialDirectory ?? Properties.Settings.Default.lastDataFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = openFileDialog.ShowDialog(this);

            if (result == true)
            {
                Properties.Settings.Default.lastDataFileLocation = Path.GetFullPath(openFileDialog.FileName);
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
