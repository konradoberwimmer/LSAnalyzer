using LSAnalyzer.Helper;
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

        DataProviderTestResults TestProvider();

        bool InstallDependencies();

        DataProviderTestResults TestFileAccess(dynamic values);

        bool LoadFileIntoGlobalEnvironment(dynamic values);

        List<Variable> GetDatasetVariables(dynamic values);
    }
}
