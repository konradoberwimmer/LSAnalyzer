﻿using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Views;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration _configuration;

        public MainWindow(Configuration configuration)
        {
            _configuration = configuration;

            InitializeComponent();

            Closed += WindowClosed;
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItemDatasetTypes_Click (object sender, RoutedEventArgs e)
        {
            ConfigDatasetTypes configDatasetTypesView = new(new ViewModels.ConfigDatasetTypes(_configuration));
            configDatasetTypesView.ShowDialog();
        }

        private void MenuItemAnalysisSelectFile_Click (object sender, RoutedEventArgs e)
        {
            SelectAnalysisFile selectAnalysisFileView = new(new ViewModels.SelectAnalysisFile(_configuration));
            selectAnalysisFileView.ShowDialog();
        }
    }
}
