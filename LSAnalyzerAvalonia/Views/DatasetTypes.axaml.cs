using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views.CustomControls;

namespace LSAnalyzerAvalonia.Views;

public partial class DatasetTypes : Window
{
    private IAppConfiguration _appConfiguration = null!;
    
    [ExcludeFromCodeCoverage]
    public DatasetTypes() // design-time only parameterless constructor
    {
        InitializeComponent();
    }
    
    public DatasetTypes(DatasetTypesViewModel viewModel, IAppConfiguration appConfiguration)
    {
        InitializeComponent();
        
        DataContext = viewModel;
        
        _appConfiguration = appConfiguration;
    }

    private async void Remove_OnClick(object? sender, RoutedEventArgs e)
    {
        YesNoDialog dialog = new("Are you sure you want to remove this dataset type?");
        var confirmation = await DialogHost.Show(dialog, "questions");

        if (confirmation is true && DataContext is DatasetTypesViewModel viewModel)
        {
            viewModel.RemoveDatasetTypeCommand.Execute(null);
        }
    }

    private async void Export_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DatasetTypesViewModel viewModel || viewModel.SelectedDatasetType is null)
        {
            return;
        }

        if (!viewModel.SelectedDatasetType.Validate())
        {
            viewModel.Message = "Dataset type has errors. Please fix them before exporting!";
            viewModel.ShowMessage = true;
            return;
        }

        IsEnabled = false;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export dataset type",
            DefaultExtension = "json",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(_appConfiguration.LastOutFileLocation)
        });
        IsEnabled = true;
        
        if (file is not null)
        {
            viewModel.ExportDatasetTypeCommand.Execute(file.Path.AbsolutePath);
        }
    }

    private async void Import_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DatasetTypesViewModel viewModel)
        {
            return;
        }
        
        IsEnabled = false;
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import dataset type",
            AllowMultiple = false,
            FileTypeFilter = [ new FilePickerFileType("JSON") { Patterns = [ "*.json" ], MimeTypes = [ "application/json" ]} ],
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(_appConfiguration.LastInFileLocation)
        });
        IsEnabled = true;
        
        if (file.Count > 0)
        {
            viewModel.ImportDatasetTypeCommand.Execute(file[0].Path.AbsolutePath);
        }
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not DatasetTypesViewModel viewModel || viewModel.UnsavedDatasetTypeNames.Count == 0 || DialogHost.IsDialogOpen("questions")) return;

        e.Cancel = true;

        YesNoDialog dialog = new($"There are unsaved dataset types ({ string.Join(", ", viewModel.UnsavedDatasetTypeNames.ToArray()) }). Do you really want to close and lose pending changes?");
        var confirmation = await DialogHost.Show(dialog, "questions");

        if (confirmation is true)
        {
            Closing -= Window_OnClosing;
            Close();
        }
    }
}