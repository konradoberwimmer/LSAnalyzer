using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter;

public class LastInCollectionToVisibility : IMultiValueConverter
{

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [{ } value, ICollection collection])
        {
            var genericList = collection.Cast<object>().ToList();

            if (genericList.Contains(value) && Equals(value, genericList.Last()))
            {
                return Visibility.Visible;
            }
        }

        return parameter is true ? Visibility.Collapsed : Visibility.Hidden;
    }

    [ExcludeFromCodeCoverage]
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}