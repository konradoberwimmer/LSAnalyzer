using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LSAnalyzer.ViewModels.ValueConverter;

public class EnumCamelCase : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue) return value;
        
        var enumString = enumValue.ToString();
        var camelCaseString = Regex.Replace(enumString, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ").ToLower();
        return char.ToUpper(camelCaseString[0]) + camelCaseString.Substring(1);
    }

    [ExcludeFromCodeCoverage]
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}