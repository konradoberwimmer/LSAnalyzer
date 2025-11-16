using System.Collections.Generic;

namespace LSAnalyzer.Views;

public class WindowSettings
{
    private static Dictionary<string, (int width, int height)> _settings = new()
    {
        { "Univar", (width: 800, height: 600) },
        { "Freq", (width: 800, height: 600) },
        { "Percentiles", (width: 800, height: 600) },
        { "MeanDiff", (width: 800, height: 600) },
        { "Corr" , (width: 800, height: 600) },
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

    public static int WidthFreq
    {
        get => _settings["Freq"].width;
        set
        {
            var currentSettings = _settings["Freq"];
            currentSettings.width = value;
            _settings["Freq"] = currentSettings;
        }
    }

    public static int HeightFreq
    {
        get => _settings["Freq"].height;
        set
        {
            var currentSettings = _settings["Freq"];
            currentSettings.height = value;
            _settings["Freq"] = currentSettings;
        }
    }

    public static int WidthPercentiles
    {
        get => _settings["Percentiles"].width;
        set
        {
            var currentSettings = _settings["Percentiles"];
            currentSettings.width = value;
            _settings["Percentiles"] = currentSettings;
        }
    }

    public static int HeightPercentiles
    {
        get => _settings["Percentiles"].height;
        set
        {
            var currentSettings = _settings["Percentiles"];
            currentSettings.height = value;
            _settings["Percentiles"] = currentSettings;
        }
    }
    
    public static int WidthMeanDiff
    {
        get => _settings["MeanDiff"].width;
        set
        {
            var currentSettings = _settings["MeanDiff"];
            currentSettings.width = value;
            _settings["MeanDiff"] = currentSettings;
        }
    }
    
    public static int HeightMeanDiff
    {
        get => _settings["MeanDiff"].height;
        set
        {
            var currentSettings = _settings["MeanDiff"];
            currentSettings.height = value;
            _settings["MeanDiff"] = currentSettings;
        }
    }

    public static int WidthCorr
    {
        get => _settings["Corr"].width;
        set
        {
            var currentSettings = _settings["Corr"];
            currentSettings.width = value;
            _settings["Corr"] = currentSettings;
        }
    }

    public static int HeightCorr
    {
        get => _settings["Corr"].height;
        set
        {
            var currentSettings = _settings["Corr"];
            currentSettings.height = value;
            _settings["Corr"] = currentSettings;
        }
    }
}