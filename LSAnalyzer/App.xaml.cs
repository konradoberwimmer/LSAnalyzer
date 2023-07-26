using LSAnalyzer.Services;
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
                return;
            }

            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            window.Show();
        }
    }
}
