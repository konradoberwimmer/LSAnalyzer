using System;
using System.Collections.Generic;
using System.Text.Json;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public class SettingsService : ISettingsService
{
    public string? RLocation => Properties.Settings.Default.RLocation;
    
    public void SetAlternativeRLocation(string alternativeRLocation)
    {
        Properties.Settings.Default.RLocation = alternativeRLocation;
        Properties.Settings.Default.Save();
    }

    public Dictionary<int, string> DatasetTypeHashes
    {
        get
        {
            var jsonEncoded = Properties.Settings.Default.datasetTypeHashes ?? string.Empty;

            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonEncoded);
                
                return dictionary ?? [];
            }
            catch (Exception)
            {
                return [];
            }
        }
        set
        {
            var jsonEncoded = JsonSerializer.Serialize(value);
            
            Properties.Settings.Default.datasetTypeHashes = jsonEncoded;
            Properties.Settings.Default.Save();
        }
    }

    public List<VirtualVariable> VirtualVariables
    {
        get
        {
            var jsonEncoded = Properties.Settings.Default.virtualVariables ?? string.Empty;

            try
            {
                var virtualVariables = JsonSerializer.Deserialize<List<VirtualVariable>>(jsonEncoded);

                return virtualVariables ?? [];
            } catch (Exception)
            {
                return [];
            }
        }
        set
        {
            var jsonEncoded = JsonSerializer.Serialize(value);
            
            Properties.Settings.Default.virtualVariables = jsonEncoded;
            Properties.Settings.Default.Save();
        }
    }
}