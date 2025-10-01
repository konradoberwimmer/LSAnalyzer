using System.Globalization;
using LSAnalyzer.ViewModels.ValueConverter;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestIntegerToStringLenient
{
    [Theory, ClassData(typeof(TestCasesConvert))]
    public void TestConvert(int integer, string expected)
    {
        IntegerToStringLenient converter = new();
        Assert.Equal(converter.Convert(integer, typeof(string), null, CultureInfo.InvariantCulture), expected);
    }
    
    [Fact]
    public void TestConvertOnNonInteger()
    {
        IntegerToStringLenient converter = new();
        Assert.Equal(converter.Convert(new DateTime(), typeof(string), null, CultureInfo.InvariantCulture), "0");
    }

    class TestCasesConvert : TheoryData<int, string>
    {
        public TestCasesConvert()
        {
            Add(0, "0");
            Add(17, "17");
            Add(-2, "-2");
        }
    }
    
    [Theory, ClassData(typeof(TestCasesConvertBack))]
    public void TestConvertBack(string myString, int expected)
    {
        IntegerToStringLenient converter = new();
        Assert.Equal(converter.ConvertBack(myString, typeof(int), null, CultureInfo.InvariantCulture), expected);
    }

    class TestCasesConvertBack : TheoryData<string, int>
    {
        public TestCasesConvertBack()
        {
            Add("0", 0);
            Add("17", 17);
            Add("-2", -2);
            Add("xy", 0);
        }
    }
}