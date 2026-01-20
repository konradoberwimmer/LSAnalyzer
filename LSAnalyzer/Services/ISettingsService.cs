namespace LSAnalyzer.Services;

public interface ISettingsService
{
    public string? RLocation { get; }

    public void SetAlternativeRLocation(string alternativeRLocation);
}