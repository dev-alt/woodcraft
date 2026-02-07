using Woodcraft.Core.Models;

namespace Woodcraft.Core.Interfaces;

/// <summary>
/// Service for CAD operations via Python bridge.
/// </summary>
public interface ICadService
{
    /// <summary>
    /// Whether the CAD service is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the Python CAD server.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the Python CAD server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Generate an optimized cut list.
    /// </summary>
    Task<CutListResult> GenerateCutListAsync(
        double stockLength,
        double stockWidth,
        string material = "plywood",
        double thickness = 0.75,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate cut list SVG visualization.
    /// </summary>
    Task<string> GenerateCutListSvgAsync(
        double stockLength,
        double stockWidth,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate bill of materials.
    /// </summary>
    Task<BillOfMaterials> GenerateBOMAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export project to STEP format.
    /// </summary>
    Task<string> ExportStepAsync(string? partId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export project to STL format.
    /// </summary>
    Task<string> ExportStlAsync(string? partId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate 2D drawing for a part.
    /// </summary>
    Task<string> GenerateDrawingAsync(
        string partId,
        string[] views,
        string format = "svg",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate lumber requirements.
    /// </summary>
    Task<Dictionary<string, object>> CalculateLumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the design for issues.
    /// </summary>
    Task<ValidationResult> ValidateDesignAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of design validation.
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<ValidationIssue> Issues { get; init; } = [];
}

/// <summary>
/// A single validation issue.
/// </summary>
public record ValidationIssue
{
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
    public string? PartId { get; init; }
}
