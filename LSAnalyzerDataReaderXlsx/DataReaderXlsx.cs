using System.Reflection;
using ClosedXML.Excel;
using LSAnalyzerAvalonia.IPlugins;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerDataReaderXlsx;

public class DataReaderXlsx : IDataReaderPlugin
{
    public string DllName => "LSAnalyzerDataReaderXlsx.dll";
    
    public Version Version => Assembly.GetAssembly(typeof(DataReaderXlsx))!.GetName().Version ?? new Version(0, 0, 0);
    
    public string ClassName => GetType().Name;
    
    public string Description => "Read data from XLSX files (Microsoft Excel 2007-current)";
    
    public string DisplayName => "Microsoft Excel (XLSX)";

    public Matrix<double> ReadDataFile(string path)
    {
        using XLWorkbook workbook = new();
        workbook.AddWorksheet("new");
        
        return Matrix<double>.Build.Random(3, 4);
    }
}