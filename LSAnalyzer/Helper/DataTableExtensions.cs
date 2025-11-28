using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LSAnalyzer.Helper;

public static class DataTableExtensions
{
    public static string ToCsvString(this DataTable dataTable, CultureInfo cultureInfo, bool useColumnNames = true)
    {
        var separator = cultureInfo.TextInfo.ListSeparator[0];
        
        StringBuilder stringBuilder = new();

        if (useColumnNames)
        {
            var columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => "\"" + column.ColumnName + "\"");
            stringBuilder.AppendLine(string.Join(separator, columnNames));
        }

        foreach (DataRow row in dataTable.Rows)
        {
            var fields = row.ItemArray.Select(field => field switch
            {
                double.NaN => string.Empty,
                double aDouble when double.IsInfinity(aDouble) => string.Empty,
                double aDouble => aDouble.ToString(cultureInfo),
                string aString => "\"" + aString.Replace("\"", "\"\"") + "\"",
                _ =>  field?.ToString()?.Replace("\"", "\"\"") ?? string.Empty
            });
            stringBuilder.AppendLine(string.Join(separator, fields));
        }
        
        return stringBuilder.ToString();
    }
}