using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSAnalyzerAvalonia.Builtins.DataReader;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.Services;
using LSAnalyzerAvalonia.ViewModels;
using LSAnalyzerAvalonia.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LSAnalyzerAvalonia;

public partial class App : Application
{
    private List<string> _startupMessages = [];
    
    public IServiceProvider Services { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        ServiceCollection collection = new();
        AddCommonServices(collection);
        Services = collection.BuildServiceProvider();
    }

    private void AddCommonServices(IServiceCollection collection)
    {
        collection.AddSingleton<IAppConfiguration, AppConfiguration>(_ => 
            new AppConfiguration(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lsanalyzer_user_settings.json"), 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lsanalyzer_dataset_types.json")
            )
        );
        collection.AddSingleton<Services.IPlugins, Plugins>();
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddTransient<SelectAnalysisFileViewModel>(
            provider => new SelectAnalysisFileViewModel(
                [ new DataReaderCsv() ],
                provider.GetRequiredService<Services.IPlugins>(), 
                typeof(UserControl)
            )
        );
        collection.AddTransient<DatasetTypesViewModel>();
        collection.AddTransient<ManagePluginsViewModel>();
        collection.AddSingleton<MainWindow>();
        collection.AddTransient<SelectAnalysisFile>();
        collection.AddTransient<DatasetTypes>();
        collection.AddTransient<ManagePlugins>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var mainWindow = Services.GetRequiredService<MainWindow>();

            if (_startupMessages.Count > 0 && mainWindow.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Message = "There were start-up messages ..." + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, _startupMessages);
                viewModel.ShowMessage = true;
            }
            
            desktop.MainWindow = mainWindow;
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