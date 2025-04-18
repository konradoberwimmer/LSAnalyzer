using System.ComponentModel;

namespace LSAnalyzerAvalonia.IPlugins.ViewModels;

public interface ICompletelyFilled : INotifyPropertyChanged
{
    public bool IsCompletelyFilled { get; }
}