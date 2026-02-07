namespace Woodcraft.Core.Interfaces;

/// <summary>
/// Service for exporting project data to various formats.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export cut list to a text-based format.
    /// </summary>
    Task<string> ExportCutListPdfAsync(string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export bill of materials to CSV.
    /// </summary>
    Task<string> ExportBOMCsvAsync(string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export assembly guide as Markdown.
    /// </summary>
    Task<string> ExportAssemblyGuideAsync(string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export 3D model to STEP.
    /// </summary>
    Task<string> ExportStepAsync(string outputPath, string? partId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export 3D model to STL.
    /// </summary>
    Task<string> ExportStlAsync(string outputPath, string? partId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export drawing to DXF.
    /// </summary>
    Task<string> ExportDrawingDxfAsync(string outputPath, string partId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of supported export formats.
    /// </summary>
    IReadOnlyList<ExportFormat> SupportedFormats { get; }
}

/// <summary>
/// Information about a supported export format.
/// </summary>
public record ExportFormat
{
    public string Name { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}
