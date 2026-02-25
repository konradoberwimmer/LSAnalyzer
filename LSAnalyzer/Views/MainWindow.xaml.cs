using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

            DataContext = new ViewModels.MainWindow(_serviceProvider.GetRequiredService<IRservice>(), _serviceProvider.GetRequiredService<IAnalysisQueue>());

            Closed += WindowClosed;

            WeakReferenceMessenger.Default.Register<AnalysisQueue.FailureWithAnalysisCalculationMessage>(this, (r, m) =>
            {
                if (DataContext is not ViewModels.MainWindow mainWindowViewModel || mainWindowViewModel.Analyses.All(presentation => presentation.Analysis != m.Value)) return;
                
                MessageBox.Show("Something went wrong with analysis '" + m.Value.AnalysisName + "'!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            WeakReferenceMessenger.Default.Register<AnalysisPresentation.FileInUseMessage>(this, (r, m) =>
            {
                MessageBox.Show("File '" + m.FileName + "' is currently in use by another process. Please close the file and start export again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.RequestRestartMessage>(this, (_, _) =>
            {
                var wantsRestart = MessageBox.Show(
                    "Do you want to restart LSAnalyzer for the new settings to take effect?",
                    "Restart", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (wantsRestart != MessageBoxResult.Yes || Environment.ProcessPath is null) return;

                _serviceProvider.GetRequiredService<IRservice>().Dispose();
                System.Diagnostics.Process.Start(Environment.ProcessPath);
                Application.Current.Shutdown();
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
                var lastDirectory = Path.GetDirectoryName(mainWindowViewModel.AnalysisConfiguration.FileName);
                selectAnalysisFileView.InitialDirectory = Directory.Exists(lastDirectory) ? lastDirectory : null;
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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

            var requestAnalysisViewModel = CreateRequestAnalysisViewModel();
            
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
            batchAnalyzeViewModel.ClearAnalysisData();
            batchAnalyzeViewModel.HasCurrentFile = mainWindowViewModel!.AnalysisConfiguration != null;
            batchAnalyzeViewModel.CurrentConfiguration = mainWindowViewModel.AnalysisConfiguration;
            batchAnalyzeViewModel.CurrentSubsetting = mainWindowViewModel.SubsettingExpression;

            if (batchAnalyzeViewModel.FileName != null && !File.Exists(batchAnalyzeViewModel.FileName))
            {
                batchAnalyzeViewModel.FileName = null;
            }

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
                mainWindowViewModel.SaveAnalysesDefinitionsCommand.Execute(saveFileDialog.FileName);
            }
        }

        private void MenuItemDataProviders_Click(object sender, RoutedEventArgs e)
        {
            DataProviders dataProvidersView = _serviceProvider.GetService<DataProviders>()!;
            dataProvidersView.ShowDialog();
        }

        private void RemoveAllAnalyses_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainWindow mainWindowViewModel)
            {
                return;
            }

            var confirmation = MessageBox.Show("Do you want to remove all analyses from view?", "Confirm removal",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                mainWindowViewModel.RemoveAllAnalysesCommand.Execute(null);
            }
        }

        private void ButtonMassExport_OnClick(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;

            var massExportViewModel = _serviceProvider.GetRequiredService<ViewModels.MassExport>();
            massExportViewModel.AnalysisPresentations = mainWindowViewModel?.Analyses.ToList() ?? [];

            MassExport massExportView = new(massExportViewModel);
            massExportView.ShowDialog();
        }

        private RequestAnalysis CreateRequestAnalysisViewModel()
        {
            var mainWindowViewModel = DataContext as ViewModels.MainWindow;
            
            var requestAnalysisViewModel = _serviceProvider.GetRequiredService<RequestAnalysis>();
            requestAnalysisViewModel.AnalysisConfiguration = mainWindowViewModel!.AnalysisConfiguration!;
            requestAnalysisViewModel.AvailableVariables = new ObservableCollection<Variable>(mainWindowViewModel.CurrentDatasetVariables);
            requestAnalysisViewModel.BifieSurveyVersion = mainWindowViewModel.BifieSurveyVersion;
            
            return requestAnalysisViewModel;
        }

        private void MenuItemVirtualVariables_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainWindow mainWindowViewModel || mainWindowViewModel.AnalysisConfiguration is null) return;
            
            var virtualVariablesViewModel = _serviceProvider.GetRequiredService<ViewModels.VirtualVariables>();
            virtualVariablesViewModel.AnalysisConfiguration = mainWindowViewModel.AnalysisConfiguration;
            
            VirtualVariables virtualVariablesView = new(virtualVariablesViewModel);
            virtualVariablesView.ShowDialog();
        }
    }
}
