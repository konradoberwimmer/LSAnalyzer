using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Helper
{
    public static class ClosedXMLExtensions
    {
        // NaN- and Inf-save call to XLWorkbook.AddWorksheet() ...
        public static void AddWorksheetDataTable(this XLWorkbook workbook, DataTable table, bool useStyles = true)
        {
            DataTable tableCopy = table.Copy();

            for (int i = 0; i < tableCopy.Rows.Count; i++)
            {
                for (int j = 0; j < tableCopy.Columns.Count; j++)
                {
                    var cellValue = tableCopy.Rows[i][j];
                    if (cellValue is double doubleValue && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)))
                    {
                        tableCopy.Rows[i][j] = DBNull.Value;
                    }
                }
            }

            if (useStyles)
            {
                workbook.AddWorksheet(tableCopy);
            }
            else
            {
                var worksheet = workbook.AddWorksheet(tableCopy.TableName);
                worksheet.Cell("A1").InsertData(tableCopy.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray(), true);
                worksheet.Cell("A2").InsertData(tableCopy);
            }
        }
    }
}
