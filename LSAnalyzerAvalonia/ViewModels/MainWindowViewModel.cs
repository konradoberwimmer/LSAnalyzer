using System;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzerAvalonia.Services;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private Services.IPlugins _plugins = null!;

    [ExcludeFromCodeCoverage]
    public MainWindowViewModel() // design-time only parameterless constructor
    {
        
    }

    public MainWindowViewModel(Services.IPlugins plugins)
    {
        _plugins = plugins;
    }

    [RelayCommand]
    private static void OpenWindow(Type windowType)
    {
        WeakReferenceMessenger.Default.Send(new OpenWindowMessage(windowType));
    }

    public class OpenWindowMessage(Type windowType)
    {
        public Type WindowType => windowType;
    }
}