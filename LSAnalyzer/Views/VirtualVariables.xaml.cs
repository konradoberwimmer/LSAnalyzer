using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.Views.VirtualVariableCreation;
using Microsoft.Extensions.DependencyInjection;

namespace LSAnalyzer.Views;

public partial class VirtualVariables : Window
{
    private IServiceProvider _serviceProvider;
    
    protected bool ShowLabels = true;
    
    public VirtualVariables(ViewModels.VirtualVariables viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        
        _serviceProvider = serviceProvider;
        
        DataContext = viewModel;
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.VariableNameNotAvailableMessage>(this, (_, _) =>
        {
            MessageBox.Show($"Cannot save: Variable name '{viewModel.SelectedVirtualVariable?.Name ?? string.Empty}' is already in use.", "Saving not possible",  MessageBoxButton.OK, MessageBoxImage.Information);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.PreviewImpossibleMessage>(this, (_, _) =>
        {
            MessageBox.Show("Preview not possible - check your virtual variable definition!", "Preview not possible", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }
    
    internal void ContextMenuShowLabels_Click(object sender, RoutedEventArgs e)
    {
        ShowLabels = !ShowLabels;

        SetShowLabels(this);
    }

    internal void SetShowLabels(Visual visual)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);

            if (childVisual is ListBox listBox)
            {
                listBox.DisplayMemberPath = ShowLabels ? "Info" : "Name";
            }
            else
            {
                SetShowLabels(childVisual);
            }
        }
    }

    private void ComboBoxSelectedType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || DataContext is not ViewModels.VirtualVariables viewModel || viewModel.SelectedVirtualVariableType is null) return;

        viewModel.NewVirtualVariableCommand.Execute(null);
    }

    private void ListBoxAvailableVariables_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || DataContext is not ViewModels.VirtualVariables viewModel) return;

        viewModel.HandleAvailableVariablesCommand.Execute(listBox.SelectedItems.Cast<Variable>().ToList());
    }

    private void ButtonRemoveVirtualVariable_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.VirtualVariables { SelectedVirtualVariable: VirtualVariable virtualVariable } viewModel) return;

        var result = MessageBox.Show($"Do you want to remove virtual variable '{virtualVariable.Name}'?", "Confirm removal",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.RemoveSelectedVirtualVariableCommand.Execute(null);
        }
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ViewModels.VirtualVariables viewModel) return;

        if (viewModel.CurrentVirtualVariables.Any(v => v.IsChanged))
        {
            var result =
                MessageBox.Show(
                    "Unsaved changes to virtual variables will be lost. Do you really want to close the window?",
                    "Confirm close", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        if (viewModel.HasChangedVirtualVariables)
        {
            var confirmReload = MessageBox.Show("Do you want to reload the current dataset so that new or changed virtual variables take effect?", "Reload dataset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmReload == MessageBoxResult.Yes)
            {
                viewModel.IsBusy = true;
                WeakReferenceMessenger.Default.Send(new ReloadCurrentDatasetMessage());
            }
        }
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public class ReloadCurrentDatasetMessage;

    private void ComboBoxCreate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || DataContext is not ViewModels.VirtualVariables viewModel || e.AddedItems.Count == 0 || e.AddedItems[0] is not ComboBoxItem item) return;

        switch (item.Content)
        {
            case "Dichotomization":
                ViewModels.VirtualVariableCreation.Dichotomization dichotomizationViewModel = new(viewModel);
                Dichotomization dichotomization = new(dichotomizationViewModel);
                dichotomization.ShowDialog();
                comboBox.SelectedIndex = -1;
                break;
            case "Equal frequency binning":
                ViewModels.VirtualVariableCreation.EqualFrequencyBinning equalFrequencyBinningViewModel = new(viewModel, _serviceProvider.GetRequiredService<IRservice>());
                EqualFrequencyBinning equalFrequencyBinning = new(equalFrequencyBinningViewModel);
                equalFrequencyBinning.ShowDialog();
                comboBox.SelectedIndex = -1;
                break;
            default:
                break;
        }
    }
}