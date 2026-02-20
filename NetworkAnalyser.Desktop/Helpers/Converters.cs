using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NetworkAnalyser.Desktop.Helpers;

/// <summary>
/// Converts a boolean (IsSuspicious) to a brush color for row highlighting.
/// </summary>
public class SuspiciousToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSuspicious && isSuspicious)
            return new SolidColorBrush(Color.FromArgb(40, 255, 70, 70)); // translucent red
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converts byte count to a human-readable string (KB, MB, GB).
/// </summary>
public class BytesToReadableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes switch
            {
                >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
                >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
                >= 1024 => $"{bytes / 1024.0:F2} KB",
                _ => $"{bytes} B"
            };
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converts boolean to Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converts connection state to a colored brush.
/// </summary>
public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string state)
        {
            return state switch
            {
                "ESTABLISHED" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // green
                "LISTEN" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),       // blue
                "CLOSE_WAIT" or "TIME_WAIT" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // orange
                "CLOSED" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),      // gray
                _ => new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converts boolean (IsMonitoring) to status text.
/// </summary>
public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isMonitoring)
            return isMonitoring ? "● MONITORING" : "○ STOPPED";
        return "○ STOPPED";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converts boolean (IsMonitoring) to status color brush.
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isMonitoring && isMonitoring)
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // green
        return new SolidColorBrush(Color.FromRgb(255, 82, 82)); // red
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
