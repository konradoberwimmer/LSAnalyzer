using System.Collections.Immutable;
using LSAnalyzerAvalonia.IPlugins.ViewModels;
using MathNet.Numerics.LinearAlgebra;

namespace LSAnalyzerAvalonia.IPlugins;

public interface IDataReaderPlugin : IPluginCommons
{
    public List<string> SuggestedFileExtensions { get; }
    
    public ICompletelyFilled ViewModel { get; }
    
    public object? View { get; }
    
    public void CreateView(Type uiType);
    
    public (bool success, ImmutableList<string> columns) ReadFileHeader(string path);
    
    public Matrix<double> ReadDataFile(string path);
}