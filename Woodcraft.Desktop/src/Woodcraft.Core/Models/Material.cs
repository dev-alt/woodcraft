using System.Text.Json.Serialization;

namespace Woodcraft.Core.Models;

/// <summary>
/// Material specification for a project or part.
/// </summary>
public record Material
{
    [JsonPropertyName("species")]
    public string Species { get; init; } = "pine";

    [JsonPropertyName("thickness")]
    public double Thickness { get; init; } = 0.75;

    [JsonPropertyName("finish")]
    public string Finish { get; init; } = "none";

    public Material() { }

    public Material(string species, double thickness, string finish = "none")
    {
        Species = species;
        Thickness = thickness;
        Finish = finish;
    }
}

/// <summary>
/// Wood species information.
/// </summary>
public record WoodSpecies
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int JankaHardness { get; init; }
    public double DensityLbFt3 { get; init; }
    public string Workability { get; init; } = "good";
    public string Grain { get; init; } = "fine";
    public List<string> CommonUses { get; init; } = [];
    public string PriceCategory { get; init; } = "medium";
    public string Notes { get; init; } = string.Empty;
}
