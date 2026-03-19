using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace LSAnalyzer.Views.CustomControls.VirtualVariable;

public partial class VirtualVariableRecode : UserControl
{
    public VirtualVariableRecode()
    {
        InitializeComponent();
    }

    private void ButtonRemoveLastVariable_Click(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new RemoveLastVariableMessage());
    }

    private void ButtonAddRule_OnClick(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new AddRuleMessage());
    }

    private void ButtonRemoveRule_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Models.VirtualVariableRecode.Rule rule }) return;
        
        WeakReferenceMessenger.Default.Send(new RemoveRuleMessage { Rule = rule });
    }
    
    public class AddRuleMessage;
    
    public class RemoveLastVariableMessage;

    public class RemoveRuleMessage
    {
        public required Models.VirtualVariableRecode.Rule Rule { init; get; }
    }
}