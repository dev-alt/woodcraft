using Woodcraft.Core.Models;

namespace Woodcraft.Core.Interfaces;

/// <summary>
/// Service for managing woodworking projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Current active project.
    /// </summary>
    Project? CurrentProject { get; }

    /// <summary>
    /// Event raised when the current project changes.
    /// </summary>
    event EventHandler<Project?>? CurrentProjectChanged;

    /// <summary>
    /// Create a new project.
    /// </summary>
    Task<Project> CreateProjectAsync(string name, Units units = Units.Inches, Material? material = null);

    /// <summary>
    /// Open a project from file.
    /// </summary>
    Task<Project> OpenProjectAsync(string filePath);

    /// <summary>
    /// Save the current project.
    /// </summary>
    Task SaveProjectAsync(string? filePath = null);

    /// <summary>
    /// Close the current project.
    /// </summary>
    Task CloseProjectAsync();

    /// <summary>
    /// Add a part to the current project.
    /// </summary>
    Task<Part> AddPartAsync(string id, PartType partType, Dimensions dimensions);

    /// <summary>
    /// Remove a part from the current project.
    /// </summary>
    Task<bool> RemovePartAsync(string partId);

    /// <summary>
    /// Update a part in the current project.
    /// </summary>
    Task UpdatePartAsync(Part part);

    /// <summary>
    /// Add joinery between parts.
    /// </summary>
    Task<Joint> AddJointAsync(JoineryType type, string partAId, string partBId);

    /// <summary>
    /// Add hardware to the project.
    /// </summary>
    Task<Hardware> AddHardwareAsync(string name, int quantity, double unitCost = 0);
}
