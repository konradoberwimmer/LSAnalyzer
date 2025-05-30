using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerDataReaderXlsx.ViewModels;

namespace LSAnalyzerDataReaderXlsx.Views;

public partial class DataReaderXlsx : UserControl
{
    [ExcludeFromCodeCoverage]
    public DataReaderXlsx() // design-time only parameterless constructor
    {
        InitializeComponent();
    }

    public DataReaderXlsx(DataReaderXlsxViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}