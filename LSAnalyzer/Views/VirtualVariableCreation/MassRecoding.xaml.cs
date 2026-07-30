using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views.VirtualVariableCreation;

public partial class MassRecoding : Window, ICloseable
{
    private bool _showLabels = true;
    
    public MassRecoding(ViewModels.VirtualVariableCreation.MassRecoding viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariableCreation.MassRecoding.VariableNameExistsMessage>(this, (_, message) =>
        {
            MessageBox.Show(this, $"The following variables already exist: {string.Join(", ", message.ExistingNames)}", "Existing variable names", MessageBoxButton.OK, MessageBoxImage.Warning);        
        });
        
        WeakReferenceMessenger.Default.Register<ViewModels.VirtualVariableCreation.MassRecoding.VariableNameDuplicatedMessage>(this, (_, message) =>
        {
            MessageBox.Show(this, $"The following variables are declared multiple times: {string.Join(", ", message.DuplicatedNames.Distinct())}", "Duplicated variable names", MessageBoxButton.OK, MessageBoxImage.Warning);        
        });
    }

    private void ContextMenuShowLabels_Click(object sender, RoutedEventArgs e)
    {
        _showLabels = !_showLabels;

        SetShowLabels(this);
    }

    private void SetShowLabels(Visual visual)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);

            if (childVisual is ListBox listBox)
            {
                listBox.DisplayMemberPath = _showLabels ? "Info" : "Name";
            }
            else
            {
                SetShowLabels(childVisual);
            }
        }
    }
    
    private void ListBoxAvailableVariables_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || DataContext is not ViewModels.VirtualVariableCreation.MassRecoding viewModel) return;

        viewModel.HandleAvailableVariablesCommand.Execute(listBox.SelectedItems.Cast<Variable>().ToList());
    }
    
    private void ButtonRemoveRecoding_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ViewModels.VirtualVariableCreation.MassRecoding.Recoding recoding }) return;
        
        recoding.Parent.RemoveRecodingCommand.Execute(recoding);
    }
}