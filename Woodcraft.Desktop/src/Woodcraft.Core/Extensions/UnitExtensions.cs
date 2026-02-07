using Woodcraft.Core.Models;

namespace Woodcraft.Core.Extensions;

/// <summary>
/// Extension methods for unit conversion.
/// </summary>
public static class UnitExtensions
{
    private static readonly Dictionary<Units, double> ToInchesFactors = new()
    {
        [Units.Inches] = 1.0,
        [Units.Millimeters] = 1.0 / 25.4,
        [Units.Centimeters] = 1.0 / 2.54,
        [Units.Feet] = 12.0
    };

    /// <summary>
    /// Convert a value to inches.
    /// </summary>
    public static double ToInches(this double value, Units fromUnit)
        => value * ToInchesFactors[fromUnit];

    /// <summary>
    /// Convert a value from inches to another unit.
    /// </summary>
    public static double FromInches(this double value, Units toUnit)
        => value / ToInchesFactors[toUnit];

    /// <summary>
    /// Convert between any two units.
    /// </summary>
    public static double Convert(this double value, Units fromUnit, Units toUnit)
        => value.ToInches(fromUnit).FromInches(toUnit);

    /// <summary>
    /// Format a value as a fractional string (e.g., "3 1/2").
    /// </summary>
    public static string ToFraction(this double value, int precision = 16)
    {
        var whole = (int)value;
        var frac = value - whole;

        if (frac < 1.0 / (precision * 2))
            return whole == 0 ? "0" : whole.ToString();

        var numerator = (int)Math.Round(frac * precision);
        if (numerator == precision)
            return (whole + 1).ToString();

        // Simplify fraction
        var gcd = GCD(numerator, precision);
        numerator /= gcd;
        var denominator = precision / gcd;

        return whole > 0 ? $"{whole} {numerator}/{denominator}" : $"{numerator}/{denominator}";
    }

    private static int GCD(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    /// <summary>
    /// Parse a fractional string to a double.
    /// </summary>
    public static double ParseFraction(string text)
    {
        text = text.Trim();

        if (!text.Contains('/'))
            return double.Parse(text);

        double whole = 0;
        string fracPart;

        if (text.Contains(' '))
        {
            var parts = text.Split(' ', 2);
            whole = double.Parse(parts[0]);
            fracPart = parts[1];
        }
        else
        {
            fracPart = text;
        }

        var fracParts = fracPart.Split('/');
        var fraction = double.Parse(fracParts[0]) / double.Parse(fracParts[1]);

        return whole + fraction;
    }
}
