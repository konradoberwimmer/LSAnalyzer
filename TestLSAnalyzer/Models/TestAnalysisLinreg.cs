using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestAnalysisLinreg
    {
        [Fact]
        public void TestShortInfo()
        {
            AnalysisLinreg analysisLinreg = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                    {
                        new(1, "x1", false),
                        new(2, "x2", false),
                    },
                Dependent = new(3, "y", false),
            };

            Assert.Equal("Linear regression (y by x1, x2 - BIST-UE; wgt)", analysisLinreg.ShortInfo);
        }
    }
}
