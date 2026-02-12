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

    // Assembly opacities (configurable)
    private double _assemblyCurrent = 1.0;
    private double _assemblyPrevious = 0.5;
    private double _assemblyFuture = 0.15;

    // Stored config defaults for ResetCamera
    private double _defaultCameraDistance = 100;
    private double _defaultCameraRotationX = 30;
    private double _defaultCameraRotationY = 45;

    [ObservableProperty]
    private Part? _secondarySelectedPart;

    [ObservableProperty]
    private bool _canAddJoint;

    public ObservableCollection<Part3DModel> Models { get; } = [];
    public ObservableCollection<JointLineModel> JointLines { get; } = [];

    public event Action? JointAdded;
    public event Action<Part>? PartPositionChanged;

    public Viewer3DViewModel(ICadService cadService, IConfigService config)
    {
        _cadService = cadService;

        _defaultCameraDistance = config.GetDouble("viewer3d.camera_distance", 100);
        _defaultCameraRotationX = config.GetDouble("viewer3d.camera_rotation_x", 30);
        _defaultCameraRotationY = config.GetDouble("viewer3d.camera_rotation_y", 45);
        _cameraDistance = _defaultCameraDistance;
        _cameraRotationX = _defaultCameraRotationX;
        _cameraRotationY = _defaultCameraRotationY;
        _explosionFactor = config.GetDouble("viewer3d.explosion_factor", 2.0);
        _assemblyCurrent = config.GetDouble("viewer3d.assembly_current_opacity", 1.0);
        _assemblyPrevious = config.GetDouble("viewer3d.assembly_previous_opacity", 0.5);
        _assemblyFuture = config.GetDouble("viewer3d.assembly_future_opacity", 0.15);
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
                    RotationZ = part.Rotation[2],
                    GrainDirection = part.GrainDirection,
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
        CameraDistance = _defaultCameraDistance;
        CameraRotationX = _defaultCameraRotationX;
        CameraRotationY = _defaultCameraRotationY;
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
        CameraRotationX = _defaultCameraRotationX;
        CameraRotationY = _defaultCameraRotationY;
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
        PartPositionChanged?.Invoke(model.Part);
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
        if (Project == null) return;

        foreach (var model in Models)
        {
            if (value)
            {
                // Store original positions and spread parts apart
                model.OriginalPositionX ??= model.PositionX;
                model.OriginalPositionY ??= model.PositionY;
                model.PositionX = model.OriginalPositionX.Value * ExplosionFactor;
                model.PositionY = model.OriginalPositionY.Value * ExplosionFactor;
            }
            else if (model.OriginalPositionX.HasValue)
            {
                model.PositionX = model.OriginalPositionX.Value;
                model.PositionY = model.OriginalPositionY!.Value;
                model.OriginalPositionX = null;
                model.OriginalPositionY = null;
            }
        }

        RebuildJointLines();
    }

    partial void OnShowWireframeChanged(bool value)
    {
        foreach (var model in Models)
            model.IsWireframe = value;
    }

    partial void OnShowDimensionsChanged(bool value)
    {
        foreach (var model in Models)
            model.ShowDimensionText = value;
    }

    // --- Alignment Tools ---

    private List<Part3DModel> GetAlignmentTargets()
    {
        // If primary + secondary selected, operate on those two
        if (SelectedPart != null && SecondarySelectedPart != null)
        {
            return Models.Where(m =>
                m.Part?.Id == SelectedPart.Id || m.Part?.Id == SecondarySelectedPart.Id).ToList();
        }
        return Models.ToList();
    }

    [RelayCommand]
    private void AlignLeft()
    {
        var targets = GetAlignmentTargets();
        if (targets.Count < 2) return;
        var minX = targets.Min(m => m.PositionX);
        foreach (var m in targets) { m.PositionX = minX; UpdatePartPosition(m); }
        RebuildJointLines();
    }

    [RelayCommand]
    private void AlignTop()
    {
        var targets = GetAlignmentTargets();
        if (targets.Count < 2) return;
        var minY = targets.Min(m => m.PositionY);
        foreach (var m in targets) { m.PositionY = minY; UpdatePartPosition(m); }
        RebuildJointLines();
    }

    [RelayCommand]
    private void AlignCenterH()
    {
        var targets = GetAlignmentTargets();
        if (targets.Count < 2) return;
        var centerY = targets.Average(m => m.PositionY + m.SizeY / 2);
        foreach (var m in targets) { m.PositionY = centerY - m.SizeY / 2; UpdatePartPosition(m); }
        RebuildJointLines();
    }

    [RelayCommand]
    private void AlignCenterV()
    {
        var targets = GetAlignmentTargets();
        if (targets.Count < 2) return;
        var centerX = targets.Average(m => m.PositionX + m.SizeX / 2);
        foreach (var m in targets) { m.PositionX = centerX - m.SizeX / 2; UpdatePartPosition(m); }
        RebuildJointLines();
    }

    [RelayCommand]
    private void DistributeH()
    {
        var targets = GetAlignmentTargets().OrderBy(m => m.PositionX).ToList();
        if (targets.Count < 3) return;
        var totalWidth = targets.Sum(m => m.SizeX);
        var totalSpace = targets.Last().PositionX + targets.Last().SizeX - targets.First().PositionX;
        var gap = (totalSpace - totalWidth) / (targets.Count - 1);
        var currentX = targets.First().PositionX;
        foreach (var m in targets)
        {
            m.PositionX = currentX;
            currentX += m.SizeX + gap;
            UpdatePartPosition(m);
        }
        RebuildJointLines();
    }

    [RelayCommand]
    private void DistributeV()
    {
        var targets = GetAlignmentTargets().OrderBy(m => m.PositionY).ToList();
        if (targets.Count < 3) return;
        var totalHeight = targets.Sum(m => m.SizeY);
        var totalSpace = targets.Last().PositionY + targets.Last().SizeY - targets.First().PositionY;
        var gap = (totalSpace - totalHeight) / (targets.Count - 1);
        var currentY = targets.First().PositionY;
        foreach (var m in targets)
        {
            m.PositionY = currentY;
            currentY += m.SizeY + gap;
            UpdatePartPosition(m);
        }
        RebuildJointLines();
    }

    // --- Assembly Highlighting ---

    public void HighlightAssemblyStep(Woodcraft.Core.Models.AssemblyStep? step, IList<Woodcraft.Core.Models.AssemblyStep> allSteps, int currentIndex)
    {
        if (step == null) { ClearAssemblyHighlight(); return; }

        // Collect part IDs for current and previous steps
        var currentPartIds = new HashSet<string>(step.PartIds);
        var previousPartIds = new HashSet<string>();
        for (int i = 0; i < currentIndex; i++)
            foreach (var id in allSteps[i].PartIds) previousPartIds.Add(id);

        foreach (var model in Models)
        {
            var partId = model.Part?.Id ?? "";
            if (currentPartIds.Contains(partId))
                model.AssemblyOpacity = _assemblyCurrent;
            else if (previousPartIds.Contains(partId))
                model.AssemblyOpacity = _assemblyPrevious;
            else
                model.AssemblyOpacity = _assemblyFuture;
        }
    }

    public void ClearAssemblyHighlight()
    {
        foreach (var model in Models)
            model.AssemblyOpacity = 1.0;
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

    // Original positions (stored when exploded)
    public double? OriginalPositionX { get; set; }
    public double? OriginalPositionY { get; set; }

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

    [ObservableProperty]
    private bool _isWireframe;

    [ObservableProperty]
    private bool _showDimensionText = true;

    [ObservableProperty]
    private double _rotationZ;

    [ObservableProperty]
    private GrainDirection _grainDirection = GrainDirection.None;

    [ObservableProperty]
    private double _assemblyOpacity = 1.0;

    // Material-specific wood appearance
    [ObservableProperty]
    private IBrush _woodBrush = new SolidColorBrush(Color.Parse("#CD853F"));

    [ObservableProperty]
    private IBrush _woodBorderBrush = new SolidColorBrush(Color.Parse("#8B4513"));

    private static readonly IBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);

    /// <summary>
    /// Returns transparent when wireframe mode is active, otherwise the wood brush.
    /// </summary>
    public IBrush DisplayBrush => IsWireframe ? TransparentBrush : WoodBrush;

    partial void OnIsWireframeChanged(bool value) => OnPropertyChanged(nameof(DisplayBrush));
    partial void OnWoodBrushChanged(IBrush value) => OnPropertyChanged(nameof(DisplayBrush));

    /// <summary>
    /// Returns a VisualBrush with grain lines based on GrainDirection.
    /// </summary>
    public IBrush? GrainOverlayBrush => GrainDirection switch
    {
        GrainDirection.Length => CreateGrainBrush(horizontal: true),
        GrainDirection.Width => CreateGrainBrush(horizontal: false),
        _ => null
    };

    partial void OnGrainDirectionChanged(GrainDirection value) => OnPropertyChanged(nameof(GrainOverlayBrush));

    private static IBrush CreateGrainBrush(bool horizontal)
    {
        var lineColor = Color.FromArgb(30, 0, 0, 0);
        var lineBrush = new SolidColorBrush(lineColor);

        if (horizontal)
        {
            return new VisualBrush
            {
                TileMode = TileMode.Tile,
                SourceRect = new RelativeRect(0, 0, 20, 6, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, 20, 6, RelativeUnit.Absolute),
                Visual = new Avalonia.Controls.Canvas
                {
                    Width = 20, Height = 6,
                    Children =
                    {
                        new Avalonia.Controls.Shapes.Line
                        {
                            StartPoint = new Point(0, 3), EndPoint = new Point(20, 3),
                            Stroke = lineBrush, StrokeThickness = 1
                        }
                    }
                }
            };
        }
        else
        {
            return new VisualBrush
            {
                TileMode = TileMode.Tile,
                SourceRect = new RelativeRect(0, 0, 6, 20, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, 6, 20, RelativeUnit.Absolute),
                Visual = new Avalonia.Controls.Canvas
                {
                    Width = 6, Height = 20,
                    Children =
                    {
                        new Avalonia.Controls.Shapes.Line
                        {
                            StartPoint = new Point(3, 0), EndPoint = new Point(3, 20),
                            Stroke = lineBrush, StrokeThickness = 1
                        }
                    }
                }
            };
        }
    }
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
