using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Core.Models;

public partial class AssemblyStep : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("step_number")]
    private int _stepNumber;

    [ObservableProperty]
    [JsonPropertyName("description")]
    private string _description = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("part_ids")]
    private List<string> _partIds = [];

    [ObservableProperty]
    [JsonPropertyName("joint_indices")]
    private List<int> _jointIndices = [];

    [ObservableProperty]
    [JsonPropertyName("notes")]
    private string _notes = string.Empty;
}
