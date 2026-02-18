using System.Windows.Controls;
using System.Windows.Input;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Views.CustomControls;

public partial class Percentiles : UserControl
{
    public Percentiles()
    {
        InitializeComponent();
    }

    private void TextBoxNewPercentile_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (DataContext is RequestAnalysis viewModel && e.Key == Key.Enter)
        {
            viewModel.AddPercentileCommand.Execute(null);
        }
    }
}