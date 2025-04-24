using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using LSAnalyzerAvalonia.Helper;

namespace LSAnalyzerAvalonia.Models;

public static class DatasetTypeListExtensions
{
    public static List<(DatasetType datasetType, int priority)> GetCompatibleDatasetTypes(this IEnumerable<DatasetType> datasetTypes, ImmutableList<string> columnNames)
    {
        List<(DatasetType, int)> possibleDatasetTypes = [];

        foreach (var datasetType in datasetTypes)
        {
            var priority = 0;
                
            var weightVariables = datasetType.Weight.Split(";").Select(wgt => wgt.Trim());
            if (!weightVariables.All(wgt => columnNames.Any(var => var.Trim() == wgt))) continue;
            
            if (!string.IsNullOrWhiteSpace(datasetType.IDvar) && columnNames.All(var => var != datasetType.IDvar)) continue;
            if (!string.IsNullOrWhiteSpace(datasetType.MIvar) && columnNames.All(var => var != datasetType.MIvar)) continue;

            var foundAllNecessaryPvVars = 
                datasetType.PVvarsList.
                    Where(pvVar => pvVar.Mandatory).
                    All(mandatoryPvVar => columnNames.Count(var => Regex.IsMatch(var, StringFormats.EncapsulateRegex(mandatoryPvVar.Regex, datasetType.AutoEncapsulateRegex)!)) == datasetType.NMI);
            if (!foundAllNecessaryPvVars) continue;

            if (!string.IsNullOrWhiteSpace(datasetType.RepWgts))
            {
                if (!columnNames.Any(var => Regex.IsMatch(var, StringFormats.EncapsulateRegex(datasetType.RepWgts, datasetType.AutoEncapsulateRegex)!)))
                {
                    continue;
                } 
                
                priority++;
            }

            if (!string.IsNullOrWhiteSpace(datasetType.JKzone) && columnNames.All(var => var != datasetType.JKzone)) continue;
            if (!string.IsNullOrWhiteSpace(datasetType.JKrep) && columnNames.All(var => var != datasetType.JKrep)) continue;

            possibleDatasetTypes.Add((datasetType, priority));
        }

        return possibleDatasetTypes;
    }
}