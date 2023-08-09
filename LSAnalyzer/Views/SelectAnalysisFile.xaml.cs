using LSAnalyzer.Helper;
using Microsoft.Win32;
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
    /// Interaktionslogik für SelectAnalysisFile.xaml
    /// </summary>
    public partial class SelectAnalysisFile : Window, ICloseable
    {
        public SelectAnalysisFile(ViewModels.SelectAnalysisFile selectAnalysisFileViewModel)
        {
            InitializeComponent();

            DataContext = selectAnalysisFileViewModel;
        }

        private void ButtonSelectFile_Click (object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "SPSS Data Files (*.sav)|*.sav";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = openFileDialog.ShowDialog(this);

            if (result == true)
            {
                var selectAnalysisFileViewModel = DataContext as ViewModels.SelectAnalysisFile;
                selectAnalysisFileViewModel!.FileName = openFileDialog.FileName;
            }
        }
    }
}
