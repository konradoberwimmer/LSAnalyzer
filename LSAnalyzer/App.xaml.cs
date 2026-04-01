using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace LSAnalyzer
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        public IServiceProvider ServiceProvider
        {
            get => _serviceProvider;
        }

        public App()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var secretsId = Assembly.GetEntryAssembly()!.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;
            var secretsPath = PathHelper.GetSecretsPathFromSecretsId(secretsId);
            if (!Path.Exists(Path.GetDirectoryName(secretsPath)))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
                } catch
                {
                    Console.WriteLine("Could not create user secrets path!");
                }
            }

            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddUserSecrets<Configuration>();

            // Services
            services.AddSingleton<IServiceProvider>(_ => _serviceProvider);
            services.AddSingleton<ILogging, Logging>();
            services.AddTransient<IBatchAnalyzeService, BatchAnalyzeService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IDatasetTypeRepository, DatasetTypeRepository>();
            services.AddTransient<Configuration>(_ => { 
                var userDatasetTypesConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LSAnalyzer", "datasetTypes.json");
                return new Configuration(userDatasetTypesConfigFile, configurationBuilder, new SettingsService(), new RegistryService()); 
            });
            services.AddSingleton<IExportService, ExportService>();
            services.AddTransient<IResultService, ResultService>();
            services.AddSingleton<IRservice, Rservice>();
            services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
            // ViewModels
            services.AddTransient<ConfigDatasetTypes>();
            services.AddTransient<DataProviders>();
            services.AddSingleton<MassExport>();
            services.AddTransient<SystemSettings>();
            services.AddTransient<SelectAnalysisFile>();
            services.AddTransient<Subsetting>();
            services.AddTransient<RequestAnalysis>();
            services.AddTransient<MainWindow>();
            services.AddSingleton<BatchAnalyze>();
            services.AddTransient<VirtualVariables>();
            // Views
            services.AddTransient<Views.ConfigDatasetTypes>();
            services.AddTransient<Views.DataProviders>();
            services.AddTransient<Views.MassExport>();
            services.AddTransient<Views.SystemSettings>();
            services.AddTransient<Views.SelectAnalysisFile>();
            services.AddTransient<Views.BatchAnalyze>();
            services.AddTransient<Views.VirtualVariables>();
            services.AddSingleton<Views.MainWindow>(_ => new Views.MainWindow(_serviceProvider));
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            LSAnalyzer.Properties.Settings.Default.Reload();
            
            var configuration = _serviceProvider.GetService<Configuration>()!;
            var rService = _serviceProvider.GetService<IRservice>()!;
            var settingsService = _serviceProvider.GetService<ISettingsService>()!;

            rService.RLocation = configuration.GetRLocation() ?? (string.Empty, string.Empty);
            if (!rService.Connect())
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(settingsService.RLocation)
                        ? "No R installation was found automatically!\n\nPlease configure manually (Config -> System)."
                        : $"""
                           No R installation was found at "{settingsService.RLocation}"!
                           
                           Please revise configuration (Config -> System).
                           """,
                    "R not found", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (rService.IsConnected && !rService.CheckNecessaryRPackages())
            {
                var rVersion = rService.GetRVersion()!;
                
                var wantsInstall = MessageBox.Show($"""
                                                   It seems that not all necessary R packages (BIFIEsurvey, foreign) are available. 
                                                   
                                                   Do you want to install them now? 
                                                   NOTE: This requires an active internet connection and may take a while!
                                                   
                                                   Otherwise, you can do this manually (restart LSAnalyzer!) or via Config -> System.
                                                   
                                                   (Using {rVersion} with library "{rService.RLocation.rPath}".)
                                                   """, "R packages not available", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (wantsInstall == MessageBoxResult.Yes)
                {
                    var successfulInstall = rService.InstallNecessaryRPackages();
                    if (successfulInstall)
                    {
                        MessageBox.Show("R package installation successful. Restarting LSAnalyzer ...", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        if (Environment.ProcessPath is null)
                        {
                            Shutdown(0);
                            return;
                        }
                        
                        _serviceProvider.GetRequiredService<IRservice>().Dispose();
                        System.Diagnostics.Process.Start(Environment.ProcessPath);
                        Shutdown(0);
                        return;
                    } else
                    {
                        MessageBox.Show("R package installation did not succeed. Please handle this manually in your R installation and restart LSAnalyzer afterwards!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            if (rService.IsConnected && !rService.InjectAppFunctions())
            {
                MessageBox.Show("There was a problem putting specific functions for LSAnalyzer into the global environment!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var configurationService = _serviceProvider.GetRequiredService<Configuration>();
            var fileDatasetTypes = configurationService.DatasetTypesConfigFile;
            if (!File.Exists(fileDatasetTypes))
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(fileDatasetTypes)))
                    { 
                        Directory.CreateDirectory(Path.GetDirectoryName(fileDatasetTypes)!);
                    }
                    var defaultDatasetTypes = JsonSerializer.Serialize(DatasetType.CreateDefaultDatasetTypes());
                    File.WriteAllText(fileDatasetTypes, defaultDatasetTypes);
                } catch
                {
                    MessageBox.Show("There was an error storing the default dataset types. Please contact your administrator!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }
            }

            Views.MainWindow window = _serviceProvider.GetRequiredService<Views.MainWindow>();
            window.Show();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown(2);
        }
    }
}
