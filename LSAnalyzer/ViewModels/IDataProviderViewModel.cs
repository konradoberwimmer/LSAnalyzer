using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels
{
    public interface IDataProviderViewModel
    {
        public string ProviderName { get; }

        public string? FileInformation {  get; }
        
        public string SerializeFileInformation();

        public Dictionary<string, object> GetUsageAttributes();

        public SelectAnalysisFile ParentViewModel { get; set; }

        public bool IsConfigurationReady { get; }

        public bool LoadDataForUsage();

        public List<Variable> LoadDataTemporarilyAndGetVariables();
    }
}
