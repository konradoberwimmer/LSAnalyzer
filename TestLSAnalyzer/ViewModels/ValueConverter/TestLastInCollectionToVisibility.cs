using System.Globalization;
using System.Windows;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.ValueConverter;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestLastInCollectionToVisibility
{
    [Fact]
    public void TestConvert()
    {
        LastInCollectionToVisibility converter = new();
        
        Assert.Equal(Visibility.Hidden, converter.Convert([], typeof(Visibility), false, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Hidden, converter.Convert([1], typeof(Visibility), false, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert([1, new List<int> [2]], typeof(Visibility), true, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Hidden, converter.Convert([1, new List<int> { 1, 2 } ], typeof(Visibility), false, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert([2, new List<int> { 1, 2 } ], typeof(Visibility), false, CultureInfo.InvariantCulture));

        Variable variable = new Variable(2, "y");
        Assert.Equal(Visibility.Visible, converter.Convert([variable, new List<Variable> { new(1, "x"), variable }], typeof(Visibility), false, CultureInfo.InvariantCulture));
    }
}