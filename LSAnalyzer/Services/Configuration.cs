using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public class Configuration
    {
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

        public Configuration(string datasetTypesConfigFile) 
        { 
            _datasetTypesConfigFile = datasetTypesConfigFile;
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
