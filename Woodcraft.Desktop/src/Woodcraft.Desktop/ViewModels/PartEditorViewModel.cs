using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class PartEditorViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private Part? _part;

    [ObservableProperty]
    private string _partId = string.Empty;

    [ObservableProperty]
    private PartType _partType = PartType.Panel;

    [ObservableProperty]
    private double _length = 24;

    [ObservableProperty]
    private double _width = 12;

    [ObservableProperty]
    private double _thickness = 0.75;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private string _material = "pine";

    [ObservableProperty]
    private GrainDirection _grainDirection = GrainDirection.Length;

    [ObservableProperty]
    private string _notes = string.Empty;

    public ObservableCollection<PartType> PartTypes { get; } =
        new(Enum.GetValues<PartType>());

    public ObservableCollection<GrainDirection> GrainDirections { get; } =
        new(Enum.GetValues<GrainDirection>());

    public ObservableCollection<string> Materials { get; } =
    [
        "pine", "red_oak", "white_oak", "hard_maple", "soft_maple",
        "cherry", "walnut", "poplar", "ash", "birch", "hickory", "plywood", "mdf"
    ];

    public event Action? ChangesApplied;

    public PartEditorViewModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    partial void OnPartChanged(Part? value)
    {
        if (value == null)
        {
            ClearFields();
            return;
        }

        // Load part data into fields
        PartId = value.Id;
        PartType = value.PartType;
        Length = value.Dimensions.Length;
        Width = value.Dimensions.Width;
        Thickness = value.Dimensions.Thickness;
        Quantity = value.Quantity;
        Material = value.Material ?? "pine";
        GrainDirection = value.GrainDirection;
        Notes = value.Notes;
    }

    private void ClearFields()
    {
        PartId = string.Empty;
        PartType = PartType.Panel;
        Length = 24;
        Width = 12;
        Thickness = 0.75;
        Quantity = 1;
        Material = "pine";
        GrainDirection = GrainDirection.Length;
        Notes = string.Empty;
    }

    [RelayCommand]
    private async Task ApplyChangesAsync()
    {
        if (Part == null) return;

        // Update part with field values
        Part.PartType = PartType;
        Part.Dimensions = new Dimensions(Length, Width, Thickness);
        Part.Quantity = Quantity;
        Part.Material = Material;
        Part.GrainDirection = GrainDirection;
        Part.Notes = Notes;

        await _projectService.UpdatePartAsync(Part);
        ChangesApplied?.Invoke();
    }

    [RelayCommand]
    private void ResetChanges()
    {
        // Reload from part
        OnPartChanged(Part);
    }

    // Convenience methods for common thicknesses
    [RelayCommand]
    private void SetThickness(string thickness)
    {
        Thickness = thickness switch
        {
            "1/4" => 0.25,
            "3/8" => 0.375,
            "1/2" => 0.5,
            "5/8" => 0.625,
            "3/4" => 0.75,
            "1" => 1.0,
            "1-1/2" => 1.5,
            _ => Thickness
        };
    }

    // Quick dimension presets
    [RelayCommand]
    private void SetPreset(string preset)
    {
        switch (preset)
        {
            case "shelf":
                Length = 36;
                Width = 10;
                Thickness = 0.75;
                PartType = PartType.Shelf;
                break;
            case "side":
                Length = 36;
                Width = 12;
                Thickness = 0.75;
                PartType = PartType.Side;
                break;
            case "drawer_front":
                Length = 18;
                Width = 6;
                Thickness = 0.75;
                PartType = PartType.DrawerFront;
                break;
            case "back_panel":
                Length = 36;
                Width = 24;
                Thickness = 0.25;
                PartType = PartType.Back;
                Material = "plywood";
                break;
        }
    }
}
