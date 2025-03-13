using System;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    [RelayCommand]
    public static void OpenWindow(Type windowType)
    {
        WeakReferenceMessenger.Default.Send(new OpenWindowMessage(windowType));
    }

    public class OpenWindowMessage(Type windowType)
    {
        public Type WindowType => windowType;
    }
}