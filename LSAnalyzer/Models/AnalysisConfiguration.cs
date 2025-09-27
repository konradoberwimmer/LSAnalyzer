using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models
{
    public class AnalysisConfiguration
    {
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public DatasetType? DatasetType { get; set; }
        public bool? ModeKeep { get; set; }

        public AnalysisConfiguration() { }

        public AnalysisConfiguration (AnalysisConfiguration analysisConfiguration)
        {
            FileName = analysisConfiguration.FileName;
            DatasetType = analysisConfiguration.DatasetType != null ? new(analysisConfiguration.DatasetType) : null;
            ModeKeep = analysisConfiguration.ModeKeep;
        }

        public bool HasSystemVariable(string name)
        {
            return DatasetType?.HasSystemVariable(name) ?? false;
        }

        public List<string> GetRegexNecessaryVariables()
        {
            return DatasetType?.GetRegexNecessaryVariables() ?? new();
        }

        public bool IsEqual(AnalysisConfiguration analysisConfiguration)
        {
            return 
                ObjectTools.PublicInstancePropertiesEqual(this, analysisConfiguration, [ "DatasetType" ]) &&
                DatasetType != null && analysisConfiguration.DatasetType != null &&
                ObjectTools.PublicInstancePropertiesEqual(DatasetType, analysisConfiguration.DatasetType, [ "Errors", "IsChanged", "PVvarsList" ]) &&
                DatasetType.PVvarsList.ElementObjectsEqual(analysisConfiguration.DatasetType.PVvarsList, [ "Errors" ]);
        }
    }
}
