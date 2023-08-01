﻿using LSAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using RDotNet;
using System;
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
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Rservice>();
            services.AddSingleton<MainWindow>();
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
                var wantsInstall = MessageBox.Show("It seems that not all necessary R packages (BIFIEsurvey, foreign) are available.\n\nDo you want to install them now?\nNOTE: This requires an active internat connection and may take a while!", "R packages not available", MessageBoxButton.YesNo, MessageBoxImage.Error);
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

            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            window.Show();
        }
    }
}
