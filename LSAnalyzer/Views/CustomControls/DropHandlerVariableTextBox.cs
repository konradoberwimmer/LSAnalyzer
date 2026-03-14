using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;
using LSAnalyzer.Models;

namespace LSAnalyzer.Views.CustomControls;

public class DropHandlerVariableTextBox : IDropTarget
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
            case VirtualVariableScale virtualVariableScale:
                virtualVariableScale.InputVariable = variable.Clone();
                break;
            default:
                throw new NotImplementedException();
        }
    }
}