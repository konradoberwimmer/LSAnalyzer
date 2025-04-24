using LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;

namespace TestLSAnalyzerAvalonia.Builtins.DataReader.ViewModels;

public class TestDataReaderCsvViewModel
{
    [Fact]
    public void TestIsCompletelyFilled()
    {
        DataReaderCsvViewModel viewModel = new();
        
        Assert.True(viewModel.IsCompletelyFilled);
        
        viewModel.SeparatorCharacter = string.Empty;
        
        Assert.False(viewModel.IsCompletelyFilled);

        viewModel.QuotingCharacter = "\"";
        
        Assert.False(viewModel.IsCompletelyFilled);
    }
}