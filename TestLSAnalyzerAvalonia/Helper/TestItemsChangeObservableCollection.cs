using System.Collections.Specialized;
using LSAnalyzerAvalonia.Models;

namespace TestLSAnalyzerAvalonia.Helper;

public class TestItemsChangeObservableCollection
{
    [Fact]
    public void TestClearItems()
    {
        var datasetTypeWithPVs = DatasetType.CreateDefaultDatasetTypes().First(dst => dst.Name == "PIAAC");
        
        var exception = Record.Exception(() => { datasetTypeWithPVs.PVvarsList.Clear(); });
        Assert.Null(exception);
    }
    
    [Fact]
    public void TestReplaceItems()
    {
        var datasetTypeWithPVs = DatasetType.CreateDefaultDatasetTypes().First(dst => dst.Name == "PIAAC");
        
        var exception = Record.Exception(() => { datasetTypeWithPVs.PVvarsList[0] = new PlausibleValueVariable { Regex = "new", DisplayName = "new" }; });
        Assert.Null(exception);

        var collectionChanged = false;
        void NotifyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            collectionChanged = true;
        }

        datasetTypeWithPVs.PVvarsList.CollectionChanged += NotifyCollectionChanged;
        datasetTypeWithPVs.PVvarsList[0].DisplayName = "newest";
        
        Assert.True(collectionChanged);
    }
}