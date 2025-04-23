using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;

namespace LSAnalyzerAvalonia.Views;

public partial class SelectAnalysisFile : Window
{
    private readonly IAppConfiguration _appConfiguration = null!;
    
    [ExcludeFromCodeCoverage]
    public SelectAnalysisFile()
    {
        InitializeComponent();
    }

    public SelectAnalysisFile(SelectAnalysisFileViewModel viewModel, IAppConfiguration appConfiguration)
    {
        InitializeComponent();
        
        DataContext = viewModel;
        
        _appConfiguration = appConfiguration;
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not SelectAnalysisFileViewModel viewModel) return;
        
        viewModel.UnregisterPluginListeners();
    }

    private async void SelectFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SelectAnalysisFileViewModel viewModel) return;
        
        IsEnabled = false;
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose plugin",
            AllowMultiple = false,
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(_appConfiguration.LastInFileLocation)
        });
        IsEnabled = true;

        if (file.Count == 0) return;
        
        viewModel.FilePath = file[0].Path.AbsolutePath;
        _appConfiguration.LastInFileLocation = Path.GetDirectoryName(file[0].Path.AbsolutePath)!;
    }
}