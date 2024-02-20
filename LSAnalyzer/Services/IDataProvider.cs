using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public interface IDataProvider
    {
        IDataProviderConfiguration Configuration { get; set; }

        bool TestProvider();

        bool InstallDependencies();
    }
}
