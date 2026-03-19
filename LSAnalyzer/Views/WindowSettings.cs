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
        { "Linreg", (width: 800, height: 600) },
        { "LogistReg", (width: 800, height: 600) },
        { "BatchAnalyze", (width: 800, height: 600) },
        { "Subsetting", (width: 800, height: 450) },
        { "VirtualVariables", (width: 1200, height: 600) },
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

    public static int WidthLinreg
    {
        get => _settings["Linreg"].width;
        set
        {
            var currentSettings = _settings["Linreg"];
            currentSettings.width = value;
            _settings["Linreg"] = currentSettings;
        }
    }

    public static int HeightLinreg
    {
        get => _settings["Linreg"].height;
        set
        {
            var currentSettings = _settings["Linreg"];
            currentSettings.height = value;
            _settings["Linreg"] = currentSettings;
        }
    }
    
    public static int WidthLogistReg
    {
        get => _settings["LogistReg"].width;
        set
        {
            var currentSettings = _settings["LogistReg"];
            currentSettings.width = value;
            _settings["LogistReg"] = currentSettings;
        }
    }
    
    public static int HeightLogistReg
    {
        get => _settings["LogistReg"].height;
        set
        {
            var currentSettings = _settings["LogistReg"];
            currentSettings.height = value;
            _settings["LogistReg"] = currentSettings;
        }
    }

    public static int WidthBatchAnalyze
    {
        get => _settings["BatchAnalyze"].width;
        set
        {
            var currentSettings = _settings["BatchAnalyze"];
            currentSettings.width = value;
            _settings["BatchAnalyze"] = currentSettings;
        }
    }
    
    public static int HeightBatchAnalyze 
    {
        get => _settings["BatchAnalyze"].height;
        set
        {
            var currentSettings = _settings["BatchAnalyze"];
            currentSettings.height = value;
            _settings["BatchAnalyze"] = currentSettings;
        }
    }
    
    public static int WidthSubsetting
    {
        get => _settings["Subsetting"].width;
        set
        {
            var currentSettings = _settings["Subsetting"];
            currentSettings.width = value;
            _settings["Subsetting"] = currentSettings;
        }
    }
    
    public static int HeightSubsetting
    {
        get => _settings["Subsetting"].height;
        set
        {
            var currentSettings = _settings["Subsetting"];
            currentSettings.height = value;
            _settings["Subsetting"] = currentSettings;
        }
    }
    
    public static int WidthVirtualVariables
    {
        get => _settings["VirtualVariables"].width;
        set
        {
            var currentSettings = _settings["VirtualVariables"];
            currentSettings.width = value;
            _settings["VirtualVariables"] = currentSettings;
        }
    }
    
    public static int HeightVirtualVariables
    {
        get => _settings["VirtualVariables"].height;
        set
        {
            var currentSettings = _settings["VirtualVariables"];
            currentSettings.height = value;
            _settings["VirtualVariables"] = currentSettings;
        }
    }
}