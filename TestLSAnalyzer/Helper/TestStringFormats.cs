using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Helper
{
    public class TestStringFormats
    {
        [Theory, MemberData(nameof(GetMaxRelevantDigitsTestCases))]
        public void TestGetMaxRelevantDigits(double[] values, int maxDigits, int expected)
        {
            Assert.Equal(expected, StringFormats.getMaxRelevantDigits(values, maxDigits));
        }
        public static IEnumerable<object[]> GetMaxRelevantDigitsTestCases =>
            new List<object[]>
            {
                new object[] { new double[] { 1.000, -2.000, 3.000 }, 3, 0},
                new object[] { new double[] { 1.000, -2.100, 3.000 }, 3, 1},
                new object[] { new double[] { 1.000, -2.000, 3.030 }, 3, 2},
                new object[] { new double[] { 1.123, -2.000, 3.030 }, 3, 3},
                new object[] { new double[] { 1.200, -2.322, 3.020 }, 2, 2},
            };
    }
}
