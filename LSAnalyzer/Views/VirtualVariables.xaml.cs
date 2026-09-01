using System;
using System.ComponentModel;
using System.IO;
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
using Microsoft.Win32;

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
            MessageBox.Show(this, $"Cannot save: Variable name '{viewModel.SelectedVirtualVariable?.Name ?? string.Empty}' is already in use.", "Saving not possible",  MessageBoxButton.OK, MessageBoxImage.Information);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.VariableNameMatchesPvRegexMessage>(this, (_, _) =>
        {
            MessageBox.Show(this, $"Cannot save: Variable name '{viewModel.SelectedVirtualVariable?.Name ?? string.Empty}' as it would match a plausible value variable definition.", "Saving not possible",  MessageBoxButton.OK, MessageBoxImage.Information);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.PreviewImpossibleMessage>(this, (_, _) =>
        {
            MessageBox.Show(this, "Preview not possible - check your virtual variable definition!", "Preview not possible", MessageBoxButton.OK, MessageBoxImage.Information);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.VirtualVariablesFileInvalidMessage>(this, (_, message) =>
        {
            MessageBox.Show(this, $"File '{message.FileName}' does not contain virtual variable definitions.", "File not valid", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.IgnoredVirtualVariablesAtImportMessage>(this, (_, message) => {
            MessageBox.Show(this, $"Ignored the following virtual variables because of missing variables in the current data file:\n{string.Join('\n', message.VirtualVariables.Select(vv => $"- {vv.Name}: {vv.Info}"))}", "Ignored virtual variables", MessageBoxButton.OK, MessageBoxImage.Information);
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariables.DuplicatedVirtualVariablesAtImportMessage>(this, (_, message) => {
            MessageBox.Show(this, $"Ignored the following virtual variables because their name already exists in the current data file (or matches a plausible value variable definition):\n{string.Join('\n', message.VirtualVariables.Select(vv => $"- {vv.Name}: {vv.Info}"))}", "Ignored virtual variables", MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (viewModel.SelectedVirtualVariable is not VirtualVariableCompute)
        {
            viewModel.HandleAvailableVariablesCommand.Execute(listBox.SelectedItems.Cast<Variable>().ToList());
        }
        else if (listBox.SelectedItem is Variable variable)
        {
            WeakReferenceMessenger.Default.Send(new DoubleClickedAvailableVariablesMessage(variable));
        }
    }

    private void ButtonRemoveVirtualVariable_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.VirtualVariables viewModel) return;

        var virtualVariables = DataGridVirtualVariables.SelectedItems.Cast<VirtualVariable>().ToList();

        if (virtualVariables.Count == 0) return;

        var result = MessageBox.Show($"Do you want to remove virtual variable{(virtualVariables.Count > 1 ? "s" : "")} {string.Join(", ", virtualVariables.Select(vv => "'" + vv.Name + "'"))}?", "Confirm removal",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.RemoveSelectedVirtualVariableCommand.Execute(virtualVariables);
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
            case "Mass recoding":
                if (viewModel.SelectedVirtualVariable is not VirtualVariableRecode { IsChanged: false, Variables.Count: 1 })
                {
                    MessageBox.Show(this, "Mass recoding is only possible with a single input variable.", "Not possible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    comboBox.SelectedIndex = -1;
                    break;
                }
                ViewModels.VirtualVariableCreation.MassRecoding massRecodingViewModel = new(viewModel);
                MassRecoding massRecoding = new(massRecodingViewModel);
                massRecoding.ShowDialog();
                comboBox.SelectedIndex = -1;
                break;
            default:
                break;
        }
    }

    private void ButtonExportVirtualVariables_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.VirtualVariables viewModel)
        {
            return;
        }

        if (DataGridVirtualVariables.SelectedItems.Cast<VirtualVariable>().Any(vv => vv.IsChanged))
        {
            MessageBox.Show(this, "Please save changed virtual variables before exporting.", "Unsaved changes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "JSON File (*.json)|*.json",
            InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        var wantsSave = saveFileDialog.ShowDialog(this);

        if (wantsSave != true) return;
        
        Properties.Settings.Default.lastResultOutFileLocation = Path.GetDirectoryName(saveFileDialog.FileName);
        viewModel.ExportVirtualVariablesCommand.Execute(new ViewModels.VirtualVariables.ExportVirtualVariablesParameters
        {
            VirtualVariables = DataGridVirtualVariables.SelectedItems.Cast<VirtualVariable>().ToList(),
            FileName = saveFileDialog.FileName
        });
    }

    private void ButtonImportVirtualVariables_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.VirtualVariables viewModel) return;
        
        OpenFileDialog openFileDialog = new();
        openFileDialog.Filter = "JSON File (*.json)|*.json";
        openFileDialog.InitialDirectory = Properties.Settings.Default.lastResultOutFileLocation ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var result = openFileDialog.ShowDialog(this);

        if (result == true)
        {
            viewModel.ImportVirtualVariablesCommand.Execute(openFileDialog.FileName);
        }
    }

    public record DoubleClickedAvailableVariablesMessage(Variable Variable);
}