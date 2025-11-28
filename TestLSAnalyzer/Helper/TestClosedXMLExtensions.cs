using System.Data;
using ClosedXML.Excel;
using LSAnalyzer.Helper;

namespace TestLSAnalyzer.Helper;

public class TestClosedXMLExtensions
{
    [Fact]
    public void TestAddWorksheetDataTable()
    {
        DataTable table = new("styles");
        table.Columns.Add("col1",  typeof(string));
        table.Columns.Add("col2",  typeof(double));
        table.Rows.Add(["A", 1.2]);
        table.Rows.Add(["B1", double.PositiveInfinity]);
        table.Rows.Add(["B2", double.NegativeInfinity]);
        table.Rows.Add(["C", double.NaN]);

        XLWorkbook workbook = new();
        
        workbook.AddWorksheetDataTable(table);
        
        Assert.Equal(1, workbook.Worksheets.Count);
        Assert.Equal("styles", workbook.Worksheets.First().Name);
        Assert.Equal(5, workbook.Worksheets.First().RowsUsed().Count());
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.First().Cell("B3").Value);
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.First().Cell("B4").Value);
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.First().Cell("B5").Value);
        Assert.Single(workbook.Worksheets.First().Tables);

        table.TableName = "no styles";
        workbook.AddWorksheetDataTable(table, false);
        
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("no styles", workbook.Worksheets.Last().Name);
        Assert.Equal(5, workbook.Worksheets.Last().RowsUsed().Count());
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.Last().Cell("B3").Value);
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.Last().Cell("B4").Value);
        Assert.Equal(XLCellValue.FromObject(DBNull.Value), workbook.Worksheets.Last().Cell("B5").Value);
        Assert.Empty(workbook.Worksheets.Last().Tables);
    }
}