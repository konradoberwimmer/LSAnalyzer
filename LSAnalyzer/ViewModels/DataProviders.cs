using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
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

        [ObservableProperty]
        private ObservableCollection<IDataProviderConfiguration> _configurations = new();

        [ObservableProperty]
        private IDataProviderConfiguration? _selectedConfiguration;

        [ObservableProperty]
        private List<Type> _types = GetInstalledDataProviderConfigurationTypes();

        [ObservableProperty]
        private Type? _selectedType;

        private static List<Type> GetInstalledDataProviderConfigurationTypes() => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "LSAnalyzer.Models.DataProviderConfiguration" && t.GetInterfaces().Contains(typeof(IDataProviderConfiguration))).ToList();

        [ExcludeFromCodeCoverage]
        public DataProviders() 
        {
            //design-time only parameterless constructor
            _configuration = new("");
        }

        public DataProviders(Configuration configuration)
        {
            _configuration = configuration;
            Configurations = new(configuration.GetDataProviderConfigurations());
            foreach (var config in Configurations)
            {
                config.AcceptChanges();
            }
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
    }
}
