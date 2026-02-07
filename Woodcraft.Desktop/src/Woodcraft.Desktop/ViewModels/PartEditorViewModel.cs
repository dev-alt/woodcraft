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

    private Project? _project;
    public Project? Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public ObservableCollection<JointDisplay> PartJoints { get; } = [];

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

    public ObservableCollection<MaterialInfo> MaterialOptions { get; } = new(MaterialInfo.All);

    private MaterialInfo? _selectedMaterialInfo;
    public MaterialInfo? SelectedMaterialInfo
    {
        get => _selectedMaterialInfo;
        set
        {
            if (SetProperty(ref _selectedMaterialInfo, value) && value != null)
            {
                if (Material != value.Id)
                    Material = value.Id;
            }
        }
    }

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

        // Sync material info selection
        _selectedMaterialInfo = MaterialOptions.FirstOrDefault(m => m.Id == Material);
        OnPropertyChanged(nameof(SelectedMaterialInfo));

        RefreshPartJoints();
    }

    public void RefreshPartJoints()
    {
        PartJoints.Clear();
        if (Part == null || Project == null) return;

        foreach (var joint in Project.Joinery)
        {
            if (joint.PartAId == Part.Id || joint.PartBId == Part.Id)
            {
                var otherPartId = joint.PartAId == Part.Id ? joint.PartBId : joint.PartAId;
                PartJoints.Add(new JointDisplay(
                    joint,
                    AddJointDialogViewModel.GetTypeDisplayName(joint.JoineryType),
                    otherPartId));
            }
        }
    }

    [RelayCommand]
    private void RemoveJoint(JointDisplay jointDisplay)
    {
        if (Project == null) return;
        Project.Joinery.Remove(jointDisplay.Joint);
        RefreshPartJoints();
        ChangesApplied?.Invoke();
    }

    partial void OnMaterialChanged(string value)
    {
        var info = MaterialOptions.FirstOrDefault(m => m.Id == value);
        if (info != null && _selectedMaterialInfo != info)
        {
            _selectedMaterialInfo = info;
            OnPropertyChanged(nameof(SelectedMaterialInfo));
        }
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
        _selectedMaterialInfo = MaterialOptions.FirstOrDefault(m => m.Id == "pine");
        OnPropertyChanged(nameof(SelectedMaterialInfo));
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

public record JointDisplay(Joint Joint, string TypeName, string OtherPartId);
