using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LSAnalyzer.Helper;
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
    public class Subsetting : INotifyPropertyChanged
    {
        private readonly Rservice _rservice;

        private AnalysisConfiguration? _analysisConfiguration;
        public AnalysisConfiguration? AnalysisConfiguration
        {
            get => _analysisConfiguration;
            set
            {
                _analysisConfiguration = value;
                NotifyPropertyChanged(nameof(AnalysisConfiguration));

                if (AnalysisConfiguration != null)
                {
                    var currentDatasetVariables = _rservice.GetCurrentDatasetVariables(AnalysisConfiguration);
                    if (currentDatasetVariables != null)
                    {
                        ObservableCollection<Variable> newAvailableVariables = new();
                        foreach (var variable in currentDatasetVariables)
                        {
                            newAvailableVariables.Add(variable);
                        }
                        AvailableVariables = newAvailableVariables;
                    }
                }
            }
        }

        private ObservableCollection<Variable> _availableVariables;
        public ObservableCollection<Variable> AvailableVariables
        {
            get => _availableVariables;
            set
            {
                _availableVariables = value;
                NotifyPropertyChanged(nameof(AvailableVariables));
            }
        }

        private bool _isCurrentlySubsetting = false;
        public bool IsCurrentlySubsetting
        {
            get => _isCurrentlySubsetting;
        }

        private string? _subsetExpression;
        public string? SubsetExpression
        {
            get => _subsetExpression;
            set
            {
                _subsetExpression = value;
                NotifyPropertyChanged(nameof(SubsetExpression));
            }
        }

        [ExcludeFromCodeCoverage]
        public Subsetting()
        {
            // design-time-only constructor
        }

        public Subsetting(Rservice rservice)
        {
            _rservice = rservice;
            AvailableVariables = new ObservableCollection<Variable>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetCurrentSubsetting(string subsettingExpression)
        {
            SubsetExpression = subsettingExpression;
            _isCurrentlySubsetting = true;
        }

        private RelayCommand<ICloseable?> _clearSubsettingCommand;
        public ICommand ClearSubsettingCommand
        {
            get
            {
                if (_clearSubsettingCommand == null)
                    _clearSubsettingCommand = new RelayCommand<ICloseable?>(this.ClearSubsetting);
                return _clearSubsettingCommand;
            }
        }

        private void ClearSubsetting(ICloseable? window)
        {
            SubsetExpression = null;
            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));

            window?.Close();
        }

        private RelayCommand<ICloseable?> _useSubsettingCommand;
        public ICommand UseSubsettingCommand
        {
            get
            {
                if (_useSubsettingCommand == null)
                    _useSubsettingCommand = new RelayCommand<ICloseable?>(this.UseSubsetting);
                return _useSubsettingCommand;
            }
        }

        private void UseSubsetting(ICloseable? window)
        {
            WeakReferenceMessenger.Default.Send(new SetSubsettingExpressionMessage(SubsetExpression));

            window?.Close();
        }
    }

    internal class SetSubsettingExpressionMessage : ValueChangedMessage<string?>
    {
        public SetSubsettingExpressionMessage(string? subsettingExpression) : base(subsettingExpression)
        {

        }
    }
}
