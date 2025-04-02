using System.Collections.Generic;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Services;

public class Plugins : IPlugins
{
    private List<IDataReaderPlugin> _dataReaderPlugins = [];

    public List<IDataReaderPlugin> DataReaderPlugins => _dataReaderPlugins;
    
    public void AddDataReaderPlugin(IDataReaderPlugin plugin) => _dataReaderPlugins.Add(plugin);
}