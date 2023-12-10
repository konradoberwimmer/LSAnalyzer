using GalaSoft.MvvmLight.Threading;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using RDotNet;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace LSAnalyzer
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            DispatcherHelper.Initialize();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Logging>();
            services.AddSingleton<Rservice>();
            services.AddTransient<Services.BatchAnalyze>();
            services.AddTransient<Configuration>(provider => { 
                var userDatasetTypesConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LSAnalyzer", "datasetTypes.json");
                return new Configuration(userDatasetTypesConfigFile); 
            });
            services.AddTransient<ConfigDatasetTypes>();
            services.AddTransient<SystemSettings>();
            services.AddTransient<SelectAnalysisFile>();
            services.AddTransient<Subsetting>();
            services.AddTransient<RequestAnalysis>();
            services.AddTransient<MainWindow>();
            services.AddTransient<ViewModels.BatchAnalyze>();
            services.AddTransient<Views.ConfigDatasetTypes>();
            services.AddTransient<Views.SystemSettings>();
            services.AddTransient<Views.SelectAnalysisFile>();
            services.AddTransient<Views.BatchAnalyze>();
            services.AddSingleton<Views.MainWindow>(provider =>
            {
                return new Views.MainWindow(_serviceProvider);
            });
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var rService = _serviceProvider.GetService<Rservice>()!;
            if (!rService.Connect())
            {
                MessageBox.Show("An R installation was not found!\n\nPlease make sure that R (>=4.3.0) is installed, registered in Windows Registry and fully available to the current user", "R not found", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            if (!rService.CheckNecessaryRPackages())
            {
                var wantsInstall = MessageBox.Show("It seems that not all necessary R packages (BIFIEsurvey, foreign, openxlsx) are available.\n\nDo you want to install them now?\nNOTE: This requires an active internat connection and may take a while!", "R packages not available", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (wantsInstall == MessageBoxResult.Yes)
                {
                    var succesfulInstall = rService.InstallNecessaryRPackages();
                    if (succesfulInstall)
                    {
                        MessageBox.Show("R package installation successful. Please restart application!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    } else
                    {
                        MessageBox.Show("R package installation did not succeed. Please handle this manually in your R installation and restart app afterwards!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Shutdown(1);
                return;
            }

            if (!rService.InjectAppFunctions())
            {
                MessageBox.Show("There was a problem putting specific functions for LSAnalyzer into the global environment!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
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
    }
}
