using System.Reflection;
using Avalonia.Controls;
using ClosedXML.Excel;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.IPlugins.ViewModels;
using LSAnalyzerDataReaderXlsx.ViewModels;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerDataReaderXlsx;

public class DataReaderXlsx : IDataReaderPlugin
{
    public IPluginCommons.PluginTypes PluginType => IPluginCommons.PluginTypes.DataReader;
    
    public string DllName => "LSAnalyzerDataReaderXlsx.dll";
    
    public Version Version => Assembly.GetAssembly(typeof(DataReaderXlsx))!.GetName().Version ?? new Version(0, 0, 0);
    
    public string ClassName => GetType().Name;
    
    public string Description => "Read data from XLSX files (Microsoft Excel 2007-current)";
    
    public string DisplayName => "Microsoft Excel (XLSX)";

    public object? View { get; private set; }
    
    public ICompletelyFilled ViewModel { get; } = new DataReaderXlsxViewModel();

    public void CreateView(Type uiType)
    {
        if (View != null) return;
        
        if (uiType == typeof(UserControl))
        { 
            View = new Views.DataReaderXlsx((ViewModel as DataReaderXlsxViewModel)!);
            return;
        }
        
        Console.WriteLine($"Could not provide view for type {uiType.FullName}.");
    }

    public Matrix<double> ReadDataFile(string path)
    {
        using XLWorkbook workbook = new();
        workbook.AddWorksheet("new");
        
        return Matrix<double>.Build.Random(3, 4);
    }
}