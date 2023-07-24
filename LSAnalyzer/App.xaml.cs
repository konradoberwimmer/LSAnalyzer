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
            services.AddSingleton<MainWindow>();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var rPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\R-core\\R64", "InstallPath", null);

            if (rPath == null)
            {
                return;
            }

            var rPathString = rPath.ToString()!;
            rPathString = rPathString.Replace("\\", "/");

            using var engine = REngine.GetInstance();
            engine.Evaluate("Sys.setenv(PATH = paste(\"" + rPathString + "/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))"); //ugly workaround for now!
            string[] a = engine.Evaluate("paste0('Result: ', stats::sd(c(1,2,3)))").AsCharacter().ToArray();

            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            window.Title = a[0];
            window.Show();
        }
    }
}
