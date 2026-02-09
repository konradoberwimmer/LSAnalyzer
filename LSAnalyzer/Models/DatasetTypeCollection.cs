using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzer.Models;

public partial class DatasetTypeCollection : ObservableValidator
{
    [MinLength(3, ErrorMessage = "Name must have length of at least three characters!")]
    [ObservableProperty] private string _name = string.Empty;

    [Required]
    [ObservableProperty] private ObservableCollection<Entry> _entries = [];

    public partial class Entry : ObservableValidator
    {
        [ObservableProperty] private int _datasetTypeId;

        [Required(ErrorMessage = "Filename is required!")]
        [ObservableProperty] private string _fileName = string.Empty;
        
        [ObservableProperty] private string _hash = string.Empty;
    }
}