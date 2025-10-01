using System;
using System.Globalization;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter;

public class IntegerToStringLenient : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int integer ? integer.ToString(culture) : "0";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string stringValue && int.TryParse(stringValue, out _) ? int.Parse(stringValue, culture) : 0;
    }
}