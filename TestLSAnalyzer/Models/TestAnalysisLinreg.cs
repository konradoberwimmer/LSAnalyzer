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
                        new(1, "x1"),
                        new(2, "x2"),
                    },
                Dependent = new(3, "y"),
            };

            Assert.Equal("Linear regression (y ~ 1 + x1 + x2 - BIST-UE)", analysisLinreg.ShortInfo);

            analysisLinreg.WithIntercept = false;
            
            Assert.Equal("Linear regression (y ~ 0 + x1 + x2 - BIST-UE)", analysisLinreg.ShortInfo);
        }
        
        [Fact]
        public void TestShortInfoWithGroupBy()
        {
            AnalysisLinreg analysisLinreg = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                {
                    new(1, "x1"),
                    new(2, "x2"),
                },
                GroupBy = new()
                {
                    new(3, "cat"),
                },
                Dependent = new(3, "y"),
            };

            Assert.Equal("Linear regression (y ~ 1 + x1 + x2 by cat - BIST-UE)", analysisLinreg.ShortInfo);
        }
        
        [Fact]
        public void TestShortInfoForwardBackwardRegression()
        {
            AnalysisLinreg analysisLinreg = new(new() { DatasetType = new() { Name = "BIST-UE", Weight = "wgt" } })
            {
                Vars = new()
                {
                    new(1, "x1"),
                    new(2, "x2"),
                },
                Dependent = new(3, "y"),
                Sequence = AnalysisRegression.RegressionSequence.Forward,
            };

            Assert.Equal("Linear regression forward (y ~ 1 + x1 + x2 - BIST-UE)", analysisLinreg.ShortInfo);

            analysisLinreg.Sequence = AnalysisRegression.RegressionSequence.Backward;
            
            Assert.Equal("Linear regression backward (y ~ 1 + x1 + x2 - BIST-UE)", analysisLinreg.ShortInfo);
        }
    }
}
