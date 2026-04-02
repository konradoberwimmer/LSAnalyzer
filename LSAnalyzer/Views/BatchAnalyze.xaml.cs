using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.ViewModels;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaktionslogik für BatchAnalyze.xaml
    /// </summary>
    public partial class BatchAnalyze : Window, ICloseable
    {
        public BatchAnalyze(ViewModels.BatchAnalyze batchAnalyzeViewModel)
        {
            InitializeComponent();

            DataContext = batchAnalyzeViewModel;

            WeakReferenceMessenger.Default.Register<RecentFileInvalidMessage>(this, (_, m) => 
                MessageBox.Show($"File '{ m.FileName }' is no longer available.", "File unavailable", MessageBoxButton.OK, MessageBoxImage.Warning));
            
            WeakReferenceMessenger.Default.Register<ViewModels.BatchAnalyze.BatchAnalyzeFailureMessage>(this, (r, m) =>
            {
                MessageBox.Show(m.Message, "Error running analysis requests", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        private void ButtonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "JSON File (*.json)|*.json";
            openFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var result = openFileDialog.ShowDialog(this);

            if (result == true)
            {
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(openFileDialog.FileName);
                Properties.Settings.Default.Save();
                var batchAnalyzeViewModel = DataContext as ViewModels.BatchAnalyze;
                batchAnalyzeViewModel!.FileName = openFileDialog.FileName;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var batchAnalyzeViewModel = DataContext as ViewModels.BatchAnalyze;

            if (batchAnalyzeViewModel?.IsBusy == true)
            {
                e.Cancel = true;
                return;
            }
            
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
