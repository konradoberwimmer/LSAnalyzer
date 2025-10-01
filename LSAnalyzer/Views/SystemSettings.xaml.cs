using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            WeakReferenceMessenger.Default.Register<LoadedDefaultDatasetTypesMessage>(this, (r, m) =>
            {
                MessageBox.Show("Loading default dataset types successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            
            WeakReferenceMessenger.Default.Register<SavedSettingsMessage>(this, (_, _) => MessageBox.Show("Settings saved.", "Info", MessageBoxButton.OK, MessageBoxImage.Information));
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
    }
}
