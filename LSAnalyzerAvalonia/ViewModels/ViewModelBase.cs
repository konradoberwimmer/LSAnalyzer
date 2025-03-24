﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzerAvalonia.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] protected bool showMessage = false;

    [ObservableProperty] protected string message = string.Empty;
}