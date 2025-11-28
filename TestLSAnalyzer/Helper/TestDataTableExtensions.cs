using System.Data;
using System.Globalization;
using LSAnalyzer.Helper;

namespace TestLSAnalyzer.Helper;

public class TestDataTableExtensions
{
    [Fact]
    public void TestToCsvString()
    {
        DataTable table = new();
        table.Columns.Add("col1",  typeof(string));
        table.Columns.Add("col2",  typeof(double));
        table.Rows.Add(["A", 1.2]);
        table.Rows.Add(["""B"1""", double.PositiveInfinity]);
        table.Rows.Add(["B2", double.NegativeInfinity]);
        table.Rows.Add(["C", double.NaN]);

        var csvStringDE = table.ToCsvString(CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("""
                     "col1";"col2"
                     "A";1,2
                     "B""1";
                     "B2";
                     "C";
                     
                     """, csvStringDE);
        
        var csvStringEN = table.ToCsvString(CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("""
                     "col1","col2"
                     "A",1.2
                     "B""1",
                     "B2",
                     "C",
                     
                     """, csvStringEN);
        
        var csvStringNoColumnNames = table.ToCsvString(CultureInfo.GetCultureInfo("en-US"), false);
        Assert.Equal("""
                     "A",1.2
                     "B""1",
                     "B2",
                     "C",

                     """, csvStringNoColumnNames);
    }
}