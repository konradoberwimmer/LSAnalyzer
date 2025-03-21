using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using LSAnalyzerAvalonia.ViewModels;

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
}