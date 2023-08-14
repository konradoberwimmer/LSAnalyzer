using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IServiceProvider _serviceProvider;
        
        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            InitializeComponent();

            Closed += WindowClosed;
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItemDatasetTypes_Click (object sender, RoutedEventArgs e)
        {
            ConfigDatasetTypes configDatasetTypesView = _serviceProvider.GetRequiredService<ConfigDatasetTypes>();
            configDatasetTypesView.ShowDialog();
        }

        private void MenuItemAnalysisSelectFile_Click (object sender, RoutedEventArgs e)
        {
            SelectAnalysisFile selectAnalysisFileView = _serviceProvider.GetRequiredService<SelectAnalysisFile>();

            var mainWindowViewModel = DataContext as ViewModels.MainWindow;
            if (mainWindowViewModel?.AnalysisConfiguration?.FileName != null)
            {
                selectAnalysisFileView.InitialDirectory = Path.GetDirectoryName(mainWindowViewModel.AnalysisConfiguration.FileName);
            }

            selectAnalysisFileView.ShowDialog();
        }

        private void MenuItemAnalysisUnivar_Click (object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;

            RequestAnalysisUnivar requestAnalysisUnivarView = new(requestAnalysisViewModel);
            requestAnalysisUnivarView.ShowDialog();
        }
    }
}
