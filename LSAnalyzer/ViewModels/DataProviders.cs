using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels
{
    public partial class DataProviders : ObservableObject
    {
        private readonly Configuration _configuration;
        private readonly IServiceProvider? _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<IDataProviderConfiguration> _configurations = new();

        [ObservableProperty]
        private IDataProviderConfiguration? _selectedConfiguration;
        partial void OnSelectedConfigurationChanged(IDataProviderConfiguration? value)
        {
            TestResults = new();
        }

        [ObservableProperty]
        private List<Type> _types = GetInstalledDataProviderConfigurationTypes();

        [ObservableProperty]
        private Type? _selectedType;

        [ObservableProperty]
        private DataProviderTestResults _testResults = new();

        private static List<Type> GetInstalledDataProviderConfigurationTypes() => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "LSAnalyzer.Models.DataProviderConfiguration" && t.GetInterfaces().Contains(typeof(IDataProviderConfiguration))).ToList();

        [ExcludeFromCodeCoverage]
        public DataProviders() 
        {
            //design-time only parameterless constructor
            _configuration = new("");
        }

        public DataProviders(Configuration configuration, IServiceProvider? serviceProvider)
        {
            _configuration = configuration;
            Configurations = new(configuration.GetDataProviderConfigurations());
            foreach (var config in Configurations)
            {
                config.AcceptChanges();
            }

            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private void NewDataProvider(Type providerType)
        {
            if (!typeof(IDataProviderConfiguration).IsAssignableFrom(providerType))
            {
                return;
            }

            var newConfiguration = (IDataProviderConfiguration)Activator.CreateInstance(providerType)!;

            Configurations.Add(newConfiguration);
            SelectedConfiguration = newConfiguration;

            SelectedType = null;
        }

        [RelayCommand]
        private void SaveDataProvider()
        {
            if (SelectedConfiguration == null)
            {
                return;
            }

            if (SelectedConfiguration.Id == 0)
            {
                int minPossibleId = 1;
                while (Configurations.Where(conf => conf.Id == minPossibleId).Any())
                {
                    minPossibleId++;
                }

                SelectedConfiguration.Id = minPossibleId;
            }

            if (SelectedConfiguration is ObservableValidatorExtended observableValidatorExtended)
            {
                observableValidatorExtended.Validate();
                if (observableValidatorExtended.HasErrors)
                {
                    return;
                }
            }

            _configuration.StoreDataProviderConfiguration(SelectedConfiguration);
            SelectedConfiguration.AcceptChanges();
        }

        [RelayCommand]
        private void DeleteDataProvider()
        {
            if (SelectedConfiguration == null)
            {
                return;
            }

            _configuration.DeleteDataProviderConfiguration(SelectedConfiguration);
            
            Configurations.Remove(SelectedConfiguration);
            SelectedConfiguration = null;
        }

        [RelayCommand]
        private void TestDataProvider()
        {
            if (SelectedConfiguration == null || _serviceProvider == null)
            {
                return;
            }

            var success = SelectedConfiguration.CreateService(_serviceProvider).TestProvider();

            TestResults = new() { IsSuccess = success, Message = success ? "Data provider works" : "Data provider not working " };
        }
    }

    public class DataProviderTestResults
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}
