namespace LSAnalyzer.Services;

public class SettingsService : ISettingsService
{
    public string? RLocation => Properties.Settings.Default.RLocation;
}