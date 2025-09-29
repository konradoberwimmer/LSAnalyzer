using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public DataProviderTestResults TestProvider()
        {
            if (Configuration is not DataverseConfiguration dataverseConfiguration)
            {
                return new() { IsSuccess = false, Message = "Mismatch between data provider configuration and service" };
            }

            if (!_rservice.CheckNecessaryRPackages("dataverse"))
            {
                WeakReferenceMessenger.Default.Send(new MissingRPackageMessage("dataverse") { DataProvider = this });
                return new() { IsSuccess = false, Message = "Missing R package 'dataverse'" };
            }

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("X-Dataverse-key", dataverseConfiguration.ApiToken);
            var testTokenEndpoint = dataverseConfiguration.Url + "/api/users/token";
            HttpResponseMessage response;
            
            try
            {
                response = client.GetAsync(testTokenEndpoint).Result;
            }
            catch
            {
                return new() { IsSuccess = false, Message = "Data provider not working: URL wrong?" };
            }

            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized: return new() { IsSuccess = false, Message = "Data provider not working: API token wrong?" };
                    case HttpStatusCode.NotFound: return new() { IsSuccess = false, Message = "Data provider not working: API token wrong?" };
                    default: return new() { IsSuccess = false, Message = "Data provider not working: URL wrong?" }; 
                }
            }
            
            return new() { IsSuccess = true, Message = "Data provider works" };
        }

        public bool InstallDependencies()
        {
            return _rservice.InstallNecessaryRPackages("dataverse");
        }

        private bool FetchFile(DataverseConfiguration dataverseConfiguration, string objectName, string fileName, string dataset, string format)
        {
            bool success = true;

            success = success && _rservice.Execute($$"""
                Sys.setenv(DATAVERSE_SERVER = "{{dataverseConfiguration.Url}}");
                Sys.setenv(DATAVERSE_KEY = "{{dataverseConfiguration.ApiToken}}")
                Sys.setenv(DATAVERSE_USE_CACHE = "none")
                
                if (utils::packageVersion("dataverse") == "0.3.15") {
                  library(dataverse)
                
                  api_get_impl_fixed <- function (url, ..., key = NULL, as = "text") 
                  {
                    if (!is.null(key)) 
                      key <- httr::add_headers("X-Dataverse-key" = key)
                    r <- httr::GET(url, ..., key)
                    httr::stop_for_status(r, task = httr::content(r)$message)
                    httr::content(r, as = as, encoding = "UTF-8")
                  }
                  
                  assignInNamespace("api_get_impl", api_get_impl_fixed, ns = "dataverse")
                }
                """);

            if (format == "tsv")
            {
                success = success && _rservice.Execute($$"""
                    {{objectName}} <- as.data.frame(dataverse::get_dataframe_by_name(
                        filename = "{{fileName}}",
                        dataset = "{{dataset}}",
                        original = FALSE))
                    """);
            } else if (format == "spss") {
                success = success && _rservice.Execute($$"""
                    {{objectName}} <- as.data.frame(dataverse::get_dataframe_by_name(
                        filename = "{{fileName}}",
                        dataset = "{{dataset}}",
                        .f = function(file) { return(foreign::read.spss(file, use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)) },
                        original = TRUE))
                    """);
            } else {
                return false;
            }

            success = success && _rservice.Execute($$"""
                {{objectName}}_nrow <- nrow({{objectName}})
                {{objectName}}_colnames <- colnames({{objectName}})
                """);

            return success;
        }

        public DataProviderTestResults TestFileAccess(dynamic values)
        {
            if (Configuration is not DataverseConfiguration dataverseConfiguration)
            {
                return new() { IsSuccess = false, Message = "Mismatch between data provider configuration and service" };
            }
            
            if (!ObjectTools.DoesPropertyExist(values, "File") || string.IsNullOrWhiteSpace(values.File) || !ObjectTools.DoesPropertyExist(values, "Dataset") || string.IsNullOrWhiteSpace(values.Dataset))
            {
                return new() { IsSuccess = false, Message = "Missing filename or dataset" };
            }

            if (!_rservice.CheckNecessaryRPackages("dataverse"))
            {
                WeakReferenceMessenger.Default.Send(new MissingRPackageMessage("dataverse") { DataProvider = this });
                return new() { IsSuccess = false, Message = "Missing R package 'dataverse'" };
            }

            var success = FetchFile(dataverseConfiguration, "lsanalyzer_test_file_raw", values.File, values.Dataset, values.FileFormat);

            return new() { IsSuccess = success, Message = success ? "File access works" : "File access not working" };
        }

        public List<Variable> GetDatasetVariables(dynamic values)
        {
            try
            {
                if (Configuration is not DataverseConfiguration dataverseConfiguration)
                {
                    return new();
                }

                if (!ObjectTools.DoesPropertyExist(values, "File") || string.IsNullOrWhiteSpace(values.File) || !ObjectTools.DoesPropertyExist(values, "Dataset") || string.IsNullOrWhiteSpace(values.Dataset))
                {
                    return new();
                }

                _rservice.Execute("""if (exists("lsanalyzer_some_file_raw")) rm(lsanalyzer_some_file_raw)""");

                var successLoadFile = FetchFile(dataverseConfiguration, "lsanalyzer_some_file_raw", values.File, values.Dataset, values.FileFormat);

                if (!successLoadFile)
                {
                    return new();
                }

                var variables = _rservice.Fetch("lsanalyzer_some_file_raw_colnames")?.AsCharacter();

                if (variables == null)
                {
                    return new();
                }

                List<Variable> variableList = new();
                int vv = 0;
                foreach (var variable in variables)
                {
                    variableList.Add(new(++vv, variable, false));
                }

                return variableList;
            }
            catch
            {
                return new();
            }
        }

        public bool LoadFileIntoGlobalEnvironment(dynamic values)
        {
            try
            {
                if (Configuration is not DataverseConfiguration dataverseConfiguration)
                {
                    return new();
                }

                if (!ObjectTools.DoesPropertyExist(values, "File") || string.IsNullOrWhiteSpace(values.File) || !ObjectTools.DoesPropertyExist(values, "Dataset") || string.IsNullOrWhiteSpace(values.Dataset))
                {
                    return new();
                }

                _rservice.Execute("""if (exists("lsanalyzer_dat_raw_stored")) rm(lsanalyzer_dat_raw_stored)""");

                var successLoadFile = FetchFile(dataverseConfiguration, "lsanalyzer_dat_raw_stored", values.File, values.Dataset, values.FileFormat);

                if (!successLoadFile)
                {
                    return false;
                }

                _rservice.Execute("""lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored""");

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
