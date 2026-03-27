using System.Windows;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Views.VirtualVariableCreation;

public partial class Dichotomization : Window, ICloseable
{
    public Dichotomization(ViewModels.VirtualVariableCreation.Dichotomization viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}