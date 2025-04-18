using LSAnalyzerAvalonia.IPlugins.ViewModels;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin : IPluginCommons
{
    public ICompletelyFilled ViewModel { get; }
    
    public object? View { get; }
    
    public void CreateView(Type uiType);
    
    public Matrix<double> ReadDataFile(string path);
}