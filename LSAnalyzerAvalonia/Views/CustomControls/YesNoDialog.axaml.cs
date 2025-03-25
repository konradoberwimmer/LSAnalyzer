using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;

namespace LSAnalyzerAvalonia.Views.CustomControls;

public partial class YesNoDialog : UserControl
{
    public YesNoDialog() // design-time parameterless constructor
    {
        InitializeComponent();
    }

    public YesNoDialog(string question)
    {
        InitializeComponent();
        
        Question.Text = question;
    }

    private void Yes_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Parent?.Parent?.Parent is DialogHost dialogHost)
        {
            dialogHost.CloseDialogCommand.Execute(true);
        }
    }

    private void No_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Parent?.Parent?.Parent is DialogHost dialogHost)
        {
            dialogHost.CloseDialogCommand.Execute(false);
        }
    }
}