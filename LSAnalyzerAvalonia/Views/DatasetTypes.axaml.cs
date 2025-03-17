using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.ViewModels;

namespace LSAnalyzerAvalonia.Views;

public partial class DatasetTypes : Window
{
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