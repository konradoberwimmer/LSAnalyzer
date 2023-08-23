using LSAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Helper
{
    public class TestCombinations
    {
        [Fact]
        public void TestGetCombinations()
        {
            List<int> values = new() { 1, 2, 3, 4 };

            var combinations = Combinations.GetCombinations(values);

            Assert.NotEmpty(combinations);
            Assert.Equal(16, combinations.Count());
        }
    }
}
