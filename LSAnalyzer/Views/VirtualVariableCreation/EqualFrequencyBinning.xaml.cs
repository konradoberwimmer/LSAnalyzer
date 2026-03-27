using System.Windows;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Views.VirtualVariableCreation;

public partial class EqualFrequencyBinning : Window, ICloseable
{
    public EqualFrequencyBinning(ViewModels.VirtualVariableCreation.EqualFrequencyBinning viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}