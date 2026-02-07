using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Woodcraft.Desktop.ViewModels;

public partial class NewProjectDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _projectName = "My Project";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ProjectTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _units = "inches";

    [ObservableProperty]
    private string _defaultMaterial = "pine";

    [ObservableProperty]
    private string? _validationError;

    public bool DialogResult { get; private set; }

    public ObservableCollection<string> UnitOptions { get; } = ["inches", "millimeters"];

    public ObservableCollection<string> Materials { get; } =
    [
        "pine", "red_oak", "white_oak", "hard_maple", "soft_maple",
        "cherry", "walnut", "poplar", "ash", "birch", "hickory", "plywood", "mdf"
    ];

    public ObservableCollection<ProjectTemplate> Templates { get; } =
    [
        new ProjectTemplate("Empty Project", "Start with a blank project", [], "empty"),
        new ProjectTemplate("Simple Bookshelf", "Basic bookshelf with 2 sides, top, bottom, and 2 shelves",
            ["left_side", "right_side", "top", "bottom", "shelf_1", "shelf_2"], "bookshelf"),
        new ProjectTemplate("Wall Cabinet", "Kitchen-style wall cabinet with door",
            ["left_side", "right_side", "top", "bottom", "back", "shelf", "door"], "cabinet"),
        new ProjectTemplate("Drawer Box", "Simple drawer with front, sides, back, and bottom",
            ["front", "left_side", "right_side", "back", "bottom"], "drawer"),
        new ProjectTemplate("Storage Box", "Simple box with lid",
            ["front", "back", "left_side", "right_side", "bottom", "lid"], "box"),
        new ProjectTemplate("Workbench Top", "Solid workbench top with stretchers",
            ["top_front", "top_back", "top_center", "front_stretcher", "back_stretcher", "side_stretcher_left", "side_stretcher_right"], "workbench"),
    ];

    public event Action? CloseRequested;

    public NewProjectDialogViewModel()
    {
        SelectedTemplate = Templates[0]; // Empty project by default
    }

    partial void OnSelectedTemplateChanged(ProjectTemplate? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.Id) && value.Id != "empty")
        {
            // Update project name based on template
            ProjectName = value.Name;
        }
    }

    [RelayCommand]
    private void Create()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            ValidationError = "Project name is required";
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
}

public record ProjectTemplate(
    string Name,
    string Description,
    string[] DefaultParts,
    string Id);
