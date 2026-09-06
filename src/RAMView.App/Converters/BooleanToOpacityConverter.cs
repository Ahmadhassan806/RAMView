using System;
using System.Globalization;
using System.Windows.Data;

namespace RAMView.App.Converters;

/// <summary>
/// Converts boolean search match state into smooth opacity (1.0 for matches, 0.20 for dimmed non-matches) (§13).
/// </summary>
public class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isMatch = value is bool b && b;
        return isMatch ? 1.0 : 0.22;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
