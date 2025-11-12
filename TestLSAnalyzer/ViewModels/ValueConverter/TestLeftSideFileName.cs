using System.Globalization;
using LSAnalyzer.ViewModels.ValueConverter;

namespace TestLSAnalyzer.ViewModels.ValueConverter;

public class TestLeftSideFileName
{
    [Theory, ClassData(typeof(TestLeftSideFileNameData))]
    public void TestConvert(string? fileName, string expected)
    {
        LeftSideFileName converter = new();
        Assert.Equal(converter.Convert(fileName, typeof(string), null, CultureInfo.InvariantCulture), expected);
    }

    public class TestLeftSideFileNameData : TheoryData<string?, string?>
    {
        public TestLeftSideFileNameData() : base()
        {
            Add(null, null);
            Add(".", ".");
            Add("anywhere.txt", "anywhere.txt");
            Add("directory/somewhere.txt", "somewhere.txt - directory");
            Add("""{"File":"myFile.tab","Dataset":"doi:12345/689"}""", """{"File":"myFile.tab","Dataset":"doi:12345/689"}""");
            Add("""[ Provider: myProvider, File: myFile.tab, Dataset: doi:12345/689 ]""", """[ Provider: myProvider, File: myFile.tab, Dataset: doi:12345/689 ]""");
            Add(@"C:\myProject\myData\data.sav", @"data.sav - C:\myProject\myData");
        }
    }
}