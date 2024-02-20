using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models;

public class DataProviderConfigurationsList
{
    public List<IDataProviderConfiguration> DataProviders { get; set; } = new();
}
