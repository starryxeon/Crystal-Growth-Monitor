using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Crystal_Growth_Monitor.Converters;

public class PaneWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? 300 : 0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}