using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Woodcraft.Desktop.Converters;

/// <summary>
/// Static converters for use in XAML.
/// </summary>
public static class Converters
{
    public static readonly IValueConverter BoolToColor = new BoolToColorConverter();
    public static readonly IValueConverter BoolToFontWeight = new BoolToFontWeightConverter();
    public static readonly IValueConverter Scale = new ScaleConverter();
    public static readonly IValueConverter InchesToFraction = new InchesToFractionConverter();
}

/// <summary>
/// Converts boolean to connection status color.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts double to scaled value.
/// </summary>
public class ScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), out var scale))
        {
            return d * scale;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts decimal inches to fractional string.
/// </summary>
public class InchesToFractionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double d) return value?.ToString();

        var whole = (int)d;
        var frac = d - whole;

        if (frac < 0.03125) // Less than 1/32
            return whole == 0 ? "0" : whole.ToString();

        // Find closest fraction to 16ths
        var sixteenths = (int)Math.Round(frac * 16);
        if (sixteenths == 16)
            return (whole + 1).ToString();

        // Simplify
        var numerator = sixteenths;
        var denominator = 16;
        while (numerator % 2 == 0 && denominator > 1)
        {
            numerator /= 2;
            denominator /= 2;
        }

        return whole > 0
            ? $"{whole} {numerator}/{denominator}\""
            : $"{numerator}/{denominator}\"";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts boolean to FontWeight.
/// </summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? FontWeight.SemiBold : FontWeight.Normal;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
