using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SpinnerDemo;

public class BoolToReverseConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = (bool)value;
        return !val;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = (bool)value;
        if (parameter is string param && (param.ToString().Equals("inverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("reverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("opposite", StringComparison.OrdinalIgnoreCase)))
            val = !val;
        return val ? Visibility.Visible : Visibility.Hidden;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

public class PathToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            string imagePath = (string)value;
            BitmapImage imageBitmap = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
            return imageBitmap;
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"PathToImageConverter: {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}


public class ImagePathConverter : IValueConverter
{
    string imageDirectory = System.IO.Directory.GetCurrentDirectory();
    public string ImageDirectory
    {
        get { return imageDirectory; }
        set { imageDirectory = value; }
    }

    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        try
        {
            string imagePath = System.IO.Path.Combine(ImageDirectory, (string)value);
            return new BitmapImage(new Uri(imagePath));
        }
        catch (Exception ex)
        {
            Extensions.WriteToLog($"ImagePathConverter: {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Converts a <see cref="MenuItemRole"/> to Visibility.
/// Shows the element only if the role matches the parameter (e.g., "SubmenuHeader").
/// </summary>
/// <remarks>
/// <see cref="MenuItemRole"/> is an internal enum WPF uses to distinguish between top‑level headers, submenu headers, and regular items.
/// </remarks>
public class MenuItemRoleToVisibilityConverter : IValueConverter
{
    // Singleton instance for easy XAML reference
    public static readonly MenuItemRoleToVisibilityConverter Instance = new MenuItemRoleToVisibilityConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MenuItemRole role && parameter is string param)
        {
            if (Enum.TryParse(param, out MenuItemRole targetRole))
            {
                return role == targetRole ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Turns PulseWidth and PulseDuration into usable offsets and TimeSpans.
/// </summary>
public class HalfConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)value / 2;
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
public class HalfNegConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => -(double)value / 2;
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
public class SecondsToTimeSpanConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => TimeSpan.FromSeconds((double)value);
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}


