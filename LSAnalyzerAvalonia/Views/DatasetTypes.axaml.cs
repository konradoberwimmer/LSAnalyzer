using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.ViewModels;

namespace LSAnalyzerAvalonia.Views;

public partial class DatasetTypes : Window
{
    public DatasetTypes(DatasetTypesViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}