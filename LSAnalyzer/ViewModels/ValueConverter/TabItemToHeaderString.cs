using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter
{
    public class TabItemToHeaderString : IValueConverter
    {
        [ExcludeFromCodeCoverage]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TabItem tabItem)
            {
                return string.Empty;
            }

            return tabItem.Header?.ToString() ?? string.Empty;
        }
    }
}
