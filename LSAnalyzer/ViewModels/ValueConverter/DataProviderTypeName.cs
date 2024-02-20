using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter
{
    public class DataProviderTypeName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Type type || !typeof(IDataProviderConfiguration).IsAssignableFrom(type))
            {
                return string.Empty;
            }

            return type.Name.Replace("Configuration", "");
        }

        [ExcludeFromCodeCoverage]
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
