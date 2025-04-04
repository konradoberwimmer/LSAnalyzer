using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin : IPluginCommons
{
    public Matrix<double> ReadDataFile(string path);
}