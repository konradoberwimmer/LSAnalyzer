using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

            DataContext = new ViewModels.MainWindow(_serviceProvider.GetRequiredService<Rservice>(), _serviceProvider.GetRequiredService<ILogger<MainWindow>>());

            Closed += WindowClosed;

            WeakReferenceMessenger.Default.Register<FailureWithAnalysisCalculationMessage>(this, (r, m) =>
            {
                MessageBox.Show("Something went wrong with analysis '" + m.Value.AnalysisName + "'!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
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

        private void MenuItemSystemSettings_Click (object sender, RoutedEventArgs e)
        {
            SystemSettings systemSettingsView = _serviceProvider.GetRequiredService<SystemSettings>();
            systemSettingsView.ShowDialog();
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

        private void MenuItemAnalysisSubsetting_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            ViewModels.Subsetting subsettingViewModel = _serviceProvider.GetRequiredService<ViewModels.Subsetting>();
            subsettingViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.SubsettingExpression != null)
            {
                subsettingViewModel.SetCurrentSubsetting(mainWindowViewModel.SubsettingExpression);
            }

            Subsetting subsettingView = new(subsettingViewModel);
            subsettingView.ShowDialog();
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
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisUnivar))) 
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisUnivar)]);
            }

            RequestAnalysisUnivar requestAnalysisUnivarView = new(requestAnalysisViewModel);
            requestAnalysisUnivarView.ShowDialog();
        }

        private void MenuItemAnalysisMeanDiff_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisMeanDiff)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisMeanDiff)]);
            }

            RequestAnalysisMeanDiff requestAnalysisMeanDiffView = new(requestAnalysisViewModel);
            requestAnalysisMeanDiffView.ShowDialog();
        }

        private void MenuItemAnalysisFreq_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisFreq)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisFreq)]);
            }

            RequestAnalysisFreq requestAnalysisFreqView = new(requestAnalysisViewModel);
            requestAnalysisFreqView.ShowDialog();
        }

        private void MenuItemAnalysisPercentiles_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisPercentiles)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisPercentiles)]);
            }

            RequestAnalysisPercentiles requestAnalysisPercentilesView = new(requestAnalysisViewModel);
            requestAnalysisPercentilesView.ShowDialog();
        }

        private void MenuItemAnalysisCorrelations_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisCorr)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisCorr)]);
            }

            RequestAnalysisCorr requestAnalysisCorrView = new(requestAnalysisViewModel);
            requestAnalysisCorrView.ShowDialog();
        }

        private void MenuItemAnalysisLinreg_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisLinreg)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisLinreg)]);
            }

            RequestAnalysisLinreg requestAnalysisLinregView = new(requestAnalysisViewModel);
            requestAnalysisLinregView.ShowDialog();
        }

        private void MenuItemAnalysisLogistReg_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            if (mainWindowViewModel!.AnalysisConfiguration == null)
            {
                return;
            }

            RequestAnalysis requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration;
            if (mainWindowViewModel.RecentAnalyses.ContainsKey(typeof(AnalysisLogistReg)))
            {
                requestAnalysisViewModel.InitializeWithAnalysis(mainWindowViewModel.RecentAnalyses[typeof(AnalysisLogistReg)]);
            }

            RequestAnalysisLogistReg requestAnalysisLogistRegView = new(requestAnalysisViewModel);
            requestAnalysisLogistRegView.ShowDialog();
        }

        private void MenuItemBatchAnalyze_Click(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            ViewModels.BatchAnalyze batchAnalyzeViewModel = _serviceProvider.GetRequiredService<ViewModels.BatchAnalyze>();
            batchAnalyzeViewModel.HasCurrentFile = mainWindowViewModel!.AnalysisConfiguration != null;
            if (batchAnalyzeViewModel.HasCurrentFile)
            {
                batchAnalyzeViewModel.UseCurrentFile = true;
            }
            batchAnalyzeViewModel.CurrentModeKeep = mainWindowViewModel.AnalysisConfiguration?.ModeKeep != false;

            Views.BatchAnalyze batchAnalyzeView = new(batchAnalyzeViewModel);
            batchAnalyzeView.ShowDialog();
        }

        private void ItemsControlAnalysesOutline_Click(object sender, RoutedEventArgs e) 
        { 
            if (e.Source is Button button && button.DataContext is AnalysisPresentation analysisPresentation)
            {
                ((ContentPresenter)itemsControlAnalysesFull.ItemContainerGenerator.ContainerFromItem(analysisPresentation)).BringIntoView();
            }
        }

        private void ButtonDownloadAnalysesDefinitions_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;
            if (mainWindowViewModel == null || mainWindowViewModel.Analyses.Count == 0)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "JSON File (*.json)|*.json";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this);

            if (wantsSave == true)
            {
                mainWindowViewModel.SaveAnalysesDefintionsCommand.Execute(saveFileDialog.FileName);
            }
        }
    }
}
