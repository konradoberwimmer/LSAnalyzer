using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.ViewModels;

namespace LSAnalyzerAvalonia.Views;

public partial class SelectAnalysisFile : Window
{
    public SelectAnalysisFile()
    {
        InitializeComponent();
    }

    public SelectAnalysisFile(SelectAnalysisFileViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}