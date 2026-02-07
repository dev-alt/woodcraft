using System.Text.Json.Serialization;

namespace Woodcraft.Core.Models;

/// <summary>
/// Complete bill of materials for a project.
/// </summary>
public record BillOfMaterials
{
    [JsonPropertyName("project_name")]
    public string ProjectName { get; init; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<BOMCategory> Categories { get; init; } = [];

    [JsonPropertyName("total_cost")]
    public double TotalCost { get; init; }

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// A category of BOM items.
/// </summary>
public record BOMCategory
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public List<BOMItem> Items { get; init; } = [];

    [JsonPropertyName("subtotal")]
    public double Subtotal { get; init; }
}

/// <summary>
/// A single item in the bill of materials.
/// </summary>
public record BOMItem
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public double Quantity { get; init; }

    [JsonPropertyName("unit")]
    public string Unit { get; init; } = "each";

    [JsonPropertyName("unit_cost")]
    public double UnitCost { get; init; }

    [JsonPropertyName("total_cost")]
    public double TotalCost { get; init; }

    [JsonPropertyName("supplier")]
    public string Supplier { get; init; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
