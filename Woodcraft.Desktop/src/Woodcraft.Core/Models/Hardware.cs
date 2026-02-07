using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Core.Models;

/// <summary>
/// Hardware item for bill of materials.
/// </summary>
public partial class Hardware : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("name")]
    private string _name = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("description")]
    private string _description = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("quantity")]
    private int _quantity = 1;

    [ObservableProperty]
    [JsonPropertyName("unit")]
    private string _unit = "each";

    [ObservableProperty]
    [JsonPropertyName("cost")]
    private double _unitCost;

    [ObservableProperty]
    [JsonPropertyName("supplier")]
    private string _supplier = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("notes")]
    private string _notes = string.Empty;

    public double TotalCost => Quantity * UnitCost;

    public Hardware() { }

    public Hardware(string name, int quantity, double unitCost = 0)
    {
        Name = name;
        Quantity = quantity;
        UnitCost = unitCost;
    }
}
