using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin : IPluginCommons
{
    public object ViewModel { get; }
    
    public object? View { get; }
    
    public Type ViewType => View?.GetType() ?? typeof(object);
    
    public void CreateView(Type uiType);
    
    public Matrix<double> ReadDataFile(string path);
}