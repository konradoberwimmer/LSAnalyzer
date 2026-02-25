using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public interface ISettingsService
{
    public string? RLocation { get; }

    public void SetAlternativeRLocation(string alternativeRLocation);
    
    public Dictionary<int, string> DatasetTypeHashes { get; set; }

    public List<VirtualVariable> VirtualVariables  { get; set; }
}