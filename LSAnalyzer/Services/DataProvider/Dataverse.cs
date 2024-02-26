using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Models.DataProviderConfiguration;
using RDotNet;
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

            var success = _rservice.Execute($$"""
                Sys.setenv(DATAVERSE_SERVER = "{{ dataverseConfiguration.Url }}");
                Sys.setenv(DATAVERSE_KEY = "{{ dataverseConfiguration.ApiToken }}")
                dataverse1 <- dataverse::get_dataverse(dataverse::dataverse_search("*", type = "dataverse")[1, "identifier"])
                """);

            return new() { IsSuccess = success, Message = success ? "Data provider works" : "Data provider not working " };
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
                """);

            if (format == "tsv")
            {
                success = success && _rservice.Execute($$"""
                    {{objectName}} <- dataverse::get_dataframe_by_name(
                        filename = "{{fileName}}",
                        dataset = "{{dataset}}",
                        original = FALSE)
                    """);
            } else if (format == "spss") {
                success = success && _rservice.Execute($$"""
                    {{objectName}} <- dataverse::get_dataframe_by_name(
                        filename = "{{fileName}}",
                        dataset = "{{dataset}}",
                        .f = function(file) { return(foreign::read.spss(file, use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)) },
                        original = TRUE)
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
            if (string.IsNullOrWhiteSpace(values.File) || string.IsNullOrWhiteSpace(values.Dataset))
            {
                return new() { IsSuccess = false, Message = "Missing filename or dataset" };
            }
            
            if (Configuration is not DataverseConfiguration dataverseConfiguration)
            {
                return new() { IsSuccess = false, Message = "Mismatch between data provider configuration and service" };
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
                if (string.IsNullOrWhiteSpace(values.File) || string.IsNullOrWhiteSpace(values.Dataset))
                {
                    return new();
                }

                if (Configuration is not DataverseConfiguration dataverseConfiguration)
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
                if (string.IsNullOrWhiteSpace(values.File) || string.IsNullOrWhiteSpace(values.Dataset))
                {
                    return new();
                }

                if (Configuration is not DataverseConfiguration dataverseConfiguration)
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
