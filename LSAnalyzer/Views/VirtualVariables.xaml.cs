using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views;

public partial class VirtualVariables : Window
{
    public VirtualVariables(ViewModels.VirtualVariables viewModel)
    {
        InitializeComponent();
        
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
        if (DataContext is not ViewModels.VirtualVariables viewModel ||
            !viewModel.CurrentVirtualVariables.Any(v => v.IsChanged)) return;
        
        var result = MessageBox.Show("Unsaved changes to virtual variables will be lost. Do you really want to close the window?", "Confirm close", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
        {
            e.Cancel = true;
        }
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}