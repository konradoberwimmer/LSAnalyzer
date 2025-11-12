using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows.Data;
namespace LSAnalyzer.ViewModels.ValueConverter;

public class LeftSideFileName : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string fileName || 
            fileName.StartsWith('{') ||
            fileName.StartsWith('['))
        {
            return value;
        }
        
        var fileNameWithPath = !string.IsNullOrEmpty(Path.GetDirectoryName(fileName));
        
        return Path.GetFileName(fileName) + (fileNameWithPath ? " - " + Path.GetDirectoryName(fileName) : string.Empty);
    }

    [ExcludeFromCodeCoverage]
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}