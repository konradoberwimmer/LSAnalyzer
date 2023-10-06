using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestAnalysisPercentiles
    {
        [Theory]
        [InlineData(false, false, false, "Percentiles (no interpolation; x, y - BIST-UE; wgt)", "Without standard errors (BIFIE.ecdf) and without interpolation (quanttype = 2)")]
        [InlineData(false, true, false, "Percentiles (interpolation; x, y - BIST-UE; wgt)", "Without standard errors (BIFIE.ecdf) and with interpolation (quanttype = 1)")]
        [InlineData(true, false, false, "Percentiles (no interpolation; x, y - BIST-UE; wgt)", "With standard errors and without interpolation (mimic BIFIE.ecdf, quanttype = 2)")]
        [InlineData(true, true, false, "Percentiles (interpolation; x, y - BIST-UE; wgt)", "With standard errors and with interpolation (mimic BIFIE.ecdf, quanttype = 1)")]
        [InlineData(true, false, true, "Percentiles (like IDBanalyzer; x, y - BIST-UE; wgt)", "With standard errors and without interpolation (mimic IDBanalyzer)")]
        public void TestShortAndPercentileInfo(bool calculateSE, bool useInterpolation, bool mimicIdbAnalyzer, string expectedShortInfo, string expectedPercentileInfo)
        {
            AnalysisPercentiles analysisPercentiles = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                {
                    new(1, "x", false),
                    new(2, "y", false),
                },
                CalculateSE = calculateSE,
                UseInterpolation = useInterpolation,
                MimicIdbAnalyzer = mimicIdbAnalyzer,
            };

            Assert.Equal(expectedShortInfo, analysisPercentiles.ShortInfo);
            Assert.Equal(expectedPercentileInfo, analysisPercentiles.PercentileTypeInfo);
        }
    }
}
