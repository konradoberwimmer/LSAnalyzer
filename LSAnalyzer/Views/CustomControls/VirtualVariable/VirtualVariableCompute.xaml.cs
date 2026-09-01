using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace LSAnalyzer.Views.CustomControls.VirtualVariable;

public partial class VirtualVariableCompute : UserControl
{
    private int _lastTextBoxExpressionPosition = -1;
    
    public VirtualVariableCompute()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<VirtualVariables.DoubleClickedAvailableVariablesMessage>(this, (_, message) =>
        {
            if (_lastTextBoxExpressionPosition < 0 || _lastTextBoxExpressionPosition > TextBoxExpression.Text.Length)
            {
                _lastTextBoxExpressionPosition = TextBoxExpression.Text.Length;
            }
            
            TextBoxExpression.Text = TextBoxExpression.Text.Insert(_lastTextBoxExpressionPosition, message.Variable.Name);
            TextBoxExpression.Focus();
            TextBoxExpression.CaretIndex = _lastTextBoxExpressionPosition + message.Variable.Name.Length;
        });
    }

    private void TextBoxExpression_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _lastTextBoxExpressionPosition = TextBoxExpression.CaretIndex;
    }
}