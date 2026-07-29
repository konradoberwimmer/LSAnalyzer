using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels.VirtualVariableCreation;

namespace LSAnalyzer.Views.CustomControls;

public class DropHandlerVariables : IDropTarget
{
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = DragDropEffects.Copy;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not Variable && (dropInfo.Data is not IEnumerable<object> enumerable || enumerable.Any(o => o is not Variable))) return;
        
        if (dropInfo.VisualTarget is not ContentControl contentControl) return;
        
        var variables = 
            dropInfo.Data is Variable variable ? 
                [ variable ] : 
                (dropInfo.Data as IEnumerable<object>)!.Cast<Variable>().ToList();

        switch (contentControl.DataContext)
        {
            case MassRecoding massRecoding:
                massRecoding.HandleAvailableVariablesCommand.Execute(variables);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}