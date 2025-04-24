using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzerAvalonia.IPlugins.ViewModels;

namespace LSAnalyzerAvalonia.Builtins.DataReader.ViewModels;

public partial class DataReaderCsvViewModel : ObservableObject, ICompletelyFilled
{
    [ObservableProperty] private string _separatorCharacter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
    partial void OnSeparatorCharacterChanged(string value)
    {
        OnPropertyChanged(nameof(IsCompletelyFilled));
    }
    
    [ObservableProperty] private string _quotingCharacter = string.Empty;

    public bool IsCompletelyFilled => !string.IsNullOrEmpty(SeparatorCharacter);
}