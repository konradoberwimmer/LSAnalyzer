using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views.CustomControls;

namespace LSAnalyzerAvalonia.Views;

public partial class ManagePlugins : Window
{
    public ManagePlugins()
    {
        InitializeComponent();
    }

    public ManagePlugins(ManagePluginsViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
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

    private async void Remove_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataGridPlugins.SelectedItem is null || DataContext is not ManagePluginsViewModel viewModel || DialogHost.IsDialogOpen("questions")) return;
        
        var selectedPlugin = (DataGridPlugins.SelectedItem as IPluginCommons)!;
        
        YesNoDialog dialog = new($"Do you really want to remove { selectedPlugin.ClassName }?");
        var confirmation = await DialogHost.Show(dialog, "questions");

        if (confirmation is true)
        {
            viewModel.RemovePluginCommand.Execute(selectedPlugin);
        }
    }
}