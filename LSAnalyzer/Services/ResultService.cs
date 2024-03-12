using DocumentFormat.OpenXml.Spreadsheet;
using LSAnalyzer.Models;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace LSAnalyzer.Services;

public class ResultService : IResultService
{
    public Analysis? Analysis { get; set; }

    public DataTable? CreatePrimaryTable()
    {
        if (Analysis == null)
        {
            return null;
        }

        DataTable table = new(Analysis.AnalysisName);
        foreach (var column in Analysis.TableColumns.Values)
        {
            table.Columns.Add(column);
        }

        foreach (var result in Analysis.Result)
        {
            var dataFrame = result[Analysis.PrimaryDataFrameName].AsDataFrame();
            var groupColumns = GetGroupColumns(dataFrame);

            foreach (var dataFrameRow in dataFrame.GetRows())
            {
                DataRow tableRow = table.NewRow();

                List<object?> cellValues = new();
                foreach (var column in Analysis.TableColumns.Keys)
                {
                    cellValues.Add(GetValueFromDataFrameRow(dataFrameRow, column, groupColumns, cellValues.LastOrDefault()));
                }

                tableRow.ItemArray = cellValues.ToArray();
                table.Rows.Add(tableRow);
            }
        }

        return table;
    }

    private object? GetValueFromDataFrameRow(DataFrameRow row, string value, Dictionary<string, string> groupColumns, object? lastValue = null)
    {
        if (Regex.IsMatch(value, "^groupval[0-9]*$") && groupColumns.ContainsKey(Analysis!.TableColumns[value].ColumnName))
        {
            return row[groupColumns[Analysis!.TableColumns[value].ColumnName]];
        }
        else if (Regex.IsMatch(value, "^\\$label_"))
        {
            if ((double?)lastValue == null)
            {
                return null;
            }

            var groupByVariable = value.Substring(value.IndexOf("_") + 1);
            var valueLabels = Analysis!.ValueLabels[groupByVariable];
            // TODO this is a rather ugly shortcut of getting the value that we need the label for!!!
            var posValueLabel = valueLabels["value"].AsNumeric().ToList().IndexOf((double)lastValue);

            if (posValueLabel != -1)
            {
                var valueLabel = valueLabels["label"].AsCharacter()[posValueLabel];
                return valueLabel;
            }
            else
            {
                return null;
            }
        }
        else if (Regex.IsMatch(value, "^\\$varlabel_"))
        {
            if (row["var"] is string varName && Analysis!.VariableLabels.ContainsKey(varName))
            {
                return Analysis.VariableLabels[varName];
            }
            else
            {
                return null;
            }
        }
        else if (row.DataFrame.ColumnNames.Contains(value))
        {
            return row[value];
        }
        else
        {
            return null;
        }
    }

    public static Dictionary<string, string> GetGroupColumns(DataFrame dataFrame)
    {
        Dictionary<string, string> groupColumns = new();

        var groupNameColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupvar[0-9]*$")).ToArray();
        var groupValColumns = dataFrame.ColumnNames.Where(columnName => Regex.IsMatch(columnName, "^groupval[0-9]*$")).ToArray();
        for (int i = 0; i < groupNameColumns.Length; i++)
        {
            groupColumns.Add(dataFrame[groupNameColumns[i]].AsCharacter().First(), groupValColumns[i]);
        }

        return groupColumns;
    }
}
