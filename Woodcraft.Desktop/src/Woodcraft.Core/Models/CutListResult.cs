using System.Text.Json.Serialization;

namespace Woodcraft.Core.Models;

/// <summary>
/// Result of cut list optimization.
/// </summary>
public record CutListResult
{
    [JsonPropertyName("sheets")]
    public List<SheetLayout> Sheets { get; init; } = [];

    [JsonPropertyName("unplaced")]
    public List<UnplacedPiece> Unplaced { get; init; } = [];

    [JsonPropertyName("total_stock_area")]
    public double TotalStockArea { get; init; }

    [JsonPropertyName("total_parts_area")]
    public double TotalPartsArea { get; init; }

    [JsonPropertyName("waste_percentage")]
    public double WastePercentage { get; init; }
}

/// <summary>
/// Layout of pieces on a single sheet.
/// </summary>
public record SheetLayout
{
    [JsonPropertyName("stock")]
    public StockInfo Stock { get; init; } = new();

    [JsonPropertyName("pieces")]
    public List<PlacedPiece> Pieces { get; init; } = [];
}

/// <summary>
/// Stock sheet information.
/// </summary>
public record StockInfo
{
    [JsonPropertyName("width")]
    public double Width { get; init; }

    [JsonPropertyName("length")]
    public double Length { get; init; }

    [JsonPropertyName("material")]
    public string Material { get; init; } = "plywood";

    [JsonPropertyName("thickness")]
    public double Thickness { get; init; }
}

/// <summary>
/// A piece placed on a sheet.
/// </summary>
public record PlacedPiece
{
    [JsonPropertyName("part_id")]
    public string PartId { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }

    [JsonPropertyName("width")]
    public double Width { get; init; }

    [JsonPropertyName("height")]
    public double Height { get; init; }

    [JsonPropertyName("rotated")]
    public bool Rotated { get; init; }
}

/// <summary>
/// A piece that couldn't be placed.
/// </summary>
public record UnplacedPiece
{
    [JsonPropertyName("part_id")]
    public string PartId { get; init; } = string.Empty;

    [JsonPropertyName("length")]
    public double Length { get; init; }

    [JsonPropertyName("width")]
    public double Width { get; init; }
}
