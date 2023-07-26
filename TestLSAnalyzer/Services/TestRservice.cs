using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Services
{
    public class TestRservice
    {
        [Fact]
        public void TestConnect()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
        }
    }
}
