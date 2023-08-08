using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestDictionaryHasElementToBool
    {
        [Fact]
        public void TestConvertOnWrongObject()
        {
            var converter = new DictionaryHasElementToBool();

            Assert.Equal(false, converter.Convert("string", Type.GetType("Boolean")!, "", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("falsches", false)]
        [InlineData("richtiges", true)]
        public void TestConvert(string elementName, bool expected)
        {
            var converter = new DictionaryHasElementToBool();

            Dictionary<string, string> dictionary = new()
            {
                { "richtiges", "richtig" }
            };

            Assert.Equal(expected, converter.Convert(dictionary, Type.GetType("Boolean")!, elementName, CultureInfo.InvariantCulture));
        }
    }
}
