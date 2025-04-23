using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.IPlugins.ViewModels;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.Builtins.DataReader;

public class DataReaderCsv : IDataReaderPlugin
{
    [ExcludeFromCodeCoverage]
    public IPluginCommons.PluginTypes PluginType => IPluginCommons.PluginTypes.DataReader;
    
    [ExcludeFromCodeCoverage]
    public string DllName => string.Empty;
    
    [ExcludeFromCodeCoverage]
    public Version Version => Assembly.GetAssembly(typeof(DataReaderCsv))!.GetName().Version ?? new Version(0, 0, 0);
    
    [ExcludeFromCodeCoverage]
    public string ClassName => GetType().Name;
    
    [ExcludeFromCodeCoverage]
    public string Description => "Read data from CSV files (with options)";
    
    [ExcludeFromCodeCoverage]
    public string DisplayName => "Comma-separated values (CSV)";
    
    [ExcludeFromCodeCoverage]
    public List<string> SuggestedFileExtensions => [ "csv", "tsv" ];
    
    public ICompletelyFilled ViewModel { get; } = new DataReaderCsvViewModel();
    
    public object? View { get; private set; }
    
    public void CreateView(Type uiType)
    {
        if (View != null) return;

        if (uiType == typeof(UserControl))
        {
            View = new Views.DataReaderCsv((DataReaderCsvViewModel)ViewModel);
            return;
        }
        
        Console.WriteLine($"Could not provide view for type {uiType.FullName}.");
    }

    public Matrix<double> ReadDataFile(string path)
    {
        throw new NotImplementedException();
    }
}