using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin
{
    public string Name { get; }
    
    public Matrix<double> ReadDataFile(string path);
}