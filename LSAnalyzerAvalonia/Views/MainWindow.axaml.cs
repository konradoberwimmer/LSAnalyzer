using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzerAvalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LSAnalyzerAvalonia.Views;

public partial class MainWindow : Window
{
    [ExcludeFromCodeCoverage]
    public MainWindow() // design-time only parameterless constructor
    {
        InitializeComponent();
    }
    
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
        
        WeakReferenceMessenger.Default.Register<MainWindowViewModel.OpenWindowMessage>(this, (_, m) =>
        {
            var view = (Application.Current as App)!.Services.GetRequiredService(m.WindowType) as Window;
            view?.ShowDialog(this);
        });
    }
}