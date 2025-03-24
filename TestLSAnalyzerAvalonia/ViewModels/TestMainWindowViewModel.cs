using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views;

namespace TestLSAnalyzerAvalonia.ViewModels;

public class TestMainWindowViewModel
{
    [Fact]
    public void TestOpenWindowCommand()
    {
        MainWindowViewModel.OpenWindowMessage? sentMessage = null;
        WeakReferenceMessenger.Default.Register<MainWindowViewModel.OpenWindowMessage>(this, (_, m) =>
        {
            sentMessage = m;
        });

        MainWindowViewModel viewModel = new();
        
        viewModel.OpenWindowCommand.Execute(typeof(DatasetTypes));
        
        Assert.NotNull(sentMessage);
        Assert.Equal(typeof(DatasetTypes), sentMessage.WindowType);
    }
}