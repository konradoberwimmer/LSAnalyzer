using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestEmptyVariableCollection
    {
        [Theory, MemberData(nameof(ConvertTestData))]
        public void TestConvert(object testObject, bool expected)
        {
            EmptyVariableCollection emptyVariableCollectionConverter = new();

            Assert.Equal(expected, emptyVariableCollectionConverter.Convert(testObject, Type.GetType("bool")!, "", CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> ConvertTestData =>
            new List<object[]>()
            {
                new object[] { new Variable(1, "Ulldrael", true), false },
                new object[] { new ObservableCollection<Variable>(), true },
                new object[] { new ObservableCollection<Variable>() { new(1, "juhu", false) }, false },
            };
    }
}
