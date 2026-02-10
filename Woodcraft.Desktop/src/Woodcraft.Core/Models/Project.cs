using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Core.Models;

/// <summary>
/// A complete woodworking project.
/// </summary>
public partial class Project : ObservableObject
{
    [ObservableProperty]
    [JsonPropertyName("name")]
    private string _name = "Untitled Project";

    [ObservableProperty]
    [JsonPropertyName("units")]
    private Units _units = Units.Inches;

    [ObservableProperty]
    [JsonPropertyName("material")]
    private Material _material = new();

    [ObservableProperty]
    [JsonPropertyName("notes")]
    private string _notes = string.Empty;

    [ObservableProperty]
    [JsonPropertyName("parts")]
    private ObservableCollection<Part> _parts = [];

    [ObservableProperty]
    [JsonPropertyName("joinery")]
    private ObservableCollection<Joint> _joinery = [];

    [ObservableProperty]
    [JsonPropertyName("hardware")]
    private ObservableCollection<Hardware> _hardware = [];

    [ObservableProperty]
    [JsonPropertyName("assembly_steps")]
    private ObservableCollection<AssemblyStep> _assemblySteps = [];

    /// <summary>
    /// File path if the project has been saved.
    /// </summary>
    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// Whether the project has unsaved changes.
    /// </summary>
    [JsonIgnore]
    public bool IsDirty { get; set; }

    public Project() { }

    public Project(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Get a part by its ID.
    /// </summary>
    public Part? GetPart(string id) => Parts.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Add a part to the project.
    /// </summary>
    public void AddPart(Part part)
    {
        if (GetPart(part.Id) != null)
            throw new InvalidOperationException($"Part with ID '{part.Id}' already exists");
        Parts.Add(part);
        IsDirty = true;
    }

    /// <summary>
    /// Remove a part by ID.
    /// </summary>
    public bool RemovePart(string id)
    {
        var part = GetPart(id);
        if (part == null) return false;
        Parts.Remove(part);
        IsDirty = true;
        return true;
    }

    /// <summary>
    /// Calculate total board feet for all parts.
    /// </summary>
    public double TotalBoardFeet => Parts.Sum(p => p.Dimensions.BoardFeet * p.Quantity);

    /// <summary>
    /// Calculate total hardware cost.
    /// </summary>
    public double TotalHardwareCost => Hardware.Sum(h => h.TotalCost);
}
