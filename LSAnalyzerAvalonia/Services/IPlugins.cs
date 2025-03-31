using System.Collections.Generic;
using LSAnalyzerAvalonia.IPlugins;

namespace LSAnalyzerAvalonia.Services;

public interface IPlugins
{
    public List<IDataReaderPlugin> DataReaderPlugins { get; }
}