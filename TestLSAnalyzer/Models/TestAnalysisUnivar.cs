using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestAnalysisUnivar
    {
        [Fact]
        public void TestShortInfo()
        {
            AnalysisUnivar analysisUnivar = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                {
                    new(1, "x", false),
                    new(2, "y", false),
                },
            };

            Assert.Equal("Univariate (x, y - BIST-UE)", analysisUnivar.ShortInfo);
        }
        
        [Fact]
        public void TestShortInfoWithGroups()
        {
            AnalysisUnivar analysisUnivar = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                {
                    new(1, "x", false),
                    new(2, "y", false),
                },
                GroupBy = new()
                {
                    new(3, "cat", false),
                },
            };

            Assert.Equal("Univariate (x, y by cat - BIST-UE)", analysisUnivar.ShortInfo);
        }
    }
}
