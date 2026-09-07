using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views;

public partial class WeightVariables : Window, ICloseable
{
    private bool _appliedChanges = false;

    public List<WeightVariable>? FormerWeightVariables { get; set; }

    public WeightVariables()
    {
        InitializeComponent();
    }

    private void ButtonApply_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.ConfigDatasetTypes viewModel)
        {
            return;
        }

        _appliedChanges = true;
        
        viewModel.MakeWeightFromPossibleWeightVariablesCommand.Execute(this);
        
        _appliedChanges = false;
    }

    private void WeightVariables_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_appliedChanges || FormerWeightVariables is null || DataContext is not ViewModels.ConfigDatasetTypes { SelectedDatasetType: not null } viewModel) return;

        viewModel.SelectedDatasetType.PossibleWeightVariables = [..FormerWeightVariables];
    }
}