namespace LSAnalyzer.Services;

public class SettingsService : ISettingsService
{
    public string? RLocation => Properties.Settings.Default.RLocation;
    
    public void SetAlternativeRLocation(string alternativeRLocation)
    {
        Properties.Settings.Default.RLocation = alternativeRLocation;
        Properties.Settings.Default.Save();
    }
}