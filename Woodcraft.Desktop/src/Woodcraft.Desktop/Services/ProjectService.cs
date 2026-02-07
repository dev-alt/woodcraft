using System.Text.Json;
using Microsoft.Extensions.Logging;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.Services;

/// <summary>
/// Service for managing woodworking projects.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IPythonBridge _bridge;
    private readonly ILogger<ProjectService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private Project? _currentProject;

    public Project? CurrentProject => _currentProject;

    public event EventHandler<Project?>? CurrentProjectChanged;

    public ProjectService(IPythonBridge bridge, ILogger<ProjectService> logger)
    {
        _bridge = bridge;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public async Task<Project> CreateProjectAsync(string name, Units units = Units.Inches, Material? material = null)
    {
        _logger.LogInformation("Creating new project: {Name}", name);

        // Call Python to create project
        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("create_project", new
            {
                name = name,
                units = units.ToString().ToLower(),
                material_species = material?.Species ?? "pine",
                material_thickness = material?.Thickness ?? 0.75,
                material_finish = material?.Finish ?? "none"
            });
        }

        var project = new Project(name)
        {
            Units = units,
            Material = material ?? new Material()
        };

        SetCurrentProject(project);
        return project;
    }

    public async Task<Project> OpenProjectAsync(string filePath)
    {
        _logger.LogInformation("Opening project from: {Path}", filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project file not found", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize project");

        project.FilePath = filePath;
        project.IsDirty = false;

        // Sync with Python server if connected
        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("load_project", new { filepath = filePath });
        }

        SetCurrentProject(project);
        return project;
    }

    public async Task SaveProjectAsync(string? filePath = null)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        var savePath = filePath ?? _currentProject.FilePath
            ?? throw new InvalidOperationException("No file path specified for new project");

        _logger.LogInformation("Saving project to: {Path}", savePath);

        var json = JsonSerializer.Serialize(_currentProject, _jsonOptions);
        await File.WriteAllTextAsync(savePath, json);

        _currentProject.FilePath = savePath;
        _currentProject.IsDirty = false;

        // Also save via Python server
        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("save_project", new { filename = Path.GetFileName(savePath) });
        }
    }

    public Task CloseProjectAsync()
    {
        _logger.LogInformation("Closing project");
        SetCurrentProject(null);
        return Task.CompletedTask;
    }

    public async Task<Part> AddPartAsync(string id, PartType partType, Dimensions dimensions)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        _logger.LogInformation("Adding part: {Id}", id);

        var part = new Part(id, partType, dimensions)
        {
            Material = _currentProject.Material.Species
        };

        _currentProject.AddPart(part);

        // Sync with Python
        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("add_part", new
            {
                part_id = id,
                part_type = partType.ToString().ToLower(),
                length = dimensions.Length,
                width = dimensions.Width,
                thickness = dimensions.Thickness
            });
        }

        return part;
    }

    public async Task<bool> RemovePartAsync(string partId)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        _logger.LogInformation("Removing part: {Id}", partId);

        var result = _currentProject.RemovePart(partId);

        if (result && _bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("remove_part", new { part_id = partId });
        }

        return result;
    }

    public async Task UpdatePartAsync(Part part)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        _logger.LogInformation("Updating part: {Id}", part.Id);

        _currentProject.IsDirty = true;

        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("update_part", new
            {
                part_id = part.Id,
                length = part.Dimensions.Length,
                width = part.Dimensions.Width,
                thickness = part.Dimensions.Thickness,
                quantity = part.Quantity,
                notes = part.Notes
            });
        }
    }

    public async Task<Joint> AddJointAsync(JoineryType type, string partAId, string partBId)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        _logger.LogInformation("Adding joint between {A} and {B}", partAId, partBId);

        var joint = new Joint(type, partAId, partBId);
        _currentProject.Joinery.Add(joint);
        _currentProject.IsDirty = true;

        if (_bridge.IsConnected)
        {
            await _bridge.CallToolRawAsync("add_joinery", new
            {
                joint_type = type.ToString().ToLower(),
                part_a_id = partAId,
                part_b_id = partBId
            });
        }

        return joint;
    }

    public Task<Hardware> AddHardwareAsync(string name, int quantity, double unitCost = 0)
    {
        if (_currentProject == null)
            throw new InvalidOperationException("No project is open");

        _logger.LogInformation("Adding hardware: {Name}", name);

        var hardware = new Hardware(name, quantity, unitCost);
        _currentProject.Hardware.Add(hardware);
        _currentProject.IsDirty = true;

        return Task.FromResult(hardware);
    }

    private void SetCurrentProject(Project? project)
    {
        _currentProject = project;
        CurrentProjectChanged?.Invoke(this, project);
    }
}
