using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views.CustomControls;

namespace LSAnalyzerAvalonia.Views;

public partial class DatasetTypes : Window
{
    [ExcludeFromCodeCoverage]
    public DatasetTypes() // design-time only parameterless constructor
    {
        InitializeComponent();
    }
    
    public DatasetTypes(DatasetTypesViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }

    private async void RemoveDatasetType_OnClick(object? sender, RoutedEventArgs e)
    {
        YesNoDialog dialog = new("Are you sure you want to remove this dataset type?");
        var confirmation = await DialogHost.Show(dialog, "questions");

        if (confirmation is true && DataContext is DatasetTypesViewModel viewModel)
        {
            viewModel.RemoveDatasetTypeCommand.Execute(null);
        }
    }
}