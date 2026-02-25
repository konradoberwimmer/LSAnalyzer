using System.Windows;

namespace LSAnalyzer.Views;

public partial class VirtualVariables : Window
{
    public VirtualVariables(ViewModels.VirtualVariables viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}