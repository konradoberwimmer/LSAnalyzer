using System.Collections.Generic;

namespace LSAnalyzer.Views;

public class WindowSettings
{
    private static Dictionary<string, (int width, int height)> _settings = new()
    {
        { "Univar", (width: 800, height: 600) }
    };

    public static int WidthUnivar
    {
        get => _settings["Univar"].width;
        set
        {
            var currentSettings = _settings["Univar"];
            currentSettings.width = value;
            _settings["Univar"] = currentSettings;
        }
    }
    
    public static int HeightUnivar
    {
        get => _settings["Univar"].height;
        set
        {
            var currentSettings = _settings["Univar"];
            currentSettings.height = value;
            _settings["Univar"] = currentSettings;
        }
    }
}