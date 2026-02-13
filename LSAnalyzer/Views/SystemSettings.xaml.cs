using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaktionslogik für SystemSettings.xaml
    /// </summary>
    public partial class SystemSettings : Window
    {
        public SystemSettings(ViewModels.SystemSettings systemSettingsViewModel)
        {
            InitializeComponent();

            DataContext = systemSettingsViewModel;

            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.LoadedDefaultDatasetTypesMessage>(this, (r, m) =>
            {
                MessageBox.Show("Loading default dataset types successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.DatasetTypeRepositoryUrlInvalidMessage>(this, (_, m) =>
            {
                    MessageBox.Show($"Cannot reach {m.Url} or it is not a valid repository!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.CollectionNotInDatasetTypeRepositoryMessage>(this, (_, m) =>
            {
                MessageBox.Show($"Collection not found! Available collections in repository: {string.Join(", ", m.ValidNames)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.DatasetTypeUrlInvalidMessage>(this, (_, m) =>
            {
                MessageBox.Show($"Invalid file encountered in repository: {m.Url}! Aborting ... please inform the repository owner!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.FetchDatasetTypeCollectionSuccessfulMessage>(this, (_, m) =>
            {
                MessageBox.Show($"Fetched {m.Count} dataset types from repository!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);        
            });
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.SavedSettingsMessage>(this, (_, _) => MessageBox.Show("Settings saved.", "Info", MessageBoxButton.OK, MessageBoxImage.Information));
            
            WeakReferenceMessenger.Default.Register<ViewModels.SystemSettings.ImpossibleRLocationMessage>(this, (_, m) =>
            {
                MessageBox.Show($"""Ignoring, because "{Path.Combine(m.Path, "bin", "x64", "R.dll")}" does not exist!""", "Wrong directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        [ExcludeFromCodeCoverage]
        private void HyperLinkGPL3_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var sInfo = new System.Diagnostics.ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }

        private void ButtonSaveSessionRcode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || DataContext is not ViewModels.SystemSettings systemSettingsViewModel)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "R Script File (*.R)|*.R";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this);

            if (wantsSave == true)
            {
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
                Properties.Settings.Default.Save();
                systemSettingsViewModel.SaveSessionRcodeCommand.Execute(saveFileDialog.FileName);
            }
        }

        private void ButtonSaveSessionLog_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || DataContext is not ViewModels.SystemSettings systemSettingsViewModel)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var wantsSave = saveFileDialog.ShowDialog(this);

            if (wantsSave == true)
            {
                Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
                Properties.Settings.Default.Save();
                systemSettingsViewModel.SaveSessionLogCommand.Execute(saveFileDialog.FileName);
            }
        }

        private void ButtonLoadDefaultDatasetConfiguration_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.SystemSettings systemSettingsViewModel)
            {
                return;
            }

            var result = MessageBox.Show("This will load all default dataset types of the current version. Your user-specific dataset types will be preserved, except for the very rare case where their IDs may collide with default values. Consider exporting your user-specific datasets beforehand!\n\nDo you want to proceed?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) 
            {
                return;
            }

            systemSettingsViewModel.LoadDefaultDatasetTypesCommand.Execute(null);
        }

        private void SystemSettings_OnClosing(object? sender, CancelEventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        private void ButtonSelectRLocation_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button || DataContext is not ViewModels.SystemSettings systemSettingsViewModel)
            {
                return;
            }

            OpenFolderDialog dialog = new();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            
            var wantsToSelect = dialog.ShowDialog(this);

            if (wantsToSelect == true)
            {
                systemSettingsViewModel.SetAlternativeRLocationCommand.Execute(dialog.FolderName);
            }
        }

        private void ButtonClearRLocation_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button || DataContext is not ViewModels.SystemSettings systemSettingsViewModel)
            {
                return;
            }
            
            systemSettingsViewModel.ClearAlternativeRLocationCommand.Execute(null);
        }
    }
}
