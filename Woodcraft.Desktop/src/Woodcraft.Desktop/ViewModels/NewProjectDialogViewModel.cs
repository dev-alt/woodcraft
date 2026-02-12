using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Woodcraft.Core.Interfaces;

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

    public ObservableCollection<ProjectTemplate> Templates { get; } = [];

    public event Action? CloseRequested;

    public NewProjectDialogViewModel()
    {
        var config = Program.Services?.GetService<IConfigService>();

        if (config != null)
        {
            _projectName = config.GetString("projects.default_name", "My Project");
            _units = config.GetString("projects.default_units", "inches");
            _defaultMaterial = config.GetString("projects.default_material", "pine");
        }

        // Load templates from config
        var templates = config?.GetList("projects.templates", row =>
        {
            var parts = new List<string>();
            var partsTable = row as IConfigTable;
            // Try reading the "parts" sub-table as a string list
            // We need to access the raw row for nested tables
            if (row is Services.LuaConfigTable luaRow)
            {
                var partsVal = luaRow.Raw.Get("parts");
                if (partsVal.Type == MoonSharp.Interpreter.DataType.Table)
                {
                    foreach (var pair in partsVal.Table.Pairs)
                    {
                        if (pair.Value.Type == MoonSharp.Interpreter.DataType.String)
                            parts.Add(pair.Value.String);
                    }
                }
            }
            return new ProjectTemplate(
                row.GetString("name", ""),
                row.GetString("description", ""),
                parts.ToArray(),
                row.GetString("id", ""));
        });

        if (templates != null && templates.Count > 0)
        {
            foreach (var t in templates) Templates.Add(t);
        }
        else
        {
            Templates.Add(new("Empty Project", "Start with a blank project", [], "empty"));
            Templates.Add(new("Simple Bookshelf", "Basic bookshelf with 2 sides, top, bottom, and 2 shelves",
                ["left_side", "right_side", "top", "bottom", "shelf_1", "shelf_2"], "bookshelf"));
            Templates.Add(new("Wall Cabinet", "Kitchen-style wall cabinet with door",
                ["left_side", "right_side", "top", "bottom", "back", "shelf", "door"], "cabinet"));
            Templates.Add(new("Drawer Box", "Simple drawer with front, sides, back, and bottom",
                ["front", "left_side", "right_side", "back", "bottom"], "drawer"));
            Templates.Add(new("Storage Box", "Simple box with lid",
                ["front", "back", "left_side", "right_side", "bottom", "lid"], "box"));
            Templates.Add(new("Workbench Top", "Solid workbench top with stretchers",
                ["top_front", "top_back", "top_center", "front_stretcher", "back_stretcher", "side_stretcher_left", "side_stretcher_right"], "workbench"));
        }

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
