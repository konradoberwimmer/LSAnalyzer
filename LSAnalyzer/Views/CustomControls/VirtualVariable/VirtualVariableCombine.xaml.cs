using System.Windows.Controls;
using System.Windows.Input;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views.CustomControls.VirtualVariable;

public partial class VirtualVariableCombine : UserControl
{
    public VirtualVariableCombine()
    {
        InitializeComponent();
    }

    private void ListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox ||
            DataContext is not Models.VirtualVariableCombine virtualVariableCombine) return;

        for (var i = listBox.SelectedItems.Count - 1; i >= 0; i--)
        {
            virtualVariableCombine.Variables.Remove((listBox.SelectedItems[i] as Variable)!);
        }
    }

    private void ListBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (sender is not ListBox listBox ||
                DataContext is not Models.VirtualVariableCombine virtualVariableCombine) return;

            for (var i = listBox.SelectedItems.Count - 1; i >= 0; i--)
            {
                virtualVariableCombine.Variables.Remove((listBox.SelectedItems[i] as Variable)!);
            }
        }
    }
}