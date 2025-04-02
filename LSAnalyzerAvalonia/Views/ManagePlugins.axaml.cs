using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LSAnalyzerAvalonia.ViewModels;

namespace LSAnalyzerAvalonia.Views;

public partial class ManagePlugins : Window
{
    public ManagePlugins()
    {
        InitializeComponent();
    }

    private async void Add_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ManagePluginsViewModel viewModel)
        {
            return;
        }
        
        IsEnabled = false;
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose plugin",
            AllowMultiple = false,
            FileTypeFilter = [ new FilePickerFileType("Zip File") { Patterns = [ "*.zip" ], MimeTypes = [ "application/zip" ]} ],
        });
        IsEnabled = true;
        
        if (file.Count > 0)
        {
            viewModel.AddPluginCommand.Execute(file[0].Path.AbsolutePath);
        }
    }
}