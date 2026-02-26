using LSAnalyzer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LSAnalyzer.Services.Stubs;
using LSAnalyzer.ViewModels.ValueConverter;

namespace LSAnalyzer.Services;

public class Configuration
{
    private IConfigurationRoot? _config;
    private readonly IConfigurationBuilder? _configurationBuilder;
    private readonly ISettingsService _settingsService;
    private readonly IRegistryService _registryService;

    private string _datasetTypesConfigFile;
    public string DatasetTypesConfigFile
    {
        get => _datasetTypesConfigFile;
    }

    [ExcludeFromCodeCoverage]
    public Configuration()
    {
        // parameter-less constructor for testing only
        _datasetTypesConfigFile = string.Empty;
        _settingsService = new SettingsServiceStub();
        _registryService = new RegistryServiceStub();
    }

    public Configuration(string datasetTypesConfigFile, IConfigurationBuilder? configurationBuilder, ISettingsService settingsService, IRegistryService registryService) 
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
        _settingsService = settingsService;
        _registryService = registryService;
    }

    public (string rHome, string rPath)? GetRLocation()
    {
        var storedRLocation = _settingsService.RLocation;
        
        if (!string.IsNullOrWhiteSpace(storedRLocation) && Directory.Exists(storedRLocation))
        {
            return (rHome: storedRLocation, rPath: Path.Combine(storedRLocation, "bin", "x64"));
        }

        var defaultRLocation = _registryService.GetDefaultRLocation();

        return defaultRLocation is null ? null : (rHome: defaultRLocation, rPath: Path.Combine(defaultRLocation, "bin", "x64"));
    }

    public string? GetStoredRLocation()
    {
        return _settingsService.RLocation;
    }

    public void SetAlternativeRLocation(string alternativeRLocation)
    {
        _settingsService.SetAlternativeRLocation(alternativeRLocation);
    }

    public virtual List<IDataProviderConfiguration> GetDataProviderConfigurations()
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

    public virtual IDataProviderConfiguration? GetMatchingDataProviderConfiguration(IDataProviderConfiguration dataProviderConfiguration)
    {
        var dataProviderConfigurations = GetDataProviderConfigurations();

        return dataProviderConfigurations.FirstOrDefault(dpc => dpc.IsMatching(dataProviderConfiguration));
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

    public virtual void StoreDatasetType(DatasetType datasetType)
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

    public virtual void TrimRecentSubsettingExpressions(int numberOfRecentSubsettingExpressions)
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
    
    public virtual List<RecentFileForAnalysis> GetStoredRecentFiles(int dataProviderId)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default.recentFiles) ??
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
    
    public void StoreRecentFile(int dataProviderId, RecentFileForAnalysis recentFile)
    {
        if (Properties.Settings.Default.numberRecentFiles < 1)
        {
            return;
        }
        
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            if (!storedRecentFiles.ContainsKey(dataProviderId))
            {
                storedRecentFiles.Add(dataProviderId, [ recentFile ]);
            }
            else
            {
                storedRecentFiles[dataProviderId].RemoveAll(srf => srf.IsEqualFile(recentFile));
                
                storedRecentFiles[dataProviderId] =
                    storedRecentFiles[dataProviderId].Prepend(recentFile).ToList();
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
                JsonSerializer.Serialize(new Dictionary<int, List<RecentFileForAnalysis>> { { dataProviderId, [ recentFile ] } });
            Properties.Settings.Default.Save();
        }
    }

    public void RemoveRecentFilesByDataProviderId(int dataProviderId)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            storedRecentFiles.Remove(dataProviderId);
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        } catch { }
    }
    
    public void RemoveRecentFilesByDatasetTypeId(int datasetTypeId)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            foreach (var entry in storedRecentFiles)
            {
                entry.Value.RemoveAll(srf => srf.DatasetTypeId == datasetTypeId);
            }
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        } catch { }
    }
    
    public void RemoveRecentFile(RecentFileForAnalysis recentFileForAnalysis)
    {
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default
                    .recentFiles) ??
                new();
            
            foreach (var entry in storedRecentFiles)
            {
                entry.Value.RemoveAll(srf => srf.IsEqualFile(recentFileForAnalysis));
            }
            
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(storedRecentFiles);
            Properties.Settings.Default.Save();
        } catch { }
    }

    public virtual void TrimRecentFiles(int numberOfRecentFiles)
    {
        if (numberOfRecentFiles < 1)
        {
            Properties.Settings.Default.recentFiles =
                JsonSerializer.Serialize(new Dictionary<int, List<RecentFileForAnalysis>>());
            Properties.Settings.Default.Save();
            return;
        }
        
        try
        {
            var storedRecentFiles =
                JsonSerializer.Deserialize<Dictionary<int, List<RecentFileForAnalysis>>>(Properties.Settings.Default
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

    public List<string> GetStoredRecentBatchAnalyzeFiles()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(Properties.Settings.Default.recentBatchAnalyzeFiles) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public virtual void StoreRecentBatchAnalyzeFile(string fileName)
    {
        if (Properties.Settings.Default.numberRecentFiles < 1) return;
        
        var recentBatchAnalyzeFiles = GetStoredRecentBatchAnalyzeFiles();

        recentBatchAnalyzeFiles = recentBatchAnalyzeFiles.Where(file => file != fileName).ToList();
        recentBatchAnalyzeFiles = recentBatchAnalyzeFiles.Prepend(fileName).ToList();

        try
        {
            Properties.Settings.Default.recentBatchAnalyzeFiles =
                JsonSerializer.Serialize(recentBatchAnalyzeFiles);
            Properties.Settings.Default.Save();
        }
        catch
        {
            // ignored
        }
    }

    public void RemoveRecentBatchAnalyzeFile(string fileName)
    {
        var recentBatchAnalyzeFiles = GetStoredRecentBatchAnalyzeFiles();
        
        recentBatchAnalyzeFiles = recentBatchAnalyzeFiles.Where(file => file != fileName).ToList();

        try
        {
            Properties.Settings.Default.recentBatchAnalyzeFiles =
                JsonSerializer.Serialize(recentBatchAnalyzeFiles);
            Properties.Settings.Default.Save();
        }
        catch
        {
            // ignore
        }
    }

    public virtual void TrimRecentBatchAnalyzeFiles(int numberOfRecentFiles)
    {
        if (numberOfRecentFiles < 1)
        {
            Properties.Settings.Default.recentBatchAnalyzeFiles =
                JsonSerializer.Serialize(new List<string>());
            Properties.Settings.Default.Save();
            return;
        }
        
        var recentBatchAnalyzeFiles = GetStoredRecentBatchAnalyzeFiles();

        while (recentBatchAnalyzeFiles.Count > numberOfRecentFiles)
        {
            recentBatchAnalyzeFiles.RemoveAt(recentBatchAnalyzeFiles.Count - 1);
        }
        
        try
        {
            Properties.Settings.Default.recentBatchAnalyzeFiles =
                JsonSerializer.Serialize(recentBatchAnalyzeFiles);
            Properties.Settings.Default.Save();
        }
        catch
        {
            // ignore
        }
    }

    public List<VirtualVariable> GetVirtualVariablesFor(string fileName, DatasetType datasetType)
    {
        return _settingsService.VirtualVariables.Where(vv => vv.ForFileName == fileName || vv.ForDatasetTypeId == datasetType.Id).ToList();
    }

    public int GetNextVirtualVariableId()
    {
        var virtualVariables = _settingsService.VirtualVariables;

        if (virtualVariables.Count == 0) return 1;
        
        return virtualVariables.Max(vv => vv.Id) + 1;
    }

    public void RemoveVirtualVariable(VirtualVariable virtualVariable)
    {
        var virtualVariables = _settingsService.VirtualVariables;
        
        virtualVariables.Where(vv => vv.Id == virtualVariable.Id).ToList().ForEach(vv => virtualVariables.Remove(vv));
        
        _settingsService.VirtualVariables = virtualVariables;
    }

    public virtual void StoreVirtualVariable(VirtualVariable virtualVariable)
    {
        var virtualVariables = _settingsService.VirtualVariables;
        
        virtualVariables.Where(vv => vv.Id == virtualVariable.Id).ToList().ForEach(vv => virtualVariables.Remove(vv));
        
        virtualVariables.Add(virtualVariable);
        
        _settingsService.VirtualVariables = virtualVariables;
    }
    
    public class RecentFileForAnalysis
    {
        protected static LeftSideFileName _leftSideFileNameConverter = new();
        
        public string FileName { get; set; } = string.Empty;
        
        public Dictionary<string, object> UsageAttributes { get; set; } = new();

        public bool ConvertCharacters { get; set; } = true;

        public int DatasetTypeId { get; set; } = 0;
        
        public string Weight { get; set; } = string.Empty;
        
        public bool ModeKeep { get; set; } = true;
        
        public bool IsEqualFile(RecentFileForAnalysis other) => 
            FileName == other.FileName && DatasetTypeId == other.DatasetTypeId && Weight == other.Weight;

        [JsonIgnore]
        public Func<string, string> FormatFileName { get; set; } = fileName => (_leftSideFileNameConverter.Convert(fileName, typeof(string), null, CultureInfo.InvariantCulture) as string)!;
        
        [JsonIgnore]
        public string DisplayString => $"{FormatFileName(FileName)} ({Weight})";
    }
}
