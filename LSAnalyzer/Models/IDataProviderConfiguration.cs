using LSAnalyzer.Models.DataProviderConfiguration;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    [JsonDerivedType(typeof(DataverseConfiguration), typeDiscriminator: "dataverse")]
    public interface IDataProviderConfiguration : IChangeTracking
    {
        int Id { get; set; }
        string Name { get; set; }

        IDataProvider CreateService(IServiceProvider serviceProvider);

        IDataProviderViewModel GetViewModel(IServiceProvider serviceProvider, Configuration configuration);
    }
}
