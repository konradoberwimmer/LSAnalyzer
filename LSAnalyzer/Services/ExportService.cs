using System;
using System.Collections.Generic;
using System.IO;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;

namespace LSAnalyzer.Services;

public class ExportService : IExportService
{
    public List<string> AllFileNames(ExportOptions options, Analysis analysis)
    {
        var path = Path.GetDirectoryName(options.FileName);
        var baseFileName = Path.GetFileNameWithoutExtension(options.FileName);
        var extension = Path.GetExtension(options.FileName);
        
        if (path is null)
        {
            throw new NotImplementedException();
        }
        
        return options.ExportType.Name switch
        {
            "excelWithStyles" or "excelWithoutStyles" or "csvMainTable" => [options.FileName],
            "csvMultiple" => analysis switch
                {
                    AnalysisUnivar or AnalysisPercentiles or AnalysisLinreg or AnalysisLogistReg => [ 
                        options.FileName, 
                        Path.Combine(path, baseFileName + "_meta" + extension)
                    ],
                    AnalysisFreq or AnalysisMeanDiff or AnalysisCorr => [
                        options.FileName, 
                        analysis switch
                        {
                            AnalysisFreq => Path.Combine(path, baseFileName + "_bivariate" + extension),
                            AnalysisMeanDiff => Path.Combine(path, baseFileName + "_anova" + extension),
                            AnalysisCorr => Path.Combine(path, baseFileName + "_covariance" + extension),
                            _ => throw new NotImplementedException()
                        },
                        Path.Combine(path, baseFileName + "_meta" + extension)
                    ],
                    _ => throw new NotImplementedException()
                },
            _ => throw new NotImplementedException()
        };
    }
}