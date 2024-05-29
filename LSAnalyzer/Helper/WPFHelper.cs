using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace LSAnalyzer.Helper;

internal static class WPFHelper
{
    // Kudos to Bryce Kahle
    public static T? FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                var childItem = FindVisualChild<T>(child!);
                if (childItem != null) return childItem;
            }
        }
        return null;
    }
}
