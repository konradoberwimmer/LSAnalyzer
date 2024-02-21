using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.ViewModels.DataProvider
{
    public partial class Dataverse : ObservableObject, IDataProviderViewModel
    {
        [ObservableProperty]
        private string _file = string.Empty;
        partial void OnFileChanged(string value)
        {
            TestResults = new();
        }

        [ObservableProperty]
        private string _dataset = string.Empty;
        partial void OnDatasetChanged(string value)
        {
            TestResults = new();
        }

        [ObservableProperty]
        private DataProviderTestResults _testResults = new();

        [RelayCommand]
        public void TestFileAccess()
        {
            if (string.IsNullOrWhiteSpace(File) || string.IsNullOrWhiteSpace(Dataset))
            {
                return;
            }

            TestResults = new() { IsSuccess = false, Message = "Did not work but did not even try ;-)" };
        }
    }
}
