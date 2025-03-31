using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml;
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
    
    private Assembly? LoadPlugin(string fullPath)
    {
        PluginLoadContext pluginLoadContext = new();

        if (!pluginLoadContext.SetAssemblyDependencyResolverFromFullPath(fullPath))
        {
            _startupMessages.Add($"Failed to load plugin '{fullPath}'!");
            return null;
        }
        
        return pluginLoadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(fullPath))) ;
    }
    
    private IDataReaderPlugin? CreateDataReader(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IDataReaderPlugin).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type) as IDataReaderPlugin;
            }
        }
        
        _startupMessages.Add($"Failed to find usable plugin interface in assembly '{assembly.FullName}'!");
        return null;
    }

    private void AddCommonServices(IServiceCollection collection)
    {
        collection.AddSingleton<Services.IPlugins, Plugins>(_ =>
        {
            Plugins plugins = new();
            
            return plugins;
        });
        collection.AddSingleton<IAppConfiguration, AppConfiguration>(_ => 
            new AppConfiguration(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "lsanalyzer_user_settings.json"), 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lsanalyzer_dataset_types.json")
            )
        );
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddTransient<SelectAnalysisFileViewModel>();
        collection.AddTransient<DatasetTypesViewModel>();
        collection.AddSingleton<MainWindow>();
        collection.AddTransient<SelectAnalysisFile>();
        collection.AddTransient<DatasetTypes>();
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