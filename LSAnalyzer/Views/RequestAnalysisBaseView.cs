using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LSAnalyzer.Views
{
    public class RequestAnalysisBaseView : Window
    {
        protected bool ShowLabels = true;

        public RequestAnalysisBaseView() : base()
        {
        }

        internal void ContextMenuShowLabels_Click(object sender, RoutedEventArgs e)
        {
            ShowLabels = !ShowLabels;

            SetShowLabels(this);
        }

        internal void SetShowLabels(Visual visual)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);

                if (childVisual is ListBox listBox)
                {
                    listBox.DisplayMemberPath = ShowLabels ? "Info" : "Name";
                }
                else
                {
                    SetShowLabels(childVisual);
                }
            }
        }

        internal void ListBoxVariables_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            if (new string[] { "listBoxVariablesDataset", "listBoxVariablesAnalyze" }.Contains(listBox.Name)  && FindName("buttonMoveToAndFromAnalysisVariables") is Button moveToAndFromAnalysisButton)
            {
                ButtonAutomationPeer peer = new(moveToAndFromAnalysisButton);
                IInvokeProvider? invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            if (listBox.Name == "listBoxVariablesGroupBy" && FindName("buttonMoveToAndFromGroupByVariables") is Button moveToAndFromGroupByButton)
            {
                ButtonAutomationPeer peer = new(moveToAndFromGroupByButton);
                IInvokeProvider? invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }

            if (listBox.Name == "listBoxVariablesDependent" && FindName("buttonMoveToAndFromDependentVariable") is Button moveToAndFromDependentButton)
            {
                ButtonAutomationPeer peer = new(moveToAndFromDependentButton);
                IInvokeProvider? invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
        }

        internal void AvailableVariablesCollectionView_FilterSystemVariables(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
            if (e.Item is Variable variable && variable.IsSystemVariable)
            {
                e.Accepted = false;
            }
        }

        internal void CheckBoxIncludeSystemVariables_Checked(object sender, RoutedEventArgs e)
        {
            var availableVariablesCollectionView = Resources["AvailableVariablesCollectionView"] as CollectionViewSource;
            if (((CheckBox)sender).IsChecked == true)
            {
                availableVariablesCollectionView!.Filter -= AvailableVariablesCollectionView_FilterSystemVariables;
            }
            else
            {
                availableVariablesCollectionView!.Filter += AvailableVariablesCollectionView_FilterSystemVariables;
            }
        }

        internal void ButtonMoveToAndFromAnalysisVariables_Click(object sender, RoutedEventArgs e)
        {
            var listBoxVariablesDataset = (ListBox)this.FindName("listBoxVariablesDataset");
            var listBoxVariablesAnalyze = (ListBox)this.FindName("listBoxVariablesAnalyze");

            if (listBoxVariablesDataset == null || listBoxVariablesAnalyze == null)
            {
                return;
            }

            ((Button)sender).CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDataset.SelectedItems.Cast<Variable>().ToList(),
                SelectedTo = listBoxVariablesAnalyze.SelectedItems.Cast<Variable>().ToList(),
            };
        }

        internal void ButtonMoveToAndFromGroupByVariables_Click(object sender, RoutedEventArgs e)
        {
            var listBoxVariablesDataset = (ListBox)this.FindName("listBoxVariablesDataset");
            var listBoxVariablesGroupBy = (ListBox)this.FindName("listBoxVariablesGroupBy");

            if (listBoxVariablesDataset == null || listBoxVariablesGroupBy == null)
            {
                return;
            }

            ((Button)sender).CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDataset.SelectedItems.Cast<Variable>().ToList(),
                SelectedTo = listBoxVariablesGroupBy.SelectedItems.Cast<Variable>().ToList(),
            };
        }


        internal void ButtonMoveToAndFromDependentVariable_Click(object sender, RoutedEventArgs e)
        {
            var listBoxVariablesDataset = (ListBox)this.FindName("listBoxVariablesDataset");
            var listBoxVariablesDependent = (ListBox)this.FindName("listBoxVariablesDependent");

            if (listBoxVariablesDataset == null || listBoxVariablesDependent == null)
            {
                return;
            }

            ((Button)sender).CommandParameter = new MoveToAndFromVariablesCommandParameters()
            {
                SelectedFrom = listBoxVariablesDependent.Items.Count == 0 && listBoxVariablesDataset.SelectedItems.Count > 0 ? new List<Variable>() { listBoxVariablesDataset.SelectedItems.Cast<Variable>().First() } : new List<Variable>(),
                SelectedTo = listBoxVariablesDependent.SelectedItems.Cast<Variable>().ToList(),
            };
        }
    }
}
