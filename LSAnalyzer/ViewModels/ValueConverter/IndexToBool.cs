using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter
{
    public class IndexToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [ExcludeFromCodeCoverage]
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
