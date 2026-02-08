using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class Viewer3DViewModel : ViewModelBase
{
    private readonly ICadService _cadService;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private Part? _selectedPart;

    [ObservableProperty]
    private bool _showExploded;

    [ObservableProperty]
    private double _explosionFactor = 2.0;

    [ObservableProperty]
    private bool _showWireframe;

    [ObservableProperty]
    private bool _showDimensions = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _modelPath;

    // Camera position
    [ObservableProperty]
    private double _cameraDistance = 100;

    [ObservableProperty]
    private double _cameraRotationX = 30;

    [ObservableProperty]
    private double _cameraRotationY = 45;

    [ObservableProperty]
    private Part? _secondarySelectedPart;

    [ObservableProperty]
    private bool _canAddJoint;

    public ObservableCollection<Part3DModel> Models { get; } = [];
    public ObservableCollection<JointLineModel> JointLines { get; } = [];

    public event Action? JointAdded;

    public Viewer3DViewModel(ICadService cadService)
    {
        _cadService = cadService;
    }

    partial void OnProjectChanged(Project? value)
    {
        if (value != null)
        {
            _ = RefreshModelsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    StatusMessage = $"Failed to load models: {t.Exception?.InnerException?.Message}";
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            Models.Clear();
        }
    }

    [RelayCommand]
    private async Task RefreshModelsAsync()
    {
        if (Project == null) return;

        IsLoading = true;
        try
        {
            Models.Clear();

            // Auto-layout parts in a grid if all positions are at origin
            var allAtOrigin = Project.Parts.All(p =>
                p.Position[0] == 0 && p.Position[1] == 0 && p.Position[2] == 0);

            double offsetX = 20;
            double offsetY = 20;
            double maxRowHeight = 0;
            double currentX = offsetX;
            double currentY = offsetY;
            const double padding = 15;
            const double maxWidth = 500;

            foreach (var part in Project.Parts)
            {
                double posX, posY;

                if (allAtOrigin && Project.Parts.Count > 1)
                {
                    // Auto-layout: flow parts left-to-right, wrap rows
                    if (currentX + part.Dimensions.Length > maxWidth)
                    {
                        currentX = offsetX;
                        currentY += maxRowHeight + padding;
                        maxRowHeight = 0;
                    }

                    posX = currentX;
                    posY = currentY;
                    currentX += part.Dimensions.Length + padding;
                    maxRowHeight = Math.Max(maxRowHeight, part.Dimensions.Width);
                }
                else
                {
                    posX = part.Position[0] + offsetX;
                    posY = part.Position[1] + offsetY;
                }

                var model = new Part3DModel
                {
                    Part = part,
                    PositionX = posX,
                    PositionY = posY,
                    PositionZ = part.Position[2],
                    SizeX = part.Dimensions.Length,
                    SizeY = part.Dimensions.Width,
                    SizeZ = part.Dimensions.Thickness,
                    WoodBrush = MaterialInfo.CreateWoodBrush(part.Material),
                    WoodBorderBrush = MaterialInfo.CreateBorderBrush(part.Material),
                };

                Models.Add(model);
            }

            RebuildJointLines();
            await Task.CompletedTask;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RebuildJointLines()
    {
        JointLines.Clear();
        if (Project == null) return;

        foreach (var joint in Project.Joinery)
        {
            var modelA = Models.FirstOrDefault(m => m.Part?.Id == joint.PartAId);
            var modelB = Models.FirstOrDefault(m => m.Part?.Id == joint.PartBId);
            if (modelA == null || modelB == null) continue;

            JointLines.Add(new JointLineModel
            {
                StartX = modelA.PositionX + modelA.SizeX / 2,
                StartY = modelA.PositionY + modelA.SizeY / 2,
                EndX = modelB.PositionX + modelB.SizeX / 2,
                EndY = modelB.PositionY + modelB.SizeY / 2,
                Label = AddJointDialogViewModel.GetTypeDisplayName(joint.JoineryType),
            });
        }
    }

    [ObservableProperty]
    private string? _statusMessage;

    [RelayCommand]
    private async Task ExportStlAsync()
    {
        if (!_cadService.IsConnected)
        {
            StatusMessage = "STL export requires the CAD engine. Click 'Connect' in the toolbar.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Exporting STL...";
            ModelPath = await _cadService.ExportStlAsync(SelectedPart?.Id);
            StatusMessage = $"STL exported: {ModelPath}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ResetCamera()
    {
        CameraDistance = 100;
        CameraRotationX = 30;
        CameraRotationY = 45;
    }

    [RelayCommand]
    private void ViewFront()
    {
        CameraRotationX = 0;
        CameraRotationY = 0;
    }

    [RelayCommand]
    private void ViewTop()
    {
        CameraRotationX = 90;
        CameraRotationY = 0;
    }

    [RelayCommand]
    private void ViewSide()
    {
        CameraRotationX = 0;
        CameraRotationY = 90;
    }

    [RelayCommand]
    private void ViewIsometric()
    {
        CameraRotationX = 30;
        CameraRotationY = 45;
    }

    public void SelectPartByModel(Part3DModel? model, bool isCtrlHeld = false)
    {
        if (isCtrlHeld && SelectedPart != null && model?.Part != null && model.Part != SelectedPart)
        {
            SecondarySelectedPart = model.Part;
            CanAddJoint = true;
            // Update visual - mark secondary with dashed style
            foreach (var m in Models)
                m.IsSecondarySelected = m.Part?.Id == model.Part.Id;
        }
        else
        {
            SecondarySelectedPart = null;
            CanAddJoint = false;
            foreach (var m in Models)
                m.IsSecondarySelected = false;
            SelectedPart = model?.Part;
        }
    }

    [RelayCommand]
    private void AddJoint(AddJointDialogViewModel dialogResult)
    {
        if (Project == null || SelectedPart == null || SecondarySelectedPart == null) return;

        var joint = new Joint(dialogResult.SelectedType, SelectedPart.Id, SecondarySelectedPart.Id);
        Project.Joinery.Add(joint);

        // Clear secondary selection
        SecondarySelectedPart = null;
        CanAddJoint = false;
        foreach (var m in Models)
            m.IsSecondarySelected = false;

        RebuildJointLines();
        JointAdded?.Invoke();
    }

    public void UpdatePartPosition(Part3DModel model)
    {
        if (model.Part == null) return;
        model.Part.Position[0] = model.PositionX - 20; // Remove layout offset
        model.Part.Position[1] = model.PositionY - 20;
    }

    partial void OnSelectedPartChanged(Part? value)
    {
        // Highlight selected part in the 3D view
        foreach (var model in Models)
        {
            model.IsSelected = model.Part?.Id == value?.Id;
        }
    }

    partial void OnShowExplodedChanged(bool value)
    {
        // Recalculate positions for exploded view
        if (Project == null) return;

        foreach (var model in Models)
        {
            if (value)
            {
                // Move parts apart
                model.ExplodedPositionX = model.PositionX * ExplosionFactor;
                model.ExplodedPositionY = model.PositionY * ExplosionFactor;
                model.ExplodedPositionZ = model.PositionZ * ExplosionFactor;
            }
            else
            {
                model.ExplodedPositionX = model.PositionX;
                model.ExplodedPositionY = model.PositionY;
                model.ExplodedPositionZ = model.PositionZ;
            }
        }
    }
}

public partial class Part3DModel : ObservableObject
{
    public Part? Part { get; set; }

    // Position
    [ObservableProperty]
    private double _positionX;

    [ObservableProperty]
    private double _positionY;

    [ObservableProperty]
    private double _positionZ;

    // Exploded position
    [ObservableProperty]
    private double _explodedPositionX;

    [ObservableProperty]
    private double _explodedPositionY;

    [ObservableProperty]
    private double _explodedPositionZ;

    // Size
    [ObservableProperty]
    private double _sizeX;

    [ObservableProperty]
    private double _sizeY;

    [ObservableProperty]
    private double _sizeZ;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isSecondarySelected;

    // Material-specific wood appearance
    public IBrush WoodBrush { get; set; } = new SolidColorBrush(Color.Parse("#CD853F"));
    public IBrush WoodBorderBrush { get; set; } = new SolidColorBrush(Color.Parse("#8B4513"));
}

public partial class JointLineModel : ObservableObject
{
    [ObservableProperty]
    private double _startX;

    [ObservableProperty]
    private double _startY;

    [ObservableProperty]
    private double _endX;

    [ObservableProperty]
    private double _endY;

    [ObservableProperty]
    private string _label = string.Empty;

    public Point Start => new(StartX, StartY);
    public Point End => new(EndX, EndY);
    public double MidX => (StartX + EndX) / 2;
    public double MidY => (StartY + EndY) / 2;
}
