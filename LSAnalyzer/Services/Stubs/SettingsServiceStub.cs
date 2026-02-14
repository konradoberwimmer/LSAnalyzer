using System.Collections.Generic;

namespace LSAnalyzer.Services.Stubs;

public class SettingsServiceStub : ISettingsService
{
    public string? RLocation => null;
    
    public void SetAlternativeRLocation(string alternativeRLocation)
    {
       
    }

    public Dictionary<int, string> DatasetTypeHashes { get; set; } = [];
}