using CommunityToolkit.Mvvm.Input;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSAnalyzer.ViewModels
{
    public class ConfigDatasetTypes : INotifyPropertyChanged
    {
        private Configuration _configuration;

        private ObservableCollection<DatasetType> _datasetTypes;
        public ObservableCollection<DatasetType> DatasetTypes
        {
            get => _datasetTypes;
            set
            {
                _datasetTypes = value;
                NotifyPropertyChanged(nameof(DatasetTypes));
            }
        }

        private DatasetType? _selectedDatasetType;
        public DatasetType? SelectedDatasetType
        {
            get => _selectedDatasetType;
            set
            {
                _selectedDatasetType = value;
                NotifyPropertyChanged(nameof(SelectedDatasetType));
                if (_selectedDatasetType != null && !_unsavedDatasetTypeIds.Contains(_selectedDatasetType.Id))
                {
                    _unsavedDatasetTypeIds.Add(_selectedDatasetType.Id);
                }
            }
        }

        private List<int> _unsavedDatasetTypeIds = new();
        public List<string> UnsavedDatasetTypeNames
        {
            get
            {
                var unsavedDatasetNames = new List<string>();
                foreach (var unsavedDatasetId in  _unsavedDatasetTypeIds)
                {
                    unsavedDatasetNames.Add(_datasetTypes.Where(dst => dst.Id == unsavedDatasetId).First().Name);
                }
                return unsavedDatasetNames;
            }
        }

        [ExcludeFromCodeCoverage]
        public ConfigDatasetTypes()
        {
            // design-time only parameter-less constructor
        }

        public ConfigDatasetTypes(Configuration configuration)
        {
            _configuration = configuration;
            DatasetTypes = new ObservableCollection<DatasetType>();
            try
            {
                var storedDatasetTypes = _configuration.GetStoredDatasetTypes();
                if (storedDatasetTypes != null)
                {
                    storedDatasetTypes.OrderBy(d => d.Name).ToList().ForEach(d => DatasetTypes.Add(d));
                }
            }
            catch
            {

            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private RelayCommand<object?> _newDatasetTypeCommand;
        public ICommand NewDatasetTypeCommand
        {
            get
            {
                if (_newDatasetTypeCommand == null)
                    _newDatasetTypeCommand = new RelayCommand<object?>(this.NewDatasetType);
                return _newDatasetTypeCommand;
            }
        }

        private void NewDatasetType(object? dummy)
        {
            int maxDatasetTypeId = 0;
            foreach (var datasetType in DatasetTypes)
            {
                if (datasetType.Id > maxDatasetTypeId)
                {
                    maxDatasetTypeId = datasetType.Id;
                }
            }
            DatasetTypes.Add(new() { Id = maxDatasetTypeId + 1, Name = "New dataset type", JKreverse = false });
            SelectedDatasetType = DatasetTypes.LastOrDefault();
            _unsavedDatasetTypeIds.Add(maxDatasetTypeId + 1);
        }

        private RelayCommand<object?> _saveSelectedDatasetTypeCommand;
        public ICommand SaveSelectedDatasetTypeCommand
        {
            get
            {
                if (_saveSelectedDatasetTypeCommand == null)
                    _saveSelectedDatasetTypeCommand = new RelayCommand<object?>(this.SaveSelectedDatasetType);
                return _saveSelectedDatasetTypeCommand;
            }
        }

        private void SaveSelectedDatasetType(object? dummy)
        {
            if (SelectedDatasetType == null || !SelectedDatasetType.Validate())
            {
                return;
            }

            _configuration.StoreDatasetType(SelectedDatasetType);

            if (_unsavedDatasetTypeIds.Contains(SelectedDatasetType.Id))
            {
                _unsavedDatasetTypeIds.Remove(SelectedDatasetType.Id);
            }
        }

        private RelayCommand<object?> _removeDatasetTypeCommand;
        public ICommand RemoveDatasetTypeCommand
        {
            get
            {
                if (_removeDatasetTypeCommand == null)
                    _removeDatasetTypeCommand = new RelayCommand<object?>(this.RemoveDatasetType);
                return _removeDatasetTypeCommand;
            }
        }

        private void RemoveDatasetType(object? dummy)
        {
            if (SelectedDatasetType == null)
            {
                return;
            }

            _configuration.RemoveDatasetType(SelectedDatasetType);

            if (_unsavedDatasetTypeIds.Contains(SelectedDatasetType.Id))
            {
                _unsavedDatasetTypeIds.Remove(SelectedDatasetType.Id);
            }

            DatasetTypes.Remove(SelectedDatasetType);
            SelectedDatasetType = null;
        }
    }
}
