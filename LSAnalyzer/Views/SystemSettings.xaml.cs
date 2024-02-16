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
        }

        [ExcludeFromCodeCoverage]
        private void hyperLinkGPL3_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var sInfo = new System.Diagnostics.ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
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
    }
}
