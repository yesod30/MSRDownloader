using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MSRDownloader.Helpers;

public class NegativeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number)
        {
            return -number;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}