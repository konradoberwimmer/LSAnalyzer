using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LSAnalyzerAvalonia;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        ServiceCollection collection = new();
        AddCommonServices(collection);
        Services = collection.BuildServiceProvider();
    }

    private static void AddCommonServices(IServiceCollection collection)
    {
        collection.AddSingleton<IAppConfiguration, AppConfiguration>(_ => 
            new AppConfiguration(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lsanalyzer_user_settings.json"), 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lsanalyzer_dataset_types.json")
            )
        );
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddTransient<DatasetTypesViewModel>();
        collection.AddSingleton<MainWindow>();
        collection.AddTransient<DatasetTypes>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}