using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisConfiguration
    {
        public string? FileName { get; set; }
        public DatasetType? DatasetType { get; set; }
        public bool? ModeKeep { get; set; }

        public bool HasSystemVariable(string name)
        {
            return DatasetType?.HasSystemVariable(name) ?? false;
        }
    }
}
