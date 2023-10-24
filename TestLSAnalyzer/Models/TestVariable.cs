using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Models
{
    public class TestVariable
    {
        [Theory]
        [InlineData("x", null, "x")]
        [InlineData("y", "dependent variable", "y (dependent variable)")]
        public void TestInfo(string name, string? label, string expected)
        {
            Variable variable = new Variable(1, name, false);
            variable.Label = label;

            Assert.Equal(expected, variable.Info);
        }
    }
}
