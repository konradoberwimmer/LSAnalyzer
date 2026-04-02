using System;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using Microsoft.Win32;

namespace LSAnalyzer.Views;

public partial class MassExport : Window, ICloseable
{
    public MassExport(ViewModels.MassExport massExportViewModel)
    {
        InitializeComponent();
        
        DataContext = massExportViewModel;
        
        WeakReferenceMessenger.Default.Register<ViewModels.MassExport.FileInUseMessage>(this, (r, m) =>
        {
            MessageBox.Show("File '" + m.FileName + "' is currently in use by another process. Please close the file and start export again.", "File in use", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void ButtonSelect_OnClick(object sender, RoutedEventArgs e)
    {
        var massExportViewModel = DataContext as ViewModels.MassExport;
        
        OpenFolderDialog openFolderDialog = new()
        {
            InitialDirectory = string.IsNullOrWhiteSpace(massExportViewModel?.Folder ?? "") ? 
                Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : 
                massExportViewModel!.Folder,
            Multiselect = false
        };

        var result = openFolderDialog.ShowDialog(this);

        if (result is not true) return;
        
        massExportViewModel!.Folder = openFolderDialog.FolderName;
    }
}