using Microsoft.Extensions.DependencyInjection;
using RDotNet;
using System;
using System.Runtime.InteropServices;
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
            services.AddSingleton<MainWindow>();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            using var engine = REngine.GetInstance();
            string[] a = engine.Evaluate("'Hi there .NET, from the R engine'").AsCharacter().ToArray();

            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            window.Title = a[0];
            window.Show();
        }
    }
}
