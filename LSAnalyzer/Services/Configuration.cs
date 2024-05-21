using LSAnalyzer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public class Configuration
    {
        private IConfigurationRoot? _config;
        private readonly IConfigurationBuilder? _configurationBuilder;

        private string _datasetTypesConfigFile;
        public string DatasetTypesConfigFile
        {
            get => _datasetTypesConfigFile;
        }

        [ExcludeFromCodeCoverage]
        public Configuration()
        {
            // parameter-less constructor for testing only
        }

        public Configuration(string datasetTypesConfigFile, IConfigurationBuilder? configurationBuilder = null) 
        { 
            _datasetTypesConfigFile = datasetTypesConfigFile;
            try
            {
                _config = configurationBuilder?.Build() ?? new ConfigurationBuilder().Build();
            } catch
            {
                _config = new ConfigurationBuilder().Build();
            }
            _configurationBuilder = configurationBuilder;
        }

        public List<IDataProviderConfiguration> GetDataProviderConfigurations()
        {
            if (_config == null || !_config.GetSection("DataProviders").Exists() || _configurationBuilder?.Sources.Where(source => source.GetType() == typeof(JsonConfigurationSource)).LastOrDefault() is not JsonConfigurationSource configurationSource)
            {
                return new();
            }

            var fileInfo = configurationSource.FileProvider.GetFileInfo(configurationSource.Path);

            var fileContent = string.Empty;
            try
            {
                fileContent = File.ReadAllText(fileInfo.PhysicalPath);
            } catch
            {
                dynamic configuration = new { DataProviders = new List<IDataProviderConfiguration>() };
                File.WriteAllText(fileInfo.PhysicalPath, JsonSerializer.Serialize(configuration));
                _config.Reload();
            }

            return JsonSerializer.Deserialize<DataProviderConfigurationsList>(fileContent)?.DataProviders ?? new();
        }

        public void StoreDataProviderConfiguration(IDataProviderConfiguration dataProviderConfiguration)
        {
            if (_configurationBuilder?.Sources.Where(source => source.GetType() == typeof(JsonConfigurationSource)).LastOrDefault() is not JsonConfigurationSource configurationSource)
            {
                return;
            }

            var currentDataProviderConfigurations = GetDataProviderConfigurations();
            currentDataProviderConfigurations.RemoveAll(dpc => dpc.Id == dataProviderConfiguration.Id);
            currentDataProviderConfigurations.Add(dataProviderConfiguration);
            dynamic configuration = new { DataProviders = currentDataProviderConfigurations };

            var fileInfo = configurationSource.FileProvider.GetFileInfo(configurationSource.Path);
            if (!File.Exists(fileInfo.PhysicalPath))
            {
                var fs = File.Create(fileInfo.PhysicalPath!);
                fs.Close();
            }
            File.WriteAllText(fileInfo.PhysicalPath, JsonSerializer.Serialize(configuration));

            _config?.Reload();
        }

        public void DeleteDataProviderConfiguration(IDataProviderConfiguration dataProviderConfiguration)
        {
            if (_configurationBuilder?.Sources.Where(source => source.GetType() == typeof(JsonConfigurationSource)).LastOrDefault() is not JsonConfigurationSource configurationSource)
            {
                return;
            }

            var currentDataProviderConfigurations = GetDataProviderConfigurations();
            currentDataProviderConfigurations.RemoveAll(dpc => dpc.Id == dataProviderConfiguration.Id);
            dynamic configuration = new { DataProviders = currentDataProviderConfigurations };

            var fileInfo = configurationSource.FileProvider.GetFileInfo(configurationSource.Path);
            File.WriteAllText(fileInfo.PhysicalPath, JsonSerializer.Serialize(configuration));

            _config?.Reload();
        }

        public List<DatasetType>? GetStoredDatasetTypes()
        {
            if (!File.Exists(_datasetTypesConfigFile))
            {
                return new List<DatasetType>();
            }

            var fileContent = File.ReadAllText(_datasetTypesConfigFile);
            return JsonSerializer.Deserialize<List<DatasetType>>(fileContent);
        }

        public void StoreDatasetType(DatasetType datasetType)
        {
            if (!File.Exists(_datasetTypesConfigFile))
            {
                return;
            }

            var fileContent = File.ReadAllText(_datasetTypesConfigFile);
            List<DatasetType> storedDatasetTypes = new();
            try
            {
                storedDatasetTypes = JsonSerializer.Deserialize<List<DatasetType>>(fileContent) ?? storedDatasetTypes;
            } catch { }

            storedDatasetTypes.RemoveAll(dst => dst.Id == datasetType.Id);
            storedDatasetTypes.Add(datasetType);

            File.WriteAllText(_datasetTypesConfigFile, JsonSerializer.Serialize(storedDatasetTypes));
        }

        public void RemoveDatasetType(DatasetType datasetType)
        {
            if (!File.Exists(_datasetTypesConfigFile))
            {
                return;
            }

            var fileContent = File.ReadAllText(_datasetTypesConfigFile);
            List<DatasetType> storedDatasetTypes = new();
            try
            {
                storedDatasetTypes = JsonSerializer.Deserialize<List<DatasetType>>(fileContent) ?? storedDatasetTypes;
            }
            catch { }

            storedDatasetTypes.RemoveAll(dst => dst.Id == datasetType.Id);

            File.WriteAllText(_datasetTypesConfigFile, JsonSerializer.Serialize(storedDatasetTypes));
        }
    }
}
