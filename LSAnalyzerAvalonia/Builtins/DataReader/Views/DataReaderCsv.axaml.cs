using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;

namespace LSAnalyzerAvalonia.Builtins.DataReader.Views;

public partial class DataReaderCsv : UserControl
{
    [ExcludeFromCodeCoverage]
    public DataReaderCsv()
    {
        InitializeComponent();
    }

    public DataReaderCsv(DataReaderCsvViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}