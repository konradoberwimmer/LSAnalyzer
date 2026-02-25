using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;
using LSAnalyzer.Models;

namespace LSAnalyzer.ViewModels.ValueConverter;

public class VirtualVariableTypeToName : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Type type) return string.Empty;
        
        var dummyObject = Activator.CreateInstance(type);
        
        return dummyObject is not VirtualVariable virtualVariable ? string.Empty : virtualVariable.TypeName;
    }

    [ExcludeFromCodeCoverage]
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}