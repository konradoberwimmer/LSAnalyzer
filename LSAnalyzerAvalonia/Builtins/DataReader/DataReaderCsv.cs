using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using CsvHelper;
using CsvHelper.Configuration;
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
    
    public (bool success, ImmutableList<string> columns) ReadFileHeader(string path)
    {
        try
        {
            using StreamReader fileReader = new(path);
            
            var dataReaderCsvViewModel = (ViewModel as DataReaderCsvViewModel)!;
            
            CsvConfiguration configuration = new(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = Regex.Unescape(dataReaderCsvViewModel.SeparatorCharacter),
                Mode =  string.IsNullOrWhiteSpace(dataReaderCsvViewModel.QuotingCharacter) ? CsvMode.NoEscape : CsvMode.RFC4180,
                Quote = string.IsNullOrWhiteSpace(dataReaderCsvViewModel.QuotingCharacter) ? '"' : dataReaderCsvViewModel.QuotingCharacter[0],
                LineBreakInQuotedFieldIsBadData = false,
            };
            
            using CsvReader csvReader = new(fileReader, configuration);
            csvReader.Read();
            var canReadHeader = csvReader.ReadHeader();

            return !canReadHeader ? (false, []) : (true, ImmutableList.Create(csvReader.HeaderRecord!));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, []);
        }
    }

    public Matrix<double> ReadDataFile(string path)
    {
        throw new NotImplementedException();
    }
}