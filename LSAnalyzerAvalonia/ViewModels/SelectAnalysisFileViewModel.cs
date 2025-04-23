using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSAnalyzerAvalonia.IPlugins;
using LSAnalyzerAvalonia.Models;
using LSAnalyzerAvalonia.Services;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class SelectAnalysisFileViewModel : ViewModelBase
{
    [ObservableProperty] private string _filePath = string.Empty;
    partial void OnFilePathChanged(string value)
    {
        var extension = value.Contains('.') ? value[(value.LastIndexOf('.') + 1)..] : string.Empty;

        if (!string.IsNullOrEmpty(extension))
        {
            foreach (var dataReaderPlugin in DataReaderPlugins)
            {
                if (dataReaderPlugin.SuggestedFileExtensions.Any(sfe => sfe.ToLower().Equals(extension.ToLower())))
                {
                    SelectedDataReaderPlugin = dataReaderPlugin;
                    break;
                }
            }
        }
        
        OnPropertyChanged(nameof(IsReadyToLoad));
    }
    
    [ObservableProperty] private ObservableCollection<IDataReaderPlugin> _dataReaderPlugins = [];
    
    [ObservableProperty] private IDataReaderPlugin? _selectedDataReaderPlugin;
    partial void OnSelectedDataReaderPluginChanged(IDataReaderPlugin? value)
    {
        OnPropertyChanged(nameof(IsReadyToLoad));
    }

    [ObservableProperty] private bool _convertNonNumerical = true;
    
    [ObservableProperty] private ObservableCollection<DatasetType> _datasetTypes = [];
    
    [ObservableProperty] private DatasetType? _selectedDatasetType;
    partial void OnSelectedDatasetTypeChanged(DatasetType? value)
    {
        SelectedWeight = null;
        Weights = new ObservableCollection<string>(value?.Weight.Split(";").Select(w => w.Trim()) ?? []);
        SelectedWeight = Weights.FirstOrDefault() ?? string.Empty;
        
        OnPropertyChanged(nameof(IsReadyToLoad));
    }

    [ObservableProperty] private ObservableCollection<string> _weights = [];

    [ObservableProperty] private string? _selectedWeight;
    partial void OnSelectedWeightChanged(string? value)
    {
        OnPropertyChanged(nameof(IsReadyToLoad));
    }
    
    public bool IsReadyToLoad => 
        !string.IsNullOrEmpty(FilePath) && 
        (SelectedDataReaderPlugin?.ViewModel.IsCompletelyFilled ?? false) &&
        SelectedDatasetType != null &&
        !string.IsNullOrEmpty(SelectedWeight);
    
    [ExcludeFromCodeCoverage]
    public SelectAnalysisFileViewModel() // design-time only parameterless constructor
    {
        
    }

    public SelectAnalysisFileViewModel(List<IDataReaderPlugin> builtins, Services.IPlugins plugins, Type uiType, IAppConfiguration appConfiguration)
    {
        DatasetTypes = new ObservableCollection<DatasetType>(appConfiguration.GetStoredDatasetTypes() ?? []);
        
        foreach (var plugin in builtins.Concat(plugins.DataReaderPlugins))
        {
            plugin.CreateView(uiType);
            plugin.ViewModel.PropertyChanged += ListenSelectedDataReaderPluginViewModel;
            DataReaderPlugins.Add(plugin);
        }
    }

    private void ListenSelectedDataReaderPluginViewModel(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsReadyToLoad));
    }

    [RelayCommand]
    private void GuessDatasetType()
    {
        Message = "Not implemented yet.";
        ShowMessage = true;
    }

    [RelayCommand]
    private void LoadData()
    {
        if (!IsReadyToLoad) return;
        
        Message = "Not implemented yet.";
        ShowMessage = true;
    }

    public void UnregisterPluginListeners()
    {
        foreach (var plugin in DataReaderPlugins)
        {
            plugin.ViewModel.PropertyChanged -= ListenSelectedDataReaderPluginViewModel;
        }
    }
}