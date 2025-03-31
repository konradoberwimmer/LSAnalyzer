using ClosedXML.Excel;
using LSAnalyzerAvalonia.IPlugins;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerDataReaderXlsx;

public class DataReaderXlsx : IDataReaderPlugin
{
    public string Name => "Microsoft Excel (XLSX)";

    public Matrix<double> ReadDataFile(string path)
    {
        using XLWorkbook workbook = new();
        workbook.AddWorksheet("new");
        
        return Matrix<double>.Build.Random(3, 4);
    }
}