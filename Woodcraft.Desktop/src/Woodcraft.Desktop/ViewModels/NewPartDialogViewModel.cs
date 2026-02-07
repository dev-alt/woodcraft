using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class NewPartDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _partName = string.Empty;

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
    private PartPreset? _selectedPreset;

    [ObservableProperty]
    private string? _validationError;

    public bool DialogResult { get; private set; }

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

    public ObservableCollection<PartPreset> Presets { get; } =
    [
        new PartPreset("Shelf", PartType.Shelf, 36, 10, 0.75, "pine", "Standard shelf"),
        new PartPreset("Side Panel", PartType.Side, 36, 12, 0.75, "pine", "Cabinet side"),
        new PartPreset("Top/Bottom", PartType.Top, 36, 12, 0.75, "pine", "Cabinet top or bottom"),
        new PartPreset("Drawer Front", PartType.DrawerFront, 18, 6, 0.75, "pine", "Drawer face"),
        new PartPreset("Drawer Side", PartType.DrawerSide, 18, 6, 0.5, "pine", "Drawer side panel"),
        new PartPreset("Drawer Bottom", PartType.DrawerBottom, 17, 15, 0.25, "plywood", "Drawer floor"),
        new PartPreset("Back Panel", PartType.Back, 36, 24, 0.25, "plywood", "Cabinet back"),
        new PartPreset("Door", PartType.Door, 24, 18, 0.75, "pine", "Cabinet door"),
        new PartPreset("Face Frame Rail", PartType.Rail, 36, 2, 0.75, "pine", "Horizontal frame piece"),
        new PartPreset("Face Frame Stile", PartType.Stile, 24, 2, 0.75, "pine", "Vertical frame piece"),
    ];

    public event Action? CloseRequested;

    public NewPartDialogViewModel()
    {
        GeneratePartName();
        _selectedMaterialInfo = MaterialOptions.FirstOrDefault(m => m.Id == Material);
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

    public void Initialize(int existingPartCount)
    {
        PartName = $"part_{existingPartCount + 1}";
    }

    private void GeneratePartName()
    {
        if (string.IsNullOrEmpty(PartName))
        {
            PartName = "new_part";
        }
    }

    partial void OnSelectedPresetChanged(PartPreset? value)
    {
        if (value == null) return;

        PartType = value.PartType;
        Length = value.Length;
        Width = value.Width;
        Thickness = value.Thickness;
        Material = value.Material;

        // Auto-generate name from preset
        if (string.IsNullOrEmpty(PartName) || PartName.StartsWith("part_") || PartName.StartsWith("new_"))
        {
            var typeName = value.Name.ToLower().Replace(" ", "_");
            PartName = typeName;
        }
    }

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
            "1-1/4" => 1.25,
            "1-1/2" => 1.5,
            _ => Thickness
        };
    }

    [RelayCommand]
    private void Create()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(PartName))
        {
            ValidationError = "Part name is required";
            return;
        }

        if (Length <= 0 || Width <= 0 || Thickness <= 0)
        {
            ValidationError = "Dimensions must be positive values";
            return;
        }

        if (Quantity < 1)
        {
            ValidationError = "Quantity must be at least 1";
            return;
        }

        ValidationError = null;
        DialogResult = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseRequested?.Invoke();
    }

    public Part CreatePart()
    {
        return new Part(PartName, PartType, new Dimensions(Length, Width, Thickness))
        {
            Quantity = Quantity,
            Material = Material,
            GrainDirection = GrainDirection
        };
    }
}

public record PartPreset(
    string Name,
    PartType PartType,
    double Length,
    double Width,
    double Thickness,
    string Material,
    string Description);
