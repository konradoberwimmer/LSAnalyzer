using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.IPlugins.ViewModels;

namespace LSAnalyzerDataReaderXlsx.ViewModels;

public class DataReaderXlsxViewModel : ObservableObject, ICompletelyFilled
{
    public bool IsCompletelyFilled => true;
}