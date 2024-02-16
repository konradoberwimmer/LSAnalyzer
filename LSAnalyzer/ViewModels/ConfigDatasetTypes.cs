using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
            }
        }

        public List<string> UnsavedDatasetTypeNames
        {
            get
            {
                return _datasetTypes.Where(dst => dst.IsChanged).Select(dst => dst.Name).ToList();
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
                    storedDatasetTypes.OrderBy(d => d.Name).ToList().ForEach(d => {
                        d.AcceptChanges();
                        DatasetTypes.Add(d);
                    });
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
            int minAvailableDatasetTypeId = 1;
            while (DatasetTypes.Where(dst => dst.Id == minAvailableDatasetTypeId).Any())
            {
                minAvailableDatasetTypeId++;
            }

            DatasetType newDatasetType = new() { Id = minAvailableDatasetTypeId, Name = "New dataset type", JKreverse = false };

            DatasetTypes.Add(newDatasetType);
            SelectedDatasetType = newDatasetType;
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
            SelectedDatasetType.AcceptChanges();
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

            DatasetTypes.Remove(SelectedDatasetType);
            SelectedDatasetType = null;
        }

        private RelayCommand<string?> _importDatasetTypeCommand;
        public ICommand ImportDatasetTypeCommand
        {
            get
            {
                if (_importDatasetTypeCommand == null)
                    _importDatasetTypeCommand = new RelayCommand<string?>(this.ImportDatasetType);
                return _importDatasetTypeCommand;
            }
        }

        private void ImportDatasetType(string? filename)
        {
            if (filename == null)
            {
                return;
            }

            try
            {
                var fileContent = File.ReadAllText(filename);
                var newDatasetType = JsonSerializer.Deserialize<DatasetType>(fileContent);
                
                if (newDatasetType == null)
                {
                    WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid file"));
                    return;
                }

                int minAvailableDatasetTypeId = 1;
                while (DatasetTypes.Where(dst => dst.Id == minAvailableDatasetTypeId).Any())
                {
                    minAvailableDatasetTypeId++;
                }

                newDatasetType.Id = minAvailableDatasetTypeId;

                if (!newDatasetType.Validate())
                {
                    WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid dataset type"));
                    return;
                }

                _configuration.StoreDatasetType(newDatasetType);

                DatasetTypes.Add(newDatasetType);
                newDatasetType.AcceptChanges();
                SelectedDatasetType = newDatasetType;
            }
            catch (Exception)
            {
                WeakReferenceMessenger.Default.Send(new FailureImportDatasetTypeMessage("invalid file"));
            }
        }

        private RelayCommand<string?> _exportDatasetTypeCommand;
        public ICommand ExportDatasetTypeCommand
        {
            get
            {
                if (_exportDatasetTypeCommand == null)
                    _exportDatasetTypeCommand = new RelayCommand<string?>(this.ExportDatasetType);
                return _exportDatasetTypeCommand;
            }
        }

        private void ExportDatasetType(string? filename)
        {
            if (SelectedDatasetType == null || !SelectedDatasetType.Validate() || filename == null)
            {
                return;
            }

            File.WriteAllText(filename, JsonSerializer.Serialize(SelectedDatasetType));
        }
    }

    internal class FailureImportDatasetTypeMessage : ValueChangedMessage<string>
    {
        public FailureImportDatasetTypeMessage(string message) : base(message)
        {

        }
    }
}
