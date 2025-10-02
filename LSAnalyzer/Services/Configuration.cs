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
using System.Threading.Tasks;

namespace LSAnalyzer.Services;

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

    public List<string> GetStoredRecentSubsettingExpressions(int datasetTypeId)
    {
        try
        {
            var storedRecentSubsettingExpressions =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default.recentSubsettingExpressions) ??
                new();
            
            if (storedRecentSubsettingExpressions.TryGetValue(datasetTypeId, out var recentSubsettingExpressions))
            {
                return recentSubsettingExpressions;
            }
        }
        catch
        {
            return [];
        }

        return [];
    }
    
    public void StoreRecentSubsettingExpression(int datasetTypeId, string subsettingExpression)
    {
        if (Properties.Settings.Default.numberRecentSubsettingExpressions < 1)
        {
            return;
        }
        
        try
        {
            var storedRecentSubsettingExpressions =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentSubsettingExpressions) ??
                new();
            
            if (!storedRecentSubsettingExpressions.ContainsKey(datasetTypeId))
            {
                storedRecentSubsettingExpressions.Add(datasetTypeId, [ subsettingExpression ]);
            }
            else
            {
                storedRecentSubsettingExpressions[datasetTypeId].Remove(subsettingExpression);
                storedRecentSubsettingExpressions[datasetTypeId] =
                    storedRecentSubsettingExpressions[datasetTypeId].Prepend(subsettingExpression).ToList();
            }

            if (storedRecentSubsettingExpressions[datasetTypeId].Count >
                Properties.Settings.Default.numberRecentSubsettingExpressions)
            {
                storedRecentSubsettingExpressions[datasetTypeId].RemoveAt(storedRecentSubsettingExpressions[datasetTypeId].Count - 1);
            }
            
            Properties.Settings.Default.recentSubsettingExpressions =
                JsonSerializer.Serialize(storedRecentSubsettingExpressions);
            Properties.Settings.Default.Save();
        }
        catch
        {
            Properties.Settings.Default.recentSubsettingExpressions =
                JsonSerializer.Serialize(new Dictionary<int, List<string>> { { datasetTypeId, [ subsettingExpression ] } });
            Properties.Settings.Default.Save();
        }
    }

    public void RemoveRecentSubsettingExpressions(int datasetTypeId)
    {
        try
        {
            var storedRecentSubsettingExpressions =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentSubsettingExpressions) ??
                new();
            
            storedRecentSubsettingExpressions.Remove(datasetTypeId);
            
            Properties.Settings.Default.recentSubsettingExpressions =
                JsonSerializer.Serialize(storedRecentSubsettingExpressions);
            Properties.Settings.Default.Save();
        } catch { }
    }

    public void TrimRecentSubsettingExpressions(int numberOfRecentSubsettingExpressions)
    {
        if (numberOfRecentSubsettingExpressions < 1)
        {
            Properties.Settings.Default.recentSubsettingExpressions =
                JsonSerializer.Serialize(new Dictionary<int, List<string>>());
            Properties.Settings.Default.Save();
            return;
        }
        
        try
        {
            var storedRecentSubsettingExpressions =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentSubsettingExpressions) ??
                new();
            
            foreach (var entry in storedRecentSubsettingExpressions)
            {
                while (entry.Value.Count > numberOfRecentSubsettingExpressions)
                {
                    entry.Value.RemoveAt(entry.Value.Count - 1);
                }
            }
            
            Properties.Settings.Default.recentSubsettingExpressions =
                JsonSerializer.Serialize(storedRecentSubsettingExpressions);
            Properties.Settings.Default.Save();
        } catch { }
    }
    
    public List<string> GetStoredRecentFiles(int dataProviderId)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default.recentFiles) ??
                new();
            
            if (storedRecentFiles.TryGetValue(dataProviderId, out var recentFiles))
            {
                return recentFiles;
            }
        }
        catch
        {
            return [];
        }

        return [];
    }
    
    public void StoreRecentFile(int dataProviderId, string fileName)
    {
        if (Properties.Settings.Default.numberRecentFiles < 1)
        {
            return;
        }
        
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            if (!storedRecentFiles.ContainsKey(dataProviderId))
            {
                storedRecentFiles.Add(dataProviderId, [ fileName ]);
            }
            else
            {
                storedRecentFiles[dataProviderId].Remove(fileName);
                storedRecentFiles[dataProviderId] =
                    storedRecentFiles[dataProviderId].Prepend(fileName).ToList();
            }

            if (storedRecentFiles[dataProviderId].Count >
                Properties.Settings.Default.numberRecentFiles)
            {
                storedRecentFiles[dataProviderId].RemoveAt(storedRecentFiles[dataProviderId].Count - 1);
            }
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        }
        catch
        {
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(new Dictionary<int, List<string>> { { dataProviderId, [ fileName ] } });
            Properties.Settings.Default.Save();
        }
    }

    public void RemoveRecentFiles(int dataProviderId)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            storedRecentFiles.Remove(dataProviderId);
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        } catch { }
    }

    public void TrimRecentFiles(int numberOfRecentFiles)
    {
        if (numberOfRecentFiles < 1)
        {
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(new Dictionary<int, List<string>>());
            Properties.Settings.Default.Save();
            return;
        }
        
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            foreach (var entry in storedRecentFiles)
            {
                while (entry.Value.Count > numberOfRecentFiles)
                {
                    entry.Value.RemoveAt(entry.Value.Count - 1);
                }
            }
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        } catch { }
    }
}
