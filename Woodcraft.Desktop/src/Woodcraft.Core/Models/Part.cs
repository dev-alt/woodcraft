using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Core.Models;

/// <summary>
/// A woodworking part definition.
/// </summary>
public partial class Part : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("id")]
    private string _id = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("type")]
    private PartType _partType = PartType.Panel;

    [ObservableProperty]
    [JsonPropertyName("dimensions")]
    private Dimensions _dimensions = new();

    [ObservableProperty]
    [JsonPropertyName("quantity")]
    private int _quantity = 1;

    [ObservableProperty]
    [JsonPropertyName("grain_direction")]
    private GrainDirection _grainDirection = GrainDirection.Length;

    [ObservableProperty]
    [JsonPropertyName("material")]
    private string? _material;

    [ObservableProperty]
    [JsonPropertyName("notes")]
    private string _notes = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("position")]
    private double[] _position = [0, 0, 0];

    [ObservableProperty]
    [JsonPropertyName("rotation")]
    private double[] _rotation = [0, 0, 0];

    public Part() { }

    public Part(string id, PartType partType, Dimensions dimensions)
    {
        Id = id;
        PartType = partType;
        Dimensions = dimensions;
    }

    /// <summary>
    /// Create a deep copy of this part.
    /// </summary>
    public Part Clone() => new()
    {
        Id = Id,
        PartType = PartType,
        Dimensions = Dimensions with { },
        Quantity = Quantity,
        GrainDirection = GrainDirection,
        Material = Material,
        Notes = Notes,
        Position = [.. Position],
        Rotation = [.. Rotation]
    };
}
