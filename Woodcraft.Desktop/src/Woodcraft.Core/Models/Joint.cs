using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Core.Models;

/// <summary>
/// Definition of a joint between two parts.
/// </summary>
public partial class Joint : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("type")]
    private JoineryType _joineryType = JoineryType.Butt;

    [ObservableProperty]
    [JsonPropertyName("part_a")]
    private string _partAId = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("part_b")]
    private string _partBId = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("position_a")]
    private double[] _positionA = [0, 0, 0];

    [ObservableProperty]
    [JsonPropertyName("position_b")]
    private double[] _positionB = [0, 0, 0];

    [ObservableProperty]
    [JsonPropertyName("parameters")]
    private Dictionary<string, object>? _parameters;

    public Joint() { }

    public Joint(JoineryType type, string partAId, string partBId)
    {
        JoineryType = type;
        PartAId = partAId;
        PartBId = partBId;
    }
}
