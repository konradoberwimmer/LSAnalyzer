using System.Collections.Immutable;
using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader;
using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;

namespace TestLSAnalyzerAvalonia.Builtins.DataReader;

public class TestDataReaderCsv
{
    [Fact]
    public void TestCreateView()
    {
        DataReaderCsv dataReaderCsv = new();
        
        dataReaderCsv.CreateView(typeof(string));
        
        Assert.Null(dataReaderCsv.View);
        
        dataReaderCsv.CreateView(typeof(UserControl));
        
        Assert.NotNull(dataReaderCsv.View);
        Assert.Equal(dataReaderCsv.ViewModel, ((UserControl)dataReaderCsv.View).DataContext);
        
        var savedView = dataReaderCsv.View;
        
        dataReaderCsv.CreateView(typeof(UserControl));
        
        Assert.Equal(savedView, dataReaderCsv.View);
    }
    
    [Theory, MemberData(nameof(TestReadFileHeaderData))]
    public void TestReadFileHeader(string content, string separator, string quoting, bool expectedSuccess, ImmutableList<string> expectedColumns)
    {
        DataReaderCsv dataReaderCsv = new();
        ((DataReaderCsvViewModel)dataReaderCsv.ViewModel).SeparatorCharacter = separator;
        ((DataReaderCsvViewModel)dataReaderCsv.ViewModel).QuotingCharacter = quoting;
        
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        
        var (success, columns) = dataReaderCsv.ReadFileHeader(path);
        
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedColumns, columns);
        
        File.Delete(path);
    }

    public static TheoryData<string, string, string, bool, ImmutableList<string>> TestReadFileHeaderData
    {
        get
        {
            TheoryData<string, string, string, bool, ImmutableList<string>> data = new();
            data.Add(string.Empty, ";", "\"", false, []);
            data.Add("\"h1\";\"h2\"", ";", "\"", true, [ "h1", "h2" ]);
            data.Add("h1;h2", ";", "\"", true, [ "h1", "h2" ]);
            data.Add("'h1';'h2'", ";", "'", true, [ "h1", "h2" ]);
            data.Add("h1;h2", ";", string.Empty, true, [ "h1", "h2" ]);
            data.Add("h1;h2\n1;2", ";", string.Empty, true, [ "h1", "h2" ]);
            data.Add("h1;h2;", ";", string.Empty, true, [ "h1", "h2", "" ]);
            data.Add("h1;h2;", ",", string.Empty, true, [ "h1;h2;" ]);
            data.Add("h1;h2;", ",", "'", true, [ "h1;h2;" ]);
            data.Add("'h1;h2;'", ",", "'", true, [ "h1;h2;" ]);
            data.Add("h1\th2", "\t", string.Empty, true, [ "h1", "h2" ]);
            data.Add("h1\th2", "\\\t", string.Empty, true, [ "h1", "h2" ]);
            return data;
        }
    }
}