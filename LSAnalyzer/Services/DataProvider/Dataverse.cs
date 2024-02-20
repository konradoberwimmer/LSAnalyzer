using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services.DataProvider
{
    public class Dataverse : IDataProvider
    {
        private readonly Rservice _rservice;

        public IDataProviderConfiguration Configuration { get; set; } = null!;

        public Dataverse(Rservice rservice)
        {
            _rservice = rservice;
        }

        public bool TestProvider()
        {
            if (Configuration is not DataverseConfiguration dataverseConfiguration)
            {
                return false;
            }

            if (!_rservice.CheckNecessaryRPackages("dataverse"))
            {
                WeakReferenceMessenger.Default.Send(new MissingRPackageMessage("dataverse") { DataProvider = this });
                return false;
            }

            return _rservice.Execute($$"""
                Sys.setenv(DATAVERSE_SERVER = "{{ dataverseConfiguration.Url }}");
                Sys.setenv(DATAVERSE_KEY = "{{ dataverseConfiguration.ApiToken }}")
                dataverse1 <- dataverse::get_dataverse(dataverse::dataverse_search("*", type = "dataverse")[1, "identifier"])
                """);
        }

        public bool InstallDependencies()
        {
            return _rservice.InstallNecessaryRPackages("dataverse");
        }
    }
}
