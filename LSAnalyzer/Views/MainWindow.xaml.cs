﻿using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
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

            DataContext = new ViewModels.MainWindow(_serviceProvider.GetRequiredService<Rservice>());

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

            RequestAnalysisFreq requestAnalysisFreqView = new(requestAnalysisViewModel);
            requestAnalysisFreqView.ShowDialog();
        }

        private void ButtonDownloadXlsx_Click (object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not AnalysisPresentation analysisPresentationViewModel)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this);

            if (wantsSave == true)
            {
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
                Properties.Settings.Default.Save();
                analysisPresentationViewModel.SaveDataTableXlsxCommand.Execute(saveFileDialog.FileName);
            }
        }

        private void DataGridResults_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(double))
            {
                if (e.Column is DataGridTextColumn dataGridTextColumn)
                {
                    dataGridTextColumn.Binding.StringFormat = "{0:0.###}";
                }
            }
        }
    }
}
