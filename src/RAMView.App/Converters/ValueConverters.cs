using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RAMView.Core.Models;

namespace RAMView.App.Converters;

public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            try
            {
                var image = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class VisualStateToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SystemMemoryVisualState state)
        {
            return state switch
            {
                SystemMemoryVisualState.Critical => new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),   // Red
                SystemMemoryVisualState.High => new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),      // Amber
                SystemMemoryVisualState.Normal => new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),   // Emerald
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool flag && flag;
        if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            isNotNull = !isNotNull;
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
