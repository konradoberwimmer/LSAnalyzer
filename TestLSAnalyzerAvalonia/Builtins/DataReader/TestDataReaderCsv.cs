using Avalonia.Controls;
using LSAnalyzerAvalonia.Builtins.DataReader;

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
}