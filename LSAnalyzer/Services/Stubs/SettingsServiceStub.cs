using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services.Stubs;

public class SettingsServiceStub : ISettingsService
{
    public string? RLocation => null;
    
    public void SetAlternativeRLocation(string alternativeRLocation)
    {
       
    }

    public Dictionary<int, string> DatasetTypeHashes { get; set; } = [];

    public List<VirtualVariable> VirtualVariables
    {
        get => [];
        set
        {
            
        }
    }
}