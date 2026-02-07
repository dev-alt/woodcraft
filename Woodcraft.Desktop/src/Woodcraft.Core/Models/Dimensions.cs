using System.Text.Json.Serialization;

namespace Woodcraft.Core.Models;

/// <summary>
/// Represents the dimensions of a woodworking part.
/// </summary>
public record Dimensions
{
    [JsonPropertyName("length")]
    public double Length { get; init; }

    [JsonPropertyName("width")]
    public double Width { get; init; }

    [JsonPropertyName("thickness")]
    public double Thickness { get; init; }

    public Dimensions() { }

    public Dimensions(double length, double width, double thickness)
    {
        Length = length;
        Width = width;
        Thickness = thickness;
    }

    /// <summary>
    /// Calculate volume in cubic inches.
    /// </summary>
    public double Volume => Length * Width * Thickness;

    /// <summary>
    /// Calculate board feet.
    /// </summary>
    public double BoardFeet => (Length * Width * Thickness) / 144.0;

    /// <summary>
    /// Calculate square feet (face area).
    /// </summary>
    public double SquareFeet => (Length * Width) / 144.0;

    public override string ToString() => $"{Length}\" × {Width}\" × {Thickness}\"";
}
