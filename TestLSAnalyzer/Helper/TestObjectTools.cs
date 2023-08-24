using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.Helper
{
    public class TestObjectTools
    {
        [Fact]
        public void TestPublicInstancePropertiesEqual()
        {
            DatasetType dst1 = new DatasetType();
            DatasetType dst2 = new DatasetType(dst1);

            Assert.True(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "Errors" }));

            dst1.FayFac = 2;

            Assert.False(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "Errors" }));

            dst2.FayFac = 2;

            Assert.True(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "Errors" }));
        }
    }
}
