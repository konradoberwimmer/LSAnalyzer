using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter
{
    public class EmptyEnumerableToVisibilityCollapsed : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value is not IEnumerable<object> enumerable)
            {
                return Visibility.Collapsed;
            }

            if (enumerable.Count() == 0)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        [ExcludeFromCodeCoverage]
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
