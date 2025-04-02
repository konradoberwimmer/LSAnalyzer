using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin : IPluginCommons
{
    public new static PluginTypes PluginType => PluginTypes.DataReader;
    
    public Matrix<double> ReadDataFile(string path);
}