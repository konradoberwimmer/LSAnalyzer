using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using GongSolutions.Wpf.DragDrop;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views.CustomControls;

public class DropHandlerVariable : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = DragDropEffects.Copy;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not Variable && dropInfo.Data is not IEnumerable<Variable>) return;
        
        if (dropInfo.VisualTarget is not ContentControl contentControl) return;
        
        var variable = dropInfo.Data as Variable ?? (dropInfo.Data as IEnumerable<Variable>)!.First();

        switch (contentControl.DataContext)
        {
            case VirtualVariableCompute:
                WeakReferenceMessenger.Default.Send(new VirtualVariables.DoubleClickedAvailableVariablesMessage(variable));
                break;
            case VirtualVariableScale virtualVariableScale:
                virtualVariableScale.InputVariable = variable.Clone();
                break;
            case VirtualVariableRecode virtualVariableRecode:
                virtualVariableRecode.AddVariable(variable.Clone());
                break;
            default:
                throw new InvalidOperationException();
        }
    }
}